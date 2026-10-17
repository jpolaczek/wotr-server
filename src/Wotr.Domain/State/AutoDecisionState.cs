using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class AutoDecisionState
{
    public long Version { get; set; }
    public AutoDecisionPolicy? Policy { get; set; }
    public string LastSkippedBattleId { get; set; } = "";
    public int LastSkippedRound { get; set; }
}
