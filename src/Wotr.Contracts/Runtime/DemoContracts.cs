namespace Wotr.Contracts
{
    // DEMO-01: the demo match started from a waiting room; the player plays You, a simple CPU the other side.
    public sealed class DemoMatchView
    {
        public string MatchId { get; set; } = "";
        public Side You { get; set; }
    }
}
