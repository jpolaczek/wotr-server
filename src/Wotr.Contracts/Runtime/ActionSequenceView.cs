using System.Collections.Generic;

namespace Wotr.Contracts
{
    public sealed class ActionSequenceView
    {
        public CommandKind Kind { get; set; }
        public Side Owner { get; set; }
        public string DieId { get; set; } = "";
        public DieFace Face { get; set; }
        public string AbilityId { get; set; } = "";
        public int CompletedSteps { get; set; }
        public int Remaining { get; set; }
        public List<string> UsedIds { get; set; } = new List<string>();
        public List<string> UsedRegions { get; set; } = new List<string>();
    }
}
