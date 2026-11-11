namespace Wotr.Contracts
{
    // A reviewed card effect is waiting for the Free Peoples' companion journey
    // or its follow-up Guide choice. This contains only public journey rules.
    public sealed class CompanionJourneyView
    {
        public string CardId { get; set; } = "";
        public string Stage { get; set; } = "";
        public bool AllowSeparation { get; set; }
        public bool AllowNoMovement { get; set; }
        public int MaximumGroups { get; set; }
        public int? FixedLevel { get; set; }
        public int Bonus { get; set; }
        public bool AllowBesiegedDestination { get; set; }
    }
}
