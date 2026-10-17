using Wotr.Contracts;

namespace Wotr.Domain;

// Persisted facts have the visibility of their GameEvent. Never store a command payload here.
public sealed class ActionEvent
{
    public ActionIdentity Action { get; set; } = new();
    public string ParentStepId { get; set; } = "";
    public int Turn { get; set; }
    public Side? Actor { get; set; }
    public CommandKind? Command { get; set; }
    public bool Automatic { get; set; }
    public ActionLogStatus Status { get; set; }
    public ActionContext Context { get; set; } = new();
    public List<ActionCost> Costs { get; set; } = [];
    public List<ActionMovement> Movements { get; set; } = [];
    public ActionFacts Facts { get; set; } = new();
}

public sealed class ActionIdentity
{
    public string Id { get; set; } = "";
    public Side? Initiator { get; set; }
    public CommandKind? Command { get; set; }
    public int Turn { get; set; }
    public bool PartialHistory { get; set; }
    public ActionContext Context { get; set; } = new();
    public List<ActionCost> Costs { get; set; } = [];
}

public sealed class ActionLogContinuation
{
    public ActionIdentity Action { get; set; } = new();
    public string LastStepId { get; set; } = "";
}
