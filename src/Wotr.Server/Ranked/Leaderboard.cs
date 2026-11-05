using Npgsql;
using Wotr.Contracts;

namespace Wotr.Server;

// RANK-08 (R35, docs/RANKED_PLAY.md D13): the top 25 by rating among accounts with a finished ranked game; ties go to
// more games, then the account id. A rating row exists from an account's first ranked start, so games > 0 keeps out a
// player whose first match has not ended. The query walks player_ratings_order, and a player's own rank counts the rows
// ahead of theirs within it. Account ids stay here: a client knows its own row by its rank.
public sealed class Leaderboard(Database database)
{
    public const int Size = 25;

    public const string TopQuery =
        """
        SELECT a.username, r.rating, r.games, r.wins, r.losses
        FROM player_ratings r JOIN accounts a ON a.id = r.account_id
        WHERE r.games > 0
        ORDER BY r.rating DESC, r.games DESC, r.account_id
        LIMIT 25
        """;

    public const string AheadQuery =
        """
        SELECT count(*) FROM player_ratings b
        WHERE b.games > 0 AND b.rating >= $1
            AND (b.rating > $1 OR b.games > $2 OR (b.games = $2 AND b.account_id < $3))
        """;

    public async Task<LeaderboardView> Read(string account, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        return new LeaderboardView
        {
            Rows = await Top(connection, cancellation),
            You = await Own(connection, account, cancellation)
        };
    }

    private static async Task<List<LeaderboardRow>> Top(NpgsqlConnection connection, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null, TopQuery);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var rows = new List<LeaderboardRow>();
        while (await reader.ReadAsync(cancellation))
            rows.Add(Row(reader, rows.Count + 1));
        return rows;
    }

    private static async Task<LeaderboardRow?> Own(NpgsqlConnection connection, string account, CancellationToken cancellation)
    {
        await using var read = Database.Command(connection, null,
            """
            SELECT a.username, r.rating, r.games, r.wins, r.losses
            FROM player_ratings r JOIN accounts a ON a.id = r.account_id
            WHERE r.account_id = $1 AND r.games > 0
            """, account);
        LeaderboardRow own;
        double rating;
        await using (var reader = await read.ExecuteReaderAsync(cancellation))
        {
            if (!await reader.ReadAsync(cancellation))
                return null;
            rating = reader.GetDouble(1);
            own = Row(reader, 0);
        }
        await using var ahead = Database.Command(connection, null, AheadQuery, rating, own.Games, account);
        own.Rank = (int)(long)(await ahead.ExecuteScalarAsync(cancellation))! + 1;
        return own;
    }

    private static LeaderboardRow Row(NpgsqlDataReader reader, int rank) => new()
    {
        Rank = rank, Name = reader.GetString(0), Rating = RatingLedger.Shown(reader.GetDouble(1)),
        Games = reader.GetInt32(2), Wins = reader.GetInt32(3), Losses = reader.GetInt32(4)
    };
}
