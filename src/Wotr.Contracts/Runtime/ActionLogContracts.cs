using System.Collections.Generic;

namespace Wotr.Contracts
{
    public enum ActionLogFilter { All, You, Opponent }
    public enum ActionLogStatus { Unknown, InProgress, Completed }
    public enum ActionContextKind { General, Army, Characters, Politics, Cards, Fellowship, Hunt, Battle, Dice, Resources }
    public enum ActionCostKind { Die, Ring, CouncilToken, RulerToken, Card, Ability, Free, Figure }
    public enum ActionMovementKind { March, Travel, Attack, Retreat, Advance }

    public sealed class ActionContext
    {
        public ActionContextKind Kind { get; set; }
        public List<string> Regions { get; set; } = new List<string>();
        public string DefinitionId { get; set; } = "";
        public string NationId { get; set; } = "";
        public string BattleId { get; set; } = "";
        public int? BattleRound { get; set; }
        public string HuntChainId { get; set; } = "";
    }

    public sealed class ActionCost
    {
        public ActionCostKind Kind { get; set; }
        public string Id { get; set; } = "";
        public Side? Owner { get; set; }
        public string Source { get; set; } = "";
        public DieFace? Face { get; set; }
        public DieFace? Use { get; set; }
        public string AbilityId { get; set; } = "";
        public int Count { get; set; } = 1;
    }

    public sealed class ActionParticipant
    {
        public string Id { get; set; } = "";
        public Side Side { get; set; }
        public string NationId { get; set; } = "";
        public UnitKind? UnitKind { get; set; }
        public string DefinitionId { get; set; } = "";
        public bool Garrison { get; set; }
        public int? Level { get; set; }
        public int? Leadership { get; set; }
    }

    public sealed class ActionMovement
    {
        public ActionMovementKind Kind { get; set; }
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public List<string> Route { get; set; } = new List<string>();
        public List<ActionParticipant> Participants { get; set; } = new List<ActionParticipant>();
    }

    public sealed class ActionLogStep
    {
        public string Id { get; set; } = "";
        public string ActionId { get; set; } = "";
        public string ParentStepId { get; set; } = "";
        public long Revision { get; set; }
        public int Order { get; set; }
        public int? Turn { get; set; }
        public Side? Actor { get; set; }
        public CommandKind? Command { get; set; }
        public bool Automatic { get; set; }
        public bool Legacy { get; set; }
        public string Kind { get; set; } = "";
        public string Text { get; set; } = "";
        public ActionContext Context { get; set; } = new ActionContext();
        public List<ActionCost> Costs { get; set; } = new List<ActionCost>();
        public List<ActionMovement> Movements { get; set; } = new List<ActionMovement>();
        public ActionFacts Facts { get; set; } = new ActionFacts();
        public ActionLogStatus Status { get; set; }
    }

    public sealed class ActionLogSummary
    {
        public string Id { get; set; } = "";
        public string Version { get; set; } = "";
        public Side? Initiator { get; set; }
        public CommandKind? Command { get; set; }
        public int? Turn { get; set; }
        public bool PartialHistory { get; set; }
        public bool Legacy { get; set; }
        public ActionContext Context { get; set; } = new ActionContext();
        public List<ActionCost> Costs { get; set; } = new List<ActionCost>();
        public ActionLogStatus Status { get; set; }
        public int StepCount { get; set; }
    }

    public sealed class ActionLogPage
    {
        public string MatchId { get; set; } = "";
        public List<ActionLogStep> Steps { get; set; } = new List<ActionLogStep>();
        public List<ActionLogSummary> Actions { get; set; } = new List<ActionLogSummary>();
        public string NextCursor { get; set; } = "";
        public string LatestCursor { get; set; } = "";
    }

    public sealed class ActionLogView
    {
        public string MatchId { get; set; } = "";
        public ActionLogSummary Action { get; set; } = new ActionLogSummary();
        public List<ActionLogStep> Steps { get; set; } = new List<ActionLogStep>();
    }
}
