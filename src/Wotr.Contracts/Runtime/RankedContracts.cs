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
