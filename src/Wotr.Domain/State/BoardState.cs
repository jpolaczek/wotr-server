using Wotr.Contracts;

namespace Wotr.Domain;

// These persistence models are internal server data, never endpoint response DTOs.
public sealed class UnitState
{
    public string Id { get; set; } = "";
    public string Nation { get; set; } = "";
    public UnitKind Kind { get; set; }
    public string Location { get; set; } = "reserve";
    public bool Garrison { get; set; }
}

public sealed class NationState
{
    public string Id { get; set; } = "";
    public bool Active { get; set; }
    public int StepsToWar { get; set; }
}

public sealed class DieState
{
    public string Id { get; set; } = "";
    public Side Owner { get; set; }
    public string Source { get; set; } = "standard";
    public DieFace Face { get; set; }
    public DieLocation Location { get; set; } = DieLocation.Unrolled;
    public string HuntOrigin { get; set; } = "";
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Side? SpentWith { get; set; }
    public bool RemoveAtRecovery { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public SpecialDieState? Special { get; set; }
}

public sealed class RingState
{
    public string Id { get; set; } = "";
    public Side? Owner { get; set; }
}

public sealed class TileState
{
    public string Id { get; set; } = "";
    public string Location { get; set; } = "pool";
}
