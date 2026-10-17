using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsPolitics
{
    public const string ProgramId = "kings.remove-corruption";
    public static EffectDefinition Program { get; } = new(ProgramId, [new RemoveCorruptionStep()]);

    public static void Record(MatchState state, string nation)
    {
        if (!state.Configuration.Kings || state.Kings is null)
            return;
        foreach (var sovereign in state.Kings.Sovereigns.Where(s => SovereignCatalog.All[s.Id].Nation == nation))
            if (Eligible(state, sovereign))
                state.Kings.PendingCorruptionRemoval.Add(sovereign.Id);
    }

    internal static bool Eligible(MatchState state, SovereignState sovereign) =>
        KingsCardRestrictions.CanRemoveCorruption(state) && sovereign.Status == SovereignStatus.Initial && sovereign.Tiles.Any(id => !KingsTiles.IsKings(id)) &&
        SovereignAwakening.AppropriateCompanion(state, sovereign);

    // A queued political change interrupts only between commands or effect steps.
    public static bool BeginPending(MatchState state)
    {
        if (!state.Configuration.Kings || state.Kings is null || state.Resolution.Frames.Any(f => f.EffectId == ProgramId))
            return false;
        while (state.Kings.PendingCorruptionRemoval.Count > 0)
        {
            var id = state.Kings.PendingCorruptionRemoval[0];
            state.Kings.PendingCorruptionRemoval.RemoveAt(0);
            if (SovereignLifecycle.Find(state, id) is not { } sovereign || !Eligible(state, sovereign))
                continue;
            var pending = state.Pending;
            EffectRuntime.BeginProgram(state, Side.FreePeoples, id, ProgramId);
            if (pending is not null)
            {
                var frame = EffectRuntime.Current(state)!;
                frame.ReturnDecision = pending;
                frame.Effect!.Return = EffectReturn.Decision;
            }
            return true;
        }
        return false;
    }

    public static bool BeforeActionCompletion(MatchState state, Side actor, bool fellowshipMove)
    {
        if (!BeginPending(state))
            return false;
        var execution = EffectRuntime.Current(state)!.Effect!;
        execution.Owner = actor;
        execution.Return = EffectReturn.FinishAction;
        execution.FellowshipMove = fellowshipMove;
        execution.CompletionWindowOffered = true;
        return true;
    }
}

public sealed record RemoveCorruptionStep : EffectChoiceStep
{
    public override string Presentation => "CorruptionRemoval";
    public override Side Chooser(MatchState state, EffectExecution execution) => Side.FreePeoples;
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) => ["remove", "decline"];
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, TargetId = execution.SourceId,
        Title = id == "remove" ? "Remove corruption" : "Keep tiles",
        Detail = id == "remove" ? $"Return one randomly chosen {KingsCorruptionRemoval.TileDescription(state)} to the Hunt Pool."
            : "Leave this sovereign's tiles in place."
    };

    public override void Choose(EffectContext context, string id)
    {
        if (id == "decline")
            return;
        KingsCorruptionRemoval.RandomHidden(context, context.Execution.SourceId);
    }

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        preview.HasUnknownResult = ids.Single() == "remove";
        preview.Summary = Describe(state, execution, ids.Single()).Detail;
    }
}
