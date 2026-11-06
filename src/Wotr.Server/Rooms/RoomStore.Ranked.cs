using Npgsql;
using Wotr.Contracts;

namespace Wotr.Server;

public sealed partial class RoomStore
{
    public const string RankedRefusal = "RankedMatch";

    // RANK-04 (R35): the unlisted room of a ranked match, written in the transaction that creates the match, so the
    // match's chat works (CHAT-01). It has no host, no invitation and no waiting room: the server seated both players.
    public static async Task InsertRanked(NpgsqlConnection connection, NpgsqlTransaction transaction, string match,
        string freePeoples, string shadow, MatchConfiguration configuration, CancellationToken cancellation)
    {
        var room = new RoomState
        {
            Id = Guid.NewGuid().ToString("N"), JoinCode = SessionStore.NewToken(), Name = "Ranked game",
            Managed = true, Visibility = RoomVisibility.Private, InvitationEnabled = false, Ranked = true,
            Configuration = configuration, MatchId = match
        };
        TakeSeat(room, freePeoples, await AccountName(connection, transaction, freePeoples, cancellation), Side.FreePeoples);
        TakeSeat(room, shadow, await AccountName(connection, transaction, shadow, cancellation), Side.Shadow);
        await using var command = Database.Command(connection, transaction,
            """
            INSERT INTO rooms(id,join_code,fp_account,sh_account,state,room_name,host_account,visibility,match_id)
            VALUES ($1,$2,$3,$4,$5::jsonb,$6,NULL,$7,$8)
            """, room.Id, room.JoinCode, freePeoples, shadow, Serialize(room), room.Name, room.Visibility.ToString(), match);
        await command.ExecuteNonQueryAsync(cancellation);
    }
}
