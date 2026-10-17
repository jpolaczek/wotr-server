using Wotr.Contracts;

namespace Wotr.Server;

public sealed partial class RoomStore
{
    public Task<RoomResult> SetReady(string id, string account, RoomReadyRequest request,
        CancellationToken cancellation = default) =>
        Change("id", id, account, Access.Player, room => Task.FromResult(
            RoomRules.SetReady(room.State, account, request.ExpectedVersion, request.Ready)), cancellation);

    // OBS-01: the code also lets observers in after the seats are taken or the match started, so the host may show
    // it at any time.
    public Task<RoomResult> Invitation(string id, string account, CancellationToken cancellation = default) =>
        Change("id", id, account, Access.Participant, room =>
        {
            if (room.State.HostAccount != account)
                return Task.FromResult<string?>("HostRequired");
            room.State.InvitationEnabled = true;
            return Task.FromResult<string?>(null);
        }, cancellation);

    // The host closes the room for everyone in it (MENU-23); MENU-32: a room nobody else is in is deleted at once.
    // OBS-01: an observer's leave frees their place at once; after the start the host only stops watching.
    public async Task<RoomResult> Leave(string id, string account, CancellationToken cancellation = default)
    {
        var empty = false;
        var result = await Change("id", id, account, Access.Participant, async room =>
        {
            var state = room.State;
            if (room.Observes(account) && (state.MatchId.Length > 0 || state.HostAccount != account))
            {
                await room.RemoveObserver(account, cancellation);
                return null;
            }
            // A concurrent start won the lock: leaving the screen must not remove a player.
            if (state.MatchId.Length > 0)
                return null;
            if (state.HostAccount == account)
            {
                empty = room.OnlyParticipant(account);
                state.Closed = true;
                RoomRules.ResetInvitation(state);
            }
            else
                RoomRules.Vacate(state, account);
            return null;
        }, cancellation);
        if (empty && result.Error.Length == 0)
            await Delete(id, cancellation);
        return result;
    }

    private async Task Delete(string id, CancellationToken cancellation)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null, "DELETE FROM rooms WHERE id=$1 AND match_id IS NULL", id);
        await command.ExecuteNonQueryAsync(cancellation);
    }

    // MENU-24: a removed participant learns why the room is gone for them; everybody else outside it still sees nothing.
    public async Task<bool> WasRemoved(string id, string account, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null, "SELECT state::text FROM rooms WHERE id=$1 AND NOT closed", id);
        return await command.ExecuteScalarAsync(cancellation) is string json && Deserialize(json).Removed.Contains(account);
    }
}
