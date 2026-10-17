using Wotr.Contracts;

namespace Wotr.Domain;

// Authoritative continuation of a paid action; not a client draft or an endpoint DTO.
public sealed class ActionSequenceState
{
    public CommandKind Kind { get; set; }
    public Side Owner { get; set; }
    public string DieId { get; set; } = "";
    public DieFace Face { get; set; }
    public string AbilityId { get; set; } = "";
    public int CompletedSteps { get; set; }
    public int Remaining { get; set; }
    public List<string> UsedIds { get; set; } = [];
    public List<string> UsedRegions { get; set; } = [];
    public List<string> InitialFigures { get; set; } = [];
}
