using Wotr.Contracts;

namespace Wotr.Domain;

internal static class FellowshipStrongholdProjection
{
    internal static FellowshipStrongholdView? For(MatchState state, HuntProcedure movement, out int activeIndex)
    {
        var reveal = RevealContext(state, 0, out activeIndex);
        if (reveal is null)
            return null;
        return new()
        {
            MovementHunt = HuntOptions.View(state, movement),
            Route = [.. reveal.Route],
            RemainingStrongholds = [.. reveal.AdditionalHuntRegions]
        };
    }

    internal static FellowshipProcedure? RevealContext(MatchState state, int sourceIndex, out int activeIndex)
    {
        var frames = state.Resolution.Frames;
        activeIndex = sourceIndex + 2;
        var balrog = frames.Count >= sourceIndex + 5 &&
            frames[sourceIndex + 3].EffectId == ShadowCharacterCards03.BalrogReaction;
        var offer = frames.Count == sourceIndex + 3 && frames[^1].EffectId == EffectCatalog.ReactionProgram;
        if (frames.Count < sourceIndex + 3 || frames.Count > sourceIndex + 7 ||
            frames[sourceIndex].Hunt is not { Stage: HuntStage.Done, Mordor: false, Reveal: true } source ||
            source.TileId == "" || state.Fellowship.Hidden || state.Fellowship.Progress != 0 ||
            frames[sourceIndex + 1].EffectId != "fellowship.Reveal" ||
            !HuntChoiceProjection.PlainFrame(frames[sourceIndex + 1]) ||
            frames[sourceIndex + 1].Fellowship is not { Kind: FellowshipProcedureKind.Reveal,
                Return: FellowshipReturn.Parent, AwaitingHunts: true, RevealEffectsPending: false,
                FireAndShadowPending: false } reveal ||
            reveal.SourceId != source.TileId || reveal.Journey is not null || reveal.FixedCompanions.Count != 0 ||
            reveal.SeparationOrigin != "" || reveal.SeparationRange != 0 || reveal.SeparationBonus != 0 ||
            reveal.ChooseGuideAfterPlacement || !ValidRoute(state, reveal, balrog || offer))
            return null;
        if (offer)
        {
            // No new Hunt exists yet: the current view still describes the
            // resolved source, while the complete Stronghold queue is suspended.
            activeIndex = sourceIndex;
            return reveal.Route.Contains("moria") &&
                state.Player(Side.Shadow).Table.Contains(ShadowCharacterCards03.Balrog) &&
                HuntChoiceProjection.RevelationOffer(state, source, frames[^1]) ? reveal : null;
        }
        if (balrog)
        {
            activeIndex += 2;
            return BalrogHuntProjection.Supports(state, sourceIndex + 1, reveal) ? reveal : null;
        }
        if (frames[activeIndex].EffectId != "hunt" || !HuntChoiceProjection.PlainFrame(frames[activeIndex]) ||
            frames[activeIndex].Hunt is not { } hunt || !StrongholdHunt(hunt, reveal.AdditionalHuntRegions[0]))
            return null;
        return reveal;
    }

    private static bool ValidRoute(MatchState state, FellowshipProcedure reveal, bool beforeStrongholds)
    {
        var route = reveal.Route;
        if (route.Count == 0 || route.Any(id => !BoardRules.Regions.ContainsKey(id)) ||
            route[^1] != state.Fellowship.RegionId || !TravelRules.ValidPath(route, route[0], route.Count - 1))
            return false;
        var all = TravelRules.AdditionalHunts(state, route);
        var remaining = reveal.AdditionalHuntRegions;
        if (beforeStrongholds)
            return all.SequenceEqual(remaining);
        return remaining.Count > 0 && remaining.Count <= all.Count &&
            all.TakeLast(remaining.Count).SequenceEqual(remaining);
    }

    internal static bool StrongholdHunt(HuntProcedure hunt, string region) =>
        hunt.SourceId == region && hunt.Return == HuntReturn.Stronghold && !hunt.Movement && !hunt.Rolled &&
        !hunt.Mordor && hunt.TileRule == HuntTileRule.All && hunt.DamageRule == HuntDamageRule.Normal &&
        hunt.Purpose == HuntPurpose.Hunt && !hunt.DeclarationHunt && !hunt.IgnoreRevealIcon &&
        !hunt.FireAndShadow && hunt.DrawnBy == Side.Shadow && hunt.DiceReduction == 0 &&
        hunt.Bonus == 0 && hunt.RollBonus == 0 && hunt.RerollBonus == 0 && hunt.EyeDamage == 0 &&
        hunt.Dice.Count == 0 && hunt.RerollSources.Count == 0;
}
