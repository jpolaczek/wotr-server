namespace Wotr.Contracts
{
    // The visible Lure elimination has returned a Companion to the map and is
    // waiting for placement or the resulting Guide choice. The target is known
    // to Free Peoples from its own choice; it is never sent to Shadow.
    public sealed class LurePlacementView
    {
        public string CompanionId { get; set; } = "";
        public string Stage { get; set; } = "";
        public DieFace CardCost { get; set; }
    }
}
