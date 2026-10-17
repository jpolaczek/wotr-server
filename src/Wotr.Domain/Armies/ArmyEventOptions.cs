using Wotr.Contracts;

namespace Wotr.Domain;

public static class ArmyEventOptions
{
    public static ActionOption Option(MatchState state, Side actor, ArmyEventOrder order)
    {
        var armies = Armies(state, actor, order).ToList();
        return new()
        {
            Kind = order == ArmyEventOrder.ReliefAttack ? CommandKind.AttackArmy : CommandKind.MoveArmies,
            Armies = armies,
            ArmyEvent = new()
            {
                RouteMovement = order != ArmyEventOrder.ReliefAttack,
                MaximumSteps = order == ArmyEventOrder.CompanionMarch ? 2 : 1,
                RequiresCompanion = order == ArmyEventOrder.CompanionMarch,
                AllowLeaderLoss = order == ArmyEventOrder.RohanPassage
            },
            Minimum = 1,
            Maximum = 1,
            EligibleIds = armies.SelectMany(a => a.UnitIds.Concat(a.CharacterIds)).Distinct().ToList(),
            Targets = armies.SelectMany(a => a.Targets.Select(t => t.RegionId)).Distinct().ToList(),
            Enabled = armies.Count > 0
        };
    }

    public static IEnumerable<ArmyOption> Armies(MatchState state, Side actor, ArmyEventOrder order)
    {
        if (ShadowMarchRules.Supports(order) || order == ArmyEventOrder.Corsairs)
        {
            foreach (var army in order == ArmyEventOrder.Corsairs ? CorsairRules.Armies(state, actor) : ShadowMarchRules.Armies(state, actor, order))
                yield return army;
            yield break;
        }
        if (ShadowArmyEvents.Supports(order))
        {
            foreach (var army in ShadowArmyEvents.Armies(state, actor, order, true))
                yield return army;
            if (!ShadowArmyEvents.Siege(order))
                foreach (var army in ShadowArmyEvents.Armies(state, actor, order, false))
                    yield return army;
            yield break;
        }
        if (order == ArmyEventOrder.ReliefAttack)
        {
            foreach (var army in BattleOptions.AttackingArmies(state, actor))
            {
                army.Targets.RemoveAll(t => t.BattleMode != BattleMode.Relief);
                if (army.Targets.Count > 0)
                    yield return army;
            }
            yield break;
        }
        foreach (var group in state.Units.Where(u => BoardRules.Owner(u) == actor && BoardRules.Regions.ContainsKey(u.Location))
            .GroupBy(u => (u.Location, u.Garrison)).Where(g => g.Any(BoardRules.IsArmyUnit)))
        {
            if (order == ArmyEventOrder.CompanionMarch && group.Key.Garrison ||
                order == ArmyEventOrder.RohanPassage && BoardRules.Region(state, group.Key.Location).Nation != "rohan")
                continue;
            var characters = state.Characters.Where(c => c.Location == group.Key.Location && c.Garrison == group.Key.Garrison &&
                c.Side == actor && CharacterRules.Mobile(c)).ToList();
            if (order == ArmyEventOrder.CompanionMarch && !characters.Any(CharacterRules.IsCompanion))
                continue;
            var army = new ArmyOption
            {
                RegionId = group.Key.Location,
                Garrison = group.Key.Garrison,
                UnitIds = group.Select(u => u.Id).ToList(),
                CharacterIds = characters.Select(c => c.Id).ToList(),
                CommanderIds = characters.Select(c => c.Id).Concat(group.Where(u => CharacterRules.Leads(state, u)).Select(u => u.Id)).ToList()
            };
            foreach (var route in ArmyEventMovement.Routes(state, actor, army.RegionId, order))
                AddRoute(state, actor, order, army, group.ToList(), route);
            if (army.Targets.Count > 0)
                yield return army;
        }
    }

    private static void AddRoute(MatchState state, Side actor, ArmyEventOrder order, ArmyOption army, List<UnitState> units, List<string> route)
    {
        var eligible = units.Where(u => route.All(to => MovementRules.CanCrossNationalBorder(state, u, to))).ToList();
        if (!eligible.Any(BoardRules.IsArmyUnit))
            return;
        var move = new ArmyMoveChoice { From = army.RegionId, To = route[^1], UnitIds = eligible.Select(u => u.Id).ToList(), CharacterIds = army.CharacterIds };
        if (ArmyEventMovement.PlanGroup(state, actor, move, route, order) is null)
        {
            // A non-moving leader can require leaving one army unit. Do not enumerate army subsets.
            move.UnitIds.Remove(eligible.First(BoardRules.IsArmyUnit).Id);
            if (ArmyEventMovement.PlanGroup(state, actor, move, route, order) is null)
                return;
        }
        army.Targets.Add(new()
        {
            RegionId = route[^1],
            Route = route,
            EligibleIds = eligible.Select(u => u.Id).Concat(army.CharacterIds).ToList()
        });
    }
}
