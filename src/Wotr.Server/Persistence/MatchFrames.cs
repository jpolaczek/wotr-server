using System.Text.Json;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Server;

// REPLAY-01: the recording of a match. A frame is written in the transaction that stores the state — at creation and
// after every accepted public change, CPU moves, battles, the Hunt and card effects included — so a rollback leaves
// none and a retried command (answered from its receipt) adds none. A frame holds both sides' read-only views
// (Projection.Observe) without their history, which match_events keeps once, with the revision it belongs to.
public static class MatchFrames
{
    // Frames never carry options, so the views need no option providers.
    private static readonly Projection Views = new();

    // The match row is new or locked, so the next step number cannot be taken twice.
    internal static async Task Record(NpgsqlConnection connection, NpgsqlTransaction? transaction, MatchState state,
        CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            """
            INSERT INTO match_frames(match_id,step,revision,turn,phase,free_peoples,shadow)
            SELECT $1,COALESCE(max(step) + 1, 0),$2,$3,$4,$5::jsonb,$6::jsonb FROM match_frames WHERE match_id=$1
            """, state.Id, state.Revision, state.Turn, state.Phase.ToString(),
            View(state, Side.FreePeoples), View(state, Side.Shadow));
        await command.ExecuteNonQueryAsync(cancellation);
    }

    public static string View(MatchState state, Side side)
    {
        var view = Views.Observe(state, side);
        view.History = [];
        return JsonSerializer.Serialize(view, BoardCatalog.Json);
    }
}
