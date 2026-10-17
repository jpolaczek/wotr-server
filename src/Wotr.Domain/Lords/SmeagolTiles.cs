using Wotr.Contracts;

namespace Wotr.Domain;

public static class SmeagolTiles
{
    public const string Pool = "smeagol";
    public static IReadOnlyList<HuntTileDefinition> Definitions { get; } = Enumerable.Range(1, 4)
        .Select(n => new HuntTileDefinition { Id = $"lords.hunt.smeagol.{n}", Pool = Pool, Value = "0" }).ToArray();

    public static void Initialize(MatchState state)
    {
        if (!state.Configuration.Lords || state.Phase != GamePhase.Setup || state.HuntTiles.Any(t => HuntPool.Definitions[t.Id].Pool == Pool))
            throw new InvalidOperationException("Sméagol tiles must be initialized once during Lords setup.");
        for (var i = 0; i < Definitions.Count; i++)
            state.HuntTiles.Add(new() { Id = Definitions[i].Id, Location = i < 2 ? "pool" : "set-aside" });
    }

    // The tile's entry/discard happens before any Hunt replacement or protection window.
    // Returning false asks the common draw path for another physical tile, including for event draws.
    public static bool ResolveDraw(MatchState state, TileState tile)
    {
        if (!state.Configuration.Lords || HuntPool.Definitions[tile.Id].Pool != Pool)
            return true;
        if (!SmeagolRules.CanEnter(state))
        {
            tile.Location = "discard";
            state.Events.Add(new() { Revision = state.Revision, Kind = "HuntTileIgnored", Text = $"{tile.Id}: Sméagol cannot return; draw another tile." });
            return false;
        }
        tile.Location = "removed";
        SmeagolRules.Enter(state);
        return true;
    }
}
