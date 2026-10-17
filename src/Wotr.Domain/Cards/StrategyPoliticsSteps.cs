using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record NationAtWarCondition(string Nation) : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => state.Nations.Single(n => n.Id == Nation).StepsToWar == 0;
}

public sealed record ActiveNationCondition(string Nation) : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => state.Nations.Single(n => n.Id == Nation).Active;
}

public sealed record AdvanceFixedNationStep(string Nation) : EffectStep
{
    public override void Apply(EffectContext context) => PoliticalRules.Advance(context.State, Nation, PoliticalAdvanceCause.Event);
}
