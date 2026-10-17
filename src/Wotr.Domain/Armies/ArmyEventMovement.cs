using Wotr.Contracts;

namespace Wotr.Domain;

public static class ArmyEventMovement
{
    public static MovementPlan? Plan(MatchState state, Side actor, CommandPayload payload, ArmyEventOrder order)
    {
        if (ShadowMarchRules.Supports(order))
            return ShadowMarchRules.Plan(state, actor, payload, order);
        if (order == ArmyEventOrder.Corsairs)
            return CorsairRules.Plan(state, actor, payload);
        if (ShadowArmyEvents.Supports(order))
            return ShadowArmyEvents.Move(state, actor, payload, order);
        if (payload.Moves.Any(m => m.Route.Count != 0) || order == ArmyEventOrder.ReliefAttack || payload.DieId != "" || payload.AbilityId != "" || payload.Moves.Count != 1)
            return null;
        return PlanGroup(state, actor, payload.Moves[0], payload.Route, order);
    }

    internal static MovementPlan? PlanGroup(MatchState state, Side actor, ArmyMoveChoice move, IReadOnlyList<string> route, ArmyEventOrder order)
    {
        var passage = order == ArmyEventOrder.RohanPassage;
        if (route.Count < 1 || route.Count > (passage ? 1 : 2) || route[^1] != move.To ||
            !BoardRules.Regions.ContainsKey(move.From) || route.Any(r => !BoardRules.Regions.ContainsKey(r)) ||
            move.UnitIds.Concat(move.CharacterIds).Distinct().Count() != move.UnitIds.Count + move.CharacterIds.Count)
            return null;
        if (passage && (BoardRules.Region(state, move.From).Nation != "rohan" || !PassageTargets(state, actor).Contains(move.To)))
            return null;
        if (!passage && !state.Characters.Any(c => move.CharacterIds.Contains(c.Id) && CharacterRules.IsCompanion(c)))
            return null;
        var board = MovementPlan.CopyBoard(state);
        var from = move.From;
        foreach (var to in route)
        {
            var step = new ArmyMoveChoice { From = from, To = to, UnitIds = move.UnitIds, CharacterIds = move.CharacterIds };
            if (RegionProtection.Blocks(state, actor, to) || !MovementRules.Move(board, actor, step, DieFace.Army, passage))
                return null;
            from = to;
        }
        return new(board, [move]);
    }

    internal static IEnumerable<string> PassageTargets(MatchState state, Side actor)
    {
        const string city = "minas-tirith";
        var unavailable = state.Control.GetValueOrDefault(city) == Side.Shadow ||
            BoardRules.Besieged(state, city, Side.FreePeoples) || BoardRules.Besieged(state, city, Side.Shadow);
        return (unavailable ? BoardRules.Regions[city].Neighbors : [city])
            .Where(to => MovementRules.DestinationFree(state, actor, to));
    }

    internal static IEnumerable<List<string>> Routes(MatchState state, Side actor, string from, ArmyEventOrder order)
    {
        if (order == ArmyEventOrder.RohanPassage)
        {
            foreach (var target in PassageTargets(state, actor))
                yield return [target];
            yield break;
        }
        foreach (var first in BoardRules.Regions[from].Neighbors.Where(to => MovementRules.CanEnter(state, actor, from, to)))
        {
            yield return [first];
            foreach (var second in BoardRules.Regions[first].Neighbors.Where(to => MovementRules.CanEnter(state, actor, first, to)))
                yield return [first, second];
        }
    }
}
