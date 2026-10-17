using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class HuntPool
{
    public static IReadOnlyDictionary<string, HuntTileDefinition> Definitions { get; } =
        BoardCatalog.LoadBase().HuntTiles.Concat(SmeagolTiles.Definitions).Concat(KingsTiles.Definitions).ToDictionary(t => t.Id);

    public static TileState Draw(MatchState state, IRandomSource random)
    {
        TileState tile;
        do
        {
            tile = DrawPhysical(state, random);
        } while (!SmeagolTiles.ResolveDraw(state, tile));
        return tile;
    }

    internal static TileState DrawPhysical(MatchState state, IRandomSource random)
    {
        var pool = state.HuntTiles.Where(t => t.Location == "pool").ToList();
        if (pool.Count == 0)
        {
            Refill(state);
            pool = state.HuntTiles.Where(t => t.Location == "pool").ToList();
        }
        if (pool.Count == 0)
            throw new InvalidOperationException("No eligible Hunt tiles remain.");
        var drawn = pool[random.Next(pool.Count)];
        drawn.Location = "drawn";
        return drawn;
    }

    public static void Discard(MatchState state, string id)
    {
        var tile = state.HuntTiles.Single(t => t.Id == id);
        if (tile.Location == "drawn")
            tile.Location = "spent";
    }

    public static void CompleteAction(MatchState state)
    {
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Location switch { "return-after-action" => "pool", "spent" => "discard", _ => tile.Location };
        if (!state.HuntTiles.Any(t => t.Location == "pool"))
            Refill(state);
    }

    private static void Refill(MatchState state)
    {
        foreach (var tile in state.HuntTiles.Where(t => t.Location == "discard" && Definitions[t.Id].Pool == "standard"))
            tile.Location = "pool";
    }

    public static void PrepareSpecial(MatchState state, string id)
    {
        var tile = state.HuntTiles.Single(t => t.Id == id);
        if (Definitions[id].Pool == "standard" || tile.Location != "set-aside")
            throw new InvalidOperationException("Only an unused special tile may enter play.");
        tile.Location = state.Fellowship.MordorStep.HasValue ? "pool" : "prepared";
    }

    public static void Remove(MatchState state, string id) => state.HuntTiles.Single(t => t.Id == id).Location = "removed";
}
