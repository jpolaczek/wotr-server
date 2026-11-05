using System.Text.Json;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Server;

public sealed record RankedQueueResult(RankedSearchView? View, string Error = "");

// RANK-05 (R35, docs/RANKED_PLAY.md D5): the live searches. A search is eligible only while its client polls; a pair
// is started in the transaction that removes both searches, so a cancel either wins (no match) or loses (the match
// stands and the cancel answers Matched). An account in a ranked match is always answered Matched, whatever it asks.
public sealed class RankedQueue(Database database, RankedOptions options, ReleaseScope release, ModuleClientPolicy modules,
    IRandomSource random, TimeProvider clock, ILogger<RankedQueue> logger)
{
    // One pairing pass at a time across server processes.
    private const long PairingLock = 779305;

    public async Task<RankedQueueResult> Search(string account, RankedSearchRequest search, string clientVersion,
        CancellationToken cancellation = default)
    {
        if (Refusal(search, clientVersion) is { } refusal)
            return new(null, refusal);
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        if (await Matched(connection, account, cancellation) is { } matched)
            return new(matched);
        var now = clock.GetUtcNow();
        await using var command = Database.Command(connection, null,
            """
            INSERT INTO ranked_tickets(account_id, search, client_version, created_at, polled_at) VALUES ($1, $2::jsonb, $3, $4, $4)
            ON CONFLICT (account_id) DO UPDATE SET search = excluded.search, client_version = excluded.client_version,
                created_at = excluded.created_at, polled_at = excluded.polled_at
            """, account, JsonSerializer.Serialize(search, BoardCatalog.Json), clientVersion, now);
        await command.ExecuteNonQueryAsync(cancellation);
        return new(await Idle(connection, account, RankedSearchStatus.Searching, cancellation));
    }

    // The client's poll: it keeps the search eligible, tells a lapsed search that it stopped, and finds a pairing.
    public async Task<RankedSearchView> Poll(string account, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        if (await Matched(connection, account, cancellation) is { } matched)
            return matched;
        var now = clock.GetUtcNow();
        var ticket = await Ticket(connection, account, cancellation);
        if (ticket is null)
            return await Idle(connection, account, RankedSearchStatus.Idle, cancellation);
        var (created, polled) = ticket.Value;
        if (polled <= now - options.Liveness)
        {
            await Remove(connection, account, cancellation);
            return await Idle(connection, account, RankedSearchStatus.Stopped, cancellation);
        }
        await using var touch = Database.Command(connection, null,
            "UPDATE ranked_tickets SET polled_at = $2 WHERE account_id = $1", account, now);
        await touch.ExecuteNonQueryAsync(cancellation);
        var view = await Idle(connection, account, RankedSearchStatus.Searching, cancellation);
        view.ElapsedSeconds = Seconds(now - created);
        return view;
    }

    public async Task<RankedSearchView> Cancel(string account, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await Remove(connection, account, cancellation);
        return await Matched(connection, account, cancellation)
               ?? await Idle(connection, account, RankedSearchStatus.Idle, cancellation);
    }

    // One pass: the live searches are paired (RankedMatching) and each pair is started in its own transaction.
    public async Task<int> PairOnce(CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        if (!await Lock(connection, "SELECT pg_try_advisory_lock($1)", cancellation))
            return 0;
        try
        {
            var now = clock.GetUtcNow();
            await Forget(connection, now, cancellation);
            var pairings = RankedMatching.Pair(await Live(connection, now, cancellation), random);
            var started = 0;
            foreach (var pairing in pairings)
                if (await Attempt(connection, pairing, cancellation))
                    started++;
            return started;
        }
        finally
        {
            await Lock(connection, "SELECT pg_advisory_unlock($1)", CancellationToken.None);
        }
    }

    // One pair that cannot start rolls back alone: both players search on and the other pairs still start.
    private async Task<bool> Attempt(NpgsqlConnection connection, RankedPairing pairing, CancellationToken cancellation)
    {
        try
        {
            return await Start(connection, pairing, cancellation);
        }
        catch (Exception error) when (!cancellation.IsCancellationRequested)
        {
            logger.LogError(error, "A ranked pair could not start.");
            return false;
        }
    }

    // A module the server has not released is unavailable; one released but newer than this client needs an update.
    private string? Refusal(RankedSearchRequest search, string clientVersion)
    {
        var unplayable = modules.Unplayable(clientVersion);
        var playable = release.Modules.Where(module => !unplayable.Contains(module)).ToList();
        var invalid = RankedRules.Invalid(search, playable);
        if (invalid != RankedRules.ExpansionsUnavailable)
            return invalid;
        return RankedRules.Invalid(search, release.Modules) is null ? ClientCompatibility.ClientUpdateRequired : invalid;
    }

    private async Task<bool> Start(NpgsqlConnection connection, RankedPairing pairing, CancellationToken cancellation)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        // A cancel that won the race left one search: the pair is dropped and the other player searches on.
        await using var take = Database.Command(connection, transaction,
            "DELETE FROM ranked_tickets WHERE account_id = ANY($1) RETURNING account_id",
            (object)new[] { pairing.FreePeoples.Account, pairing.Shadow.Account });
        var taken = 0;
        await using (var reader = await take.ExecuteReaderAsync(cancellation))
            while (await reader.ReadAsync(cancellation))
                taken++;
        if (taken != 2)
            return false;
        var start = await RankedMatches.Create(connection, transaction, pairing.FreePeoples.Account, pairing.Shadow.Account,
            pairing.Configuration, pairing.Preset, random, clock.GetUtcNow(), cancellation);
        if (start.Error.Length > 0)
            return false;
        await transaction.CommitAsync(cancellation);
        return true;
    }

    private async Task<List<RankedTicket>> Live(NpgsqlConnection connection, DateTimeOffset now, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null,
            """
            SELECT t.account_id, t.search::text, t.created_at, COALESCE(r.rating, $2)
            FROM ranked_tickets t LEFT JOIN player_ratings r ON r.account_id = t.account_id
            WHERE t.polled_at > $1
            """, now - options.Liveness, Glicko2.InitialRating);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var tickets = new List<RankedTicket>();
        while (await reader.ReadAsync(cancellation))
            tickets.Add(new(reader.GetString(0), JsonSerializer.Deserialize<RankedSearchRequest>(reader.GetString(1), BoardCatalog.Json)!,
                reader.GetDouble(3), Seconds(now - reader.GetFieldValue<DateTimeOffset>(2))));
        return tickets;
    }

    // A search nobody has polled for a long time is removed; a recently lapsed one waits for its poll to say Stopped.
    private async Task Forget(NpgsqlConnection connection, DateTimeOffset now, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null,
            "DELETE FROM ranked_tickets WHERE polled_at <= $1", now - options.Forget);
        await command.ExecuteNonQueryAsync(cancellation);
    }

    private static async Task<(DateTimeOffset Created, DateTimeOffset Polled)?> Ticket(NpgsqlConnection connection,
        string account, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null,
            "SELECT created_at, polled_at FROM ranked_tickets WHERE account_id = $1", account);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        return await reader.ReadAsync(cancellation)
            ? (reader.GetFieldValue<DateTimeOffset>(0), reader.GetFieldValue<DateTimeOffset>(1))
            : null;
    }

    private static async Task Remove(NpgsqlConnection connection, string account, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null, "DELETE FROM ranked_tickets WHERE account_id = $1", account);
        await command.ExecuteNonQueryAsync(cancellation);
    }

    // The account's ranked match and its opponent, named with their rounded rating, or nothing.
    private static async Task<RankedSearchView?> Matched(NpgsqlConnection connection, string account,
        CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null,
            """
            SELECT mine.match_id, a.username, COALESCE(r.rating, $2),
                COALESCE((SELECT own.rating FROM player_ratings own WHERE own.account_id = $1), $2)
            FROM ranked_seats mine
            JOIN ranked_seats other ON other.match_id = mine.match_id AND other.account_id <> mine.account_id
            JOIN accounts a ON a.id = other.account_id
            LEFT JOIN player_ratings r ON r.account_id = other.account_id
            WHERE mine.account_id = $1
            """, account, Glicko2.InitialRating);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        if (!await reader.ReadAsync(cancellation))
            return null;
        return new RankedSearchView
        {
            Status = RankedSearchStatus.Matched, MatchId = reader.GetString(0), Opponent = reader.GetString(1),
            OpponentRating = RatingLedger.Shown(reader.GetDouble(2)), Rating = RatingLedger.Shown(reader.GetDouble(3))
        };
    }

    private static async Task<RankedSearchView> Idle(NpgsqlConnection connection, string account, RankedSearchStatus status,
        CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null,
            "SELECT COALESCE((SELECT rating FROM player_ratings WHERE account_id = $1), $2)", account, Glicko2.InitialRating);
        var rating = (double)(await command.ExecuteScalarAsync(cancellation))!;
        return new RankedSearchView { Status = status, Rating = RatingLedger.Shown(rating) };
    }

    private static async Task<bool> Lock(NpgsqlConnection connection, string sql, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null, sql, PairingLock);
        return await command.ExecuteScalarAsync(cancellation) is true;
    }

    private static int Seconds(TimeSpan span) => Math.Max(0, (int)span.TotalSeconds);
}
