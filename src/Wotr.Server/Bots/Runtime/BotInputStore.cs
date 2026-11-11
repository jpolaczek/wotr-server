using System.Data;
using System.Security.Cryptography;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

public sealed record BotInput(BotRuntimeRequest Request, long HistoryPosition)
{
    public override string ToString() => "BotInput [private]";
}

public sealed class BotContextTooLargeException : Exception;

public sealed class BotInputStore(Database database, Projection projection, PreviewService previews)
{
    public async Task<BotInput?> Read(BotWork work, CancellationToken cancellation = default, bool includeContext = true)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellation);
        var match = await ReadMatch(connection, transaction, work, cancellation);
        if (match is null)
            return null;
        var player = projection.For(match.State, work.Side);
        var request = new BotRuntimeRequest(BotRuntimeProtocol.Version, Guid.NewGuid().ToString("N"), work.RuntimeVersion,
            Protocol.Version, Protocol.RulesVersion, Protocol.ContentVersion, work.MatchId, work.Side,
            player, [], [], [], work.Memory, RandomNumberGenerator.GetInt32(int.MaxValue), 100_000, []);
        if (!includeContext || player.Winner is not null || player.DecisionOwner != work.Side || player.Decision is null)
            return new(request, work.HistoryPosition);
        var records = await ActionLogQuery.Page(connection, transaction, work.MatchId, work.Side,
            match.EventsIndexed, ActionLogFilter.All, work.HistoryPosition, true, 4097, cancellation);
        if (records.Count > 4096)
            throw new BotContextTooLargeException();
        var history = await ActionLogStore.Assemble(connection, transaction, work.MatchId, work.Side,
            match.EventsIndexed, ActionLogFilter.All, records, cancellation);
        var ids = history.Steps.Select(step => step.Context.BattleId)
            .Concat([player.Battle?.Id ?? "", player.LastBattleRound?.Id ?? ""])
            .Where(id => id.Length > 0).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (ids.Length > 128)
            throw new BotContextTooLargeException();
        var reports = new List<BattleReportView>();
        foreach (var id in ids)
            if (await BattleReportStore.Report(connection, transaction, work.MatchId, id, work.Side,
                    match.EventsIndexed, cancellation) is { } report)
                reports.Add(report);
        return new(request with { History = [.. history.Steps], Reports = [.. reports] },
            records.Count == 0 ? work.HistoryPosition : records[^1].Position);
    }

    public async Task<BotRuntimeRequest?> AddPreviews(BotWork work, BotRuntimeRequest input,
        BotRuntimeResponse response, CancellationToken cancellation = default)
    {
        if (response.Status != "NeedPreviews" || !BotRuntimeProtocol.Valid(response, input) ||
            input.Previews.Length + response.PreviewRequests!.Length > 16)
            return null;
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellation);
        var match = await ReadMatch(connection, transaction, work, cancellation);
        if (match is null || match.State.Revision != input.Player.Revision)
            return null;
        var view = projection.For(match.State, work.Side);
        if (view.DecisionOwner != work.Side || view.Decision?.Id != input.Player.Decision?.Id || view.Winner is not null)
            return null;
        return input with
        {
            PreviewCommands = [.. input.PreviewCommands ?? [], .. response.PreviewRequests!],
            Previews = [.. input.Previews, .. response.PreviewRequests!.Select(command => previews.Preview(match.State, work.Side, command))]
        };
    }

    private static async Task<AuthorizedMatch?> ReadMatch(NpgsqlConnection connection, NpgsqlTransaction transaction,
        BotWork work, CancellationToken cancellation)
    {
        await using var query = Database.Command(connection, transaction, """
            SELECT m.state::text,m.events_indexed FROM matches m JOIN bot_assignments a ON a.match_id=m.id
            JOIN bot_runtime_jobs j ON j.assignment_id=a.id
            WHERE m.id=$1 AND a.id=$2 AND a.participant_id=$3 AND a.side=$4 AND a.status='started'
                AND (CASE WHEN $4='FreePeoples' THEN m.fp_account ELSE m.sh_account END)=$3
                AND j.generation=$5 AND j.owner_id=$6 AND j.memory_version=$7
                AND j.lease_until>clock_timestamp()
            """, work.MatchId, work.AssignmentId, work.ParticipantId, work.Side.ToString(),
            work.Generation, work.OwnerId, work.MemoryVersion);
        await using var row = await query.ExecuteReaderAsync(cancellation);
        return await row.ReadAsync(cancellation) ? new(StateCodec.Read(row.GetString(0)), work.Side, row.GetBoolean(1)) : null;
    }
}
