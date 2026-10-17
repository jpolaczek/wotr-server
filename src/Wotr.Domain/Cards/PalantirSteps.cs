using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record SarumanTableStep(bool Wormtongue) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var saruman = CharacterRules.InPlay(context.State, CharacterRules.Saruman)!;
        new LastingEffectStep(Wormtongue ? EffectModifier.RestrictRohanActivation : EffectModifier.None,
            EffectDuration.UntilRemoved, EffectExpiry.CharacterLeavesPlay, saruman.Id).Apply(context);
    }
}

public sealed record PalantirDrawStep : EffectChoiceStep
{
    public override string Presentation => "Deck";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        state.Player(execution.Owner).Decks.Where(d => d.Value.Count > 0).Select(d => d.Key).ToList();
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        Title = id == "character" ? "Character" : "Strategy",
        Detail = "Draw one card from this deck."
    };
    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) => preview.HasUnknownResult = true;
    public override void Choose(EffectContext context, string id)
    {
        CardFlow.Draw(context.State, context.Owner, id);
        CardFlow.RequireDiscard(context.State, context.Owner, DiscardContinuation.ResumeEffect);
    }
}
