using Wotr.Contracts;

namespace Wotr.Domain;

public static class ActionSequencePreview
{
    public static void Finish(MatchState state, Side actor, ActionPreview preview)
    {
        preview.Excess = state.Units.Where(u => BoardRules.Owner(u) == actor && BoardRules.IsArmyUnit(u) && BoardRules.Regions.ContainsKey(u.Location))
            .GroupBy(u => (u.Location, u.Garrison))
            .Where(g => g.Count() > (g.Key.Garrison ? 5 : 10))
            .Select(g => new ArmyExcess { RegionId = g.Key.Location, Garrison = g.Key.Garrison, Count = g.Count() - (g.Key.Garrison ? 5 : 10) }).ToList();
        var before = state.Nations.ToDictionary(n => n.Id);
        preview.PoliticsChanges = PoliticalReactions.Preview(state, state)
            .Where(n => n.Active != before[n.Id].Active || n.StepsToWar != before[n.Id].StepsToWar)
            .Select(n => new NationView { Id = n.Id, Active = n.Active, StepsToWar = n.StepsToWar }).ToList();
    }
}
