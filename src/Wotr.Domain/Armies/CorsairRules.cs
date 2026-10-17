using Wotr.Contracts;

namespace Wotr.Domain;

public static class CorsairRules
{
    public const string Origin = "umbar";

    public static IEnumerable<ArmyOption> Armies(MatchState state, Side actor)
    {
        if (actor != Side.Shadow)
            yield break;
        var units = state.Units.Where(u => u.Location == Origin && !u.Garrison && BoardRules.Owner(u) == actor).ToList();
        if (!units.Any(BoardRules.IsArmyUnit))
            yield break;
        var characters = state.Characters.Where(c => c.Location == Origin && !c.Garrison && c.Side == actor && CharacterRules.Mobile(c)).ToList();
        var army = new ArmyOption
        {
            RegionId = Origin,
            UnitIds = units.Select(u => u.Id).ToList(),
            CharacterIds = characters.Select(c => c.Id).ToList(),
            CommanderIds = units.Where(u => CharacterRules.Leads(state, u)).Select(u => u.Id).Concat(characters.Select(c => c.Id)).ToList()
        };
        foreach (var region in BoardRules.CurrentRegions(state).Where(r => r.Nation == "gondor" && r.Coastal))
        {
            if (RegionProtection.Blocks(state, actor, region.Id))
                continue;
            var eligible = units.Where(u => MovementRules.CanCrossNationalBorder(state, u, region.Id)).ToList();
            if (eligible.Any(BoardRules.IsArmyUnit))
                army.Targets.Add(new() { RegionId = region.Id, EligibleIds = eligible.Select(u => u.Id).Concat(army.CharacterIds).ToList() });
        }
        if (army.Targets.Count > 0)
            yield return army;
    }

    public static ActionOption Option(MatchState state, Side actor)
    {
        var armies = Armies(state, actor).ToList();
        return new()
        {
            Kind = CommandKind.MoveArmies,
            Armies = armies,
            ArmyEvent = new() { MaximumSteps = 1 },
            Minimum = 1,
            Maximum = 1,
            EligibleIds = armies.SelectMany(a => a.UnitIds.Concat(a.CharacterIds)).ToList(),
            Targets = armies.SelectMany(a => a.Targets.Select(t => t.RegionId)).ToList(),
            Enabled = armies.Count > 0
        };
    }

    public static MovementPlan? Plan(MatchState state, Side actor, CommandPayload payload)
    {
        if (actor != Side.Shadow || payload.DieId != "" || payload.AbilityId != "" || payload.Route.Count != 0 || payload.Moves.Count != 1)
            return null;
        var move = payload.Moves[0];
        if (!ValidGroup(state, actor, move))
            return null;
        var board = MovementPlan.CopyBoard(state);
        var activity = MovementFacts.Event(board, ActionMovementKind.March, move.From, move.To,
            board.Units.Where(u => move.UnitIds.Contains(u.Id)), board.Characters.Where(c => move.CharacterIds.Contains(c.Id)));
        foreach (var unit in board.Units.Where(u => move.UnitIds.Contains(u.Id)))
            unit.Location = move.To;
        foreach (var character in board.Characters.Where(c => move.CharacterIds.Contains(c.Id)))
            character.Location = move.To;
        StrongholdRules.ReconcileOutsideBattle(board, [Origin]);
        TerritoryRules.Arrive(board, actor, Origin, move.To);
        board.Events.Add(new() { Revision = board.Revision, Kind = "ArmyMoved", Activity = activity,
            Text = $"Shadow sailed from Umbar to {move.To}." });
        return new(board, [move]);
    }

    private static bool ValidGroup(MatchState state, Side actor, ArmyMoveChoice move)
    {
        var army = Armies(state, actor).SingleOrDefault();
        var target = army?.Targets.SingleOrDefault(t => t.RegionId == move.To);
        var selected = move.UnitIds.Concat(move.CharacterIds).ToArray();
        if (move.From != Origin || target is null || move.Route.Count != 0 ||
            !DraftSelection.IsSubset(selected, target.EligibleIds, 1, target.EligibleIds.Count) ||
            move.UnitIds.Any(id => !army!.UnitIds.Contains(id)) || move.CharacterIds.Any(id => !army!.CharacterIds.Contains(id)) ||
            !state.Units.Any(u => move.UnitIds.Contains(u.Id) && BoardRules.IsArmyUnit(u)))
            return false;
        var left = state.Units.Where(u => u.Location == Origin && !u.Garrison && BoardRules.Owner(u) == actor && !move.UnitIds.Contains(u.Id)).ToList();
        return !left.Any(u => u.Kind == UnitKind.Leader) || left.Any(BoardRules.IsArmyUnit);
    }

    public static void CompleteLanding(MatchState state, CommandPayload payload)
    {
        var execution = EffectRuntime.Current(state)!.Effect!;
        execution.RegionId = payload.Moves[0].To;
        ArmyEvents.CompleteChoice(state);
        StackLimits.OpenIfRequired(state, execution.Owner);
    }
}
