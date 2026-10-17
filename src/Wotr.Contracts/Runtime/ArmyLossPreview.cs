using System.Collections.Generic;

namespace Wotr.Contracts
{
    // A legal, uncommitted casualty plan. Never inserted into the activity log.
    public sealed class ArmyLossPreview
    {
        public int RequiredHits { get; set; }
        public int AppliedHits { get; set; }
        public bool Voluntary { get; set; }
        public List<ActionLossFact> Operations { get; set; } = new List<ActionLossFact>();
    }
}
