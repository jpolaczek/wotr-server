namespace Wotr.Domain;

public sealed record BothArmiesModifierStep(int Bonus) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var battle = BattleRuntime.Active(context.State)!;
        foreach (var force in new[] { battle.Attacker, battle.Defender })
        {
            force.Round.CombatModifier += Bonus;
            force.Round.RerollModifier += Bonus;
        }
    }
}

public sealed record VoluntaryLossesStep(int Maximum, bool AdditionalAttack = false) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        context.Execution.AppliedHits = 0;
        BattleLosses.Begin(context.State, BattleRuntime.Active(context.State)!.Force(context.Owner), Maximum,
            context.Execution.SourceId, voluntary: true, additionalAttack: AdditionalAttack);
    }
}

public sealed record SacrificeResultStep(bool AdditionalAttack) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var battle = BattleRuntime.Active(context.State)!;
        if (AdditionalAttack && context.Execution.AppliedHits > 0)
            BattleRoll.Special(context.State, battle, context.Owner, context.Execution.SourceId,
                context.Execution.AppliedHits, context.Random, target: 4);
        else if (!AdditionalAttack)
            battle.Force(context.Owner).Round.CombatModifier += context.Execution.AppliedHits;
    }
}

public sealed record GreatHostStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var battle = BattleRuntime.Active(context.State)!;
        var own = BattleRules.Units(context.State, battle.Force(context.Owner)).Count(BoardRules.IsArmyUnit);
        var enemy = BattleRules.Units(context.State, battle.Opponent(context.Owner)).Count(BoardRules.IsArmyUnit);
        if (enemy > 0 && own >= 2 * enemy)
            BattleLosses.Begin(context.State, battle.Opponent(context.Owner), 1, context.Execution.SourceId);
    }
}
