using Wotr.Contracts;

namespace Wotr.Domain;

public static class StackLimits
{
    public static bool OpenIfRequired(MatchState state, Side actor)
    {
        var army = state.Units.Where(u => BoardRules.IsArmyUnit(u) && BoardRules.Owner(u) == actor && BoardRules.Regions.ContainsKey(u.Location))
            .GroupBy(u => (u.Location, u.Garrison)).OrderBy(g => g.Key.Location, StringComparer.Ordinal).ThenBy(g => g.Key.Garrison)
            .FirstOrDefault(g => g.Count() > (g.Key.Garrison ? 5 : 10));
        if (army == null)
            return false;
        state.Open(DecisionKind.RemoveExcess, actor, [CommandKind.RemoveExcess], army.Count() - (army.Key.Garrison ? 5 : 10));
        state.Pending!.PrivateChoices = army.Select(u => u.Id).ToList();
        return true;
    }

    public static void Remove(MatchState state, Side actor, IReadOnlyList<string> ids)
    {
        foreach (var id in ids)
            BoardRules.ReturnToReserve(state.Units.Single(u => u.Id == id));
        state.Events.Add(new() { Revision = state.Revision, Kind = "ExcessReturned", Text = $"{actor} returned {ids.Count} excess units to the reserve." });
        if (!OpenIfRequired(state, actor))
        {
            if (EffectRuntime.Current(state) is not null || BattleRuntime.Current(state) is not null)
                state.Pending = null;
            else
                TurnFlow.FinishAction(state, actor);
        }
    }
}
