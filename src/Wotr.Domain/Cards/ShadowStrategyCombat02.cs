using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record MumakilCondition : ICombatCondition
{
    public bool CanPlay(MatchState state, BattleState battle, Side actor) =>
        BattleRules.Units(state, battle.Force(actor)).Any(u => u.Nation == "southrons" && u.Kind == UnitKind.Elite);
}

public sealed record MumakilHitsStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var battle = BattleRuntime.Active(context.State)!;
        var force = battle.Force(context.Owner);
        var opponent = battle.Opponent(context.Owner);
        if (BattleRoll.HitCount(force) + force.Round.SpecialHits > BattleRoll.HitCount(opponent) + opponent.Round.SpecialHits)
            force.Round.ExtraHits++;
    }
}

public sealed record SwarmOfBatsStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var battle = BattleRuntime.Active(context.State)!;
        var opposing = battle.Opponent(context.Owner).Round;
        if (opposing.CardId.Length == 0)
            battle.Force(context.Owner).Round.RerollModifier++;
        else
            opposing.CardCancelled = true;
    }
}
