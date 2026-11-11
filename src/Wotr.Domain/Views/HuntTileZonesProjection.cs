using Wotr.Contracts;

namespace Wotr.Domain;

internal sealed record HuntTileFacts(List<HuntTileZoneView> Zones, List<HuntTileFaceView> Unassigned);

internal static class HuntTileZonesProjection
{
    internal static HuntTileFacts For(MatchState state, Side viewer)
    {
        var unseen = KingsProjection.UnseenTiles(state, viewer).ToHashSet(StringComparer.Ordinal);
        // A known pool total cannot reveal which of its numeric faces was placed
        // face down. Keep the whole uncertain multiset together, without IDs.
        var hidden = state.HuntTiles.Where(t => unseen.Contains(t.Id) || unseen.Count > 0 &&
            t.Location == "pool" && KingsTiles.IsNumeric(t.Id) && !KingsTiles.IsKings(t.Id))
            .Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        var zones = state.HuntTiles.GroupBy(t => t.Location).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new HuntTileZoneView
            {
                Location = g.Key,
                Tiles = HuntPoolProjection.Faces(g.Where(t => !hidden.Contains(t.Id)).Select(t => t.Id)),
                HiddenCount = g.Count(t => hidden.Contains(t.Id)),
                SpecialTileIds = g.Where(t => !hidden.Contains(t.Id) && HuntPool.Definitions[t.Id].Pool != "standard")
                    .Select(t => t.Id).Order(StringComparer.Ordinal).ToList()
            }).ToList();
        return new(zones, HuntPoolProjection.Faces(hidden));
    }
}
