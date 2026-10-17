using System.Collections.Generic;

namespace Wotr.Contracts
{
    public enum BattleReportStatus { Unknown, InProgress, AwaitingSettlement, Completed }

    public sealed class BattleReportView
    {
        public string MatchId { get; set; } = "";
        public string Id { get; set; } = "";
        public string Version { get; set; } = "";
        public string ActionId { get; set; } = "";
        public Side? Initiator { get; set; }
        public int? Turn { get; set; }
        public bool PartialHistory { get; set; }
        public bool Legacy { get; set; }
        public BattleReportStatus Status { get; set; }
        public List<ActionCost> Costs { get; set; } = new List<ActionCost>();
        public BattleView? Initial { get; set; }
        public BattleView? Latest { get; set; }
        public BattlePlaceFact? Place { get; set; }
        public ActionOutcome? Outcome { get; set; }
        public List<BattleReportRound> Rounds { get; set; } = new List<BattleReportRound>();
        public List<ActionLogStep> Steps { get; set; } = new List<ActionLogStep>();
    }

    public sealed class BattleReportRound
    {
        public int Number { get; set; }
        public bool Completed { get; set; }
        public BattleView? Initial { get; set; }
        public BattleView? Result { get; set; }
        public List<string> StepIds { get; set; } = new List<string>();
    }

    public sealed class BattlePlaceFact
    {
        public string RegionId { get; set; } = "";
        public string NationId { get; set; } = "";
        public string Settlement { get; set; } = "";
        public Side? Controller { get; set; }
        public List<UnitView> Units { get; set; } = new List<UnitView>();
        public List<CharacterView> Characters { get; set; } = new List<CharacterView>();
    }
}
