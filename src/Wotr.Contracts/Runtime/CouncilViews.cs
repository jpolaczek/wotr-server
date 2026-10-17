namespace Wotr.Contracts
{
    public sealed class CompanionVersionOption
    {
        public string FigureId { get; set; } = "";
        public string BaseId { get; set; } = "";
        public string AlternateId { get; set; } = "";
        public bool Required { get; set; }
    }

    public sealed class CouncilTokenView
    {
        public string Id { get; set; } = "";
        public bool Spent { get; set; }
        public bool UsedTokenThisTurn { get; set; }
    }
}
