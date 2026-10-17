using System.Collections.Generic;

namespace Wotr.Contracts
{
    public enum HuntReportKind
    {
        Started, Rolled, Rerolled, TileDrawn, TileReplaced, DamageDetermined,
        DamageChanged, Response, Reaction, CompanionLeft, GuideChanged,
        Corruption, Revealed, Progressed, HuntCompleted, CharacterEliminated, CompanionPlaced
    }

    // Only known public outcomes. No hand, companion versions, pool order or random seed.
    public sealed class HuntOutcomeView
    {
        public string RegionId { get; set; } = "";
        public int Progress { get; set; }
        public int? MordorStep { get; set; }
        public int Corruption { get; set; }
        public bool Hidden { get; set; }
        public string GuideId { get; set; } = "";
    }

    public sealed class HuntReportEntryView
    {
        public long Revision { get; set; }
        public int HuntId { get; set; }
        public HuntReportKind Kind { get; set; }
        public string SourceId { get; set; } = "";
        public string CharacterId { get; set; } = "";
        public string Destination { get; set; } = "";
        public HuntResponse? Response { get; set; }
        public HuntView? Hunt { get; set; }
        public HuntOutcomeView Outcome { get; set; } = new HuntOutcomeView();
    }

    public sealed class HuntReportView
    {
        public string Id { get; set; } = "";
        public long StartedRevision { get; set; }
        public bool Completed { get; set; }
        public HuntOutcomeView Initial { get; set; } = new HuntOutcomeView();
        public HuntOutcomeView Outcome { get; set; } = new HuntOutcomeView();
        public Side? Winner { get; set; }
        public VictoryCondition? Victory { get; set; }
        public List<HuntReportEntryView> Entries { get; set; } = new List<HuntReportEntryView>();
    }
}
