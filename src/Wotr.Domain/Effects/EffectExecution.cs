using Wotr.Contracts;

namespace Wotr.Domain;

public enum EffectWait { None, Choice, Reaction, FreeEvent, Army, NazgulTravel, BalrogTravel }
public enum EffectReturn { Parent, Decision, FinishAction, BeginActions }
public enum ReactionCost { None, DiscardTableCard, TransferNenya }

// Only continuation data is serialized. Definitions and executable steps stay in the catalog.
public sealed class EffectExecution
{
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? InspectedSovereign { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public bool AnarionDrawPending { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public bool FellowshipMove { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public MadnessTarget? MadnessTarget { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Side? InspectedHand { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public RingRecoveryState? RingRecovery { get; set; }
    public Side Owner { get; set; }
    public string SourceId { get; set; } = "";
    public string NationId { get; set; } = "";
    public string RegionId { get; set; } = "";
    public List<string> TargetIds { get; set; } = [];
    public DieFace PaidFace { get; set; }
    public EffectWait Wait { get; set; }
    public ArmyEventOrder? ArmyOrder { get; set; }
    public EffectReturn Return { get; set; }
    public string ReactionWindow { get; set; } = "";
    public bool CompletionWindowOffered { get; set; }
    public bool IsEvent { get; set; }
    public bool PalantirDrawPending { get; set; }
    public int AppliedHits { get; set; }
    public List<string> OfferedReactions { get; set; } = [];
    public List<EffectSelection> Committed { get; set; } = [];
}

// Private progress belongs to its chooser, including inside another player's effect.
public sealed record EffectSelection(Side Owner, string SourceId, string ChoiceId = "", string TargetId = "");

public sealed record EffectContext(MatchState State, ResolutionFrame Frame, IRandomSource Random)
{
    public EffectExecution Execution => Frame.Effect!;
    public Side Owner => Execution.Owner;
    public void Record(string kind, string text, Side? onlyFor = null, IReadOnlyList<int>? rolls = null,
        int? hitTarget = null, int? hits = null, IReadOnlyList<string>? targets = null,
        IReadOnlyList<ActionMovement>? movements = null, IReadOnlyList<ActionCardFact>? cards = null)
    {
        var battle = BattleRuntime.Active(State);
        var activity = HistoryFacts.At(State, battle is null ? ActionContextKind.Cards : ActionContextKind.Battle,
            Execution.SourceId, Execution.RegionId, Execution.NationId);
        activity.Facts.Rolls = rolls is null ? [] : [.. rolls];
        activity.Facts.HitTarget = hitTarget;
        activity.Facts.Hits = hits;
        activity.Facts.Targets = targets is null ? [] : [.. targets];
        activity.Movements = movements is null ? [] : [.. movements];
        activity.Facts.Cards = cards is null ? [] : [.. cards];
        if (battle is not null)
            activity.Facts.Battle = BattleProjection.Public(State, battle);
        State.Events.Add(new()
        {
            Revision = State.Revision, Kind = kind, Text = text, OnlyFor = onlyFor, Activity = activity
        });
    }
}
