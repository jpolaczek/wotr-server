using Wotr.Contracts;
using static Wotr.Domain.KingsScenarioSetup;

namespace Wotr.Domain;

// TEST-13: the sovereigns' steps the earlier scenarios do not reach (KINGS pp. 8-13; rules T28): an awakening paid by the
// Ruler die with its journey home and its tiles returned, the Awakened kings' abilities with the dice that pay for them,
// Théoden's first-round Leadership, and a tile returned by politics. Each starts on turn 1 of a Kings game with the
// side to act holding what pays for the step.
public static class KingsSovereignScenarios
{
    private const string Sovereigns = "Kings · Sovereigns and corruption";
    private const string Abilities = "Kings · Sovereign abilities";
    private const string Boromir = "base.character.boromir";
    private const string Legolas = "base.character.legolas";

    private static MatchConfiguration Kings => new() { Kings = true };

    public static IEnumerable<TestScenario> All() => Scenarios().Select(scenario => scenario with { Modules = [ExpansionModules.Kings] });

    private static IEnumerable<TestScenario> Scenarios() =>
    [
        new("kings-fp-awaken-ruler-die", Sovereigns, "Awaken a sovereign with the Ruler die",
            "Thranduil is Awakened, so the Free Peoples' Ruler die is in play, showing an Event; no other die shows Muster. Rohan " +
            "is at war and Théoden, carrying two Corruption tiles, has gone to Westemnet: the Ruler die awakens him, he travels " +
            "home to Edoras within his Level and his tiles return to the Hunt Pool.",
            Side.FreePeoples, AwakenWithRuler),
        new("kings-politics-cleanse", Sovereigns, "Politics return a Corruption tile",
            "Boromir, with Gondor's icon, stands with Denethor, who carries two Corruption tiles, in Minas Tirith: advance Gondor " +
            "with the Muster die, and the Free Peoples may return one of Denethor's tiles at random; Pass keeps them.",
            Side.FreePeoples, PoliticsCleanse),
        new("kings-brand-call-to-arms", Abilities, "Brand's Call to Arms",
            "Brand is Awakened in Dale and the North is at war; the Ruler die shows Muster: recruit a Regular or Leader in two " +
            "different free North settlements and one more in Brand's region.",
            Side.FreePeoples, (id, random) => RulerAbility(id, random, SovereignCatalog.Brand, DieFace.Muster)),
        new("kings-dain-iron-guard", Abilities, "Dáin's Iron Guard",
            "Dáin is Awakened in Erebor and the Dwarves are at war; the Ruler die shows Muster: recruit a Regular or Elite in " +
            "Erebor and a Regular or Leader in another free Dwarven settlement.",
            Side.FreePeoples, (id, random) => RulerAbility(id, random, SovereignCatalog.Dain, DieFace.Muster)),
        new("kings-sovereign-upgrades", Abilities, "Dwarvenking, Stewardship and Elvenking",
            "Dáin, Denethor and Thranduil are Awakened at home and their nations are at war; three Muster dice: each king's " +
            "ability returns a Regular of his army to the reserve and recruits an Elite in its place.",
            Side.FreePeoples, Upgrades),
        new("kings-theoden-forth-eorlingas", Abilities, "Théoden's Forth Eorlingas",
            "Isengard attacks the Rohan army at the Fords of Isen, where the Awakened Théoden stands: his Leadership is 3 in the " +
            "first round of each battle and 1 after, with no card to give up. The battle waits at the Free Peoples' Combat card choice.",
            Side.FreePeoples, (id, random) => KingsCardBattles.FreePeoplesDefence(id, random, "")),
        new("kings-far-sighted-hunt", Abilities, "Thranduil's Far-sighted against the Hunt",
            "Thranduil is Awakened in the Woodland Realm, the Ruler die shows a Character, three dice wait in the Hunt Box and " +
            "Legolas has left the Fellowship to Lórien: Far-sighted moves the Fellowship with one Hunt die fewer, or moves or " +
            "separates companions one region further. The existing kings-far-sighted shows where the ability is clicked.",
            Side.FreePeoples, FarSightedHunt)
    ];

    private static MatchState AwakenWithRuler(string id, IRandomSource random) => Turn(id, random,
            DieFace.Character, DieFace.Army, DieFace.Character, DieFace.Event)
        .AtWar("rohan")
        .Change(state =>
        {
            Awaken(state, SovereignCatalog.Thranduil);
            Ruler(state, Side.FreePeoples, DieFace.Event);
            state.Units.Single(u => u.Id == SovereignCatalog.Theoden).Location = "westemnet";
            Tiles(state, SovereignCatalog.Theoden, 2);
        })
        .Act(Side.FreePeoples);

    private static MatchState PoliticsCleanse(string id, IRandomSource random) => Turn(id, random,
            DieFace.Muster, DieFace.Character, DieFace.Army, DieFace.Event)
        .Companion(Boromir, "minas-tirith")
        .Change(state => Tiles(state, SovereignCatalog.Denethor, 2))
        .Act(Side.FreePeoples);

    // The Awakened king's nation is at war and the Ruler die shows the face his second ability needs.
    private static MatchState RulerAbility(string id, IRandomSource random, string sovereign, DieFace face) => Turn(id, random,
            DieFace.Character, DieFace.Army, DieFace.Event, DieFace.Character)
        .AtWar(SovereignCatalog.All[sovereign].Nation)
        .Change(state =>
        {
            Awaken(state, sovereign);
            Ruler(state, Side.FreePeoples, face);
        })
        .Act(Side.FreePeoples);

    private static MatchState FarSightedHunt(string id, IRandomSource random) => ScenarioBoard.Setup(id, random, Kings)
        .Dice(Side.FreePeoples, DieFace.Army, DieFace.Muster, DieFace.Event, DieFace.Army)
        .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event)
        .Hunt(2, 1)
        .Companion(Legolas, "lorien")
        .Change(state =>
        {
            Awaken(state, SovereignCatalog.Thranduil);
            Ruler(state, Side.FreePeoples, DieFace.Character);
        })
        .Act(Side.FreePeoples);

    private static MatchState Upgrades(string id, IRandomSource random) => Turn(id, random,
            DieFace.Muster, DieFace.Muster, DieFace.Muster, DieFace.Event)
        .AtWar("dwarves", "gondor", "elves")
        .Change(state =>
        {
            Awaken(state, SovereignCatalog.Dain);
            Awaken(state, SovereignCatalog.Denethor);
            Awaken(state, SovereignCatalog.Thranduil);
        })
        .Act(Side.FreePeoples);

    // Turn 1 of a Kings game: the Free Peoples' four results, and the Shadow's five with an Eye and a die in the Hunt Box.
    private static ScenarioBoard Turn(string id, IRandomSource random, params DieFace[] freePeoples) =>
        ScenarioBoard.Setup(id, random, Kings)
            .Dice(Side.FreePeoples, freePeoples)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.Army)
            .Hunt(1, 1);
}
