using Wotr.Contracts;

namespace Wotr.Domain;

// TEST-09: the board each conditional Shadow event needs; the conditions are the engine's own (Cards/*.cs).
// UI-300: the board also lets every clause of the card act: a Fellowship the Nazgûl can reach, a card on the table to
// discard, Will of the West dice to discard.
public static class ShadowCardEvents
{
    private const string MithrilCoat = "base.fp.character.05";
    private const string Athelas = "base.fp.character.09";
    private const string WisdomOfElrond = "base.fp.strategy.08";
    private const string OrcsMultiplying = "base.sh.strategy.20";

    public static IReadOnlyDictionary<string, CardEventSetup> Setups { get; } = new Dictionary<string, CardEventSetup>
    {
        ["base.sh.character.01"] = MordorTile(),
        ["base.sh.character.02"] = MordorTile(),
        ["base.sh.character.03"] = MordorTile(),
        ["base.sh.character.04"] = MordorTile(),
        ["base.sh.character.05"] = Outside(),
        ["base.sh.character.06"] = Outside(),
        ["base.sh.character.07"] = Outside(),
        ["base.sh.character.08"] = Outside(),
        ["base.sh.character.09"] = Progress(),
        ["base.sh.character.10"] = Progress(),
        ["base.sh.character.11"] = new("The Fellowship stands in Hollin, outside any Stronghold, one step on (Progress 1); Mithril Coat and Sting lies on the Free Peoples table.",
            board => board.FellowshipAt("hollin", 1).Table(Side.FreePeoples, MithrilCoat)),
        ["base.sh.character.12"] = Revealed(),
        ["base.sh.character.13"] = Revealed(),
        ["base.sh.character.14"] = Revealed(),
        ["base.sh.character.15"] = new("Switch side and move the Fellowship with the Character die: five Shadow dice wait in the Hunt Box, it already moved twice this turn (+2) and the pool holds only damage tiles; take a Companion as the casualty. Athelas is in the Free Peoples hand.",
            board => board.HuntAhead(Side.Shadow, DieFace.Character).Hand(Side.FreePeoples, Athelas)),
        ["base.sh.character.16"] = new("Switch side and move the Fellowship with the Character die: before the Hunt roll the card may add +1. Five Shadow dice wait in the Hunt Box and the Fellowship already moved twice this turn.",
            board => board.HuntAhead(Side.Shadow, DieFace.Character)),
        ["base.sh.character.17"] = new("The Fellowship stands in Hollin, next to Moria. Switch side and move it with the Character die: the Hunt is almost certain and draws a Reveal tile, so its reveal may lead into Moria.",
            board => board.FellowshipAt("hollin").HuntAhead(Side.Shadow, DieFace.Character, tile => tile.Reveal && tile.Value is "1" or "2")),
        ["base.sh.character.19"] = new("A Gondor army holds North Ithilien next to the Minas Morgul army and its three Nazgûl.",
            board => board.AtWar("sauron", "gondor").Clear("osgiliath").Place("gondor", UnitKind.Regular, "north-ithilien", 2)
                .Place("sauron", UnitKind.Nazgul, "minas-morgul", 2)),
        ["base.sh.character.20"] = new("The Witch-king leads a Sauron army besieging Minas Tirith; Sauron and Gondor are at war.",
            board => board.AtWar("sauron", "gondor").Besiege("minas-tirith", "sauron", 3).Enter(CharacterRules.WitchKing, "minas-tirith")),
        ["base.sh.character.21"] = new("Saruman is in play in Orthanc; Orcs Multiplying Again waits in hand for the Event die.",
            board => board.Enter(CharacterRules.Saruman, "orthanc").Hand(Side.Shadow, OrcsMultiplying)),
        ["base.sh.character.22"] = new("Saruman is in play in Orthanc; Wisdom of Elrond is in the Free Peoples hand: switch side and see that it cannot activate Rohan.",
            board => board.Enter(CharacterRules.Saruman, "orthanc").Hand(Side.FreePeoples, WisdomOfElrond)),
        ["base.sh.character.24"] = WitchKing(),
        ["base.sh.strategy.01"] = new("The Shadow holds the Woodland Realm, an Elven stronghold.",
            board => board.Conquer(Side.Shadow, "sauron", "woodland-realm")),
        ["base.sh.strategy.02"] = new("Saruman is in play and an Isengard army besieges Helm's Deep; Isengard and Rohan are at war.",
            board => board.AtWar("isengard", "rohan").Enter(CharacterRules.Saruman, "orthanc").Besiege("helms-deep", "isengard", 3)),
        ["base.sh.strategy.03"] = new("A Sauron army besieges Minas Tirith, whose army has withdrawn into the stronghold.",
            board => board.AtWar("sauron", "gondor").Besiege("minas-tirith", "sauron", 3)),
        ["base.sh.strategy.04"] = new("Every Shadow nation is at war; the Free Peoples rolled two Will of the West results.",
            board => board.AtWar("sauron", "isengard", "southrons")
                .Dice(Side.FreePeoples, DieFace.WillOfTheWest, DieFace.Army, DieFace.Muster, DieFace.WillOfTheWest)),
        ["base.sh.strategy.05"] = new("Switch side afterwards: the Free Peoples' Muster die cannot advance a passive nation.", _ => { }),
        ["base.sh.strategy.06"] = new("The Fellowship is in Rivendell, an Elven region whose nation is not at war.", _ => { }),
        ["base.sh.strategy.09"] = ShadowAtWar(),
        ["base.sh.strategy.10"] = AtWar("southrons", "The Southrons & Easterlings are at war."),
        ["base.sh.strategy.11"] = AtWar("isengard", "Isengard is at war."),
        ["base.sh.strategy.12"] = WitchKing(),
        ["base.sh.strategy.13"] = AtWar("isengard", "Isengard is at war."),
        ["base.sh.strategy.14"] = AtWar("sauron", "Sauron is at war."),
        ["base.sh.strategy.15"] = AtWar("sauron", "Sauron is at war."),
        ["base.sh.strategy.16"] = Saruman(),
        ["base.sh.strategy.18"] = new("Aragorn is in play in Minas Tirith.",
            board => board.Companion(CharacterRules.Strider, "minas-tirith").Replace(CharacterRules.Aragorn)),
        ["base.sh.strategy.21"] = AtWar("southrons", "The Southrons & Easterlings are at war."),
        ["base.sh.strategy.23"] = ShadowAtWar(),
        ["base.sh.strategy.24"] = AtWar("sauron", "Sauron is at war.")
    };

