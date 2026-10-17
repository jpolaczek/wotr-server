using Wotr.Contracts;

namespace Wotr.Domain;

public static class ShadowMarchRules
{
    public static bool Supports(ArmyEventOrder order) => order is ArmyEventOrder.ShadowsGather or ArmyEventOrder.ShadowLengthens or ArmyEventOrder.ShadowMoving;
    public static int MaximumArmies(ArmyEventOrder order) => order == ArmyEventOrder.ShadowMoving ? 4 : order == ArmyEventOrder.ShadowLengthens ? 2 : 1;
    public static int MaximumSteps(ArmyEventOrder order) => order == ArmyEventOrder.ShadowsGather ? 3 : order == ArmyEventOrder.ShadowLengthens ? 2 : 1;

    public static ActionOption Option(MatchState state, Side actor, ArmyEventOrder order)
    {
        var armies = Armies(state, actor, order).ToList();
        return new()
        {
            Kind = CommandKind.MoveArmies,
            Armies = armies,
            ArmyEvent = new() { RouteMovement = true, SeparateRoutes = true, MaximumSteps = MaximumSteps(order) },
            Minimum = 1,
            Maximum = MaximumArmies(order),
            EligibleIds = armies.SelectMany(a => a.UnitIds.Concat(a.CharacterIds)).Distinct().ToList(),
            Targets = armies.SelectMany(a => a.Targets.Select(t => t.RegionId)).Distinct().ToList(),
            Enabled = armies.Count > 0
        };
    }

    public static IEnumerable<ArmyOption> Armies(MatchState state, Side actor, ArmyEventOrder order)
    {
        if (actor != Side.Shadow || !Supports(order))
            yield break;
        var groups = Groups(state, actor).ToArray();
        var occupied = groups.Select(g => g.Key).ToHashSet(StringComparer.Ordinal);
        var hasLeaders = state.Units.Any(u => u.Kind == UnitKind.Leader && BoardRules.Owner(u) == actor);
        foreach (var group in groups)
        {
            var besieging = BoardRules.HasEnemyArmy(state, group.Key, actor, true);
            var characters = state.Characters.Where(c => c.Side == actor && !c.Garrison && c.Location == group.Key && CharacterRules.Mobile(c))
                .Select(c => c.Id).ToList();
            var army = new ArmyOption { RegionId = group.Key, UnitIds = group.Select(u => u.Id).ToList(), CharacterIds = characters };
            foreach (var move in Candidates(state, actor, group, characters, order, occupied))
            {
                // Candidates already validate every edge and the fixed group's borders. Only
                // re-entering the source can meet a garrison released by this same march.
                // Keep full group validation for Leader figures and their rearguard constraint.
                if (!hasLeaders && (!besieging || !move.Route.Contains(group.Key)) ||
                    CanUseRoute(state, state, actor, move, order, occupied))
                    army.Targets.Add(new() { RegionId = move.To, Route = move.Route, EligibleIds = move.UnitIds.Concat(characters).ToList() });
            }
            if (army.Targets.Count > 0)
                yield return army;
        }
    }

    public static MovementPlan? Plan(MatchState state, Side actor, CommandPayload payload, ArmyEventOrder order) =>
        Plan(state, actor, payload, order, out _);

    public static MovementPlan? Plan(MatchState state, Side actor, CommandPayload payload, ArmyEventOrder order, out string? error)
    {
        error = "IllegalChoice";
        if (actor != Side.Shadow || !Supports(order) || payload.DieId != "" || payload.AbilityId != "" || payload.Route.Count != 0 ||
            payload.Moves.Count < 1 || payload.Moves.Count > MaximumArmies(order) ||
            payload.Moves.Select(m => m.From).Distinct().Count() != payload.Moves.Count)
            return null;
        var occupied = Groups(state, actor).Select(g => g.Key).ToHashSet(StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);
        var board = MovementPlan.CopyBoard(state);
        foreach (var move in payload.Moves)
            if (move.UnitIds.Concat(move.CharacterIds).Any(id => string.IsNullOrEmpty(id) || !used.Add(id)) ||
                !ApplyRoute(board, state, actor, move, order, occupied))
                return null;
        if (order == ArmyEventOrder.ShadowLengthens && payload.Moves.Count == 1 &&
            HasAnotherMove(board, state, actor, order, occupied, used, payload.Moves[0].From))
        {
            error = "MoreArmyMovesRequired";
            return null;
        }
        error = null;
        return new(board, payload.Moves);
    }

