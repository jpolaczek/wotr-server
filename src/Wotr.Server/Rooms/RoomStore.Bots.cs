using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

public sealed partial class RoomStore
{
    public Task<RoomResult> AddBot(string id, string account, AddRoomBotRequest request,
        CancellationToken cancellation = default)
    {
        if (!Guid.TryParseExact(request.RequestId, "N", out _) || request.ExpectedVersion < 1)
            return Task.FromResult(new RoomResult(Error: "InvalidBotRequest"));
        return Change("id", id, account, Access.Player, async room =>
        {
            var state = room.State;
            if (state.HostAccount != account)
                return "HostRequired";
            await using var human = Database.Command(room.Connection, room.Transaction,
                "SELECT NOT technical FROM accounts WHERE id=$1", account);
            if (await human.ExecuteScalarAsync(cancellation) is not true)
                return "HumanHostRequired";
            var retry = await BotRetry(room, account, request, cancellation);
            if (retry is not null)
                return retry.Length == 0 ? null : retry;
            if (state.MatchId.Length > 0)
                return "MatchAlreadyStarted";
            if (state.Version != request.ExpectedVersion)
                return "StaleConfiguration";
            var side = state.FreePeoples == account ? Side.Shadow : Side.FreePeoples;
            if (RoomRules.Seat(state, side).Length > 0)
                return "SeatTaken";
            var error = RoomRules.Unavailable(state.Configuration, scope) ?? BotRoomUnavailable(state.Configuration);
            if (error is not null)
                return error;
            var assignment = Guid.NewGuid().ToString("N");
            var participant = BotCredentialStore.Participant(assignment);
            await BotCredentialStore.EnsureParticipant(room.Connection, room.Transaction, assignment, cancellation);
            TakeSeat(state, participant, "CPU", side);
            RoomRules.ResetReady(state);
            state.Bot = new()
            {
                AssignmentId = assignment, ParticipantId = participant, Side = side,
                ProfileId = side == Side.Shadow ? "shadow-strong" : "free-peoples-strong",
                PolicyVersion = botRooms!.PolicyVersion,
                RuntimeVersion = runtime?.Enabled == true ? runtime.Version : null
            };
            await using var insert = Database.Command(room.Connection, room.Transaction, """
                INSERT INTO bot_assignments(id,room_id,host_account,participant_id,request_id,requested_version,
                    room_version,service_id,side,profile_id,policy_version,status,runtime_version)
                VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,'pending',NULLIF($12,''))
                """, assignment, state.Id, account, participant, request.RequestId, request.ExpectedVersion,
                state.Version, state.Bot.RuntimeVersion is null ? bots!.ServiceId : "embedded", side.ToString(),
                state.Bot.ProfileId, state.Bot.PolicyVersion, state.Bot.RuntimeVersion ?? "");
            await insert.ExecuteNonQueryAsync(cancellation);
            if (state.Bot.RuntimeVersion is not null)
            {
                await using var job = Database.Command(room.Connection, room.Transaction,
                    "INSERT INTO bot_runtime_jobs(assignment_id) VALUES ($1)", assignment);
                await job.ExecuteNonQueryAsync(cancellation);
            }
            return null;
        }, cancellation);
    }

    // null: a new request; empty string: the original reservation; otherwise a conflict/cancellation.
    private static async Task<string?> BotRetry(LockedRoom room, string account, AddRoomBotRequest request,
        CancellationToken cancellation)
    {
        await using var query = Database.Command(room.Connection, room.Transaction, """
            SELECT id,requested_version,status FROM bot_assignments
            WHERE room_id=$1 AND host_account=$2 AND request_id=$3
            """, room.State.Id, account, request.RequestId);
        await using var row = await query.ExecuteReaderAsync(cancellation);
        if (!await row.ReadAsync(cancellation))
            return null;
        if (row.GetInt32(1) != request.ExpectedVersion)
            return "BotRequestConflict";
        return row.GetString(2) == "cancelled" || room.State.Bot?.AssignmentId != row.GetString(0)
            ? "BotReservationCancelled" : "";
    }

    private string? BotRoomUnavailable(MatchConfiguration configuration, string? policyVersion = null,
        string? runtimeVersion = null, bool? embedded = null) =>
        (embedded ?? runtime?.Enabled) == true ? runtime is null ? "BotUnavailable" : runtime.Unavailable(configuration, policyVersion, runtimeVersion) :
        bots?.Enabled != true || botRooms?.Enabled != true ? "BotUnavailable" :
        policyVersion is not null && policyVersion != botRooms.PolicyVersion ? "BotVersionUnsupported" :
        !bots.Supports(configuration) ? "BotConfigurationUnsupported" : null;

    private RoomView View(RoomState room, string account, IReadOnlyList<RoomObserver> observers)
    {
        var view = room.View(account, scope, observers, audience.Limit);
        if (room.Bot is not null && (room.MatchId.Length == 0 || room.Bot.RuntimeVersion is not null) &&
            (RoomRules.Unavailable(room.Configuration, scope) ?? BotRoomUnavailable(room.Configuration,
                room.Bot.PolicyVersion, room.Bot.RuntimeVersion, room.Bot.RuntimeVersion is not null)) is { } error)
        {
            view.Bot!.Status = BotSeatStatus.Unavailable;
            if (room.MatchId.Length == 0)
                view.StartUnavailableReason = error;
        }
        if (room.Bot is { RuntimeVersion: not null, RuntimeHealthy: false } && view.Bot is not null)
        {
            view.Bot.Status = BotSeatStatus.Unavailable;
            if (room.MatchId.Length == 0)
                view.StartUnavailableReason = "BotNotReady";
        }
        return view;
    }

