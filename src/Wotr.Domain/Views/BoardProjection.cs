using Wotr.Contracts;

namespace Wotr.Domain;

public static class BoardProjection
{
    // Complete replacement set: an absent override means the printed base region again.
    public static List<RegionDefinition> Overrides(MatchState state) => BoardRules.CurrentRegions(state)
        .Where(r => !ReferenceEquals(r, BoardRules.Regions[r.Id])).Select(Copy).ToList();

    private static RegionDefinition Copy(RegionDefinition region) => new()
    {
        Id = region.Id,
        Name = region.Name,
        Nation = region.Nation,
        OriginalOwner = region.OriginalOwner,
        Settlement = region.Settlement,
        VictoryPoints = region.VictoryPoints,
        Coastal = region.Coastal,
        Neighbors = [.. region.Neighbors]
    };
}
