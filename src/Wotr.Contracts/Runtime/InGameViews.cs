using System.Collections.Generic;

namespace Wotr.Contracts
{
    public sealed class EffectTargetView
    {
        public string Kind { get; set; } = "";
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
    }

    public sealed class AvailabilityConditionView
    {
        public string Category { get; set; } = "";
        public string Code { get; set; } = "";
        public string Description { get; set; } = "";
        public bool Met { get; set; }
    }

    // These descriptions explain existing commands. They are never execution permission.
    public sealed class ActionAvailabilityView
    {
        public string SubjectId { get; set; } = "";
        public string ActionId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Source { get; set; } = "";
        public string Cost { get; set; } = "";
        public string Timing { get; set; } = "";
        public bool ConditionsMet { get; set; }
        public bool WindowOpen { get; set; }
        public bool CanAct { get; set; }
        public List<string> EligibleDice { get; set; } = new List<string>();
        public List<string> Targets { get; set; } = new List<string>();
        public List<AvailabilityConditionView> Conditions { get; set; } = new List<AvailabilityConditionView>();
    }

    public sealed class VictoryRegionView
    {
        public string RegionId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Settlement { get; set; } = "";
        public int Points { get; set; }
    }

    public sealed class MilitaryVictoryView
    {
        public Side Side { get; set; }
        public int Points { get; set; }
        public int Threshold { get; set; }
        public bool ThresholdReached { get; set; }
        public string CheckedAt { get; set; } = "";
        public string Source { get; set; } = "";
        public List<VictoryRegionView> Regions { get; set; } = new List<VictoryRegionView>();
    }
}
