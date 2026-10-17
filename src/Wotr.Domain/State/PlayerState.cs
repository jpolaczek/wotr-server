using Wotr.Contracts;

namespace Wotr.Domain;

// These persistence models are internal server data, never endpoint response DTOs.
public sealed class PlayerState
{
    public Side Side { get; set; }
    public List<string> Hand { get; set; } = [];
    public long HandVersion { get; set; }
    public AutoDecisionState AutoDecision { get; set; } = new();
    public Dictionary<string, List<string>> Decks { get; set; } = [];
    public List<string> Discard { get; set; } = [];
    public List<string> Table { get; set; } = [];
    public bool RingUsedThisTurn { get; set; }
    public bool RingActionPending { get; set; }
    // Desired number of living character contributions at the next recovery, not a delta.
    public int ExtraDiceNextRecovery { get; set; }
    public List<string> PrivateCompanionVersions { get; set; } = [];
    public string? CommittedCombatCard { get; set; }
    public bool CombatCardCommitted { get; set; }
    public List<int> HiddenCorruptionTiles { get; set; } = [];
}
