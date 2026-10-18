using Wotr.Contracts;

namespace Wotr.Server;

public sealed record AcceptResult(RoomView? Room, ChallengeError Error);

// LOBBY-01: the challenged player accepts. The challenge is claimed first (a second accept finds it taken), then a
// private table is set like any other: the challenger hosts it on the Free Peoples seat and opens its invitation, and
// the challenged player takes the other seat through it. Only then does the challenge name its room, which is how the
// challenger's next poll learns where to go. If anything fails before that, the table is removed and the challenge
// given back, so no half-set table or taken challenge stays behind.
public sealed class ChallengeAcceptance(LobbyChallenges challenges, RoomStore rooms, ILogger<ChallengeAcceptance> log)
{
    // The longest table name RoomInput.ValidName accepts.
    private const int LongestTableName = 64;

    public async Task<AcceptResult> Accept(string account, string id, CancellationToken cancellation = default)
    {
        var (claimed, error) = await challenges.Claim(account, id, cancellation);
        if (claimed is null)
            return new(null, error);
        string? roomId = null;
        try
        {
            roomId = await Open(claimed, cancellation);
            await Seat(claimed, roomId, cancellation);
            await challenges.SetRoom(claimed.Id, roomId, cancellation);
        }
        catch (Exception failure)
        {
            await GiveBack(claimed, roomId);
            if (failure is not (InvalidOperationException or ArgumentException))
                throw;
            log.LogWarning("A challenge's table could not be set: {Reason}", failure.Message);
            return new(null, ChallengeError.RoomUnavailable);
        }
        return new(await rooms.Read(roomId, account, cancellation), ChallengeError.None);
    }

    // "<challenger> vs <challenged>", cut to the longest table name.
    public static string TableName(string from, string to)
    {
        var name = from + " vs " + to;
        return name.Length <= LongestTableName ? name : name[..LongestTableName];
    }

    // The challenger's private table on the Free Peoples seat; returns its id.
    private async Task<string> Open(ClaimedChallenge challenge, CancellationToken cancellation)
    {
        var room = await rooms.Create(challenge.From, new CreateRoomRequest
        {
            Name = TableName(challenge.FromName, challenge.ToName),
            Visibility = RoomVisibility.Private,
            Side = Side.FreePeoples
        }, cancellation);
        return room.Id;
    }

    // The challenged player takes the other seat through the table's invitation, as a guest with the code would.
    private async Task Seat(ClaimedChallenge challenge, string roomId, CancellationToken cancellation)
    {
        var invitation = await rooms.Invitation(roomId, challenge.From, cancellation);
        var code = invitation.Room?.JoinCode ?? "";
        if (code.Length == 0)
            throw new InvalidOperationException(invitation.Error.Length > 0 ? invitation.Error : "NoInvitation");
        var joined = await rooms.Join(code, challenge.To, RoomRole.Player, cancellation);
        if (joined.Room is null)
            throw new InvalidOperationException(joined.Error);
    }

    // The challenged player leaves first, so the host's leaving finds the table empty and removes it.
    private async Task GiveBack(ClaimedChallenge challenge, string? roomId)
    {
        if (roomId is not null)
        {
            await rooms.Leave(roomId, challenge.To, CancellationToken.None);
            await rooms.Leave(roomId, challenge.From, CancellationToken.None);
        }
        await challenges.Release(challenge.Id, CancellationToken.None);
    }
}
