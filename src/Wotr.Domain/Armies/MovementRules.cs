using Wotr.Contracts;

namespace Wotr.Domain;

public static class MovementRules
{
    public static MovementPlan? Plan(MatchState state, Side actor, CommandPayload payload)
    {
        if (ArmyEvents.Current(state, actor) is { } order)
            return ArmyEventMovement.Plan(state, actor, payload, order);
        if (payload.Route.Count != 0 || payload.Moves.Any(m => m.Route.Count != 0))
            return null;
        var sequence = payload.Stepwise ? state.Resolution.ActionSequence : null;
        var maximum = sequence?.Remaining ?? (payload.MovementCost == DieFace.Army ? 2 : 1);
        if (payload.MovementCost is not (DieFace.Army or DieFace.Character) ||
            (sequence is null ? !CharacterCosts.CanPayArmy(state, actor, payload) : !PaidMovement(state, actor, payload, sequence)) ||
            payload.Moves.Count < 1 || payload.Moves.Count > maximum)
            return null;
        var board = MovementPlan.CopyBoard(state);
        var moved = new HashSet<string>(sequence?.UsedIds ?? [], StringComparer.Ordinal);
        foreach (var move in payload.Moves)
        {
            var ids = move.UnitIds.Concat(move.CharacterIds).ToArray();
            if (RegionProtection.Blocks(state, actor, move.To) || ids.Any(id => string.IsNullOrEmpty(id) || !moved.Add(id)) || !Move(board, actor, move, payload.MovementCost))
                return null;
        }
        return new(board, payload.Moves);
    }

    private static bool PaidMovement(MatchState state, Side actor, CommandPayload payload, ActionSequenceState sequence) =>
        state.Pending?.Kind == DecisionKind.ActionSequence && sequence.Kind == CommandKind.MoveArmies && sequence.Owner == actor &&
        sequence.DieId == payload.DieId && sequence.Face == payload.MovementCost && sequence.AbilityId == payload.AbilityId &&
        state.Resolution.PaidDieIds.Contains(sequence.DieId) && payload.Moves.Count == 1;

    public static bool CanEnter(MatchState state, Side actor, string from, string to)
    {
        if (RegionProtection.Blocks(state, actor, to) || string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to) || !BoardRules.Regions.TryGetValue(from, out var origin) ||
            !origin.Neighbors.Contains(to))
            return false;
        return DestinationFree(state, actor, to);
    }

    internal static bool DestinationFree(MatchState state, Side actor, string to)
    {
        if (!BoardRules.Regions.ContainsKey(to) || RegionProtection.Blocks(state, actor, to) || BoardRules.HasEnemyArmy(state, to, actor, false))
            return false;
        return !BoardRules.HasEnemyArmy(state, to, actor, true) ||
            state.Units.Any(u => u.Location == to && !u.Garrison && BoardRules.Owner(u) == actor && BoardRules.IsArmyUnit(u));
    }

    public static bool CanCrossNationalBorder(MatchState state, UnitState unit, string target) =>
        unit.Kind == UnitKind.Nazgul || BoardRules.Region(state, target).Nation is null ||
        BoardRules.Region(state, target).Nation == unit.Nation || state.Nations.Single(n => n.Id == unit.Nation).StepsToWar == 0;

    internal static bool Move(MatchState board, Side actor, ArmyMoveChoice move, DieFace cost, bool passage = false)
    {
        if (passage ? !DestinationFree(board, actor, move.To) : !CanEnter(board, actor, move.From, move.To))
            return false;
        var units = board.Units.Where(u => move.UnitIds.Contains(u.Id)).ToList();
        var characters = board.Characters.Where(c => move.CharacterIds.Contains(c.Id)).ToList();
        if (!ValidGroup(board, actor, move, units, characters, cost, passage))
            return false;
        var garrison = units[0].Garrison;
        var activity = MovementFacts.Event(board, ActionMovementKind.March, move.From, move.To, units, characters);
        foreach (var unit in units)
        {
            unit.Location = move.To;
            unit.Garrison = false;
        }
        foreach (var character in characters)
        {
            character.Location = move.To;
            character.Garrison = false;
        }
        if (passage)
            CasualtyRules.RemoveUnaccompanied(board, move.From, garrison, actor, CasualtyCause.Event);
        StrongholdRules.ReconcileOutsideBattle(board, [move.From]);
        TerritoryRules.Arrive(board, actor, move.From, move.To);
        foreach (var character in characters.Where(c => c.Side == Side.FreePeoples))
            PoliticalRules.CompanionArrived(board, move.To, character.NationIcon);
        board.Events.Add(new() { Revision = board.Revision, Kind = "ArmyMoved", Activity = activity,
            Text = $"{actor} moved an army from {move.From} to {move.To}." });
        return true;
    }

    private static bool ValidGroup(MatchState board, Side actor, ArmyMoveChoice move, List<UnitState> units, List<CharacterState> characters, DieFace cost, bool passage)
    {
        if (units.Count != move.UnitIds.Count || characters.Count != move.CharacterIds.Count || !units.Any(BoardRules.IsArmyUnit) ||
            units.Any(u => (!passage && u.Garrison) || u.Garrison != units[0].Garrison || u.Location != move.From || BoardRules.Owner(u) != actor || !CanCrossNationalBorder(board, u, move.To)) ||
            characters.Any(c => c.Garrison != units[0].Garrison || c.Location != move.From || c.Side != actor || !CharacterRules.Mobile(c)))
            return false;
        if (cost == DieFace.Character && characters.Count == 0 && !units.Any(u => CharacterRules.Leads(board, u)))
            return false;
        var left = board.Units.Where(u => u.Location == move.From && u.Garrison == units[0].Garrison && BoardRules.Owner(u) == actor && !move.UnitIds.Contains(u.Id)).ToList();
        return passage && units[0].Garrison || !left.Any(u => u.Kind == UnitKind.Leader) || left.Any(BoardRules.IsArmyUnit);
    }

}
