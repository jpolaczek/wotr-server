using Wotr.Contracts;

namespace Wotr.Domain;

public static class TerritoryRules
{
    public static void Arrive(MatchState state, Side actor, string from, string to, bool politicalEntry = true)
    {
        if (politicalEntry)
            PoliticalRules.EnemyEntered(state, to, actor);
        Capture(state, actor, to);
        if (state.Units.Any(u => u.Location == from && !u.Garrison && BoardRules.Owner(u) == actor && BoardRules.IsArmyUnit(u)))
            return;
        ReleaseGarrison(state, from);
    }

    public static void Capture(MatchState state, Side actor, string regionId)
    {
        var region = BoardRules.Region(state, regionId);
        var previous = state.Control.GetValueOrDefault(regionId);
        if (!BoardRules.IsSettlement(region) || previous == actor || BoardRules.HasEnemyArmy(state, regionId, actor))
            return;
        state.Control[regionId] = actor;
        LordsCharacters.SettlementCaptured(state, actor, regionId);
        if (previous.HasValue)
            PoliticalRules.SettlementCaptured(state, regionId, previous.Value, actor, region);
        state.Events.Add(new() { Revision = state.Revision, Kind = "SettlementCaptured", Text = $"{actor} controls {region.Name}." });
    }

    public static void ReleaseGarrison(MatchState state, string region)
    {
        foreach (var unit in state.Units.Where(u => u.Location == region && u.Garrison))
            unit.Garrison = false;
        foreach (var character in state.Characters.Where(c => c.Location == region && c.Garrison))
            character.Garrison = false;
        if (BattleRuntime.Active(state) is { } battle)
            foreach (var force in new[] { battle.Attacker, battle.Defender }.Where(f => f.Region == region && f.Garrison))
                force.Garrison = false;
    }
}
