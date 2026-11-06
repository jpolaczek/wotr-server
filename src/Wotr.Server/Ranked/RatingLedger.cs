using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

// RANK-04 (R35, docs/RANKED_PLAY.md): each account's rating and every change of it. A ranked match's end updates both
// players' ratings in the transaction that finishes it: releasing its seats both ends the account's active match and
// makes the update happen once, and any failure rolls the finishing command back with it.
public static class RatingLedger
{
    private sealed record Seat(string Account, Side Side);

    public static async Task Ensure(NpgsqlConnection connection, NpgsqlTransaction transaction, string[] accounts,
        CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            """
            INSERT INTO player_ratings(account_id, rating, deviation, volatility)
            SELECT account, $2, $3, $4 FROM unnest($1::text[]) AS account
            ON CONFLICT (account_id) DO NOTHING
            """, (object)accounts, Glicko2.InitialRating, Glicko2.InitialDeviation, Glicko2.InitialVolatility);
        await command.ExecuteNonQueryAsync(cancellation);
    }

    // Called with every save that has a winner; only a ranked match's first finish still holds seats.
    public static async Task Finish(NpgsqlConnection connection, NpgsqlTransaction transaction, string match, Side winner,
        CancellationToken cancellation)
    {
        var seats = await ReleaseSeats(connection, transaction, match, cancellation);
        if (seats.Count == 0)
            return;
        if (seats.Count != 2 || seats.Count(s => s.Side == winner) != 1)
            throw new InvalidOperationException("A ranked match lost a seat.");
        var winning = seats.Single(s => s.Side == winner).Account;
        var losing = seats.Single(s => s.Side != winner).Account;
        var before = await Lock(connection, transaction, [winning, losing], cancellation);
        var (won, lost) = Glicko2.Game(before[winning], before[losing]);
        await Save(connection, transaction, match, winning, before[winning], won, true, cancellation);
        await Save(connection, transaction, match, losing, before[losing], lost, false, cancellation);
    }

    // The account's own change in a finished ranked match, rounded as players see it.
    public static async Task<RankedResultView?> Result(NpgsqlConnection connection, string match, string account,
        CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null,
            "SELECT rating_before, rating_after FROM rating_changes WHERE match_id = $1 AND account_id = $2", match, account);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        if (!await reader.ReadAsync(cancellation))
            return null;
        var before = Shown(reader.GetDouble(0));
        var after = Shown(reader.GetDouble(1));
        return new RankedResultView { RatingBefore = before, RatingAfter = after, Change = after - before };
    }

    public static int Shown(double rating) => (int)Math.Round(rating, MidpointRounding.AwayFromZero);

    private static async Task<List<Seat>> ReleaseSeats(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string match, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "DELETE FROM ranked_seats WHERE match_id = $1 RETURNING account_id, side", match);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var seats = new List<Seat>();
        while (await reader.ReadAsync(cancellation))
            seats.Add(new(reader.GetString(0), Enum.Parse<Side>(reader.GetString(1))));
        return seats;
    }

    // Both rows in account order, the order every ranked finish takes, so two finishes never wait on each other.
    private static async Task<Dictionary<string, PlayerRating>> Lock(NpgsqlConnection connection,
        NpgsqlTransaction transaction, string[] accounts, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            """
            SELECT account_id, rating, deviation, volatility FROM player_ratings
            WHERE account_id = ANY($1) ORDER BY account_id FOR UPDATE
            """, (object)accounts);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var ratings = new Dictionary<string, PlayerRating>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellation))
            ratings[reader.GetString(0)] = new(reader.GetDouble(1), reader.GetDouble(2), reader.GetDouble(3));
        if (ratings.Count != accounts.Length)
            throw new InvalidOperationException("A ranked player has no rating.");
        return ratings;
    }

    private static async Task Save(NpgsqlConnection connection, NpgsqlTransaction transaction, string match,
        string account, PlayerRating before, PlayerRating after, bool won, CancellationToken cancellation)
    {
        await using var rating = Database.Command(connection, transaction,
            """
            UPDATE player_ratings SET rating = $2, deviation = $3, volatility = $4, games = games + 1,
                wins = wins + $5, losses = losses + $6, updated_at = now()
            WHERE account_id = $1
            """, account, after.Rating, after.Deviation, after.Volatility, won ? 1 : 0, won ? 0 : 1);
        await rating.ExecuteNonQueryAsync(cancellation);
        await using var change = Database.Command(connection, transaction,
            """
            INSERT INTO rating_changes(match_id, account_id, rating_before, rating_after, deviation_before, deviation_after)
            VALUES ($1, $2, $3, $4, $5, $6)
            """, match, account, before.Rating, after.Rating, before.Deviation, after.Deviation);
        await change.ExecuteNonQueryAsync(cancellation);
    }
}
