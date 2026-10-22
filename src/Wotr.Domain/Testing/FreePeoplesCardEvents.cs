using Wotr.Contracts;

namespace Wotr.Domain;

// TEST-09: the board each conditional Free Peoples event needs; the conditions are the engine's own (Cards/*.cs).
// UI-300: the board also lets every clause of the card act, an "if" clause included: Corruption to heal, Companions
// outside the Fellowship to move, a target for an attack, a second card for a free follow-up.
public static class FreePeoplesCardEvents
{
    private const string Boromir = "base.character.boromir";
    private const string Merry = "base.character.merry";
    private const string Legolas = "base.character.legolas";
    private const string Gimli = "base.character.gimli";
    private const string ElvenCloaks = "base.fp.character.01";

    public static IReadOnlyDictionary<string, CardEventSetup> Setups { get; } = new Dictionary<string, CardEventSetup>
    {
        ["base.fp.character.09"] = new("Strider guides the Fellowship, which carries 3 Corruption.",
            board => board.Guide(CharacterRules.Strider).Corruption(3)),
        ["base.fp.character.10"] = new("Every Companion has left the Fellowship in Rivendell, so Gollum guides it; it carries 2 Corruption and has been revealed.",
            board => board.GollumGuides("rivendell").Corruption(2).Reveal()),
        ["base.fp.character.11"] = new("The Fellowship carries 2 Corruption.",
            board => board.Corruption(2)),
        ["base.fp.character.12"] = new("Every Companion has left the Fellowship in Rivendell, so Gollum guides it; it carries 3 Corruption.",
            board => board.GollumGuides("rivendell").Corruption(3)),
        ["base.fp.character.13"] = new("The Fellowship carries 2 Corruption in unconquered Lórien; the Free Peoples rolled two Character results.",
            board => board.FellowshipAt("lorien").Corruption(2).Dice(Side.FreePeoples, DieFace.Character, DieFace.Event, DieFace.Character, DieFace.Army)),
        ["base.fp.character.14"] = new("Strider has left the Fellowship and stands with the Gondor army in Minas Tirith; the Hunt Pool holds the 4 Eyes and 2 other tiles, so the draw shows Eyes.",
            board => board.Companion(CharacterRules.Strider, "minas-tirith").KeepHuntTiles(EyesAndTwoOthers())),
        ["base.fp.character.15"] = OutsideAndInside(),
        ["base.fp.character.16"] = OutsideAndInside(),
        ["base.fp.character.17"] = new("Legolas has left the Fellowship and stands in the Woodland Realm.",
            board => board.Companion(Legolas, "woodland-realm")),
        ["base.fp.character.18"] = new("Strider stands with the Minas Tirith army next to a Sauron army with a Nazgûl in Osgiliath.",
            board => board.Companion(CharacterRules.Strider, "minas-tirith").Clear("osgiliath")
                .Place("sauron", UnitKind.Regular, "osgiliath", 2).Place("sauron", UnitKind.Nazgul, "osgiliath")),
        ["base.fp.character.19"] = Ents(),
        ["base.fp.character.20"] = Ents(),
        ["base.fp.character.21"] = Ents(),
        ["base.fp.character.22"] = new("Strider and Gimli have left the Fellowship and stand in Edoras, a Rohan region; a Southron army holds Erech.",
            board => board.Companion(CharacterRules.Strider, "edoras").Companion(Gimli, "edoras").Place("southrons", UnitKind.Regular, "erech", 2)),
        ["base.fp.character.23"] = new("Boromir has left the Fellowship and stands in Minas Tirith, a Gondor region.",
            board => board.Companion(Boromir, "minas-tirith")),
        ["base.fp.character.24"] = new("Strider has left the Fellowship and stands with the Gondor army in Minas Tirith.",
            board => board.Companion(CharacterRules.Strider, "minas-tirith")),
        ["base.fp.strategy.01"] = new("Aragorn is in play with a Gondor army in Osgiliath, outside the Free Peoples nations.",
            board => board.Companion(CharacterRules.Strider, "minas-tirith").Replace(CharacterRules.Aragorn)
                .Clear("osgiliath").Place("gondor", UnitKind.Regular, "osgiliath", 2).Relocate(CharacterRules.Aragorn, "osgiliath")),
        ["base.fp.strategy.04"] = new("Gimli has left the Fellowship and stands in Dale, next to Erebor, and Legolas in the Woodland Realm.",
            board => board.Companion(Gimli, "dale").Companion(Legolas, "woodland-realm")),
        ["base.fp.strategy.05"] = new("An Isengard Regular has joined the Sauron army in Dol Guldur.",
            board => board.Place("isengard", UnitKind.Regular, "dol-guldur")),
        ["base.fp.strategy.06"] = new("A Sauron army stands in North Ithilien, next to the Gondor army in Osgiliath.",
            board => board.Place("sauron", UnitKind.Regular, "north-ithilien", 2)),
        ["base.fp.strategy.07"] = new("Strider has left the Fellowship and stands in the Weather Hills, next to Bree, and Merry in Buckland, next to Bree and the Shire.",
            board => board.Companion(CharacterRules.Strider, "weather-hills").Companion(Merry, "buckland")),
        ["base.fp.strategy.09"] = new("Gondor is active.",
            board => board.Activate("gondor")),
        ["base.fp.strategy.10"] = new("A Sauron army besieges Minas Tirith; a Gondor army in Lossarnach can attack it. Gondor and Sauron are at war.",
            board => board.AtWar("gondor", "sauron").Besiege("minas-tirith", "sauron", 3).Place("gondor", UnitKind.Regular, "lossarnach", 2)),
        ["base.fp.strategy.11"] = new("Rohan is at war.",
            board => board.AtWar("rohan")),
        ["base.fp.strategy.12"] = new("Legolas has left the Fellowship and stands with the Gondor army in Minas Tirith.",
            board => board.Companion(Legolas, "minas-tirith")),
        ["base.fp.strategy.13"] = new("The Elves are at war.",
            board => board.AtWar("elves")),
        ["base.fp.strategy.16"] = new("Gimli has left the Fellowship and stands in Helm's Deep, a Rohan region, so the riders may also muster there.",
            board => board.Companion(Gimli, "helms-deep"))
    };

    // A Companion outside the Fellowship can be moved, and the Fellowship's own can be separated.
    private static CardEventSetup OutsideAndInside() => new("Strider has left the Fellowship and stands in Bree; the others are still in it.",
        board => board.Companion(CharacterRules.Strider, "bree"));

    // Three tiles drawn from 4 Eyes and 2 other tiles always show an Eye; all three are Eyes one time in five.
    private static Func<HuntTileDefinition, bool> EyesAndTwoOthers()
    {
        var others = 0;
        return tile => tile.Value == "eye" || others++ < 2;
    }

    // Gandalf the White took Gandalf the Grey's place in Fangorn after Saruman entered play; Merry waits there too.
    // UI-300: Elven Cloaks waits in hand for the free Character Event that follows.
    private static CardEventSetup Ents() => new("Gandalf the White and Merry are in Fangorn; Saruman is in play; Elven Cloaks is in hand for the free Character Event.",
        board => board.Companion(CharacterRules.Grey, "fangorn").Enter(CharacterRules.Saruman, "orthanc")
            .Replace(CharacterRules.White).Companion(Merry, "fangorn").Hand(Side.FreePeoples, ElvenCloaks));
}
