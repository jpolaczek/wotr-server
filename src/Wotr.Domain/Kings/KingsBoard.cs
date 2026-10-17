using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsBoard
{
    public const string HelmsDeep = "helms-deep";
    private static readonly RegionDefinition ShadowHelmsDeep = CreateHelmsDeep();

    public static bool WhiteHand(MatchState state) => state.Control.GetValueOrDefault(HelmsDeep) == Side.Shadow &&
        KingsWeaknesses.Active(state, SovereignCatalog.Theoden);

    public static RegionDefinition Region(MatchState state, RegionDefinition printed) =>
        printed.Id == HelmsDeep && WhiteHand(state) ? ShadowHelmsDeep : printed;

    private static RegionDefinition CreateHelmsDeep()
    {
        var printed = BoardRules.Regions[HelmsDeep];
        return new()
        {
            Id = printed.Id, Name = printed.Name, Nation = "isengard", OriginalOwner = Side.Shadow,
            Settlement = "Stronghold", VictoryPoints = printed.VictoryPoints,
            Coastal = printed.Coastal, Neighbors = [.. printed.Neighbors]
        };
    }

    // Board plans need the active modifiers, never the live corruption pool or lifecycle.
    internal static KingsState? PlanningContext(MatchState state) => state.Configuration.Kings && state.Kings is not null
        ? new() { Sovereigns = state.Kings.Sovereigns.Where(s => s.Status == SovereignStatus.Corrupted)
            .Select(s => new SovereignState { Id = s.Id, Status = s.Status, CorruptedRegion = s.CorruptedRegion }).ToList() }
        : null;
}
