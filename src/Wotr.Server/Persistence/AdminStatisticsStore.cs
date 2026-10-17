using Npgsql;

namespace Wotr.Server;

// ADMIN-01: the admin panel's counts. Games come from the match ledger (MatchLedger), so a match its players removed
// still counts; test scenarios and demo games against the CPU are counted apart. Technical accounts (test seats, the
// CPU) are never users.
public sealed class AdminStatisticsStore(Database database)
{
    private const string SummarySql =
        """
        SELECT
            (SELECT count(*) FROM accounts WHERE NOT technical),
            (SELECT count(*) FROM accounts WHERE NOT technical AND email_verified_at IS NOT NULL),
            (SELECT count(DISTINCT s.account_id) FROM sessions s JOIN accounts a ON a.id = s.account_id
                WHERE NOT a.technical AND s.created_at > now() - interval '7 days'),
            (SELECT count(*) FROM room_ledger),
            (SELECT count(*) FROM rooms WHERE NOT closed AND match_id IS NULL),
            (SELECT count(*) FROM match_ledger WHERE kind = 'game'),
            (SELECT count(*) FROM match_ledger WHERE kind = 'game' AND finished_at IS NOT NULL),
            (SELECT count(*) FROM match_ledger l WHERE l.kind = 'game' AND l.finished_at IS NULL
                AND EXISTS (SELECT 1 FROM matches m WHERE m.id = l.match_id)),
            (SELECT count(*) FROM match_ledger l WHERE l.kind = 'game' AND l.finished_at IS NULL
                AND NOT EXISTS (SELECT 1 FROM matches m WHERE m.id = l.match_id)),
            (SELECT count(*) FROM match_ledger WHERE kind = 'game' AND winner = 'FreePeoples'),
            (SELECT count(*) FROM match_ledger WHERE kind = 'game' AND winner = 'Shadow'),
            (SELECT count(*) FROM match_ledger WHERE kind = 'test'),
            (SELECT count(*) FROM match_ledger WHERE kind = 'demo')
        """;

    // Newest accounts first; accounts from before the panel have no known creation time and come last by name.
    private const string UsersSql =
        """
        SELECT a.username,
               a.created_at,
               (SELECT max(s.created_at) FROM sessions s WHERE s.account_id = a.id),
               a.email_verified_at IS NOT NULL,
               (SELECT count(*) FROM room_ledger r WHERE r.host_account = a.id),
               g.games,
               g.finished,
               g.wins
        FROM accounts a
        CROSS JOIN LATERAL (
            SELECT count(*) AS games,
                   count(l.finished_at) AS finished,
                   count(*) FILTER (WHERE (l.winner = 'FreePeoples' AND l.fp_account = a.id)
                                       OR (l.winner = 'Shadow' AND l.sh_account = a.id)) AS wins
            FROM match_ledger l
            WHERE l.kind = 'game' AND (l.fp_account = a.id OR l.sh_account = a.id)
        ) g
        WHERE NOT a.technical
        ORDER BY a.created_at DESC NULLS LAST, a.normalized_name
        LIMIT $1 OFFSET $2
        """;

    public async Task<AdminSummaryView> Summary(CancellationToken cancellation)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null, SummarySql);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        await reader.ReadAsync(cancellation);
        return new(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4),
            reader.GetInt64(5), reader.GetInt64(6), reader.GetInt64(7), reader.GetInt64(8), reader.GetInt64(9),
            reader.GetInt64(10), reader.GetInt64(11), reader.GetInt64(12), DateTimeOffset.UtcNow);
    }

    public async Task<AdminUserPage> Users(int page, int pageSize, CancellationToken cancellation)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        var users = await ReadUsers(connection, page, pageSize, cancellation);
        await using var count = Database.Command(connection, null, "SELECT count(*) FROM accounts WHERE NOT technical");
        var total = (long)(await count.ExecuteScalarAsync(cancellation))!;
        return new(users, total, page, pageSize);
    }

    private static async Task<List<AdminUserView>> ReadUsers(NpgsqlConnection connection, int page, int pageSize,
        CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null, UsersSql, pageSize, (long)page * pageSize);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var users = new List<AdminUserView>();
        while (await reader.ReadAsync(cancellation))
            users.Add(new(reader.GetString(0), Time(reader, 1), Time(reader, 2), reader.GetBoolean(3), reader.GetInt64(4),
                reader.GetInt64(5), reader.GetInt64(6), reader.GetInt64(7)));
        return users;
    }

    private static DateTimeOffset? Time(NpgsqlDataReader reader, int column) =>
        reader.IsDBNull(column) ? null : reader.GetFieldValue<DateTimeOffset>(column);
}
