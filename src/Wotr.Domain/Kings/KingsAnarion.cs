using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsAnarion
{
    public const string ProgramId = "kings.palantir-of-anarion";
    public static EffectDefinition Program { get; } = new(ProgramId, [new AnarionDrawStep()]);

    private static bool Present(MatchState state) =>
        KingsRulerAbilities.Awakened(state, SovereignCatalog.Denethor)?.Location == "minas-tirith";

    public static bool CanUse(MatchState state, Side actor, string dieId, DieFace cost) => actor == Side.FreePeoples &&
        cost == DieFace.Event && Present(state) && KingsRulerAbilities.CanPay(state, dieId, DieFace.Event);

    public static bool Begin(MatchState state, bool finishAction = false)
    {
        if (!Present(state) || !state.Player(Side.FreePeoples).Decks.Values.Any(d => d.Count > 0))
            return false;
        EffectRuntime.BeginProgram(state, Side.FreePeoples, SovereignCatalog.Denethor, ProgramId);
        if (finishAction)
        {
            var execution = EffectRuntime.Current(state)!.Effect!;
            execution.Return = EffectReturn.FinishAction;
            execution.CompletionWindowOffered = true;
        }
        return true;
    }
}

public sealed record AnarionDrawStep : EffectChoiceStep
{
    public override string Presentation => "Deck";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        state.Player(execution.Owner).Decks.Where(d => d.Value.Count > 0).Select(d => d.Key)
            .Concat(state.Kings?.TokenActionSovereign == SovereignCatalog.Denethor ? [] : new[] { "decline" }).ToArray();
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, Title = id == "decline" ? "Skip extra card" : id == "character" ? "Character" : "Strategy",
        Detail = id == "decline" ? "Complete the action without an extra draw." : "Use the Palantír of Anárion to draw from this deck."
    };
    public override void Choose(EffectContext context, string id)
    {
        if (id != "decline")
            CardFlow.Draw(context.State, context.Owner, id);
        CardFlow.RequireDiscard(context.State, context.Owner, DiscardContinuation.ResumeEffect);
    }
    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.HasUnknownResult = ids.Single() != "decline";
}
