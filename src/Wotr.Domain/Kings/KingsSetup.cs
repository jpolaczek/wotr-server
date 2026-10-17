using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsSetup
{
    public static void Initialize(MatchState state)
    {
        if (!state.Configuration.Kings || state.Phase != GamePhase.Setup || state.Kings is not null)
            throw new InvalidOperationException("Kings must be initialized once during setup.");
        state.Kings = new();
        KingsDice.Setup(state);
        foreach (var definition in SovereignCatalog.All.Values)
        {
            if (state.Configuration.Fate && definition.Id is SovereignCatalog.Brand or SovereignCatalog.Dain)
                continue;
            state.Kings.Sovereigns.Add(new() { Id = definition.Id });
            state.Units.Add(new() { Id = definition.Id, Nation = definition.Nation, Kind = UnitKind.Leader, Location = definition.Home });
        }
        foreach (var tile in KingsTiles.Definitions)
            state.HuntTiles.Add(new() { Id = tile.Id });
    }
}
