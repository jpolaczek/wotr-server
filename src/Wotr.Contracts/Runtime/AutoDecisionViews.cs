namespace Wotr.Contracts
{
    public enum AutoDecisionKind { SkipCombatCards }

    public sealed class AutoDecisionChange
    {
        public string BattleId { get; set; } = "";
        public long ExpectedVersion { get; set; }
        public long HandVersion { get; set; }
        public bool Enabled { get; set; }
    }

    public sealed class AutoDecisionPolicy
    {
        public AutoDecisionKind Kind { get; set; }
        public Side Owner { get; set; }
        public string BattleId { get; set; } = "";
        public long HandVersion { get; set; }
    }

    // Always the viewer's private preference, never part of PlayerSummary.
    public sealed class AutoDecisionView
    {
        public long Version { get; set; }
        public long HandVersion { get; set; }
        public AutoDecisionPolicy? Policy { get; set; }
        public string LastSkippedBattleId { get; set; } = "";
        public int LastSkippedRound { get; set; }
    }
}
