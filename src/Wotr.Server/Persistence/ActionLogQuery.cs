using System.Text.Json;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Server;

internal static class ActionLogQuery
{
    private const string IndexedColumns = """
        SELECT e.event_id,e.position,
            (SELECT count(*) FROM match_events p WHERE p.match_id=e.match_id AND p.revision=e.revision
                AND p.position<=e.position AND (p.event->>'onlyFor' IS NULL OR p.event->>'onlyFor'=$2)) AS visible_order,
            e.event::text FROM match_events e
        """ + "\n";

    internal const string LegacyVisible = """
        WITH legacy AS (
            SELECT e.ordinality AS position,e.value AS event,
                COALESCE(e.value->>'historyId',encode(sha256(convert_to(m.id || '/' || e.ordinality::text,'UTF8')),'hex')) AS event_id
            FROM matches m CROSS JOIN LATERAL jsonb_array_elements(m.state->'events') WITH ORDINALITY AS e(value,ordinality)
            WHERE m.id=$1
        ), source AS (
            SELECT position,event_id,COALESCE(event->'activity'->'action'->>'id',event_id) AS action_id,
                event,(event->>'revision')::bigint AS revision FROM legacy
        ),
        visible AS (
            SELECT position,event_id,action_id,event,
                row_number() OVER (PARTITION BY revision ORDER BY position) AS visible_order
            FROM source WHERE event->>'onlyFor' IS NULL OR event->>'onlyFor'=$2
        )
        """;

    public static async Task<(Side Viewer, bool Indexed)?> Authorize(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string match, string account, CancellationToken cancellation)
    {
        await using var query = Database.Command(connection, transaction,
            "SELECT fp_account=$2,events_indexed FROM matches WHERE id=$1 AND (fp_account=$2 OR sh_account=$2)", match, account);
        await using var reader = await query.ExecuteReaderAsync(cancellation);
        return await reader.ReadAsync(cancellation) ? (reader.GetBoolean(0) ? Side.FreePeoples : Side.Shadow, reader.GetBoolean(1)) : null;
    }

    public static async Task<(Side Viewer, bool Indexed)?> AuthorizeObserver(NpgsqlConnection connection,
        NpgsqlTransaction transaction, string match, string account, Side perspective, CancellationToken cancellation)
    {
        await using var query = Database.Command(connection, transaction,
            "SELECT m.events_indexed FROM matches m WHERE m.id=$1 AND " + MatchObservers.Watching, match, account);
        return await query.ExecuteScalarAsync(cancellation) is bool indexed ? (perspective, indexed) : null;
    }

    public static async Task<long> Position(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string match, Side viewer, bool indexed, ActionLogFilter filter, string id, CancellationToken cancellation)
    {
        if (id.Length == 0)
            return 0;
        var sql = indexed ? """
            SELECT position FROM match_events WHERE match_id=$1 AND event_id=$3
                AND (event->>'onlyFor' IS NULL OR event->>'onlyFor'=$2)
                AND ($4='' OR event->'activity'->>'actor'=$4)
            """ : LegacyVisible + """
            SELECT position FROM visible WHERE event_id=$3
                AND ($4='' OR event->'activity'->>'actor'=$4)
            """;
        await using var query = Database.Command(connection, transaction, sql, match, viewer.ToString(), id, Actor(viewer, filter));
        return await query.ExecuteScalarAsync(cancellation) is long position ? position : throw new ArgumentException("InvalidPage");
    }

    public static Task<List<PositionedEvent>> Page(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string match, Side viewer, bool indexed, ActionLogFilter filter, long position, bool forward, int limit, CancellationToken cancellation)
    {
        var comparison = forward ? ">" : "<";
        var direction = forward ? "ASC" : "DESC";
        var sql = indexed ? IndexedColumns + $"""
            WHERE e.match_id=$1 AND e.position {comparison} $3
                AND (e.event->>'onlyFor' IS NULL OR e.event->>'onlyFor'=$2)
                AND ($4='' OR e.event->'activity'->>'actor'=$4)
            ORDER BY e.position {direction} LIMIT $5
            """ : LegacyVisible + $"""
            SELECT event_id,position,visible_order,event::text FROM visible
            WHERE position {comparison} $3 AND ($4='' OR event->'activity'->>'actor'=$4)
            ORDER BY position {direction} LIMIT $5
            """;
        var query = Database.Command(connection, transaction, sql, match, viewer.ToString(), position, Actor(viewer, filter), limit);
        return Read(query, cancellation);
    }

    public static Task<List<PositionedEvent>> Actions(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string match, Side viewer, bool indexed, string[] ids, CancellationToken cancellation)
    {
        var sql = indexed ? IndexedColumns + """
            WHERE e.match_id=$1 AND e.position IS NOT NULL AND e.action_id=ANY($3)
                AND (e.event->>'onlyFor' IS NULL OR e.event->>'onlyFor'=$2)
            ORDER BY e.position
            """ : LegacyVisible + """
            SELECT event_id,position,visible_order,event::text FROM visible
            WHERE action_id=ANY($3) ORDER BY position
            """;
        return Read(Database.Command(connection, transaction, sql, match, viewer.ToString(), ids), cancellation);
    }

    private static string Actor(Side viewer, ActionLogFilter filter) => filter switch
    {
        ActionLogFilter.You => viewer.ToString(),
        ActionLogFilter.Opponent => TurnFlow.Other(viewer).ToString(),
        _ => ""
    };

    private static async Task<List<PositionedEvent>> Read(NpgsqlCommand query, CancellationToken cancellation)
    {
        await using var command = query;
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var result = new List<PositionedEvent>();
        while (await reader.ReadAsync(cancellation))
            result.Add(new(reader.GetString(0), reader.GetInt64(1), checked((int)reader.GetInt64(2)),
                JsonSerializer.Deserialize<GameEvent>(reader.GetString(3), BoardCatalog.Json)!));
        return result;
    }
}
