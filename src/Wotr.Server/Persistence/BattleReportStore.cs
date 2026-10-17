using System.Data;
using System.Text.Json;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Server;

public sealed class BattleReportStore(Database database)
{
    public Task<BattleReportView?> Read(string match, string battle, string account, CancellationToken cancellation = default) =>
        Read(match, battle, new MatchReader(account), cancellation);

    public async Task<BattleReportView?> Read(string match, string battle, MatchReader reader,
        CancellationToken cancellation = default)
    {
        if (battle.Length is 0 or > 256)
            return null;
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellation);
        var access = await reader.Authorize(connection, transaction, match, cancellation);
        if (access is not { } role)
            return null;
        return await Report(connection, transaction, match, battle, role.Viewer, role.Indexed, cancellation);
    }

    // One battle's report from the public steps of its actions (also the replay export).
    internal static async Task<BattleReportView?> Report(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string match, string battle, Side viewer, bool indexed, CancellationToken cancellation)
    {
        var ids = await Actions(connection, transaction, match, battle, viewer, indexed, cancellation);
        var records = ids.Length == 0 ? [] : await ActionLogQuery.Actions(connection, transaction,
            match, viewer, indexed, ids, cancellation);
        var legacy = records.Any(r => r.Event.Kind == "BattleStarted" && r.Event.Activity?.Context.BattleId == battle) ? null :
            await Legacy(connection, transaction, match, cancellation);
        return BattleReportProjection.Create(match, battle, records, legacy);
    }

    private static async Task<string[]> Actions(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string match, string battle, Side viewer, bool indexed, CancellationToken cancellation)
    {
        var sql = indexed ? """
            SELECT DISTINCT action_id FROM match_events
            WHERE match_id=$1 AND position IS NOT NULL AND event->>'onlyFor' IS NULL
                AND event->'activity'->'context'->>'battleId'=$2
            """ : ActionLogQuery.LegacyVisible + """
            SELECT DISTINCT action_id FROM visible
            WHERE event->>'onlyFor' IS NULL AND event->'activity'->'context'->>'battleId'=$3
            """;
        await using var query = indexed ? Database.Command(connection, transaction, sql, match, battle)
            : Database.Command(connection, transaction, sql, match, viewer.ToString(), battle);
        await using var reader = await query.ExecuteReaderAsync(cancellation);
        var ids = new List<string>();
        while (await reader.ReadAsync(cancellation))
            ids.Add(reader.GetString(0));
        return [.. ids];
    }

    private static async Task<BattleView?> Legacy(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string match, CancellationToken cancellation)
    {
        await using var query = Database.Command(connection, transaction,
            "SELECT (state->'lastBattleRound')::text FROM matches WHERE id=$1", match);
        return await query.ExecuteScalarAsync(cancellation) is string json
            ? JsonSerializer.Deserialize<BattleView>(json, BoardCatalog.Json) : null;
    }
}
