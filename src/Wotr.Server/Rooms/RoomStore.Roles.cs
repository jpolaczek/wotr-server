using Wotr.Contracts;

namespace Wotr.Server;

// OBS-01: joining as a player or an observer, moving between a seat and the audience before the start, and the host
// removing one participant. Every operation runs under the room's row lock and changes nothing when it is refused.
public sealed partial class RoomStore
{
    public Task<RoomResult> Join(string code, string account, CancellationToken cancellation = default) =>
        Join(code, account, RoomRole.Player, cancellation);

    public Task<RoomResult> Join(string code, string account, RoomRole role, CancellationToken cancellation = default) =>
        Change("join_code", code, account, Access.Anyone, room => Admit(room, account, role, false, cancellation),
            cancellation);

    public Task<RoomResult> JoinPublic(string id, string account, CancellationToken cancellation = default) =>
        JoinPublic(id, account, RoomRole.Player, cancellation);

    public Task<RoomResult> JoinPublic(string id, string account, RoomRole role, CancellationToken cancellation = default) =>
        Change("id", id, account, Access.Anyone, room => Admit(room, account, role, true, cancellation), cancellation);

    // A participant coming back keeps their role; a newcomer takes the free seat or a place in the audience.
    private static async Task<string?> Admit(LockedRoom room, string account, RoomRole role, bool publicJoin,
        CancellationToken cancellation)
    {
        var state = room.State;
        if (state.Contains(account))
            return null;
        if (room.Observer(account) is { } observer)
        {
            await room.AddObserver(account, observer.Name, cancellation);
            return null;
        }
        if (state.Removed.Contains(account))
            return "RemovedFromRoom";
        if (publicJoin ? state.Visibility != RoomVisibility.Public : !state.InvitationEnabled)
            return "RoomNotFound";
        return role == RoomRole.Observer
            ? await AdmitObserver(room, account, cancellation)
            : await TakeFreeSeat(room, account, cancellation);
    }

    // OBS-01: the audience is open before and during the match, never after it ended.
    private static async Task<string?> AdmitObserver(LockedRoom room, string account, CancellationToken cancellation)
    {
        if (await MatchFinished(room, cancellation))
            return "MatchFinished";
        if (room.AudienceFull)
            return "ObserversFull";
        var name = await AccountName(room.Connection, room.Transaction, account, cancellation);
        await room.AddObserver(account, name, cancellation);
        return null;
    }

    private static async Task<string?> TakeFreeSeat(LockedRoom room, string account, CancellationToken cancellation)
    {
        var state = room.State;
        if (state.MatchId.Length > 0)
            return "MatchAlreadyStarted";
        if (state.FreePeoples.Length > 0 && state.Shadow.Length > 0)
            return "RoomFull";
        var name = await AccountName(room.Connection, room.Transaction, account, cancellation);
        TakeSeat(state, account, name, state.FreePeoples.Length == 0 ? Side.FreePeoples : Side.Shadow);
        if (state.Managed)
            RoomRules.ResetReady(state);
        return null;
    }

    private static async Task<bool> MatchFinished(LockedRoom room, CancellationToken cancellation)
    {
        if (room.State.MatchId.Length == 0)
            return false;
        await using var command = Database.Command(room.Connection, room.Transaction,
            "SELECT state->>'phase'='Finished' FROM matches WHERE id=$1", room.State.MatchId);
        return await command.ExecuteScalarAsync(cancellation) is true;
    }

    public Task<RoomResult> ChangeRole(string id, string account, RoomRoleRequest request,
        CancellationToken cancellation = default) =>
        Change("id", id, account, Access.Participant, room => request.Role == RoomRole.Observer
            ? ToAudience(room, account, cancellation)
            : ToSeat(room, account, request.Side, cancellation), cancellation);

    // A player moves to the audience only while it has a free place; the seat is freed and nobody is ready any more.
    private static async Task<string?> ToAudience(LockedRoom room, string account, CancellationToken cancellation)
    {
        var state = room.State;
        if (room.Observes(account))
            return null;
        if (state.MatchId.Length > 0)
            return "RolesLocked";
        if (room.AudienceFull)
            return "ObserversFull";
        var name = state.PlayerName(account);
        RoomRules.LeaveSeat(state, account);
        await room.AddObserver(account, name, cancellation);
        return null;
    }

    // An observer or the other player takes a free seat; a taken seat is never handed over.
    private static async Task<string?> ToSeat(LockedRoom room, string account, Side side, CancellationToken cancellation)
    {
        var state = room.State;
        if (RoomRules.Seat(state, side) == account)
            return null;
        var error = RoomRules.CanTakeSeat(state, side);
        if (error is not null)
            return error;
        var name = room.Observer(account)?.Name ?? state.PlayerName(account);
        if (room.Observes(account))
            await room.RemoveObserver(account, cancellation);
        else
            RoomRules.ClearSeat(state, account);
        TakeSeat(state, account, name, side);
        RoomRules.ResetReady(state);
        return null;
    }

    public Task<RoomResult> Remove(string id, string account, CancellationToken cancellation = default) =>
        Remove(id, account, new RemoveParticipantRequest(), cancellation);

    // MENU-24: an empty name removes the other player; OBS-01: a name picks any participant but the host.
    public Task<RoomResult> Remove(string id, string account, RemoveParticipantRequest request,
        CancellationToken cancellation = default) =>
        Change("id", id, account, Access.Participant, room => request.Name.Length == 0
            ? Task.FromResult(RoomRules.Remove(room.State, account))
            : RemoveNamed(room, account, request.Name, cancellation), cancellation);

    // A player loses the seat, an observer the place, and that account cannot come back to this room.
    private static async Task<string?> RemoveNamed(LockedRoom room, string host, string name, CancellationToken cancellation)
    {
        var state = room.State;
        var error = RoomRules.CanRemove(state, host);
        if (error is not null)
            return error;
        var target = Named(room, name);
        if (target is null || target == host)
            return "ParticipantNotFound";
        if (room.Observes(target))
            await room.RemoveObserver(target, cancellation);
        else
            RoomRules.Vacate(state, target);
        state.Removed.Add(target);
        return null;
    }

    private static string? Named(LockedRoom room, string name)
    {
        var state = room.State;
        if (state.FreePeoples.Length > 0 && state.FreePeoplesName == name)
            return state.FreePeoples;
        if (state.Shadow.Length > 0 && state.ShadowName == name)
            return state.Shadow;
        return room.Observers.FirstOrDefault(o => o.Name == name)?.Account;
    }
}
