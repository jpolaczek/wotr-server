using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record HuntBeforeRollCondition : PublicEventCondition
{
    public override bool CanPlay(MatchState state, Side actor) =>
        actor == Side.Shadow && HuntRuntime.Active(state) is { Stage: HuntStage.Roll, Mordor: false } &&
        state.Dice.Any(DiceRules.IsShadowHuntDie);
}

public sealed record CrebainBonusStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        HuntRuntime.Active(context.State)!.RollBonus++;
        context.Record("HuntBonus", "Flocks of Crebain: +1 to this Hunt roll and its re-rolls.");
    }
}
