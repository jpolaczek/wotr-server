using Wotr.Contracts;

namespace Wotr.Domain;

// These persistence models are internal server data, never endpoint response DTOs.
public sealed class GameEvent
{
    public long Revision { get; set; }
    public string Kind { get; set; } = "";
    public Side? OnlyFor { get; set; }
    public string Text { get; set; } = "";
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? HistoryId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ActionEvent? Activity { get; set; }
}

public sealed class RandomSample
{
    public long Revision { get; set; }
    public int ExclusiveMaximum { get; set; }
    public int Value { get; set; }
}