    private static bool ApplyRoute(MatchState board, MatchState rules, Side actor, ArmyMoveChoice move, ArmyEventOrder order, HashSet<string> occupied)
    {
        if (!BoardRules.Regions.ContainsKey(move.From) || move.Route.Count < 1 || move.Route.Count > MaximumSteps(order) ||
            move.Route[^1] != move.To || move.To == move.From || order != ArmyEventOrder.ShadowMoving && !occupied.Contains(move.To))
            return false;
        var eventStart = board.Events.Count;
        var activity = MovementFacts.Event(board, ActionMovementKind.March, move.From, move.To,
            board.Units.Where(u => move.UnitIds.Contains(u.Id)), board.Characters.Where(c => move.CharacterIds.Contains(c.Id)), move.Route);
        var from = move.From;
        foreach (var to in move.Route)
        {
            var step = new ArmyMoveChoice { From = from, To = to, UnitIds = move.UnitIds, CharacterIds = move.CharacterIds };
            if (RegionProtection.Blocks(rules, actor, to) || !MovementRules.Move(board, actor, step, DieFace.Army))
                return false;
            from = to;
        }
        for (var index = board.Events.Count - 1; index >= eventStart; index--)
            if (board.Events[index].Kind == "ArmyMoved")
                board.Events.RemoveAt(index);
        board.Events.Add(new() { Revision = board.Revision, Kind = "ArmyMoved", Activity = activity,
            Text = $"{actor} moved an army from {move.From} to {move.To}." });
        return true;
    }

    private static bool HasAnotherMove(MatchState board, MatchState rules, Side actor, ArmyEventOrder order,
        HashSet<string> occupied, HashSet<string> used, string previousSource)
    {
        foreach (var group in Groups(board, actor, used).Where(g => g.Key != previousSource))
        {
            var characters = board.Characters.Where(c => c.Side == actor && !c.Garrison && c.Location == group.Key &&
                CharacterRules.Mobile(c) && !used.Contains(c.Id)).Select(c => c.Id).ToList();
            foreach (var move in Candidates(board, actor, group, characters, order, occupied))
                if (CanUseRoute(board, rules, actor, move, order, occupied))
                    return true;
        }
        return false;
    }

    private static bool CanUseRoute(MatchState state, MatchState rules, Side actor, ArmyMoveChoice move,
        ArmyEventOrder order, HashSet<string> occupied)
    {
        if (ApplyRoute(MovementPlan.CopyBoard(state), rules, actor, move, order, occupied))
            return true;
        if (!move.Route.Contains(move.From) || !BoardRules.HasEnemyArmy(state, move.From, actor, true))
            return false;
        var army = state.Units.Where(u => move.UnitIds.Contains(u.Id) && BoardRules.IsArmyUnit(u)).ToArray();
        if (army.Length < 2)
            return false;
        var partial = new ArmyMoveChoice
        {
            From = move.From,
            To = move.To,
            Route = move.Route,
            UnitIds = move.UnitIds.Where(id => id != army[0].Id).ToList(),
            CharacterIds = move.CharacterIds
        };
        return ApplyRoute(MovementPlan.CopyBoard(state), rules, actor, partial, order, occupied);
    }

    private static IEnumerable<IGrouping<string, UnitState>> Groups(MatchState state, Side actor, HashSet<string>? excluded = null) =>
        state.Units.Where(u => !u.Garrison && BoardRules.Owner(u) == actor && BoardRules.Regions.ContainsKey(u.Location) &&
            excluded?.Contains(u.Id) != true).GroupBy(u => u.Location).Where(g => g.Any(BoardRules.IsArmyUnit));

    private static IEnumerable<ArmyMoveChoice> Candidates(MatchState state, Side actor, IGrouping<string, UnitState> group,
        List<string> characters, ArmyEventOrder order, HashSet<string> occupied)
    {
        foreach (var route in Routes(state, actor, group.Key, MaximumSteps(order)))
        {
            if (route[^1] == group.Key || order != ArmyEventOrder.ShadowMoving && !occupied.Contains(route[^1]))
                continue;
            var eligible = group.Where(u => route.All(to => MovementRules.CanCrossNationalBorder(state, u, to))).ToList();
            if (eligible.Any(BoardRules.IsArmyUnit))
                yield return new() { From = group.Key, To = route[^1], Route = route,
                    UnitIds = eligible.Select(u => u.Id).ToList(), CharacterIds = characters };
        }
    }

    private static IEnumerable<List<string>> Routes(MatchState state, Side actor, string from, int remaining)
    {
        foreach (var target in BoardRules.Regions[from].Neighbors.Where(to => MovementRules.CanEnter(state, actor, from, to)))
        {
            yield return [target];
            if (remaining > 1)
                foreach (var tail in Routes(state, actor, target, remaining - 1))
                    yield return [target, .. tail];
        }
    }
}
