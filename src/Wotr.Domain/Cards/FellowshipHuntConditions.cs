using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record FellowshipOutsideFreeSettlement : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor)
    {
        if (state.Fellowship.MordorStep.HasValue)
            return true;
        if (!BoardRules.Regions.ContainsKey(state.Fellowship.RegionId))
            return false;
        var region = BoardRules.Region(state, state.Fellowship.RegionId);
        return !BoardRules.IsSettlement(region) || region.OriginalOwner != Side.FreePeoples;
    }
}
