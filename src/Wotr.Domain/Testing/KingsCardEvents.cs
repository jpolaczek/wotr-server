using Wotr.Contracts;
using static Wotr.Domain.KingsScenarioSetup;

namespace Wotr.Domain;

// TEST-13: the board each Kings event needs so that every clause acts, "if" clauses included (UI-300): a king with tiles
// to return, a Corrupted king to remove, Companions to move to a king, a Nazgûl beside a king, Eyes and a Hunt Pool whose
// tiles count, a battle the table card forbids. The clause names the branch when the card has two and another scenario
// shows the other one (kings-card-recruit-or-cleanse). The conditions are the engine's own (Kings/Cards/*.cs).
public sealed record KingsCardEvent(string Clause, string Description, Action<ScenarioBoard> Arrange);

public static class KingsCardEvents
{
    private const string Boromir = "base.character.boromir";
    private const string Gimli = "base.character.gimli";
    private const string Merry = "base.character.merry";
    private const string MinasTirith = "minas-tirith";

    public static IReadOnlyDictionary<string, KingsCardEvent> Setups { get; } = new Dictionary<string, KingsCardEvent>
    {
        [KingsFreePeoplesCards01.Aid] = new("",
            "Gimli has left the Fellowship to Dale, beside Brand and next to Erebor, and Merry to Westemnet, next to Edoras; " +
            "Dáin carries a Corruption tile. Move them, then awaken a king a Companion stands with: he awakens there and his tiles return.",
            board => board.Companion(Gimli, "dale").Companion(Merry, "westemnet").Change(state => Tiles(state, SovereignCatalog.Dain, 1))),
        [KingsFreePeoplesCards01.Riders] = Removal(SovereignCatalog.Theoden, "Edoras"),
        [KingsFreePeoplesCards01.Brand] = Removal(SovereignCatalog.Brand, "Dale"),
        [KingsCards02.Dain] = Removal(SovereignCatalog.Dain, "Erebor"),
        [KingsCards02.Thranduil] = Removal(SovereignCatalog.Thranduil, "the Woodland Realm"),
        [KingsCards02.Service] = new("",
            "Dáin carries two Corruption tiles in Erebor: choose him, recruit a Dwarven Regular or Elite there, and then one of " +
            "his tiles returns at random.",
            board => board.Change(state => Tiles(state, SovereignCatalog.Dain, 2))),
        [KingsCards02.Villages] = new("",
            "Every Free Peoples nation has free settlements and figures in the reserve: recruit a Regular or Leader of four " +
            "different nations, each in a settlement of its own nation. No nation is at war or activated by it.",
            _ => { }),
        [KingsCards02.Welcome] = new("",
            "Gondor is active but not at war, and Boromir stands with Denethor, who carries a Corruption tile, in Minas Tirith. " +
            "Switch side after the play: the Muster die no longer awakens Denethor, advancing Gondor returns no tile, and the " +
            "Character die discards the card because Boromir shares Denethor's region.",
            board => board.Activate("gondor").Companion(Boromir, MinasTirith).Change(state => Tiles(state, SovereignCatalog.Denethor, 1))),
        [KingsCards02.West] = new("",
            "Brand carries a Corruption tile in Dale and a Nazgûl stands next door in Northern Rhovanion; three Eyes wait in the " +
            "Hunt Box, the Free Peoples have no die left and the Hunt Pool holds only tiles of 1 to 3 and Eyes: the attempt " +
            "draws three tiles without spending an Eye, and the tile kept corrupts Brand.",
            board => board.Place("sauron", UnitKind.Nazgul, "northern-rhovanion").NoActions(Side.FreePeoples).KeepHuntTiles(CountingTiles)
                .Change(state =>
                {
                    Tiles(state, SovereignCatalog.Brand, 1);
                    Eyes(state, 2);
                })),
        [KingsCards03.Valinor] = new("",
            "Théoden and Denethor are Corrupted, so the card needs no captured Elven Stronghold and every 5 or 6 hits: the " +
            "Elves of Rivendell, the Grey Havens, Lórien and the Woodland Realm roll for their armies.",
            board => board.Change(state =>
            {
                Corrupt(state, SovereignCatalog.Theoden);
                Corrupt(state, SovereignCatalog.Denethor);
            })),
        [KingsCards03.Denethor] = new("",
            "Gondor is at war and Boromir stands with Denethor in Minas Tirith; three Sauron Regulars hold Osgiliath next door. " +
            "Switch side after the play: the Muster die no longer awakens Denethor and any die discards the card (Boromir shares " +
            "his region). Keep it, and the Shadow's second Army die attacks Minas Tirith, where the Free Peoples play no Combat card.",
            board => board.AtWar("gondor", "sauron").Companion(Boromir, MinasTirith).Clear("osgiliath")
                .Place("sauron", UnitKind.Regular, "osgiliath", 3)),
        [KingsCards03.Threats] = new("",
            "The Elves are at war, so Thranduil could awaken with the Free Peoples' Muster die; Brand carries a Corruption tile, " +
            "two Eyes wait in the Hunt Box and the Hunt Pool holds only tiles of 1 to 3 and Eyes. After the play the Muster die " +
            "neither awakens a king nor advances a passive nation; a corruption attempt on Brand then corrupts him and discards the card.",
            board => board.AtWar("elves").KeepHuntTiles(CountingTiles).Change(state =>
            {
                Tiles(state, SovereignCatalog.Brand, 1);
                Eyes(state, 1);
            })),
        [KingsCards03.Stormcrow] = new("",
            "Boromir has left the Fellowship to Minas Tirith, in Gondor, two steps from war: choose Gondor, which steps back; " +
            "Denethor, not Awakened, draws a Corruption tile and the Free Peoples eliminate one of Gondor's army units. The " +
            "Fellowship in Rivendell offers the Elves as well.",
            board => board.Companion(Boromir, MinasTirith)),
        [KingsCards03.King] = new("",
            "Denethor and Théoden are Awakened, so the card needs no Aragorn, and Minas Morgul stands empty: recruit five Sauron " +
            "Regulars and a Nazgûl there, then a Regular or a Nazgûl for each Awakened king (two).",
            board => board.Clear("minas-morgul").Change(state =>
            {
                Awaken(state, SovereignCatalog.Denethor);
                Awaken(state, SovereignCatalog.Theoden);
            })),
        [KingsCards03.Palantir] = new("",
            "Play it with a Muster die to keep it on the table; the Shadow's second Muster die then pays for Denethor's Folly or a " +
            "draw as an Event die, once this turn. Switch side: the Free Peoples may discard it with their Will of the West die, " +
            "or with any die and an Elven Ring.",
            board => board.Dice(Side.Shadow, DieFace.Muster, DieFace.Event, DieFace.Muster)
                .Dice(Side.FreePeoples, DieFace.WillOfTheWest, DieFace.Character, DieFace.Army, DieFace.Event)
                .Hand(Side.Shadow, KingsCards03.Denethor)),
        [KingsCards04.Enemy] = new("",
            "Théoden is Corrupted at Edoras, where Rohan's army stands: choose him and three dice hit that army on 4 or more; " +
            "the other kings' regions hit on 5 or more.",
            board => board.Change(state => Corrupt(state, SovereignCatalog.Theoden)))
    };

    // The card's first clause: a Corrupted king of its nation is removed, which ends his weakness, and nothing else follows.
    private static KingsCardEvent Removal(string sovereign, string region)
    {
        var title = SovereignCatalog.All[sovereign].Title;
        return new($"remove the Corrupted {title}",
            $"{title} is Corrupted at {region}: the card removes him, which ends his weakness, and draws no card. The recruit or " +
            "cleanse branch is kings-card-recruit-or-cleanse.",
            board => board.Change(state => Corrupt(state, sovereign)));
    }
}
