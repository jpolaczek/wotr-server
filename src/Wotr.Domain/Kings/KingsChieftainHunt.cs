namespace Wotr.Domain;

public static class KingsChieftainHunt
{
    public static int RerollBonus(MatchState state)
    {
        if (!state.Configuration.Kings || CharacterRules.InPlay(state, KingsChieftains.Ugluk) is not { } ugluk)
            return 0;
        var fellowship = state.Fellowship.RegionId;
        // On the Mordor Track the Fellowship stands in no region, where the Hunt has no re-roll source either
        // (HuntRoll.Sources), so a re-roll bonus has nothing to add to.
        if (!BoardRules.Regions.TryGetValue(fellowship, out var region))
            return 0;
        return ugluk.Location == fellowship || region.Neighbors.Contains(ugluk.Location) ? 1 : 0;
    }
}
