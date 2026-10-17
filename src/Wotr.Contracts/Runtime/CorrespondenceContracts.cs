using System.Collections.Generic;

namespace Wotr.Contracts
{
    public enum MatchListFilter { All, YourDecision, Waiting, Finished }
    public enum MatchStatus { YourDecision, Waiting, Finished }

    public sealed class MatchSummary
    {
        public string Name { get; set; } = "";
        public string MatchId { get; set; } = "";
        public string Opponent { get; set; } = "";
        public Side You { get; set; }
        public MatchConfiguration Configuration { get; set; } = new MatchConfiguration();
        public int Turn { get; set; }
        public GamePhase Phase { get; set; }
        public long Revision { get; set; }
        public long LastSeenRevision { get; set; } = -1;
        public bool HasUnseenChanges { get; set; }
        public MatchStatus Status { get; set; }
        public Side? DecisionOwner { get; set; }
        public DecisionKind? YourDecision { get; set; }
        public Side? Winner { get; set; }
        public VictoryCondition? Victory { get; set; }
    }

    public sealed class MatchListView
    {
        public List<MatchSummary> Matches { get; set; } = new List<MatchSummary>();
        public string NextCursor { get; set; } = "";
    }

    public sealed class MatchActivityView
    {
        public string MatchId { get; set; } = "";
        public long Revision { get; set; }
        public long LastSeenRevision { get; set; } = -1;
        public long AfterRevision { get; set; } = -1;
        public bool HasEarlierEntries { get; set; }
        public List<HistoryEntry> Entries { get; set; } = new List<HistoryEntry>();
    }

    public sealed class SeenRevisionRequest
    {
        public long? Revision { get; set; }
    }

    public sealed class SeenRevisionView
    {
        public long LastSeenRevision { get; set; }
    }
}
