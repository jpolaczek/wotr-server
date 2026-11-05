using Wotr.Contracts;

namespace Wotr.Domain;

// Explicit player-facing scope for the ordinary paid move and its Stronghold
// consequences. Card-origin Hunts and arbitrary effect programs need other contexts.
internal static class FellowshipMoveProjection
{
    public static FellowshipMoveView? For(MatchState state)
    {
        var frames = state.Resolution.Frames;
        if (frames.Count < 1 || frames[0].Hunt is not { } hunt ||
            frames[0].EffectId != "hunt" || !HuntChoiceProjection.PlainFrame(frames[0]) || !MovementHunt(state, hunt) || state.Pending is null ||
            state.Resolution.ActionSequence is not null || state.Resolution.PaidDieIds.Count != 1 ||
            !state.Dice.Any(d => d.Id == state.Resolution.PaidDieIds[0] &&
                d.Owner == Side.FreePeoples && d.Location == DieLocation.Resolving))
            return null;
        FellowshipStrongholdView? strongholds = null;
        var activeIndex = 0;
        if (frames.Count > 2 && frames[1].Fellowship is { AwaitingHunts: true })
        {
            strongholds = FellowshipStrongholdProjection.For(state, hunt, out activeIndex);
            if (strongholds is null)
                return null;
            hunt = frames[activeIndex].Hunt!;
        }
        var revelationOffer = strongholds is not null && activeIndex == 0;
        if (!revelationOffer && !HuntChoiceProjection.Supports(state, hunt, activeIndex))
            return null;
        var facts = new FellowshipMoveView { StrongholdHunts = strongholds };
        HuntChoiceProjection.CopyProgress(hunt, facts);
        return facts;
    }

    private static bool MovementHunt(MatchState state, HuntProcedure hunt) =>
        hunt.SourceId == "fellowship-move" && hunt.Movement && hunt.Return == HuntReturn.FinishMove &&
        !hunt.Rolled && hunt.TileRule == HuntTileRule.All && hunt.DamageRule == HuntDamageRule.Normal &&
        hunt.Purpose == HuntPurpose.Hunt && !hunt.DeclarationHunt && !hunt.IgnoreRevealIcon &&
        !hunt.FireAndShadow && hunt.DrawnBy == Side.Shadow &&
        (hunt.DiceReduction == 0 || state.Configuration.Kings && hunt.DiceReduction == 1 &&
            !hunt.Mordor && KingsRulerAbilities.Awakened(state, SovereignCatalog.Thranduil) is not null &&
            state.Resolution.PaidDieIds.Count == 1 && state.Dice.Any(d => d.Id == state.Resolution.PaidDieIds[0] &&
                d.Owner == Side.FreePeoples && d.Source == SovereignCatalog.RulerSource &&
                d.Face == DieFace.Character && d.Location == DieLocation.Resolving)) &&
        hunt.RerollBonus == (hunt.Stage is HuntStage.BeforeRoll or HuntStage.Roll
            ? 0 : KingsChieftainHunt.RerollBonus(state)) && hunt.RollBonus == 0;
}
