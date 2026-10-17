namespace Wotr.Domain;

public sealed record SuddenStrikeStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var battle = BattleRuntime.Active(context.State)!;
        var dice = Math.Clamp(BattleLeadership.Total(context.State, battle.Force(context.Owner)), 0, 5);
        BattleRoll.Special(context.State, battle, context.Owner, context.Execution.SourceId, dice, context.Random);
    }
}

public sealed record BraveStandStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var battle = BattleRuntime.Active(context.State)!;
        battle.Opponent(context.Owner).Round.CombatDicePenalty += CombatCardFigures.Companions(context.State, battle.Force(context.Owner)).Count();
    }
}
