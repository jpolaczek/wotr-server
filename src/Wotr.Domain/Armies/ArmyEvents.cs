using Wotr.Contracts;

namespace Wotr.Domain;

public enum ArmyEventOrder { ReliefAttack, RohanPassage, CompanionMarch, NazgulManeuver, WitchKingManeuver, GrondSiege, UrukHaiSiege, ShadowsGather, ShadowLengthens, ShadowMoving, Corsairs, BalrogManeuver, DolGuldurManeuver }

public static class ArmyEvents
{
    public static bool Optional(ArmyEventOrder? order) => ShadowArmyEvents.Optional(order) || order == ArmyEventOrder.ShadowMoving;

    public static ArmyEventOrder? Current(MatchState state, Side actor) =>
        state.Pending is { Kind: DecisionKind.ArmyEvent } pending && pending.Owner == actor &&
        EffectRuntime.Current(state)?.Effect is { Wait: EffectWait.Army } execution && execution.Owner == actor
            ? execution.ArmyOrder : null;

    public static void CompleteChoice(MatchState state)
    {
        var execution = EffectRuntime.Current(state)!.Effect!;
        execution.ArmyOrder = null;
        execution.Wait = EffectWait.None;
        state.Pending = null;
    }

    public static BattleState? Attack(MatchState state, Side actor, CommandPayload payload)
    {
        if (Current(state, actor) is { } order && ShadowArmyEvents.Supports(order))
            return ShadowArmyEvents.Attack(state, actor, payload, order);
        if (Current(state, actor) != ArmyEventOrder.ReliefAttack || payload.Moves.Count != 1 ||
            payload.DieId != "" || payload.AbilityId != "" || payload.Route.Count != 0)
            return null;
        var battle = BattleRules.Attack(state, actor, payload.Moves[0], DieFace.Army);
        if (battle?.Mode != BattleMode.Relief)
            return null;
        battle.Defender.CombatDiceReduction = state.Units.Count(u => u.Location == battle.Region &&
            u.Garrison && BoardRules.Owner(u) == actor && BoardRules.IsArmyUnit(u));
        return battle;
    }
}

public sealed record ArmyOrderStep(ArmyEventOrder Order) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        if (!ArmyEventOptions.Armies(context.State, context.Owner, Order).Any())
            return;
        context.Execution.ArmyOrder = Order;
        context.Execution.Wait = EffectWait.Army;
        context.State.Open(DecisionKind.ArmyEvent, context.Owner,
            ShadowArmyEvents.Supports(Order) ? ShadowArmyEvents.Siege(Order) ? [CommandKind.AttackArmy]
                : [CommandKind.MoveArmies, CommandKind.AttackArmy, CommandKind.FinishArmyEvent]
                : Order == ArmyEventOrder.ShadowMoving ? [CommandKind.MoveArmies, CommandKind.FinishArmyEvent]
                : [Order == ArmyEventOrder.ReliefAttack ? CommandKind.AttackArmy : CommandKind.MoveArmies]);
        context.State.Pending!.SourceId = context.Execution.SourceId;
        context.State.Pending.Presentation = Order.ToString();
    }
}

public sealed record ArmyOrderCondition(ArmyEventOrder Order) : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) =>
        (Order != ArmyEventOrder.RohanPassage || state.Nations.Single(n => n.Id == "rohan").StepsToWar == 0) &&
        ArmyEventOptions.Armies(state, actor, Order).Any();
}
