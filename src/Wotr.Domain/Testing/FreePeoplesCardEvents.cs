using Wotr.Contracts;

namespace Wotr.Domain;

// TEST-09: the board each conditional Free Peoples event needs; the conditions are the engine's own (Cards/*.cs).
public static class FreePeoplesCardEvents
{
    private const string Boromir = "base.character.boromir";
    private const string Merry = "base.character.merry";
    private const string Legolas = "base.character.legolas";

    public static IReadOnlyDictionary<string, CardEventSetup> Setups { get; } = new Dictionary<string, CardEventSetup>
    {
        ["base.fp.character.14"] = new("Strider has left the Fellowship and stands with the Gondor army in Minas Tirith.",
            board => board.Companion(CharacterRules.Strider, "minas-tirith")),
        ["base.fp.character.18"] = new("Strider stands with the Minas Tirith army next to a Sauron army with a Nazgûl in Osgiliath.",
            board => board.Companion(CharacterRules.Strider, "minas-tirith").Clear("osgiliath")
                .Place("sauron", UnitKind.Regular, "osgiliath", 2).Place("sauron", UnitKind.Nazgul, "osgiliath")),
        ["base.fp.character.19"] = Ents(),
        ["base.fp.character.20"] = Ents(),
        ["base.fp.character.21"] = Ents(),
        ["base.fp.character.22"] = new("Strider has left the Fellowship and stands in Edoras, a Rohan region.",
            board => board.Companion(CharacterRules.Strider, "edoras")),
        ["base.fp.character.23"] = new("Boromir has left the Fellowship and stands in Minas Tirith, a Gondor region.",
            board => board.Companion(Boromir, "minas-tirith")),
        ["base.fp.character.24"] = new("Strider has left the Fellowship and stands with the Gondor army in Minas Tirith.",
            board => board.Companion(CharacterRules.Strider, "minas-tirith")),
        ["base.fp.strategy.01"] = new("Aragorn is in play with a Gondor army in Osgiliath, outside the Free Peoples nations.",
            board => board.Companion(CharacterRules.Strider, "minas-tirith").Replace(CharacterRules.Aragorn)
                .Clear("osgiliath").Place("gondor", UnitKind.Regular, "osgiliath", 2).Relocate(CharacterRules.Aragorn, "osgiliath")),
        ["base.fp.strategy.05"] = new("An Isengard Regular has joined the Sauron army in Dol Guldur.",
            board => board.Place("isengard", UnitKind.Regular, "dol-guldur")),
        ["base.fp.strategy.09"] = new("Gondor is active.",
            board => board.Activate("gondor")),
        ["base.fp.strategy.10"] = new("A Sauron army besieges Minas Tirith; a Gondor army in Lossarnach can attack it. Gondor and Sauron are at war.",
            board => board.AtWar("gondor", "sauron").Besiege("minas-tirith", "sauron", 3).Place("gondor", UnitKind.Regular, "lossarnach", 2)),
        ["base.fp.strategy.11"] = new("Rohan is at war.",
            board => board.AtWar("rohan")),
        ["base.fp.strategy.12"] = new("Legolas has left the Fellowship and stands with the Gondor army in Minas Tirith.",
            board => board.Companion(Legolas, "minas-tirith")),
        ["base.fp.strategy.13"] = new("The Elves are at war.",
            board => board.AtWar("elves"))
    };

    // Gandalf the White took Gandalf the Grey's place in Fangorn after Saruman entered play; Merry waits there too.
    private static CardEventSetup Ents() => new("Gandalf the White and Merry are in Fangorn; Saruman is in play.",
        board => board.Companion(CharacterRules.Grey, "fangorn").Enter(CharacterRules.Saruman, "orthanc")
            .Replace(CharacterRules.White).Companion(Merry, "fangorn"));
}
