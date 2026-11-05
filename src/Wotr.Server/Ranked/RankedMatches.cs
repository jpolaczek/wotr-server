using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

public sealed record RankedStart(string MatchId, string Error, string ActiveMatch = "");

// RANK-04 (R35, docs/RANKED_PLAY.md): the server starts a ranked match for two accounts, with its unlisted room, its
// seats and both rating rows, in one transaction. A seat lasts until the match finishes (RatingLedger), so an account
// holds at most one active ranked match.
public sealed class RankedMatches(Database database, IRandomSource random, TimeProvider clock, RankedOptions options, MatchPresence presence)
{
    public const string ActiveRefusal = "RankedMatchActive";
    public const string NotRankedRefusal = "NotRanked";

    public async Task<RankedStart> Start(string freePeoples, string shadow, MatchConfiguration configuration,
        RankedPreset preset, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        var start = await Create(connection, transaction, freePeoples, shadow, configuration, preset, random,
            clock.GetUtcNow(), options, cancellation);
        if (start.Error.Length == 0)
        {
            await transaction.CommitAsync(cancellation);
            presence.Expect(start.MatchId);
        }
        return start;
    }

    // RANK-05: the queue starts a pair's match in the transaction that removes both searches; the caller commits.
    public static async Task<RankedStart> Create(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string freePeoples, string shadow, MatchConfiguration configuration, RankedPreset preset, IRandomSource random,
        DateTimeOffset now, RankedOptions options, CancellationToken cancellation)
    {
        if (freePeoples == shadow)
            throw new ArgumentException("A ranked match needs two accounts.");
        string[] accounts = [freePeoples, shadow];
        await LockAccounts(connection, transaction, accounts, cancellation);
        if (await ActiveMatch(connection, transaction, accounts, cancellation) is { } active)
            return new("", ActiveRefusal, active);
        var match = MatchFactory.Create(Guid.NewGuid().ToString("N"), random, ConfigurationRules.Normalize(configuration));
        TurnFlow.Start(match);
        await MatchStore.Insert(connection, transaction, match, freePeoples, shadow, MatchKind.Ranked, cancellation);
        await RoomStore.InsertRanked(connection, transaction, match.Id, freePeoples, shadow, match.Configuration, cancellation);
        await Register(connection, transaction, match.Id, freePeoples, shadow, preset, cancellation);
        await RatingLedger.Ensure(connection, transaction, accounts, cancellation);
        await SessionStore.ExtendForRankedMatch(connection, transaction, accounts, cancellation);
        // RANK-06: the first decision's clock starts with the match, with time to load the board.
        await MatchClocks.Start(connection, transaction, match, preset, now, options, cancellation);
        return new(match.Id, "");
    }

    // RANK-10: the side an account holds in this active ranked match, if it does.
    public async Task<Side?> Seat(string match, string account, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            "SELECT side FROM ranked_seats WHERE match_id = $1 AND account_id = $2", match, account);
        return await command.ExecuteScalarAsync(cancellation) is string side ? Enum.Parse<Side>(side) : null;
    }

    // RANK-10: below the client minimum an account may use a ranked match it is seated at, and may read how it ended for
    // a while, because the seat goes with the last command and the end screen still has to load.
    public async Task<bool> MayUseBelowMinimum(string match, string account, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            """
            SELECT EXISTS (SELECT 1 FROM ranked_seats WHERE match_id = $1 AND account_id = $2)
                OR EXISTS (SELECT 1 FROM rating_changes
                    WHERE match_id = $1 AND account_id = $2 AND created_at > now() - interval '1 hour')
            """, match, account);
        return (bool)(await command.ExecuteScalarAsync(cancellation))!;
    }

    public async Task<bool> HoldsSeat(string account, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            "SELECT EXISTS (SELECT 1 FROM ranked_seats WHERE account_id = $1)", account);
        return (bool)(await command.ExecuteScalarAsync(cancellation))!;
    }

    // For signing in below the minimum, by the name typed. It tells such a client whether a name holds a seat, as a 401
    // instead of a 409: ranked names are public on the leaderboard and a seat reveals nothing of the game.
    public async Task<bool> HoldsSeatByName(string? login, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            """
            SELECT EXISTS (SELECT 1 FROM ranked_seats s JOIN accounts a ON a.id = s.account_id
                WHERE a.normalized_name = $1 AND NOT a.technical)
            """, (login ?? "").Trim().ToUpperInvariant());
        return (bool)(await command.ExecuteScalarAsync(cancellation))!;
    }

    // Whether the account plays this match and whether it is ranked: null when the account does not play it.
    public async Task<bool?> RankedFor(string match, string account, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            """
            SELECT EXISTS (SELECT 1 FROM ranked_matches k WHERE k.match_id = m.id)
            FROM matches m WHERE m.id = $1 AND (m.fp_account = $2 OR m.sh_account = $2)
            """, match, account);
        return await command.ExecuteScalarAsync(cancellation) as bool?;
    }

    // The ranked match this account is playing, if any.
    public async Task<string?> Active(string account, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        return await ActiveMatch(connection, null, [account], cancellation);
    }

    // A concession as a command: no turn, no payload, and the client's id so that a retry returns the first receipt.
    public static GameCommand Concession(string match, string commandId) => new()
    {
        MatchId = match, CommandId = commandId, Kind = CommandKind.Concede, Payload = new()
    };

    // Serializes two starts that share an account, in a fixed order so that two pairings cannot deadlock.
    private static async Task LockAccounts(NpgsqlConnection connection, NpgsqlTransaction transaction, string[] accounts,
        CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "SELECT id FROM accounts WHERE id = ANY($1) ORDER BY id FOR UPDATE", (object)accounts);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var found = 0;
        while (await reader.ReadAsync(cancellation))
            found++;
        if (found != accounts.Length)
            throw new InvalidOperationException("Account missing.");
    }

    private static async Task<string?> ActiveMatch(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        string[] accounts, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "SELECT match_id FROM ranked_seats WHERE account_id = ANY($1) ORDER BY account_id LIMIT 1", (object)accounts);
        return await command.ExecuteScalarAsync(cancellation) as string;
    }

    private static async Task Register(NpgsqlConnection connection, NpgsqlTransaction transaction, string match,
        string freePeoples, string shadow, RankedPreset preset, CancellationToken cancellation)
    {
        await using var ranked = Database.Command(connection, transaction,
            "INSERT INTO ranked_matches(match_id, preset) VALUES ($1, $2)", match, preset.ToString());
        await ranked.ExecuteNonQueryAsync(cancellation);
        // My games titles it "Ranked · Free Peoples' name vs Shadow's name" (LOBBY-05).
        await using var title = Database.Command(connection, transaction,
            "UPDATE matches SET display_name = 'Ranked' WHERE id = $1", match);
        await title.ExecuteNonQueryAsync(cancellation);
        await using var seats = Database.Command(connection, transaction,
            "INSERT INTO ranked_seats(account_id, match_id, side) VALUES ($1, $3, 'FreePeoples'), ($2, $3, 'Shadow')",
            freePeoples, shadow, match);
        await seats.ExecuteNonQueryAsync(cancellation);
    }
}