    private string? SetRoomReady(RoomState state, string account, RoomReadyRequest request, BotSeatAuthority? authority)
    {
        if (state.Bot is not { } bot || bot.ParticipantId != account)
            return RoomRules.SetReady(state, account, request.ExpectedVersion, request.Ready);
        if (bot.RuntimeVersion is not null)
            return "BotCredentialsRequired";
        if (authority is null || authority.GrantId != bot.AssignmentId || authority.ServiceId != bots?.ServiceId)
            return "BotCredentialsRequired";
        var error = BotRoomUnavailable(state.Configuration, bot.PolicyVersion, embedded: false) ??
            RoomRules.SetReady(state, account, request.ExpectedVersion, request.Ready);
        if (error is null)
            bot.ReadyGeneration = request.Ready ? authority.Generation : null;
        return error;
    }

    private async Task<string?> CanStartBot(LockedRoom room, CancellationToken cancellation)
    {
        if (room.State.Bot is not { } bot)
            return null;
        if (bot.RuntimeVersion is not null)
            return await CanStartRuntime(room, cancellation);
        if (RoomRules.Seat(room.State, bot.Side) != bot.ParticipantId ||
            RoomRules.Seat(room.State, TurnFlow.Other(bot.Side)) != room.State.HostAccount || bot.ReadyGeneration is null)
            return "BotNotReady";
        if (BotRoomUnavailable(room.State.Configuration, bot.PolicyVersion, embedded: false) is { } error)
            return error;
        // The room -> grant lock order also fences reassign/revoke racing the start.
        await using var query = Database.Command(room.Connection, room.Transaction, """
            SELECT lease_until FROM bot_seat_grants WHERE id=$1 AND room_id=$2 AND participant_id=$3
                AND service_id=$4 AND side=$5 AND generation=$6 AND NOT revoked
                AND match_id IS NULL FOR SHARE
            """, bot.AssignmentId, room.State.Id, bot.ParticipantId, bots!.ServiceId,
            bot.Side.ToString(), bot.ReadyGeneration.Value);
        if (await query.ExecuteScalarAsync(cancellation) is not DateTime lease)
            return "BotNotReady";
        await using var clock = Database.Command(room.Connection, room.Transaction, "SELECT clock_timestamp()");
        return lease > (DateTime)(await clock.ExecuteScalarAsync(cancellation))! ? null : "BotNotReady";
    }

    private static async Task BindBotMatch(LockedRoom room, CancellationToken cancellation)
    {
        var bot = room.State.Bot!;
        if (bot.RuntimeVersion is null)
        {
            await using var grant = Database.Command(room.Connection, room.Transaction,
                "UPDATE bot_seat_grants SET match_id=$2 WHERE id=$1", bot.AssignmentId, room.State.MatchId);
            if (await grant.ExecuteNonQueryAsync(cancellation) != 1)
                throw new InvalidOperationException("CPU grant disappeared under the room lock.");
        }
        else
            await InvalidateRuntime(room, cancellation);
        await using var assignment = Database.Command(room.Connection, room.Transaction,
            "UPDATE bot_assignments SET match_id=$2,status='started' WHERE id=$1 AND status='pending'",
            bot.AssignmentId, room.State.MatchId);
        if (await assignment.ExecuteNonQueryAsync(cancellation) != 1)
            throw new InvalidOperationException("CPU assignment disappeared under the room lock.");
    }

    private static async Task ReconcileBot(LockedRoom room, CancellationToken cancellation)
    {
        var state = room.State;
        if (state.Bot is not { } bot)
            return;
        if (state.MatchId.Length == 0 && (state.Closed || !state.Contains(state.HostAccount) ||
            RoomRules.Seat(state, bot.Side) != bot.ParticipantId))
        {
            await using var cancel = Database.Command(room.Connection, room.Transaction,
                "UPDATE bot_assignments SET status='cancelled' WHERE id=$1", bot.AssignmentId);
            await cancel.ExecuteNonQueryAsync(cancellation);
            await using var revoke = Database.Command(room.Connection, room.Transaction,
                "UPDATE bot_seat_grants SET revoked=true WHERE id=$1", bot.AssignmentId);
            await revoke.ExecuteNonQueryAsync(cancellation);
            await InvalidateRuntime(room, cancellation);
            RoomRules.ClearSeat(state, bot.ParticipantId);
            state.Bot = null;
            return;
        }
        if ((bot.Side == Side.FreePeoples ? state.FreePeoplesAccepted : state.ShadowAccepted) != state.Version)
            bot.ReadyGeneration = null;
        if (bot.RuntimeVersion is not null)
        {
            await using var previous = Database.Command(room.Connection, room.Transaction,
                "SELECT room_version FROM bot_assignments WHERE id=$1", bot.AssignmentId);
            if (await previous.ExecuteScalarAsync(cancellation) is int version && version != state.Version)
                await InvalidateRuntime(room, cancellation);
        }
        await using var update = Database.Command(room.Connection, room.Transaction,
            "UPDATE bot_assignments SET room_version=$2 WHERE id=$1", bot.AssignmentId, state.Version);
        await update.ExecuteNonQueryAsync(cancellation);
    }
}