    // UI-300: a special tile played on the Mordor Track joins the pool at once and is drawn within two Free Peoples steps.
    private static CardEventSetup MordorTile() => new("The Fellowship stands on step 0 of the Mordor Track and the Hunt Pool holds one other tile: switch side and move it with the Character dice, and the special tile is drawn within two steps.",
        board => board.OnMordorTrack().Dice(Side.FreePeoples, DieFace.Character, DieFace.Character, DieFace.Character, DieFace.Event));

    private static CardEventSetup Outside() => new("The Fellowship has moved on from Rivendell to Hollin, outside any Free Peoples settlement.",
        board => board.FellowshipAt("hollin"));

    // UI-300: outside a Stronghold, where the Nazgûl may enter the Fellowship's region.
    private static CardEventSetup Progress() => new("The Fellowship stands in Hollin, outside any Stronghold, one step on (Progress 1).",
        board => board.FellowshipAt("hollin", 1));

    private static CardEventSetup Revealed() => new("The Fellowship has been revealed.",
        board => board.Reveal());

    private static CardEventSetup Saruman() => new("Saruman is in play in Orthanc.",
        board => board.Enter(CharacterRules.Saruman, "orthanc"));

    private static CardEventSetup WitchKing() => new("The Witch-king is in play in Minas Morgul.",
        board => board.Enter(CharacterRules.WitchKing, "minas-morgul"));

    private static CardEventSetup ShadowAtWar() => new("Every Shadow nation is at war.",
        board => board.AtWar("sauron", "isengard", "southrons"));

    private static CardEventSetup AtWar(string nation, string description) => new(description, board => board.AtWar(nation));
}
