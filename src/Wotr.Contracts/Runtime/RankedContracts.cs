using System.Collections.Generic;

namespace Wotr.Contracts
{
    // RANK-04 (R35, docs/RANKED_PLAY.md): the time presets of a ranked search.
    public enum RankedPreset { Fast, Standard, Long }

    // A player's own rating before and after a finished ranked game, rounded as players see it; Change is their
    // difference, so the three always agree.
    public sealed class RankedResultView
    {
        public int RatingBefore { get; set; }
        public int RatingAfter { get; set; }
        public int Change { get; set; }
    }

    // A concession of a ranked game. The command id makes a retried concession return its first receipt.
    public sealed class ConcedeRequest
    {
        public string CommandId { get; set; } = "";
    }

    // RANK-05 (D1-D4): Off means it must not be in the game, Allow that it may, Enforce that it must. For a faction,
    // Allow is a preference and Enforce a requirement.
    public enum RankedChoice { Off, Allow, Enforce }

    // A ranked search: each released expansion and each time preset, and a faction or none. The defaults are the
    // window's (D1, D3, D4): every expansion allowed, no faction, Standard allowed and the other presets off.
    public sealed class RankedSearchRequest
    {
        public RankedChoice Lords { get; set; } = RankedChoice.Allow;
        public RankedChoice Kings { get; set; } = RankedChoice.Allow;
        public RankedChoice Fate { get; set; } = RankedChoice.Allow;
        public Side? Faction { get; set; }
        public RankedChoice FactionChoice { get; set; } = RankedChoice.Allow;
        public RankedChoice Fast { get; set; } = RankedChoice.Off;
        public RankedChoice Standard { get; set; } = RankedChoice.Allow;
        public RankedChoice Long { get; set; } = RankedChoice.Off;
    }

    // Idle: no search. Searching: a live search. Stopped: the search lapsed because the client went quiet; said once.
    // Matched: the account plays a ranked match, which the client opens.
    public enum RankedSearchStatus { Idle, Searching, Stopped, Matched }

    // RANK-06 (D9): a big decision is a player's own move (a die, a phase, a card's whole event); a small one answers
    // within someone's move.
    public enum MoveClockKind { Big, Small }

    // The running clock of a ranked match, as the client shows it: the time left before the shown limit (the server
    // allows 3 s more), the limit, and when the warning sounds. Null in every other match.
    public sealed class MoveClockView
    {
        public Side Owner { get; set; }
        public MoveClockKind Kind { get; set; }
        public string DecisionId { get; set; } = "";
        public long RemainingMilliseconds { get; set; }
        public long LimitMilliseconds { get; set; }
        public long WarningMilliseconds { get; set; }
        // RANK-07: a paused clock stands still; each player has three pauses and either may unpause.
        public bool Paused { get; set; }
        public Side? PausedBy { get; set; }
        public int PausesLeft { get; set; }
        public int OpponentPausesLeft { get; set; }
    }

    // RANK-08 (D13): one row of the leaderboard. No account id leaves the server; a client finds its own row by rank.
    public sealed class LeaderboardRow
    {
        public int Rank { get; set; }
        public string Name { get; set; } = "";
        public int Rating { get; set; }
        public int Games { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
    }

    // The top of the leaderboard, and the player's own row wherever it stands, or null before their first ranked game.
    public sealed class LeaderboardView
    {
        public List<LeaderboardRow> Rows { get; set; } = new List<LeaderboardRow>();
        public LeaderboardRow? You { get; set; }
    }

    // The answer to every queue request. The opponent's name and rounded rating are all a search reveals of them.
    public sealed class RankedSearchView
    {
        public RankedSearchStatus Status { get; set; }
        public int ElapsedSeconds { get; set; }
        public int Rating { get; set; }
        public string MatchId { get; set; } = "";
        public string Opponent { get; set; } = "";
        public int OpponentRating { get; set; }
    }
}
