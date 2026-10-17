using System.Collections.Generic;

namespace Wotr.Contracts
{
    public sealed class EventLossView
    {
        public Side Owner { get; set; }
        public string RegionId { get; set; } = "";
        public bool Garrison { get; set; }
        public string SourceId { get; set; } = "";
        public int Hits { get; set; }
        public List<int> Rolls { get; set; } = new List<int>();
        public List<string> ParticipantIds { get; set; } = new List<string>();
    }

    public sealed class EffectChoiceView
    {
        public string Id { get; set; } = "";
        public string TargetId { get; set; } = "";
        public string SourceId { get; set; } = "";
        public string Cost { get; set; } = "";
        public string Title { get; set; } = "";
        public string Detail { get; set; } = "";
        public string SelectionGroup { get; set; } = "";
        public int GroupMinimum { get; set; } = 1;
        public int GroupMaximum { get; set; } = 1;
        public List<string> RequiresAny { get; set; } = new List<string>();
        public bool Exclusive { get; set; }
    }

    public sealed class EffectTrailView
    {
        public string SourceId { get; set; } = "";
        public string ChoiceId { get; set; } = "";
        public string TargetId { get; set; } = "";
    }

    public sealed class ActiveEffectView
    {
        public string Id { get; set; } = "";
        public string SourceId { get; set; } = "";
        public Side Owner { get; set; }
        public string TargetId { get; set; } = "";
        public string Modifier { get; set; } = "";
        public string Duration { get; set; } = "";
        public string ExpiresWhen { get; set; } = "";
        public string SourceKind { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string SourceReference { get; set; } = "";
        public string ExpiryDescription { get; set; } = "";
        public List<EffectTargetView> Targets { get; set; } = new List<EffectTargetView>();
    }

    public sealed class EventPlayOption
    {
        public string CardId { get; set; } = "";
        public List<EventCostOption> Costs { get; set; } = new List<EventCostOption>();
        public List<string> NationTargets { get; set; } = new List<string>();
    }

    public sealed class EventCostOption
    {
        public string DieId { get; set; } = "";
        public string AbilityId { get; set; } = "";
        public DieFace Face { get; set; }
    }
}
