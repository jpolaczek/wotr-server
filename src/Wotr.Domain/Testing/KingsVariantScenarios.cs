using Wotr.Contracts;
using static Wotr.Domain.KingsScenarioSetup;

namespace Wotr.Domain;

// TEST-13: the three tournament variants of Kings (KINGS pp. 17-18; rules X02, T31), each in the game configuration
// that enables it: Visible Corruption Tiles, Sequential Corruption Tile Draw and Removing Ruler Tokens (Lords, Kings and
// Dismay), the last built on the Dismay scenario's turn-2 roll.
public static class KingsVariantScenarios
{
    private const string Group = "Kings · Variants";

    // Each scenario plays Kings alone unless it names Lords as well.
    public static IEnumerable<TestScenario> All() => Scenarios().Select(scenario =>
        scenario.Modules.Count > 0 ? scenario : scenario with { Modules = [ExpansionModules.Kings] });

    private static IEnumerable<TestScenario> Scenarios() =>
    [
        new("kings-variant-visible-corruption", Group, "Visible Corruption Tiles",
            "Variant: Dáin carries two Corruption tiles whose values both sides see; two Eyes wait in the Hunt Box, and a " +
            "corruption attempt places its tile face up.",
            Side.Shadow, VisibleCorruption),
        new("kings-variant-sequential-corruption", Group, "Sequential Corruption Tile Draw",
            "Variant: four Eyes wait in the Hunt Box, so an attempt may draw up to three tiles, one at a time; stopping keeps " +
            "the last tile drawn, not the best one. The Kings tiles wait in the discard pile.",
            Side.Shadow, SequentialCorruption),
        new("kings-variant-removing-ruler-tokens", Group, "Removing Ruler Tokens",
            "Variant, with Lords and Kings: no Ruler tokens are used, so the Awakened Thranduil holds none; Théoden fell as an " +
            "Awakened king, which gave the Shadow a Dismay token. Turn 2 after the roll: a click on the Free Peoples' glowing " +
            "Ruler die spends it and re-rolls the die, Pass keeps it.",
            Side.Shadow, (id, random) => KingsDecisionScenarios.Dismay(id, random,
                new() { Lords = true, Kings = true, RemovingRulerTokens = true })) { Modules = [ExpansionModules.Kings, ExpansionModules.Lords] }
    ];

    private static MatchState VisibleCorruption(string id, IRandomSource random) =>
        Attempt(id, random, new() { Kings = true, VisibleCorruption = true }, 1)
            .Change(state => Tiles(state, SovereignCatalog.Dain, 2))
            .Act(Side.Shadow);

    private static MatchState SequentialCorruption(string id, IRandomSource random) =>
        Attempt(id, random, new() { Kings = true, SequentialCorruption = true }, 3)
            .KeepHuntTiles(tile => !KingsTiles.IsKings(tile.Id))
            .Act(Side.Shadow);

    // The Shadow's three other results with one allocated Eye and the given rolled Eyes; the Free Peoples keep four dice.
    private static ScenarioBoard Attempt(string id, IRandomSource random, MatchConfiguration configuration, int rolledEyes) =>
        ScenarioBoard.Setup(id, random, configuration)
            .Dice(Side.Shadow, DieFace.Character, DieFace.Army, DieFace.Muster)
            .Hunt(1, rolledEyes)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event);
}
