using System.Collections.Generic;

namespace Wotr.Contracts
{
    public sealed class ArmyOption
    {
        public string RegionId { get; set; } = "";
        public bool Garrison { get; set; }
        public List<string> UnitIds { get; set; } = new List<string>();
        public List<string> CharacterIds { get; set; } = new List<string>();
        public List<string> CommanderIds { get; set; } = new List<string>();
        public List<string> RequiredAnyIds { get; set; } = new List<string>();
        public List<ArmyTargetOption> Targets { get; set; } = new List<ArmyTargetOption>();
    }
    public sealed class ArmyTargetOption
    {
        public string RegionId { get; set; } = "";
        public BattleMode? BattleMode { get; set; }
        public List<string> EligibleIds { get; set; } = new List<string>();
        public List<string> Route { get; set; } = new List<string>();
    }
    public sealed class ArmyEventOption
    {
        public bool RouteMovement { get; set; }
        public bool SeparateRoutes { get; set; }
        public int MaximumSteps { get; set; } = 1;
        public bool RequiresCompanion { get; set; }
        public bool AllowLeaderLoss { get; set; }
    }
    public sealed class MovementCostOption
    {
        public string DieId { get; set; } = "";
        public string AbilityId { get; set; } = "";
        public DieFace Face { get; set; }
        public int MaximumMoves { get; set; }
        public bool RequiresCommander { get; set; }
        public string RequiredCharacterId { get; set; } = "";
    }
    public sealed class ArmyExcess
    {
        public string RegionId { get; set; } = "";
        public bool Garrison { get; set; }
        public int Count { get; set; }
    }
}
