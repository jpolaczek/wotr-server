using System.Data;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

public sealed class ActionLogStore(Database database)
{
    public Task<ActionLogPage?> Page(string match, string account, ActionLogFilter filter, int limit,
        string? before = null, string? after = null, CancellationToken cancellation = default) =>
        Page(match, new MatchReader(account), filter, limit, before, after, cancellation);

    public async Task<ActionLogPage?> Page(string match, MatchReader reader, ActionLogFilter filter, int limit,
        string? before = null, string? after = null, CancellationToken cancellation = default)
    {
        if (limit is < 1 or > 50 || !Enum.IsDefined(filter) || before is not null && after is not null)
            throw new ArgumentException("InvalidPage");
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellation);
        var role = await reader.Authorize(connection, transaction, match, cancellation);
        if (role is not { } access)
            return null;
        var (viewer, indexed) = access;
        var cursor = before ?? after;
        var position = cursor is null ? long.MaxValue : await ActionLogQuery.Position(connection, transaction,
            match, viewer, indexed, filter, ActionLogCursor.Decode(cursor, match, viewer, filter), cancellation);
        if (before is not null && position == 0)
            throw new ArgumentException("InvalidPage");
        var records = await ActionLogQuery.Page(connection, transaction, match, viewer, indexed, filter, position, after is not null, limit + 1, cancellation);
        var more = records.Count > limit;
        if (more)
            records.RemoveAt(limit);
        var page = await Assemble(connection, transaction, match, viewer, indexed, filter, records, cancellation);
        string Encode(string id) => ActionLogCursor.Encode(match, viewer, filter, id);
        page.NextCursor = more ? Encode(records[^1].Id) : "";
        page.LatestCursor = records.Count > 0 ? Encode(records.MaxBy(r => r.Position)!.Id) : after ?? Encode("");
        return page;
    }

    // The given steps and the whole actions they belong to, as one viewer sees them (also the replay export).
    internal static async Task<ActionLogPage> Assemble(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string match, Side viewer, bool indexed, ActionLogFilter filter, List<PositionedEvent> records,
        CancellationToken cancellation)
    {
        var groups = await ActionLogQuery.Actions(connection, transaction, match, viewer, indexed,
            records.Select(r => r.ActionId).Distinct().ToArray(), cancellation);
        var visibleParents = groups.Where(r => ActionLogProjection.Matches(r.Event, viewer, filter))
            .Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        return new()
        {
            MatchId = match,
            Steps = records.Select(r => ActionLogProjection.Step(r, visibleParents)).ToList(),
            Actions = groups.GroupBy(r => r.ActionId).Select(g => ActionLogProjection.Action(match, g, viewer, filter)!.Action).ToList()
        };
    }

    public Task<ActionLogView?> Action(string match, string account, string id,
        ActionLogFilter filter = ActionLogFilter.All, CancellationToken cancellation = default) =>
        Action(match, new MatchReader(account), id, filter, cancellation);

    public async Task<ActionLogView?> Action(string match, MatchReader reader, string id,
        ActionLogFilter filter = ActionLogFilter.All, CancellationToken cancellation = default)
    {
        if (!ActionLogCursor.ValidId(id))
            return null;
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellation);
        var role = await reader.Authorize(connection, transaction, match, cancellation);
        if (role is not { } access)
            return null;
        var records = await ActionLogQuery.Actions(connection, transaction, match, access.Viewer, access.Indexed, [id], cancellation);
        return ActionLogProjection.Action(match, records, access.Viewer, filter);
    }
}
