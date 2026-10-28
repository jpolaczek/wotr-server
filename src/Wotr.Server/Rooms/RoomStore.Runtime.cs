using Npgsql;

namespace Wotr.Server;

public sealed partial class RoomStore
{
    // Room -> job, the same order as start/configuration/removal. Never renew seen_at.
    public async Task<bool> ReadyRuntime(BotWork work, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        await using var read = Database.Command(connection, transaction,
            "SELECT state::text FROM rooms WHERE id=$1 FOR UPDATE", work.RoomId);
        if (await read.ExecuteScalarAsync(cancellation) is not string json)
            return false;
        var state = Deserialize(json);
        if (state.Bot is not { RuntimeVersion: not null } bot || bot.AssignmentId != work.AssignmentId ||
            state.Closed || state.MatchId.Length > 0 || state.Version != work.RoomVersion ||
            RoomRules.Seat(state, bot.Side) != work.ParticipantId || !state.Contains(state.HostAccount) ||
            bot.RuntimeVersion != work.RuntimeVersion || bot.PolicyVersion != work.PolicyVersion ||
            BotRoomUnavailable(state.Configuration, bot.PolicyVersion, bot.RuntimeVersion, true) is not null)
            return false;
        await BotJobStore.Lock(connection, transaction, work, cancellation);
        await using var presence = Database.Command(connection, transaction, """
            SELECT seen_at>clock_timestamp()-make_interval(secs => $2) FROM rooms WHERE id=$1
            """, state.Id, (emptyRooms?.EmptyAfter ?? TimeSpan.FromSeconds(30)).TotalSeconds);
        if (await presence.ExecuteScalarAsync(cancellation) is not true)
            return false;
        RoomRules.SetReady(state, bot.ParticipantId, state.Version, true);
        await using var health = Database.Command(connection, transaction, """
            UPDATE bot_runtime_jobs SET ready_version=$2,last_error='',
                health_until=clock_timestamp()+make_interval(secs => $3) WHERE assignment_id=$1
            """, bot.AssignmentId, state.Version, botHost!.Lease.TotalSeconds);
        await health.ExecuteNonQueryAsync(cancellation);
        var room = await LockedRoom.Load(state, audience, connection, transaction, cancellation);
        await Save(room, cancellation, refreshPresence: false);
        await transaction.CommitAsync(cancellation);
        return true;
    }

    private async Task<string?> CanStartRuntime(LockedRoom room, CancellationToken cancellation)
    {
        var state = room.State;
        var bot = state.Bot!;
        var error = BotRoomUnavailable(state.Configuration, bot.PolicyVersion, bot.RuntimeVersion, true);
        if (error is not null)
            return error;
        await using var query = Database.Command(room.Connection, room.Transaction, """
            SELECT health_until,ready_version FROM bot_runtime_jobs WHERE assignment_id=$1 FOR UPDATE
            """, bot.AssignmentId);
        DateTime health;
        await using (var row = await query.ExecuteReaderAsync(cancellation))
        {
            if (!await row.ReadAsync(cancellation) || row.IsDBNull(1) || row.GetInt32(1) != state.Version)
                return "BotNotReady";
            health = row.GetDateTime(0);
        }
        await using var clock = Database.Command(room.Connection, room.Transaction, "SELECT clock_timestamp()");
        return RoomRules.Seat(state, bot.Side) == bot.ParticipantId && state.Contains(state.HostAccount) &&
            health > (DateTime)(await clock.ExecuteScalarAsync(cancellation))! ? null : "BotNotReady";
    }

    private static async Task InvalidateRuntime(LockedRoom room, CancellationToken cancellation)
    {
        await using var job = Database.Command(room.Connection, room.Transaction, """
            UPDATE bot_runtime_jobs SET generation=generation+1,owner_id='',lease_until='-infinity',
                available_at=clock_timestamp(),health_until='-infinity',ready_version=NULL
            WHERE assignment_id=$1
            """, room.State.Bot!.AssignmentId);
        await job.ExecuteNonQueryAsync(cancellation);
    }

    private static async Task RuntimeHealth(NpgsqlConnection connection, IReadOnlyList<RoomState> rooms,
        CancellationToken cancellation)
    {
        var bots = rooms.Where(r => r.Bot?.RuntimeVersion is not null).Select(r => r.Bot!).ToDictionary(b => b.AssignmentId);
        var acknowledged = rooms.Where(r => r.Bot?.RuntimeVersion is not null && (r.MatchId.Length > 0 ||
            (r.Bot.Side == Wotr.Contracts.Side.FreePeoples ? r.FreePeoplesAccepted : r.ShadowAccepted) == r.Version))
            .Select(r => r.Bot!.AssignmentId).ToHashSet();
        if (bots.Count == 0)
            return;
        await using var query = Database.Command(connection, null, """
            SELECT assignment_id,health_until>clock_timestamp(),ready_version IS NOT NULL OR last_error<>''
            FROM bot_runtime_jobs WHERE assignment_id=ANY($1)
            """, (object)bots.Keys.ToArray());
        await using var row = await query.ExecuteReaderAsync(cancellation);
        while (await row.ReadAsync(cancellation))
            bots[row.GetString(0)].RuntimeHealthy = row.GetBoolean(1) ? true :
                row.GetBoolean(2) || acknowledged.Contains(row.GetString(0)) ? false : null;
    }
}
