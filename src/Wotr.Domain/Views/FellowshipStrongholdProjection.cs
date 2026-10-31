using Wotr.Contracts;

namespace Wotr.Domain;

internal static class FellowshipStrongholdProjection
{
    internal static FellowshipStrongholdView? For(MatchState state, HuntProcedure movement)
    {
        var reveal = RevealContext(state, 0);
        if (reveal is null)
            return null;
        return new()
        {
            MovementHunt = HuntOptions.View(state, movement),
            Route = [.. reveal.Route],
            RemainingStrongholds = [.. reveal.AdditionalHuntRegions]
        };
    }

    internal static FellowshipProcedure? RevealContext(MatchState state, int sourceIndex)
    {
        var frames = state.Resolution.Frames;
        if (frames.Count < sourceIndex + 3 || frames.Count > sourceIndex + 4 ||
            frames[sourceIndex].Hunt is not { Stage: HuntStage.Done, Mordor: false, Reveal: true } source ||
            source.TileId == "" || state.Fellowship.Hidden || state.Fellowship.Progress != 0 ||
            frames[sourceIndex + 1].EffectId != "fellowship.Reveal" ||
            !HuntChoiceProjection.PlainFrame(frames[sourceIndex + 1]) ||
            frames[sourceIndex + 1].Fellowship is not { Kind: FellowshipProcedureKind.Reveal,
                Return: FellowshipReturn.Parent, AwaitingHunts: true, RevealEffectsPending: false,
                FireAndShadowPending: false } reveal ||
            reveal.SourceId != source.TileId || reveal.Journey is not null || reveal.FixedCompanions.Count != 0 ||
            reveal.SeparationOrigin != "" || reveal.SeparationRange != 0 || reveal.SeparationBonus != 0 ||
            reveal.ChooseGuideAfterPlacement || !ValidRoute(state, reveal) ||
            frames[sourceIndex + 2].EffectId != "hunt" || !HuntChoiceProjection.PlainFrame(frames[sourceIndex + 2]) ||
            frames[sourceIndex + 2].Hunt is not { } hunt || !StrongholdHunt(hunt, reveal.AdditionalHuntRegions[0]))
            return null;
        return reveal;
    }

    private static bool ValidRoute(MatchState state, FellowshipProcedure reveal)
    {
        var route = reveal.Route;
        if (route.Count == 0 || route.Any(id => !BoardRules.Regions.ContainsKey(id)) ||
            route[^1] != state.Fellowship.RegionId || !TravelRules.ValidPath(route, route[0], route.Count - 1))
            return false;
        var all = TravelRules.AdditionalHunts(state, route);
        var remaining = reveal.AdditionalHuntRegions;
        return remaining.Count > 0 && remaining.Count <= all.Count &&
            all.TakeLast(remaining.Count).SequenceEqual(remaining);
    }

    private static bool StrongholdHunt(HuntProcedure hunt, string region) =>
        hunt.SourceId == region && hunt.Return == HuntReturn.Stronghold && !hunt.Movement && !hunt.Rolled &&
        !hunt.Mordor && hunt.TileRule == HuntTileRule.All && hunt.DamageRule == HuntDamageRule.Normal &&
        hunt.Purpose == HuntPurpose.Hunt && !hunt.DeclarationHunt && !hunt.IgnoreRevealIcon &&
        !hunt.FireAndShadow && hunt.DrawnBy == Side.Shadow && hunt.DiceReduction == 0 &&
        hunt.Bonus == 0 && hunt.RollBonus == 0 && hunt.RerollBonus == 0 && hunt.EyeDamage == 0 &&
        hunt.Dice.Count == 0 && hunt.RerollSources.Count == 0;
}
