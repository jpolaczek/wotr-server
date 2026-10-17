using Wotr.Contracts;

namespace Wotr.Domain;

// These persistence models are internal server data, never endpoint response DTOs.
public sealed class PendingDecision
{
    public string Id { get; set; } = "";
    public DecisionKind Kind { get; set; }
    public Side Owner { get; set; }
    public List<CommandKind> Commands { get; set; } = [];
    public List<string> PrivateChoices { get; set; } = [];
    public int RequiredCount { get; set; }
    public string Visibility { get; set; } = "owner";
    public string Presentation { get; set; } = "";
    public string SourceId { get; set; } = "";
    public string Window { get; set; } = "";
}

public sealed class ResolutionState
{
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ActionLogContinuation? ActionLog { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ActionSequenceState? ActionSequence { get; set; }
    public List<ResolutionFrame> Frames { get; set; } = [];
    public List<string> PaidDieIds { get; set; } = [];
    public string Window { get; set; } = "";
    public void Suspend(MatchState state, string effectId, int nextStep)
    {
        Frames.Add(new()
        {
            EffectId = effectId,
            NextStep = nextStep,
            ReturnDecision = state.Pending,
            Window = Window
        });
        state.Pending = null;
    }
    public void Resume(MatchState state)
    {
        if (Frames.Count == 0)
            throw new InvalidOperationException("Missing continuation.");
        var frame = Frames[^1];
        Frames.RemoveAt(Frames.Count - 1);
        Window = frame.Window;
        state.Pending = frame.ReturnDecision;
    }
}

public sealed class ResolutionFrame
{
    public string EffectId { get; set; } = "";
    public int NextStep { get; set; }
    public string Window { get; set; } = "";
    public PendingDecision? ReturnDecision { get; set; }
    public EffectExecution? Effect { get; set; }
    public FellowshipProcedure? Fellowship { get; set; }
    public HuntProcedure? Hunt { get; set; }
    public BattleState? Battle { get; set; }
    public BattleLossProcedure? BattleLoss { get; set; }
    public EventLossProcedure? EventLoss { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public CorruptionAttempt? Corruption { get; set; }
}
