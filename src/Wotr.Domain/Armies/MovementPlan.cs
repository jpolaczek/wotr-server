using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class MovementPlan(MatchState board, IReadOnlyList<ArmyMoveChoice> moves)
{
    public void Apply(MatchState state)
    {
        state.Units = board.Units;
        state.Characters = board.Characters;
        state.Nations = board.Nations;
        state.PoliticalReactions = board.PoliticalReactions;
        state.Control = board.Control;
        state.Events.AddRange(board.Events);
        SovereignLifecycle.Reconcile(state);
    }

    public void Describe(MatchState original, ActionPreview preview)
    {
        preview.Placements = moves.SelectMany(m => m.UnitIds.Concat(m.CharacterIds)
            .Select(id => new UnitPlacementChoice { UnitId = id, RegionId = m.To })).ToList();
        preview.ControlChanges = board.Control.Where(pair => original.Control.GetValueOrDefault(pair.Key) != pair.Value)
            .ToDictionary(p => p.Key, p => p.Value);
        preview.PoliticsChanges = PoliticalReactions.Preview(original, board).Where(n =>
        {
            var before = original.Nations.Single(o => o.Id == n.Id);
            return n.Active != before.Active || n.StepsToWar != before.StepsToWar;
        }).Select(n => new NationView { Id = n.Id, Active = n.Active, StepsToWar = n.StepsToWar }).ToList();
        preview.Excess = board.Units.Where(u => BoardRules.IsArmyUnit(u) && BoardRules.Regions.ContainsKey(u.Location))
            .GroupBy(u => (u.Location, u.Garrison, Side: BoardRules.Owner(u)))
            .Where(g => g.Count() > (g.Key.Garrison ? 5 : 10))
            .Select(g => new ArmyExcess { RegionId = g.Key.Location, Garrison = g.Key.Garrison, Count = g.Count() - (g.Key.Garrison ? 5 : 10) }).ToList();
        preview.RemovedIds = original.Units.Where(u => BoardRules.Regions.ContainsKey(u.Location) &&
            !BoardRules.Regions.ContainsKey(board.Units.Single(b => b.Id == u.Id).Location)).Select(u => u.Id).ToList();
    }

    internal static MatchState CopyBoard(MatchState state) => new()
    {
        Revision = state.Revision,
        Phase = state.Phase,
        Configuration = ConfigurationRules.Normalize(state.Configuration),
        Kings = KingsBoard.PlanningContext(state),
        PoliticalReactions = [.. state.PoliticalReactions],
        Control = new(state.Control),
        Nations = state.Nations.Select(n => new NationState { Id = n.Id, Active = n.Active, StepsToWar = n.StepsToWar }).ToList(),
        Units = state.Units.Select(u => new UnitState { Id = u.Id, Nation = u.Nation, Kind = u.Kind, Location = u.Location, Garrison = u.Garrison }).ToList(),
        Characters = state.Characters.Select(c => new CharacterState
        {
            Id = c.Id,
            DefinitionId = c.DefinitionId,
            Side = c.Side,
            Location = c.Location,
            Garrison = c.Garrison,
            Level = c.Level,
            Leadership = c.Leadership,
            NationIcon = c.NationIcon,
            LastArmyConversionTurn = c.LastArmyConversionTurn
        }).ToList()
    };
}
