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
            frames[0].EffectId != "hunt" || !HuntChoiceProjection.PlainFrame(frames[0]) || !MovementHunt(hunt) || state.Pending is null ||
            state.Resolution.ActionSequence is not null || state.Resolution.PaidDieIds.Count != 1 ||
            !state.Dice.Any(d => d.Id == state.Resolution.PaidDieIds[0] &&
                d.Owner == Side.FreePeoples && d.Location == DieLocation.Resolving))
            return null;
        FellowshipStrongholdView? strongholds = null;
        var activeIndex = 0;
        if (frames.Count > 2)
        {
            strongholds = FellowshipStrongholdProjection.For(state, hunt);
            if (strongholds is null)
                return null;
            activeIndex = 2;
            hunt = frames[activeIndex].Hunt!;
        }
        if (!HuntChoiceProjection.Supports(state, hunt, activeIndex))
            return null;
        var facts = new FellowshipMoveView { StrongholdHunts = strongholds };
        HuntChoiceProjection.CopyProgress(hunt, facts);
        return facts;
    }

    private static bool MovementHunt(HuntProcedure hunt) =>
        hunt.SourceId == "fellowship-move" && hunt.Movement && hunt.Return == HuntReturn.FinishMove &&
        !hunt.Rolled && hunt.TileRule == HuntTileRule.All && hunt.DamageRule == HuntDamageRule.Normal &&
        hunt.Purpose == HuntPurpose.Hunt && !hunt.DeclarationHunt && !hunt.IgnoreRevealIcon &&
        !hunt.FireAndShadow && hunt.DrawnBy == Side.Shadow && hunt.DiceReduction == 0 &&
        hunt.RerollBonus == 0 && (hunt.Stage is not (HuntStage.BeforeRoll or HuntStage.Roll) || hunt.RollBonus == 0);
}
