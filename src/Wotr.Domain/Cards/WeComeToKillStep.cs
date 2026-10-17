namespace Wotr.Domain;

public sealed record WeComeToKillStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var battle = BattleRuntime.Active(context.State)!;
        var count = Math.Min(5, ChargeStep.Elites(context.State, battle.Force(context.Owner)));
        BattleRoll.Special(context.State, battle, context.Owner, context.Execution.SourceId, count, context.Random, target: 5);
    }
}
