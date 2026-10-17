using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Server;

public sealed class MatchIndexStore(Database database)
{
    public async Task<MatchListView> List(string account, MatchListFilter filter, string cursor, int limit,
        CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null, Query, account, cursor, filter.ToString(), limit + 1);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var page = new MatchListView();
        while (await reader.ReadAsync(cancellation))
        {
            var summary = JsonSerializer.Deserialize<MatchSummary>(reader.GetString(0), BoardCatalog.Json)!;
            summary.Configuration = ConfigurationRules.Normalize(summary.Configuration);
            summary.HasUnseenChanges = summary.Revision > summary.LastSeenRevision;
            page.Matches.Add(summary);
        }
        if (page.Matches.Count > limit)
        {
            page.Matches.RemoveAt(limit);
            page.NextCursor = page.Matches[^1].MatchId;
        }
        return page;
    }

    // Only public headers and this account's read position leave PostgreSQL.
    // updated_at also changes on private preferences and must never be exposed here.
    private const string Query = """
        WITH owned AS (
            SELECT m.*, CASE WHEN m.fp_account=$1 THEN 'FreePeoples' ELSE 'Shadow' END AS viewer,
                COALESCE(r.revision, -1) AS seen,
                COALESCE(a.username, 'Opponent') AS opponent
            FROM matches m
            LEFT JOIN match_reads r ON r.match_id=m.id AND r.account_id=$1
            LEFT JOIN accounts a ON a.id=CASE WHEN m.fp_account=$1 THEN m.sh_account ELSE m.fp_account END
            WHERE (m.fp_account=$1 OR m.sh_account=$1) AND m.id COLLATE "C">$2
                AND NOT EXISTS (SELECT 1 FROM test_matches t WHERE t.match_id=m.id)
        ), classified AS (
            SELECT *, CASE
                WHEN state->>'phase'='Finished' THEN 'Finished'
                WHEN state->'pending'->>'owner'=viewer THEN 'YourDecision'
                ELSE 'Waiting' END AS status
            FROM owned
        )
        SELECT jsonb_build_object(
            'matchId', id, 'name', display_name, 'opponent', opponent, 'you', viewer,
            'configuration', state->'configuration', 'turn', state->'turn', 'phase', state->'phase',
            'revision', revision, 'lastSeenRevision', seen, 'status', status,
            'decisionOwner', CASE WHEN status<>'Finished' THEN state->'pending'->'owner' END,
            'yourDecision', CASE WHEN status='YourDecision' THEN state->'pending'->'kind' END,
            'winner', state->'winner', 'victory', state->'victory')::text
        FROM classified WHERE $3='All' OR status=$3
        ORDER BY id COLLATE "C" LIMIT $4
        """;
}
