using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record EliteInBattleCondition : ICombatCondition
{
    public bool CanPlay(MatchState state, BattleState battle, Side actor) => ChargeStep.Elites(state, battle.Force(actor)) > 0;
}

public sealed record ChargeStep : EffectStep
{
    public static int Elites(MatchState state, BattleForce force) =>
        BattleRules.Units(state, force).Count(u => u.Kind == UnitKind.Elite);

    public override void Apply(EffectContext context)
    {
        var battle = BattleRuntime.Active(context.State)!;
        var dice = Math.Min(5, Elites(context.State, battle.Force(context.Owner)));
        BattleRoll.Special(context.State, battle, context.Owner, context.Execution.SourceId, dice, context.Random);
    }
}
