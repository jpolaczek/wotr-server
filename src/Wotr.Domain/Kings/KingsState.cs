using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class KingsState
{
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? PalantirUsedTurn { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? DismayDeclinedTurn { get; set; }
    public List<SovereignState> Sovereigns { get; set; } = [];
    public List<RulerTokenState> Tokens { get; set; } = [];
    public List<string> PendingCorruptionRemoval { get; set; } = [];
    public List<Side> RulerDiscardOrder { get; set; } = [];
    public bool RulerTokenUsedThisTurn { get; set; }
    public bool ElvenRingUsedThisTurn { get; set; }
    public string TokenActionSovereign { get; set; } = "";
}

public sealed class SovereignState
{
    public string Id { get; set; } = "";
    public SovereignStatus Status { get; set; }
    public string CorruptedRegion { get; set; } = "";
    public int LastAttemptTurn { get; set; } = -1;
    public List<string> Tiles { get; set; } = [];
}

public sealed class RulerTokenState
{
    public string SovereignId { get; set; } = "";
    public string Location { get; set; } = "free-peoples";
}

public enum CorruptionStage { Drawing, Choosing, SequentialChoice }

public sealed class CorruptionAttempt
{
    public string SovereignId { get; set; } = "";
    public string DieId { get; set; } = "";
    public int DrawLimit { get; set; }
    public CorruptionStage Stage { get; set; }
    public List<string> Drawn { get; set; } = [];
    public List<string> Ignored { get; set; } = [];
    public EffectReturn Return { get; set; }
}
