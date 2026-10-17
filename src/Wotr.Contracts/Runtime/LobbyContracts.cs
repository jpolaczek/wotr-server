using System;
using System.Collections.Generic;

namespace Wotr.Contracts
{
    // AUTH-02: Username takes the player's name or the account's email address.
    public sealed class LoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    // AUTH-02 (D16): accounts are created on the game's web page with an email address; account invitations are gone.
    public sealed class RegisterRequest
    {
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public sealed class SessionView
    {
        public string Token { get; set; } = "";
        public string AccountId { get; set; } = "";
        public string Username { get; set; } = "";
        public DateTimeOffset ExpiresAt { get; set; }
    }

    public enum RoomVisibility { Private, Public }
    public enum RoomStatus { Waiting, Started, Closed }
    // OBS-01: a participant is one of the two players or one of up to sixteen observers; an observer is not a side.
    public enum RoomRole { Player, Observer }

    public sealed class CreateRoomRequest
    {
        public string Name { get; set; } = "";
        public RoomVisibility Visibility { get; set; }
        public Side Side { get; set; }
    }

    public sealed class JoinRoomRequest
    {
        public string Code { get; set; } = "";
        // OBS-01: Observer joins the audience (also after the start); Player takes the free seat as before.
        public RoomRole Role { get; set; }
    }

    // OBS-01: before the start a participant moves between the audience and a free seat; Side names the seat.
    public sealed class RoomRoleRequest
    {
        public RoomRole Role { get; set; }
        public Side Side { get; set; }
    }

    // OBS-01: the host removes one participant by name, a player or an observer; an empty name means the other player.
    public sealed class RemoveParticipantRequest
    {
        public string Name { get; set; } = "";
    }

    public sealed class RoomObserverView
    {
        public string Name { get; set; } = "";
        public bool IsHost { get; set; }
    }

    public sealed class RoomRevisionRequest
    {
        public int ExpectedVersion { get; set; }
    }

    public sealed class ConfigureRoomRequest
    {
        public int ExpectedVersion { get; set; }
        public MatchConfiguration Configuration { get; set; } = new MatchConfiguration();
    }

    public sealed class RoomReadyRequest
    {
        public int ExpectedVersion { get; set; }
        public bool Ready { get; set; }
    }

    public sealed class PublicRoomSummary
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Host { get; set; } = "";
        public Side OpenSide { get; set; }
        // OBS-01: a started room is listed for observing; CanPlay says a seat is free, CanObserve that the audience is not full.
        public RoomStatus Status { get; set; }
        public int Players { get; set; }
        public int Observers { get; set; }
        public bool CanPlay { get; set; }
        public bool CanObserve { get; set; }
    }

    public sealed class PublicRoomListView
    {
        public List<PublicRoomSummary> Rooms { get; set; } = new List<PublicRoomSummary>();
        public string NextCursor { get; set; } = "";
    }

    public sealed class RoomView
    {
        public string Name { get; set; } = "";
        public string Host { get; set; } = "";
        public bool IsHost { get; set; }
        public bool Managed { get; set; }
        public RoomVisibility Visibility { get; set; }
        public RoomStatus Status { get; set; }

        public string Id { get; set; } = "";
        public string JoinCode { get; set; } = "";
        public string FreePeoples { get; set; } = "";
        public string Shadow { get; set; } = "";
        public Side You { get; set; }
        public int Version { get; set; }
        public bool FreePeoplesAccepted { get; set; }
        public bool ShadowAccepted { get; set; }
        public MatchConfiguration Configuration { get; set; } = new MatchConfiguration();
        public string MatchId { get; set; } = "";
        public string StartUnavailableReason { get; set; } = "";
        // UI-96: false when this server offers only the base game; the client then hides expansion choices.
        public bool ExpansionsAvailable { get; set; }
        // OBS-01: the viewer's role (You names a seat only for a player) and the room's audience.
        public RoomRole Role { get; set; }
        public List<RoomObserverView> Observers { get; set; } = new List<RoomObserverView>();
        public int ObserverLimit { get; set; }
    }

    public static class RoomInput
    {
        public static bool ValidName(string value)
        {
            if (value == null || value.Trim().Length == 0 || value.Trim().Length > 64)
                return false;
            foreach (var character in value)
                if (char.IsControl(character))
                    return false;
            return true;
        }
    }

    public sealed class ApiError
    {
        public string Error { get; set; } = "";
    }
}
