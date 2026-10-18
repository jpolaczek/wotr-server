using System;
using System.Collections.Generic;

namespace Wotr.Contracts
{
    // LOBBY-01: the Open games screen's own chat, the players on that screen now, and challenges between them. The
    // menu reads all three with one poll (GET /api/lobby), which also marks its player as online.
    public sealed class LobbyMessage
    {
        public long Sequence { get; set; }
        public string Author { get; set; } = "";
        public string Text { get; set; } = "";
        public DateTimeOffset At { get; set; }
    }

    public sealed class LobbyMessageRequest
    {
        public string Text { get; set; } = "";
    }

    public sealed class OnlinePlayer
    {
        public string Name { get; set; } = "";
        public bool You { get; set; }
    }

    public enum ChallengeStatus
    {
        Pending,
        Accepted,
        Declined,
        Cancelled,
        Expired
    }

    // A challenge from one player to another; accepting it makes a private room with the challenger as its host and the
    // challenged player in the other seat (RoomId).
    public sealed class ChallengeView
    {
        public string Id { get; set; } = "";
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public ChallengeStatus Status { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public string RoomId { get; set; } = "";
    }

    public sealed class ChallengeRequest
    {
        public string Name { get; set; } = "";
    }

    public sealed class LobbyView
    {
        // Messages after the poll's `after` sequence (the latest ones when it is 0), oldest first.
        public List<LobbyMessage> Messages { get; set; } = new List<LobbyMessage>();
        public long Latest { get; set; }
        public List<OnlinePlayer> Online { get; set; } = new List<OnlinePlayer>();
        // Pending challenges to this player.
        public List<ChallengeView> Incoming { get; set; } = new List<ChallengeView>();
        // This player's recent challenges with their outcome, so an accepted one opens its room.
        public List<ChallengeView> Outgoing { get; set; } = new List<ChallengeView>();
    }

    public static class LobbyLimits
    {
        public const int LongestMessage = 200;
        public const int MessagesKept = 200;
        public const int OnlineSeconds = 20;
        public const int ChallengeSeconds = 120;
        public const int PollSeconds = 3;
    }
}
