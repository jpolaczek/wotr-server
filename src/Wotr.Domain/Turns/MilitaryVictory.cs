using Wotr.Contracts;

namespace Wotr.Domain;

public static class MilitaryVictory
{
    // Kings White Hand changes recruitment/Hunt affiliation, not the points for conquest.
    public static IEnumerable<RegionDefinition> ScoringRegions(MatchState state, Side side) =>
        BoardRules.SetupRegions(state.Configuration).Where(region => region.VictoryPoints > 0 &&
            region.OriginalOwner == TurnFlow.Other(side) && state.Control.GetValueOrDefault(region.Id) == side);

    public static int Points(MatchState state, Side side) => ScoringRegions(state, side).Sum(region => region.VictoryPoints);

    public static int Threshold(Side side) => side == Side.Shadow ? 10 : 4;

    public static bool Check(MatchState state)
    {
        if (state.Phase != GamePhase.VictoryCheck)
            return false;
        if (RingVictory.Check(state))
            return true;
        // BASE p44: Shadow military victory precedes Free Peoples military victory.
        Side? winner = Points(state, Side.Shadow) >= Threshold(Side.Shadow) ? Side.Shadow
            : Points(state, Side.FreePeoples) >= Threshold(Side.FreePeoples) ? Side.FreePeoples : null;
        if (winner is null)
            return false;
        GameEnding.Finish(state, winner.Value, winner == Side.Shadow ? VictoryCondition.ShadowMilitary : VictoryCondition.FreePeoplesMilitary);
        return true;
    }
}
