namespace Wotr.Domain;

public static class KingsChieftainHunt
{
    public static int RerollBonus(MatchState state)
    {
        if (!state.Configuration.Kings || CharacterRules.InPlay(state, KingsChieftains.Ugluk) is not { } ugluk)
            return 0;
        var fellowship = state.Fellowship.RegionId;
        return ugluk.Location == fellowship || BoardRules.Regions[fellowship].Neighbors.Contains(ugluk.Location) ? 1 : 0;
    }
}
