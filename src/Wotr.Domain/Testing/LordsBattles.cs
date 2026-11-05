using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

// TEST-12: the combat scenario of a Lords card, as CardBattles for the base cards but after the real Lords setup and
// with the figures the Lords Combat cards act on. Each side's cards share one battle in which every combat condition of
// that side holds and every effect has its target; A Balrog is Come! has its own battle, so Durin's Bane acts away from
// Moria because the Balrog takes part. The battle starts with the engine's own attack command and waits on the card
// owner's Combat card choice; the opponent commits no card.
internal static class LordsBattles
{
    private const string Ford = "fords-of-isen";
    private const string Orthanc = "orthanc";
    private const string Lorien = "lorien";
    private const string Westemnet = "westemnet";
    private const string Edoras = "edoras";
    private const string Boromir = "base.character.boromir";
    private const string Merry = "base.character.merry";
    private const string Legolas = "base.character.legolas";

    public static string Describe(CardDefinition card) =>
        card.Id == LordsShadowCards.Balrog ? BalrogDescription(card)
        : card.Side == Side.FreePeoples ? DefenceDescription(card) : SiegeDescription(card);

    public static MatchState Build(string id, IRandomSource random, CardDefinition card) =>
        card.Id == LordsShadowCards.Balrog ? BalrogAttack(id, random, card.Id)
        : card.Side == Side.FreePeoples ? FreePeoplesDefence(id, random, card.Id) : ShadowSiege(id, random, card.Id);

    private static string DefenceDescription(CardDefinition card) =>
        "Saruman and the Witch-king lead the Isengard army from Orthanc against the Rohan army at the Fords of Isen, a Rohan " +
        "region where the Fellowship stands with Strider, Gandalf the Grey, Boromir and Merry (a Leader, Companions and a " +
        $"hobbit facing two Minions); choose {card.Title} as the defender's Combat card.";

    private static string SiegeDescription(CardDefinition card) =>
        "The Witch-king and two Nazgûl lead a Sauron, Isengard and Southron army with a Southron Elite against the Elves and " +
        $"Legolas besieged in Lórien, two regions from Moria; choose {card.Title} as the attacker's Combat card.";

    private static string BalrogDescription(CardDefinition card) =>
        "The active Balrog leads a Sauron army from Westemnet against the Rohan army in Edoras, far from Moria; Durin's Bane acts " +
        $"because the Balrog takes part, and his Flame of Udûn adds to the Combat strength. Choose {card.Title} as the attacker's Combat card.";

    // A field battle in a Rohan region of the Free Peoples, where the Fellowship stands and Saruman and the Witch-king
    // attack from Orthanc.
    public static MatchState FreePeoplesDefence(string id, IRandomSource random, string card)
    {
        var board = LordsBoard.Rolled(LordsBoard.Setup(id, random))
            .AtWar("isengard", "rohan")
            .Enter(CharacterRules.Saruman, Orthanc)
            .Enter(CharacterRules.WitchKing, Orthanc)
            .Place("rohan", UnitKind.Elite, Ford)
            .Companion(CharacterRules.Strider, Ford)
            .Companion(CharacterRules.Grey, Ford)
            .Companion(Boromir, Ford)
            .Companion(Merry, Ford)
            .FellowshipAt(Ford)
            .Hand(Side.FreePeoples, card);
        var state = LordsBoard.Attack(board.Act(Side.Shadow), Side.Shadow, Orthanc, Ford);
        return LordsBoard.CombatChoice(state, Side.FreePeoples);
    }

    // A siege of Lórien: the Elves and Legolas have withdrawn into the stronghold, and the Witch-king, two Nazgûl, a
    // Southron Elite and Sauron and Isengard units attack them.
    public static MatchState ShadowSiege(string id, IRandomSource random, string card)
    {
        var board = LordsBoard.Rolled(LordsBoard.Setup(id, random))
            .AtWar("sauron", "isengard", "southrons", "elves")
            .Companion(Legolas, Lorien)
            .Besiege(Lorien, "sauron", 2)
            .Place("sauron", UnitKind.Elite, Lorien)
            .Place("isengard", UnitKind.Regular, Lorien)
            .Place("southrons", UnitKind.Elite, Lorien)
            .Place("sauron", UnitKind.Nazgul, Lorien, 2)
            .Enter(CharacterRules.WitchKing, Lorien)
            .Hand(Side.Shadow, card);
        var state = LordsBoard.Attack(board.Act(Side.Shadow), Side.Shadow, Lorien, Lorien);
        return LordsBoard.CombatChoice(state, Side.Shadow);
    }

    // The active Balrog and three Sauron Regulars attack the Rohan army in Edoras from Westemnet.
    public static MatchState BalrogAttack(string id, IRandomSource random, string card)
    {
        var board = LordsBoard.Rolled(LordsBoard.Setup(id, random))
            .AtWar("sauron", "rohan")
            .Clear(Westemnet)
            .Place("sauron", UnitKind.Regular, Westemnet, 3)
            .Change(state => LordsBoard.Put(state, LordsCharacters.Balrog, Westemnet).Level = 2)
            .Hand(Side.Shadow, card);
        var state = LordsBoard.Attack(board.Act(Side.Shadow), Side.Shadow, Westemnet, Edoras);
        return LordsBoard.CombatChoice(state, Side.Shadow);
    }
}
