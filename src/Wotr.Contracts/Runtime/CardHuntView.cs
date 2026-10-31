using System.Collections.Generic;

namespace Wotr.Contracts
{
    // Public obligations of a paid Orc Patrol, Isildur's Bane or Foul Thing event.
    // The card's definition determines its rules; this is not an effect cursor.
    public sealed class CardHuntView : HuntProgressView
    {
        public string CardId { get; set; } = "";
        public DieFace CardCost { get; set; }
        public bool PalantirDrawPending { get; set; }
        public CardHuntStrongholdView? StrongholdHunts { get; set; }
    }

    public sealed class CardHuntStrongholdView
    {
        public HuntView ResolvedCardHunt { get; set; } = new HuntView();
        public List<string> Route { get; set; } = new List<string>();
        public List<string> RemainingStrongholds { get; set; } = new List<string>();
    }
}
