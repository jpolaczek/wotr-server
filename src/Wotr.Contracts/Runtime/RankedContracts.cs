namespace Wotr.Contracts
{
    // RANK-04 (R35, docs/RANKED_PLAY.md): the time presets of a ranked search.
    public enum RankedPreset { Fast, Standard, Long }

    // A player's own rating before and after a finished ranked game, rounded as players see it; Change is their
    // difference, so the three always agree.
    public sealed class RankedResultView
    {
        public int RatingBefore { get; set; }
        public int RatingAfter { get; set; }
        public int Change { get; set; }
    }

    // A concession of a ranked game. The command id makes a retried concession return its first receipt.
    public sealed class ConcedeRequest
    {
        public string CommandId { get; set; } = "";
    }
}
