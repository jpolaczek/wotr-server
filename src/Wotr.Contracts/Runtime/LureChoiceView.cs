namespace Wotr.Contracts
{
    // A paid Lure event is waiting for its public Free Peoples choice.
    // The randomly selected Companion remains only in the chooser's legal options.
    public sealed class LureChoiceView
    {
        public string CardId { get; set; } = "";
        public DieFace CardCost { get; set; }
    }
}
