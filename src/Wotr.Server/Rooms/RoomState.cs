using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

public sealed class RoomState
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string HostAccount { get; set; } = "";
    public string HostName { get; set; } = "";
    public RoomVisibility Visibility { get; set; }
    public bool Managed { get; set; }
    public bool Closed { get; set; }
    public bool InvitationEnabled { get; set; } = true;
    public string JoinCode { get; set; } = "";
    public string FreePeoples { get; set; } = "";
    public string Shadow { get; set; } = "";
    public string FreePeoplesName { get; set; } = "";
    public string ShadowName { get; set; } = "";
    public int Version { get; set; } = 1;
    public int? FreePeoplesAccepted { get; set; }
    public int? ShadowAccepted { get; set; }
    public MatchConfiguration Configuration { get; set; } = ConfigurationRules.Normalize(new());
    public string MatchId { get; set; } = "";
    // MENU-24: accounts the host removed; they cannot take a seat in this room again.
    public List<string> Removed { get; set; } = [];

    // The two players; observers are kept apart (RoomObservers), so this never counts the audience.
    public bool Contains(string account) => account == FreePeoples || account == Shadow;

    public string PlayerName(string account) =>
        account == FreePeoples ? FreePeoplesName : account == Shadow ? ShadowName : "";

    // OBS-01: an observer sees the room and its audience but never the code of a room the host manages; You names a
    // seat only for a player (an observer's default perspective is Free Peoples).
    public RoomView View(string account, ReleaseScope release, IReadOnlyList<RoomObserver> observers, int observerLimit) => new()
    {
        Id = Id,
        Name = Name,
        Host = HostName,
        IsHost = HostAccount == account,
        Managed = Managed,
        Visibility = Visibility,
        Status = Closed ? RoomStatus.Closed : MatchId.Length > 0 ? RoomStatus.Started : RoomStatus.Waiting,
        JoinCode = InvitationEnabled && (HostAccount == account || (!Managed && Contains(account))) ? JoinCode : "",
        FreePeoples = FreePeoplesName,
        Shadow = ShadowName,
        You = account == Shadow ? Side.Shadow : Side.FreePeoples,
        Role = Contains(account) ? RoomRole.Player : RoomRole.Observer,
        Observers = RoomObservers.Views(this, observers),
        ObserverLimit = observerLimit,
        Version = Version,
        FreePeoplesAccepted = FreePeoplesAccepted == Version,
        ShadowAccepted = ShadowAccepted == Version,
        Configuration = Configuration,
        MatchId = MatchId,
        StartUnavailableReason = RoomRules.Unavailable(Configuration, release) ?? "",
        ExpansionsAvailable = release.OffersExpansions,
        OfferedModules = release.Modules.ToList()
    };
}

public sealed record RoomResult(RoomView? Room = null, string Error = "");

public static class RoomRules
{
    public static string? Configure(RoomState room, ConfigureRoomRequest request, ReleaseScope release)
    {
        var error = CanEdit(room, request.ExpectedVersion);
        var configuration = ConfigurationRules.Normalize(request.Configuration);
        if (error is not null)
            return error;
        if (!ConfigurationRules.IsValid(configuration))
            return "InvalidConfiguration";
        // EXP-01: any room, named ones and challenge tables too, may use the released modules.
        if (!release.Offers(configuration))
            return "ExpansionsUnavailable";
        room.Configuration = configuration;
        room.Version++;
        room.FreePeoplesAccepted = room.ShadowAccepted = null;
        return null;
    }

    public static string? Confirm(RoomState room, string account, int version) =>
        SetReady(room, account, version, true);

    public static string? SetReady(RoomState room, string account, int version, bool ready)
    {
        var error = CanEdit(room, version);
        if (error is not null)
            return error;
        if (account == room.FreePeoples)
            room.FreePeoplesAccepted = ready ? room.Version : null;
        else
            room.ShadowAccepted = ready ? room.Version : null;
        return null;
    }

    public static string? CanStart(RoomState room, int version, ReleaseScope release)
    {
        if (room.Version != version)
            return "StaleConfiguration";
        if (room.FreePeoples.Length == 0 || room.Shadow.Length == 0)
            return "WaitingForOpponent";
        if (room.FreePeoplesAccepted != version || room.ShadowAccepted != version)
            return "ConfirmationRequired";
        return Unavailable(room.Configuration, release);
    }

    // A table set up before the release narrowed keeps its settings but cannot start.
    public static string? Unavailable(MatchConfiguration configuration, ReleaseScope release) =>
        !release.Offers(configuration) ? "ExpansionsUnavailable"
        : ConfigurationRules.IsImplemented(configuration) ? null : "ContentNotImplemented";

    // MENU-24: the host frees the guest's seat; the guest cannot come back to this room.
    public static string? Remove(RoomState room, string host)
    {
        var error = CanRemove(room, host);
        if (error is not null)
            return error;
        // OBS-01: a host watching from the audience names the participant to remove.
        if (!room.Contains(host))
            return "ParticipantNotFound";
        var guest = room.FreePeoples == host ? room.Shadow : room.FreePeoples;
        if (guest.Length == 0)
            return "SeatEmpty";
        Vacate(room, guest);
        room.Removed.Add(guest);
        return null;
    }

    // OBS-01: removing a participant works only before the start and only for the host.
    public static string? CanRemove(RoomState room, string host) =>
        room.HostAccount != host ? "HostRequired" : room.MatchId.Length > 0 ? "MatchAlreadyStarted" : null;

    // OBS-01: roles change only in a waiting room; a seat is free or the change is refused.
    public static string? CanTakeSeat(RoomState room, Side side)
    {
        if (room.MatchId.Length > 0)
            return "RolesLocked";
        return Seat(room, side).Length > 0 ? "SeatTaken" : null;
    }

    public static string Seat(RoomState room, Side side) => side == Side.FreePeoples ? room.FreePeoples : room.Shadow;

    // A seat freed by leaving or by the host: both players are not ready any more and the old invitation stops working.
    public static void Vacate(RoomState room, string account)
    {
        ClearSeat(room, account);
        ResetInvitation(room);
    }

    // OBS-01: a player who moves to the audience or to the other seat stays in the room, so the invitation stays;
    // the line-up changed, so both players are not ready any more.
    public static void LeaveSeat(RoomState room, string account)
    {
        ClearSeat(room, account);
        ResetReady(room);
    }

    public static void ResetReady(RoomState room)
    {
        room.FreePeoplesAccepted = room.ShadowAccepted = null;
        room.Version++;
    }

    public static void ClearSeat(RoomState room, string account)
    {
        if (room.FreePeoples == account)
            room.FreePeoples = room.FreePeoplesName = "";
        else if (room.Shadow == account)
            room.Shadow = room.ShadowName = "";
    }

    public static void ResetInvitation(RoomState room)
    {
        ResetReady(room);
        room.JoinCode = SessionStore.NewToken();
        room.InvitationEnabled = false;
    }

    private static string? CanEdit(RoomState room, int version) =>
        room.MatchId.Length > 0 ? "MatchAlreadyStarted" : room.Version != version ? "StaleConfiguration" : null;
}
