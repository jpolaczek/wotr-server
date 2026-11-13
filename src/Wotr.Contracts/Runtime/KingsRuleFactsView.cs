using System.Collections.Generic;

namespace Wotr.Contracts
{
    public sealed class KingsRuleFactsView
    {
        public bool PalantirUsedThisTurn { get; set; }
        public bool DismayDeclinedThisTurn { get; set; }
        public bool RulerTokenUsedThisTurn { get; set; }
        public bool ElvenRingUsedThisTurn { get; set; }
        public string TokenActionSovereign { get; set; } = "";
        public List<Side> RulerDiscardOrder { get; set; } = new List<Side>();
        public List<string> PendingCorruptionRemoval { get; set; } = new List<string>();
        // Joint composition of the unknown numeric pool/sovereign assignments.
        // It does not say which face belongs to which zone or covered slot.
        public List<HuntTileFaceView> UnassignedHuntTiles { get; set; } = new List<HuntTileFaceView>();
    }
}
