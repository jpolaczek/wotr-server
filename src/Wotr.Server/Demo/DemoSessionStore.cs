using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

public sealed record DemoStart(DemoMatchView? Demo = null, string Error = "");

// DEMO-01: one demo match per owner, started from a waiting room. The owner keeps the side of their seat; the
// technical CPU account sits on the other side, so the existing account-to-side rules, history and sockets stay
// unchanged. Starting a new demo removes the previous one with everything stored for it. The room is untouched.
public sealed class DemoSessionStore(Database database, RoomStore rooms, IRandomSource random)
{
    public const string CpuAccount = "demo-cpu";
    public const string CpuName = "CPU";

    public async Task<DemoStart> Start(string owner, string roomId, CancellationToken cancellation = default)
    {
        var room = await rooms.Read(roomId, owner, cancellation);
        if (room is null || room.Status == RoomStatus.Closed)
            return new(Error: "RoomNotFound");
        if (room.Status != RoomStatus.Waiting)
            return new(Error: "MatchAlreadyStarted");
        // OBS-01: an observer of the waiting room has no side to play.
        if (room.Role != RoomRole.Player)
            return new(Error: "PlayerRequired");
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        await LockOwner(connection, transaction, owner, cancellation);
        await Remove(connection, transaction, owner, cancellation);
        await EnsureCpu(connection, transaction, cancellation);
        var match = NewMatch();
        var cpu = Opposite(room.You);
        var (fp, sh) = room.You == Side.FreePeoples ? (owner, CpuAccount) : (CpuAccount, owner);
        await MatchStore.Insert(connection, transaction, match, fp, sh, MatchKind.Demo, cancellation);
        await Register(connection, transaction, match.Id, owner, room.You, cpu, cancellation);
        await transaction.CommitAsync(cancellation);
        return new(new DemoMatchView { MatchId = match.Id, You = room.You });
    }

    // The CPU's side in a demo match, or nothing for every other match.
    public async Task<Side?> CpuSide(string matchId, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null, "SELECT cpu_side FROM demo_matches WHERE match_id=$1", matchId);
        return await command.ExecuteScalarAsync(cancellation) is string side ? Enum.Parse<Side>(side) : null;
    }

    // The demo is always the base game (MVP), whatever the room is set up for.
    private MatchState NewMatch()
    {
        var match = MatchFactory.Create(Guid.NewGuid().ToString("N"), random, ConfigurationRules.Normalize(new()));
        TurnFlow.Start(match);
        return match;
    }

    private static Side Opposite(Side side) => side == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;

    // Serializes the starts of one owner, so two clicks cannot leave two demos.
    private static async Task LockOwner(NpgsqlConnection connection, NpgsqlTransaction transaction, string owner,
        CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction, "SELECT id FROM accounts WHERE id=$1 FOR UPDATE", owner);
        await command.ExecuteScalarAsync(cancellation);
    }

    private static async Task Remove(NpgsqlConnection connection, NpgsqlTransaction transaction, string owner,
        CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "SELECT match_id FROM demo_matches WHERE owner_account=$1", owner);
        var previous = await command.ExecuteScalarAsync(cancellation) as string;
        if (previous is not null)
            await MatchRemoval.Remove(connection, transaction, [previous], cancellation);
    }

    // The CPU cannot sign in: no username reaches its normalized name and its password is never known.
    private static async Task EnsureCpu(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction, """
            INSERT INTO accounts(id, username, normalized_name, password_hash, is_admin, technical)
            VALUES ($1, $2, $3, $4, false, true) ON CONFLICT (id) DO NOTHING
            """, CpuAccount, CpuName, "#" + CpuAccount, "!" + SessionStore.NewToken());
        await command.ExecuteNonQueryAsync(cancellation);
    }

    private static async Task Register(NpgsqlConnection connection, NpgsqlTransaction transaction, string match, string owner,
        Side you, Side cpu, CancellationToken cancellation)
    {
        await using var row = Database.Command(connection, transaction,
            "INSERT INTO demo_matches(match_id, owner_account, cpu_side) VALUES ($1, $2, $3)", match, owner, cpu.ToString());
        await row.ExecuteNonQueryAsync(cancellation);
        await using var name = Database.Command(connection, transaction,
            "UPDATE matches SET display_name=$1 WHERE id=$2", "Demo · " + SideName(you) + " vs CPU", match);
        await name.ExecuteNonQueryAsync(cancellation);
    }

    private static string SideName(Side side) => side == Side.FreePeoples ? "Free Peoples" : "Shadow";
}
