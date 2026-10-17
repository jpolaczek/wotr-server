using Wotr.Contracts;

namespace Wotr.Domain;

// UI-141: the public composition of the Hunt pool for one viewer. Drawn tiles are revealed and special tiles enter
// openly, so the composition is public; the drawing order never leaves the server. Tiles the viewer saw leave the
// pool face down stay in its list (Unseen), so the list does not tell them apart.
public static class HuntPoolProjection
{
    private static readonly string[] PoolOrder = ["standard", "special-fp", "special-sh", SmeagolTiles.Pool];

    public static HuntPoolView For(MatchState state, Side viewer)
    {
        var pool = state.HuntTiles.Where(t => t.Location == "pool").Select(t => t.Id).ToList();
        var unseen = KingsProjection.UnseenTiles(state, viewer).ToList();
        return new()
        {
            Count = pool.Count,
            Unseen = unseen.Count,
            Tiles = Faces(pool.Concat(unseen)),
            Prepared = Faces(state.HuntTiles.Where(t => t.Location == "prepared").Select(t => t.Id))
        };
    }

    public static HuntTileFaceView Face(string id)
    {
        var definition = HuntPool.Definitions[id];
        return new()
        {
            Pool = definition.Pool, Value = definition.Value, Reveal = definition.Reveal, Stop = definition.Stop,
            Source = id[..id.IndexOf('.')]
        };
    }

    private static List<HuntTileFaceView> Faces(IEnumerable<string> ids) => ids.Select(Face)
        .GroupBy(f => (f.Source, f.Pool, f.Value, f.Reveal, f.Stop))
        .Select(group => { var face = group.First(); face.Count = group.Count(); return face; })
        .OrderBy(f => Array.IndexOf(PoolOrder, f.Pool)).ThenBy(f => f.Source, StringComparer.Ordinal)
        .ThenByDescending(f => f.Value == "eye").ThenByDescending(f => int.TryParse(f.Value, out var value) ? value : -99)
        .ThenBy(f => f.Value, StringComparer.Ordinal).ThenBy(f => f.Reveal).ThenBy(f => f.Stop)
        .ToList();
}
