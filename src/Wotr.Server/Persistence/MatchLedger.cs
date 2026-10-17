using Npgsql;
using Wotr.Contracts;

namespace Wotr.Server;

public enum MatchKind
{
    Game,
    Test,
    Demo
}

// ADMIN-01: every match that ever started and every table that was set, kept apart from the live rows so the admin
// panel's counts survive a match its players remove (MatchRemoval) or an empty table the sweeper closes.
public static class MatchLedger
{
    public static async Task Started(NpgsqlConnection connection, NpgsqlTransaction? transaction, string match, MatchKind kind,
        string fp, string sh, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "INSERT INTO match_ledger(match_id,kind,fp_account,sh_account) VALUES ($1,$2,$3,$4) ON CONFLICT (match_id) DO NOTHING",
            match, Kind(kind), fp, sh);
        await command.ExecuteNonQueryAsync(cancellation);
    }

    // Only the first save with a winner finishes the match; later saves of the ended game change nothing.
    public static async Task Finished(NpgsqlConnection connection, NpgsqlTransaction transaction, string match, Side winner,
        CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "UPDATE match_ledger SET finished_at=now(),winner=$2 WHERE match_id=$1 AND finished_at IS NULL",
            match, winner.ToString());
        await command.ExecuteNonQueryAsync(cancellation);
    }

    public static async Task TableSet(NpgsqlConnection connection, string room, string host, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null,
            "INSERT INTO room_ledger(room_id,host_account) VALUES ($1,$2) ON CONFLICT (room_id) DO NOTHING", room, host);
        await command.ExecuteNonQueryAsync(cancellation);
    }

    public static string Kind(MatchKind kind) => kind switch
    {
        MatchKind.Test => "test",
        MatchKind.Demo => "demo",
        _ => "game"
    };
}
