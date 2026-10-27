using Wotr.Contracts;
using static Wotr.Domain.KingsScenarioSetup;

namespace Wotr.Domain;

// TEST-13: a corruption attempt at each step a player wants to see (KINGS pp. 10-13; rules T28): three tiles to choose
// from, the Kings tile ending the draw, a king pushed to his resistance; then a Corrupted king's weaknesses — the
// recruitment bans of all five, Théoden's Mark of the White Hand and the siege weakness of Dáin, Denethor and Thranduil.
public static class KingsCorruptionScenarios
{
    private const string Sovereigns = "Kings · Sovereigns and corruption";
    private const string Corrupted = "Kings · Corrupted sovereigns";

    private static MatchConfiguration Kings => new() { Kings = true };

    public static IEnumerable<TestScenario> All() => Scenarios().Select(scenario => scenario with { Modules = [ExpansionModules.Kings] });

    private static IEnumerable<TestScenario> Scenarios() =>
    [
        new("kings-corruption-three-tiles", Sovereigns, "Corruption attempt: choose one of three tiles",
            "Four Eyes wait in the Hunt Box and the Free Peoples still have dice: spending one Eye leaves three, so the attempt " +
            "draws three tiles; the Shadow keeps one face down on the king and the others are revealed and return. The Kings " +
            "tiles wait in the discard pile (see kings-corruption-kings-tile).",
            Side.Shadow, ThreeTiles),
        new("kings-corruption-kings-tile", Sovereigns, "Corruption attempt: the Kings tile",
            "Three Eyes wait in the Hunt Box and the Hunt Pool holds only the two Kings tiles: the first tile drawn ends the " +
            "draw at once, stands face up on the king and counts zero toward his resistance.",
            Side.Shadow, KingsTile),
        new("kings-corruption-reaches-resistance", Sovereigns, "Corruption attempt: Brand is corrupted",
            "Brand (resistance 2) carries a Corruption tile, two Eyes wait in the Hunt Box and the Hunt Pool holds only tiles of " +
            "1 to 3 and Eyes: any tile drawn corrupts him. All his tiles are revealed, the highest is discarded and the others " +
            "return; his figure leaves Dale's army and his Corrupted marker stands there.",
            Side.Shadow, Resistance),
        new("kings-corrupted-recruitment", Corrupted, "Corrupted kings forbid recruits",
            "Every king is Corrupted and every Free Peoples nation at war; four Muster dice: a Muster recruits no North Leader " +
            "or Elite anywhere, no Rohan Leader or Elite in Rohan's regions, no Leader or Elite in Erebor or Minas Tirith and no " +
            "Elite in the Woodland Realm. Regulars and the other regions still recruit.",
            Side.FreePeoples, Recruitment),
        new("kings-theoden-white-hand", Corrupted, "Théoden's Mark of the White Hand",
            "Théoden is Corrupted and an Isengard army holds Helm's Deep: while he stays Corrupted it is an Isengard Stronghold, " +
            "so the Shadow's Muster die recruits Isengard units there.",
            Side.Shadow, WhiteHand),
        Siege("kings-siege-lonely-mountain", "Dáin's Fate of the Lonely Mountain", SovereignCatalog.Dain, "Erebor"),
        Siege("kings-siege-white-tower", "Denethor's Fate of the White Tower", SovereignCatalog.Denethor, "Minas Tirith"),
        Siege("kings-siege-mirkwood", "Thranduil's Fate of Mirkwood", SovereignCatalog.Thranduil, "the Woodland Realm")
    ];

    private static TestScenario Siege(string id, string title, string sovereign, string home) => new(id, Corrupted, title,
        $"{SovereignCatalog.All[sovereign].Title} is Corrupted and a Sauron army with an Elite besieges {home}; the first round " +
        "passed without hits: continue the siege by removing a participating Sauron Regular instead of downgrading the Elite, " +
        "once in this battle.",
        Side.Shadow, (scenario, random) => SiegeContinuation(scenario, random, sovereign));

    private static MatchState ThreeTiles(string id, IRandomSource random) => Attempt(id, random, 3)
        .KeepHuntTiles(tile => !KingsTiles.IsKings(tile.Id))
        .Act(Side.Shadow);

    private static MatchState KingsTile(string id, IRandomSource random) => Attempt(id, random, 2)
        .KeepHuntTiles(tile => KingsTiles.IsKings(tile.Id))
        .Act(Side.Shadow);

    private static MatchState Resistance(string id, IRandomSource random) => Attempt(id, random, 1)
        .KeepHuntTiles(CountingTiles)
        .Change(state => Tiles(state, SovereignCatalog.Brand, 1))
        .Act(Side.Shadow);

    private static MatchState Recruitment(string id, IRandomSource random) => ScenarioBoard.Setup(id, random, Kings)
        .Dice(Side.FreePeoples, DieFace.Muster, DieFace.Muster, DieFace.Muster, DieFace.Muster)
        .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.Army)
        .Hunt(1, 1)
        .AtWar("north", "elves", "dwarves", "gondor", "rohan")
        .Change(state =>
        {
            foreach (var sovereign in SovereignCatalog.All.Keys)
                Corrupt(state, sovereign);
        })
        .Act(Side.FreePeoples);

    private static MatchState WhiteHand(string id, IRandomSource random) => ScenarioBoard.Setup(id, random, Kings)
        .Dice(Side.Shadow, DieFace.Muster, DieFace.Muster, DieFace.Army, DieFace.Character, DieFace.Event)
        .Hunt(1, 0)
        .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
        .AtWar("isengard")
        .Conquer(Side.Shadow, "isengard", KingsBoard.HelmsDeep)
        .Change(state => Corrupt(state, SovereignCatalog.Theoden))
        .Act(Side.Shadow);

    // The Shadow besieges the Corrupted king's home and rolls only misses in the first round (ExpansionBoard's source).
    private static MatchState SiegeContinuation(string id, IRandomSource random, string sovereign)
    {
        var definition = SovereignCatalog.All[sovereign];
        var board = ScenarioBoard.Setup(id, random, Kings)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.Army)
            .Hunt(1, 0)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
            .AtWar(definition.Nation, "sauron")
            .Change(state => Corrupt(state, sovereign))
            .Besiege(definition.Home, "sauron", 3)
            .Place("sauron", UnitKind.Elite, definition.Home);
        board.Act(Side.Shadow);
        var state = KingsBattleSteps.Attack(board.State, Side.Shadow, definition.Home, definition.Home);
        return KingsBattleSteps.RunTo(state, DecisionKind.SiegeContinuation);
    }

    // The Shadow's three other results with one allocated Eye and the given rolled Eyes in the Hunt Box; the Free Peoples
    // keep four unused dice, so an attempt is allowed.
    private static ScenarioBoard Attempt(string id, IRandomSource random, int rolledEyes) => ScenarioBoard.Setup(id, random, Kings)
        .Dice(Side.Shadow, DieFace.Character, DieFace.Army, DieFace.Muster)
        .Hunt(1, rolledEyes)
        .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event);
}
