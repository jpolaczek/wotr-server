using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class FateBoard
{
    private static readonly IReadOnlyDictionary<string, RegionDefinition> Standard =
        FateSetupCatalog.Data.Regions.Select(Complete).ToDictionary(r => r.Id);
    private static readonly IReadOnlyDictionary<string, RegionDefinition> NewCities =
        FateSetupCatalog.Data.NewCities.Select(Complete).ToDictionary(r => r.Id);

    public static RegionDefinition Region(MatchConfiguration configuration, RegionDefinition printed)
    {
        if (!configuration.Fate)
            return printed;
        if (Standard.TryGetValue(printed.Id, out var replacement))
            return replacement;
        return configuration.NewCities && NewCities.TryGetValue(printed.Id, out replacement) ? replacement : printed;
    }

    private static RegionDefinition Complete(RegionDefinition change)
    {
        var printed = BoardRules.Regions[change.Id];
        return new()
        {
            Id = printed.Id,
            Name = printed.Name,
            Nation = change.Nation,
            OriginalOwner = change.OriginalOwner,
            Settlement = change.Settlement,
            VictoryPoints = change.VictoryPoints,
            Coastal = printed.Coastal,
            Neighbors = [.. printed.Neighbors]
        };
    }
}
