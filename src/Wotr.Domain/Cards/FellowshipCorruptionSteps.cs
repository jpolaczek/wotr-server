using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record FellowshipRevealedCondition : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => !state.Fellowship.Hidden;
}

public static class DirectCorruption
{
    public static void Add(EffectContext context, int amount)
    {
        context.State.Fellowship.Corruption += amount;
        context.Record("DirectCorruption", $"{context.Execution.SourceId}: {amount} Corruption; total {context.State.Fellowship.Corruption}.");
        RingVictory.CheckCorruption(context.State);
    }
}

public sealed record CandlesCorruptionStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var target = CharacterRules.GuideDefinition(context.State) == CharacterRules.Gollum ? 6 : 4;
        var dice = Enumerable.Range(0, 3).Select(_ => context.Random.Next(6) + 1).ToArray();
        context.Record("CandlesRoll", $"{context.Execution.SourceId}: [{string.Join(", ", dice)}], threshold {target}.", rolls: dice, hitTarget: target, hits: dice.Count(d => d >= target));
        DirectCorruption.Add(context, dice.Count(die => die >= target));
    }
}

public sealed record MorgulWoundStep : EffectStep
{
    public override void Apply(EffectContext context) =>
        DirectCorruption.Add(context, context.State.Fellowship.Corruption <= 3 ? 2 : 1);
}
