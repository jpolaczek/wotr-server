using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

// TEST-13: the Kings pieces a scenario prepares before its first decision, each as the game itself leaves them: a
// sovereign Corrupted at home, Awakened or carrying Corruption tiles, a chosen Ruler result, Eyes in the Hunt Box and a
// Hunt Pool that keeps only some tiles. The scenario files describe what they prepare in one sentence.
internal static class KingsScenarioSetup
{
    // A sovereign whose tiles reached his resistance earlier: no figure, his marker beside his home (KINGS pp. 12-13).
    public static SovereignState Corrupt(MatchState state, string id)
    {
        var sovereign = Sovereign(state, id);
        state.Units.RemoveAll(u => u.Id == id);
        sovereign.Status = SovereignStatus.Corrupted;
        sovereign.CorruptedRegion = SovereignCatalog.All[id].Home;
        return sovereign;
    }

    // A sovereign Awakened earlier, standing in the region where he awakened (his home unless named).
    public static CharacterState Awaken(MatchState state, string id, string? region = null)
    {
        SovereignLifecycle.Awaken(state, Sovereign(state, id), region ?? SovereignCatalog.All[id].Home);
        return state.Characters.Single(c => c.Id == id);
    }

    // Face-down Corruption tiles of the given value from the Hunt Pool, never a Kings tile.
    public static void Tiles(MatchState state, string id, int count, int value = 1)
    {
        var tiles = state.HuntTiles.Where(t => t.Location == "pool" && KingsTiles.IsNumeric(t.Id) && !KingsTiles.IsKings(t.Id) &&
            KingsTiles.CorruptionValue(t.Id) == value).Take(count).Select(t => t.Id).ToList();
        if (tiles.Count != count)
            throw new ArgumentException($"The Hunt Pool holds only {tiles.Count} tiles of value {value}.", nameof(count));
        foreach (var tile in tiles)
            CorruptionRules.Attach(state, Sovereign(state, id), tile);
    }

    // The side's Ruler die after the roll and its discard, showing a chosen result.
    public static DieState Ruler(MatchState state, Side side, DieFace face) =>
        ExpansionBoard.Show(state, KingsDice.Ruler(state, side)!.Id, face);

    // Shadow action dice moved from the end of the pool into the Hunt Box as Eyes, so the first dice keep their results.
    public static void Eyes(MatchState state, int count)
    {
        var dice = state.Dice.Where(d => d.Owner == Side.Shadow && d.Source == "standard" && d.Location == DieLocation.Available)
            .Reverse().Take(count).ToList();
        if (dice.Count != count)
            throw new ArgumentException($"The Shadow has only {dice.Count} dice to put in the Hunt Box.", nameof(count));
        foreach (var die in dice)
        {
            die.Location = DieLocation.Hunt;
            die.Face = DieFace.Eye;
            die.HuntOrigin = "rolled-eye";
        }
    }

    // A Hunt Pool of numeric tiles of 1 to 3 and the Eyes, so every Corruption tile drawn counts; the rest wait in the
    // discard pile, from which the pool refills as usual.
    public static Func<HuntTileDefinition, bool> CountingTiles => tile =>
        tile.Pool == "standard" && !KingsTiles.IsKings(tile.Id) && tile.Value is "1" or "2" or "3" or "eye";

    private static SovereignState Sovereign(MatchState state, string id) => state.Kings!.Sovereigns.Single(s => s.Id == id);
}
