using Wotr.Contracts;
using Wotr.Domain.Content;
using static Wotr.Domain.KingsScenarioSetup;

namespace Wotr.Domain;

// TEST-13: the combat scenario of a Kings card. As in CardBattles (TEST-09), each side's Kings cards share one battle in
// a Kings game where every combat condition of that side holds; the battle starts with the engine's own attack command
// and waits at the card owner's Combat card choice.
public static class KingsCardBattles
{
    private const string Ford = "fords-of-isen";
    private const string Orthanc = "orthanc";
    private const string MinasTirith = "minas-tirith";
    private const string Boromir = "base.character.boromir";
    private const string Merry = "base.character.merry";

    private static MatchConfiguration Kings => new() { Kings = true };

    public static string Describe(CardDefinition card) => card.Side == Side.FreePeoples
        ? "Isengard and a Nazgûl attack the Rohan army at the Fords of Isen, where the Fellowship stands with Strider, " +
          "Gandalf the Grey, Boromir and Merry beside the Awakened Théoden, Denethor still Initial and a Rohan Elite; " +
          $"choose {card.Title} as the defender's Combat card."
        : "Seven Sauron Regulars, a Sauron Elite, two Isengard units and two Nazgûl besiege Minas Tirith, where Gondor's three " +
          "army units and Denethor, still Initial, have withdrawn" + (NeedsMinion(card) ? " and the Black Serpent leads the besiegers" : "") +
          $"; choose {card.Title} as the attacker's Combat card.";

    public static MatchState Build(string id, IRandomSource random, CardDefinition card) => card.Side == Side.FreePeoples
        ? FreePeoplesDefence(id, random, card.Id)
        : ShadowSiege(id, random, card.Id, NeedsMinion(card));

    // Your Welcome Is Doubtful needs a Minion in the battle: a Dark Chieftain counts as one for the cards (T29).
    private static bool NeedsMinion(CardDefinition card) => card.Id == KingsCards02.Welcome;

    // A field battle in a Rohan region: the defenders have a Leader, an Elite, five Companions (the Awakened Théoden among
    // them) and two sovereigns, one Awakened and one Initial; the attackers' Nazgûl gives the Shadow a Leader re-roll.
    // An empty card id builds the same battle with no Kings card in hand.
    public static MatchState FreePeoplesDefence(string id, IRandomSource random, string card)
    {
        var board = Roll(ScenarioBoard.Setup(id, random, Kings))
            .AtWar("isengard", "sauron", "rohan")
            .Place("rohan", UnitKind.Elite, Ford)
            .Place("sauron", UnitKind.Nazgul, Orthanc)
            .Companion(CharacterRules.Strider, Ford)
            .Companion(CharacterRules.Grey, Ford)
            .Companion(Boromir, Ford)
            .Companion(Merry, Ford)
            .FellowshipAt(Ford)
            .Change(state => Sovereigns(state, Ford));
        if (card.Length > 0)
            board.Hand(Side.FreePeoples, card);
        board.Act(Side.Shadow);
        var state = KingsBattleSteps.Attack(board.State, Side.Shadow, Orthanc, Ford);
        return KingsBattleSteps.NoCard(state, Side.Shadow);
    }

    // A siege of Minas Tirith: ten Shadow army units against three, so after any round's casualties the besiegers still
    // number twice the defenders; Isengard units take part, two Nazgûl give Leadership and Denethor stands inside.
    private static MatchState ShadowSiege(string id, IRandomSource random, string card, bool chieftain)
    {
        var board = Roll(ScenarioBoard.Setup(id, random, Kings))
            .AtWar("sauron", "isengard", "gondor")
            .Change(state => BoardRules.ReturnToReserve(state.Units.First(u => u.Location == MinasTirith && u.Nation == "gondor" &&
                u.Kind == UnitKind.Regular)))
            .Besiege(MinasTirith, "sauron", 7)
            .Place("sauron", UnitKind.Elite, MinasTirith)
            .Place("isengard", UnitKind.Regular, MinasTirith)
            .Place("isengard", UnitKind.Elite, MinasTirith)
            .Place("sauron", UnitKind.Nazgul, MinasTirith, 2)
            .Hand(Side.Shadow, card);
        if (chieftain)
            board.Change(state => ExpansionBoard.Put(state, KingsChieftains.Serpent, MinasTirith));
        board.Act(Side.Shadow);
        return KingsBattleSteps.Attack(board.State, Side.Shadow, MinasTirith, MinasTirith);
    }

    // Théoden Awakened and Denethor Initial, both standing in the defending army.
    private static void Sovereigns(MatchState state, string region)
    {
        Awaken(state, SovereignCatalog.Theoden, region);
        state.Units.Single(u => u.Id == SovereignCatalog.Denethor).Location = region;
    }

    private static ScenarioBoard Roll(ScenarioBoard board) => board
        .Dice(Side.Shadow, DieFace.Army, DieFace.Character, DieFace.Muster, DieFace.Event, DieFace.Army, DieFace.Muster)
        .Hunt(1, 0)
        .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event);
}
