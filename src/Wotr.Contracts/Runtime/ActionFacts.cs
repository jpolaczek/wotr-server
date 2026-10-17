using System.Collections.Generic;

namespace Wotr.Contracts
{
    public enum ActionCardUse { Draw, Discard, Event, CombatCommit, CombatReveal, TableEntered, TableRemoved }

    // Facts describe the committed step, never the current board or a client's draft.
    public sealed class ActionFacts
    {
        public string SourceId { get; set; } = "";
        public List<string> Targets { get; set; } = new List<string>();
        public List<ActionFigureChange> Figures { get; set; } = new List<ActionFigureChange>();
        public List<ActionNationChange> Nations { get; set; } = new List<ActionNationChange>();
        public List<ActionControlChange> Control { get; set; } = new List<ActionControlChange>();
        public List<ActionDieChange> Dice { get; set; } = new List<ActionDieChange>();
        public List<ActionCardFact> Cards { get; set; } = new List<ActionCardFact>();
        public List<ActionResourceChange> Resources { get; set; } = new List<ActionResourceChange>();
        public List<BattleLossChoice> Losses { get; set; } = new List<BattleLossChoice>();
        public List<ActionLossFact> LossDetails { get; set; } = new List<ActionLossFact>();
        public List<SovereignView> Sovereigns { get; set; } = new List<SovereignView>();
        public HuntOutcomeView? FellowshipBefore { get; set; }
        public HuntOutcomeView? FellowshipAfter { get; set; }
        public List<string> FellowshipRoute { get; set; } = new List<string>();
        public BattleView? Battle { get; set; }
        public BattlePlaceFact? BattlePlace { get; set; }
        public ActionHuntFact? Hunt { get; set; }
        public ActionBudget? Budget { get; set; }
        public GamePhase? Phase { get; set; }
        public ActionOutcome? Outcome { get; set; }
        public List<int> Rolls { get; set; } = new List<int>();
        public int? HitTarget { get; set; }
        public int? Hits { get; set; }
    }

    public sealed class ActionFigureChange
    {
        public ActionParticipant? Before { get; set; }
        public ActionParticipant? After { get; set; }
        public string From { get; set; } = "";
        public string To { get; set; } = "";
    }

    public sealed class ActionNationChange
    {
        public string Id { get; set; } = "";
        public bool? WasActive { get; set; }
        public int? PreviousStepsToWar { get; set; }
        public bool Active { get; set; }
        public int StepsToWar { get; set; }
    }

    public sealed class ActionControlChange
    {
        public string RegionId { get; set; } = "";
        public Side? Before { get; set; }
        public Side? After { get; set; }
    }

    public sealed class ActionDieChange
    {
        public string Id { get; set; } = "";
        public Side Owner { get; set; }
        public string Source { get; set; } = "";
        public DieFace? Before { get; set; }
        public DieFace? After { get; set; }
        public DieLocation? From { get; set; }
        public DieLocation To { get; set; }
        public string HuntOrigin { get; set; } = "";
    }

    public sealed class ActionCardFact
    {
        public Side Owner { get; set; }
        public ActionCardUse Use { get; set; }
        public string Id { get; set; } = "";
        public string Deck { get; set; } = "";
        public int Count { get; set; } = 1;
    }

    public sealed class ActionResourceChange
    {
        public ActionCostKind Kind { get; set; }
        public string Id { get; set; } = "";
        public string From { get; set; } = "";
        public string To { get; set; } = "";
    }

    public sealed class ActionHuntFact
    {
        public string ChainId { get; set; } = "";
        public HuntReportEntryView Entry { get; set; } = new HuntReportEntryView();
        public bool Completed { get; set; }
    }

    public sealed class ActionBudget
    {
        public int Completed { get; set; }
        public int Remaining { get; set; }
    }

    public sealed class ActionLossFact
    {
        public ActionParticipant Figure { get; set; } = new ActionParticipant();
        public BattleLossKind Operation { get; set; }
        public string RegionId { get; set; } = "";
        public int Hits { get; set; }
        public string ReplacementId { get; set; } = "";
        public string ReplacementFrom { get; set; } = "";
    }

    public sealed class ActionOutcome
    {
        public int FreePeoplesPoints { get; set; }
        public int ShadowPoints { get; set; }
        public Side? Winner { get; set; }
        public VictoryCondition? Victory { get; set; }
    }
}
