using Wotr.Contracts;

namespace Wotr.Domain;

// TEST-09: the board each conditional Shadow event needs; the conditions are the engine's own (Cards/*.cs).
public static class ShadowCardEvents
{
    public static IReadOnlyDictionary<string, CardEventSetup> Setups { get; } = new Dictionary<string, CardEventSetup>
    {
        ["base.sh.character.05"] = Outside(),
        ["base.sh.character.06"] = Outside(),
        ["base.sh.character.07"] = Outside(),
        ["base.sh.character.08"] = Outside(),
        ["base.sh.character.09"] = Progress(),
        ["base.sh.character.10"] = Progress(),
        ["base.sh.character.11"] = Progress(),
        ["base.sh.character.12"] = Revealed(),
        ["base.sh.character.13"] = Revealed(),
        ["base.sh.character.14"] = Revealed(),
        ["base.sh.character.19"] = new("A Gondor army holds North Ithilien next to the Minas Morgul army and its Nazgûl.",
            board => board.AtWar("sauron", "gondor").Clear("osgiliath").Place("gondor", UnitKind.Regular, "north-ithilien", 2)),
        ["base.sh.character.20"] = new("The Witch-king leads a Sauron army besieging Minas Tirith; Sauron and Gondor are at war.",
            board => board.AtWar("sauron", "gondor").Besiege("minas-tirith", "sauron", 3).Enter(CharacterRules.WitchKing, "minas-tirith")),
        ["base.sh.character.21"] = Saruman(),
        ["base.sh.character.22"] = Saruman(),
        ["base.sh.character.24"] = WitchKing(),
        ["base.sh.strategy.01"] = new("The Shadow holds the Woodland Realm, an Elven stronghold.",
            board => board.Conquer(Side.Shadow, "sauron", "woodland-realm")),
        ["base.sh.strategy.02"] = new("Saruman is in play and an Isengard army besieges Helm's Deep; Isengard and Rohan are at war.",
            board => board.AtWar("isengard", "rohan").Enter(CharacterRules.Saruman, "orthanc").Besiege("helms-deep", "isengard", 3)),
        ["base.sh.strategy.03"] = new("A Sauron army besieges Minas Tirith, whose army has withdrawn into the stronghold.",
            board => board.AtWar("sauron", "gondor").Besiege("minas-tirith", "sauron", 3)),
        ["base.sh.strategy.04"] = ShadowAtWar(),
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

    private static CardEventSetup Outside() => new("The Fellowship has moved on from Rivendell to Hollin, outside any Free Peoples settlement.",
        board => board.FellowshipAt("hollin"));

    private static CardEventSetup Progress() => new("The Fellowship has moved one step since Rivendell (Progress 1).",
        board => board.FellowshipAt("rivendell", 1));

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
