using Wotr.Contracts;

namespace Wotr.Domain;

public static class GollumHunt
{
    public static void IgnoreNumberedReveal(MatchState state, HuntProcedure hunt)
    {
        if (CharacterRules.GuideDefinition(state) != CharacterRules.Gollum || hunt.VoluntaryReveal ||
            !HuntPool.Definitions.TryGetValue(hunt.TileId, out var tile))
            return;
        if (tile.Pool == "standard" && int.TryParse(tile.Value, out _))
            hunt.Reveal = false;
    }

    public static bool CanReveal(MatchState state)
    {
        if (!state.Fellowship.Hidden || HuntRuntime.Active(state) is not { Reveal: false, DamageRule: not HuntDamageRule.CorruptionOnly })
            return false;
        if (FellowshipFlow.Active(state) is { Kind: FellowshipProcedureKind.Declaration } declaration &&
            TravelRules.Sanctuary(state, declaration.Route[^1]))
            return false;
        return state.Fellowship.MordorStep.HasValue || TravelRules.Reachable(state, state.Fellowship.RegionId, state.Fellowship.Progress, false)
            .Any(r => !TravelRules.Sanctuary(state, r));
    }
}
