using Npgsql;
using Wotr.Domain;

namespace Wotr.Server;

// REPLAY-02: what the export needs to know about one match before it writes anything.
internal sealed record ReplayHeader(string Match, string Name, string FreePeoples, string Shadow, bool Recorded,
    bool EventsIndexed, MatchState State)
{
    public bool Finished => State.Winner is not null || State.Phase == Wotr.Contracts.GamePhase.Finished;
}

internal static class ReplaySource
{
    // Players of the match, or (for observers) the shared observer rule over `matches m` with $1/$2.
    public const string Player = "(m.fp_account=$2 OR m.sh_account=$2)";

    public static async Task<ReplayHeader?> Header(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string match, string account, string access, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            """
            SELECT m.display_name,fa.username,sa.username,m.recorded,m.events_indexed,m.state::text
            FROM matches m JOIN accounts fa ON fa.id=m.fp_account JOIN accounts sa ON sa.id=m.sh_account
            WHERE m.id=$1 AND
            """ + " " + access, match, account);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        if (!await reader.ReadAsync(cancellation))
            return null;
        return new ReplayHeader(match, reader.GetString(0), reader.GetString(1), reader.GetString(2),
            reader.GetBoolean(3), reader.GetBoolean(4), StateCodec.Read(reader.GetString(5)));
    }
}
