using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

// TEST-01: one test match per owner. The owner plays the Free Peoples with their own session; a technical
// seat account plays the Shadow, so the existing account-to-side rules, history and sockets stay unchanged.
// Starting, restarting or ending removes the previous test match with everything stored for it.
public sealed class TestSessionStore(Database database)
{
    public const string SeatPrefix = "testseat-";
    public const string SeatName = "Test opponent";

    public static bool IsSeat(string account) => account.StartsWith(SeatPrefix, StringComparison.Ordinal);

    public async Task<TestSessionView> Start(string owner, TestScenario scenario, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        await LockOwner(connection, transaction, owner, cancellation);
        await Remove(connection, transaction, owner, cancellation);
        var seat = await EnsureSeat(connection, transaction, owner, cancellation);
        var state = scenario.Create(Guid.NewGuid().ToString("N"));
        await MatchStore.Insert(connection, transaction, state, owner, seat, cancellation);
        await Register(connection, transaction, state.Id, owner, scenario, cancellation);
        var session = await SessionStore.Issue(connection, transaction, seat, SeatName, cancellation);
        await transaction.CommitAsync(cancellation);
        return new()
        {
            MatchId = state.Id,
            Scenario = scenario.View(),
            OwnerSide = Side.FreePeoples,
            Seat = session
        };
    }

    public async Task End(string owner, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        await LockOwner(connection, transaction, owner, cancellation);
        await Remove(connection, transaction, owner, cancellation);
        await transaction.CommitAsync(cancellation);
    }

    // Serializes starts of one owner, so two clicks cannot leave two test matches.
    private static async Task LockOwner(NpgsqlConnection connection, NpgsqlTransaction transaction, string owner, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction, "SELECT id FROM accounts WHERE id=$1 FOR UPDATE", owner);
        await command.ExecuteScalarAsync(cancellation);
    }

    private static async Task Remove(NpgsqlConnection connection, NpgsqlTransaction transaction, string owner, CancellationToken cancellation)
    {
        var matches = await OwnedMatches(connection, transaction, owner, cancellation);
        await MatchRemoval.Remove(connection, transaction, matches, cancellation);
        await using var revoke = Database.Command(connection, transaction,
            "UPDATE sessions SET revoked=true WHERE account_id=$1 AND NOT revoked", SeatPrefix + owner);
        await revoke.ExecuteNonQueryAsync(cancellation);
    }

    private static async Task<string[]> OwnedMatches(NpgsqlConnection connection, NpgsqlTransaction transaction, string owner, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction, "SELECT match_id FROM test_matches WHERE owner_account=$1", owner);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var matches = new List<string>();
        while (await reader.ReadAsync(cancellation))
            matches.Add(reader.GetString(0));
        return [.. matches];
    }

    // The seat cannot sign in: no username can reach its normalized name and its password is never known.
    private static async Task<string> EnsureSeat(NpgsqlConnection connection, NpgsqlTransaction transaction, string owner, CancellationToken cancellation)
    {
        var seat = SeatPrefix + owner;
        await using var command = Database.Command(connection, transaction, """
            INSERT INTO accounts(id, username, normalized_name, password_hash, is_admin, technical)
            VALUES ($1, $2, $3, $4, false, true) ON CONFLICT (id) DO NOTHING
            """, seat, SeatName, "#" + seat, "!" + SessionStore.NewToken());
        await command.ExecuteNonQueryAsync(cancellation);
        return seat;
    }

    private static async Task Register(NpgsqlConnection connection, NpgsqlTransaction transaction, string match, string owner,
        TestScenario scenario, CancellationToken cancellation)
    {
        await using var row = Database.Command(connection, transaction,
            "INSERT INTO test_matches(match_id, owner_account, scenario_id) VALUES ($1, $2, $3)", match, owner, scenario.Id);
        await row.ExecuteNonQueryAsync(cancellation);
        await using var name = Database.Command(connection, transaction,
            "UPDATE matches SET display_name=$1 WHERE id=$2", "Test · " + scenario.Title, match);
        await name.ExecuteNonQueryAsync(cancellation);
    }
}
