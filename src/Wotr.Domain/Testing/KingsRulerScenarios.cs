using Wotr.Contracts;
using static Wotr.Domain.KingsScenarioSetup;

namespace Wotr.Domain;

// TEST-13: the Ruler die and the Ruler tokens beyond the existing Ruler scenarios (KINGS pp. 6-10, 17; rules T29, X02):
// each king's token turning a Ruler result into his second ability at once (Denethor's is kings-fp-ruler-token), the
// Elven Rings and a token excluding each other in a turn, the Ruler die leaving at the next Recover once its last king
// fell, and a spent token becoming a Dismay token when Lords joins Kings.
public static class KingsRulerScenarios
{
    private const string Group = "Kings · Ruler dice";

    private static MatchConfiguration Kings => new() { Kings = true };

    // Each scenario plays Kings alone unless it names Lords as well.
    public static IEnumerable<TestScenario> All() => Scenarios().Select(scenario =>
        scenario.Modules.Count > 0 ? scenario : scenario with { Modules = [ExpansionModules.Kings] });

    private static IEnumerable<TestScenario> Scenarios() =>
    [
        Token("kings-ruler-token-brand", "Brand's Ruler token: Call to Arms", SovereignCatalog.Brand, DieFace.Event,
            "Brand is Awakened in Dale and the North is at war; the Ruler die shows an Event: his token turns it into Muster for " +
            "Call to Arms in the same action (two free North settlements and his region), and the token leaves the game."),
        Token("kings-ruler-token-dain", "Dáin's Ruler token: Iron Guard", SovereignCatalog.Dain, DieFace.Event,
            "Dáin is Awakened in Erebor and the Dwarves are at war; the Ruler die shows an Event: his token turns it into Muster " +
            "for Iron Guard in the same action (Erebor and another free Dwarven settlement), and the token leaves the game."),
        Token("kings-ruler-token-theoden", "Théoden's Ruler token: Mustering of the Mark", SovereignCatalog.Theoden, DieFace.Event,
            "Théoden is Awakened in Edoras and Rohan is at war; the Ruler die shows an Event: his token turns it into Muster for " +
            "Mustering of the Mark in the same action, and the token leaves the game."),
        new("kings-ruler-token-thranduil", Group, "Thranduil's Ruler token: Far-sighted",
            "Thranduil is Awakened in the Woodland Realm, the Ruler die shows Muster and three dice wait in the Hunt Box: his " +
            "token turns the die into a Character for Far-sighted at once — the Fellowship's move rolls one Hunt die fewer, or a " +
            "separation goes one region further.",
            Side.FreePeoples, ThranduilToken),
        new("kings-ruler-token-or-ring", Group, "A Ruler token or an Elven Ring",
            "Denethor is Awakened in Minas Tirith, the Ruler die shows Muster and the Free Peoples hold their Elven Rings: using " +
            "a Ring this turn blocks the token until the next turn, and using the token blocks the Rings.",
            Side.FreePeoples, TokenOrRing),
        new("kings-ruler-die-recovery", Group, "The Ruler die leaves at the next Recover",
            "Théoden was the only Awakened king and has fallen this turn: the Free Peoples' Ruler die keeps its Event and can " +
            "still be used now, but at the next turn's Recover it leaves play, while the Black Serpent keeps the Shadow's Ruler " +
            "die. The Shadow has no die left: use both Free Peoples dice to end the turn.",
            Side.FreePeoples, Recovery),
        new("kings-dismay-from-token", Group, "Dismay from a spent Ruler token",
            "Lords and Kings: Denethor is Awakened in Minas Tirith and the Ruler die shows Muster; his token turns it into an " +
            "Event for a draw with the Palantír of Anárion, and the spent token goes to the Shadow as a Dismay token.",
            Side.FreePeoples, DismayFromToken) { Modules = [ExpansionModules.Kings, ExpansionModules.Lords] }
    ];

    // A king's token for a recruiting ability: his nation at war and the Ruler die on a face his ability cannot use.
    private static TestScenario Token(string id, string title, string sovereign, DieFace face, string description) =>
        new(id, Group, title, description, Side.FreePeoples, (scenario, random) => Turn(scenario, random)
            .AtWar(SovereignCatalog.All[sovereign].Nation)
            .Change(state =>
            {
                Awaken(state, sovereign);
                Ruler(state, Side.FreePeoples, face);
            })
            .Act(Side.FreePeoples));

    private static MatchState ThranduilToken(string id, IRandomSource random) => ScenarioBoard.Setup(id, random, Kings)
        .Dice(Side.FreePeoples, DieFace.Army, DieFace.Muster, DieFace.Event, DieFace.Army)
        .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event)
        .Hunt(2, 1)
        .Change(state =>
        {
            Awaken(state, SovereignCatalog.Thranduil);
            Ruler(state, Side.FreePeoples, DieFace.Muster);
        })
        .Act(Side.FreePeoples);

    private static MatchState TokenOrRing(string id, IRandomSource random) => Turn(id, random)
        .Change(state =>
        {
            Awaken(state, SovereignCatalog.Denethor);
            Ruler(state, Side.FreePeoples, DieFace.Muster);
        })
        .Act(Side.FreePeoples);

    // Théoden awakened in an earlier turn, so the Ruler die was rolled this turn; he fell since. The Free Peoples keep one
    // standard Event die and the Shadow none, so the turn ends after their two actions.
    private static MatchState Recovery(string id, IRandomSource random) => ScenarioBoard.Setup(id, random, Kings)
        .Dice(Side.FreePeoples, DieFace.Event)
        .Change(state =>
        {
            Awaken(state, SovereignCatalog.Theoden);
            CharacterLifecycle.Eliminate(state, state.Characters.Single(c => c.Id == SovereignCatalog.Theoden));
            Ruler(state, Side.FreePeoples, DieFace.Event);
            ExpansionBoard.Put(state, KingsChieftains.Serpent, "near-harad");
        })
        .Act(Side.FreePeoples);

    // The real Lords and Kings setup up to the first action, so the special dice and Dismay are in play.
    private static MatchState DismayFromToken(string id, IRandomSource random)
    {
        var state = ExpansionBoard.Actions(id, random, new() { Lords = true, Kings = true });
        Awaken(state, SovereignCatalog.Denethor);
        Ruler(state, Side.FreePeoples, DieFace.Muster);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        return state;
    }

    private static ScenarioBoard Turn(string id, IRandomSource random) => ScenarioBoard.Setup(id, random, Kings)
        .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
        .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.Army)
        .Hunt(1, 1);
}
