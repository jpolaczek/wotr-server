using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

// TEST-09: the combat scenario of a card. Each side's cards share one battle in which every combat condition of that
// side holds; the battle starts with the engine's own attack command, so the Combat card choice is the real one.
public static class CardBattles
{
    private const string Ford = "fords-of-isen";
    private const string Lorien = "lorien";
        private const string Boromir = "base.character.boromir";
    private const string Merry = "base.character.merry";

    public static string Describe(CardDefinition card) => card.Side == Side.FreePeoples
        ? $"Isengard attacks the Rohan army at the Fords of Isen, where the Fellowship stands with Strider, Gandalf the Grey, Boromir and Merry; choose {card.Title} as the defender's Combat card."
        : $"A Sauron, Isengard and Southron army with two Nazgûl attacks the Elves besieged in Lórien, where the Fellowship shelters; choose {card.Title} as the attacker's Combat card.";

    public static MatchState Build(string id, IRandomSource random, CardDefinition card) =>
        card.Side == Side.FreePeoples ? FreePeoplesDefence(id, random, card.Id) : ShadowSiege(id, random, card.Id);

    // A field battle in a Rohan region of the Free Peoples, defended by an army with a Leader, Elites and Companions (a
    // hobbit and both Gandalf the Grey and Strider among them) in the Fellowship's region. The Shadow attacker plays no card.
    private static MatchState FreePeoplesDefence(string id, IRandomSource random, string card)
    {
        var board = Roll(ScenarioBoard.Setup(id, random))
            .AtWar("isengard", "rohan")
            .Place("rohan", UnitKind.Elite, Ford)
            .Companion(CharacterRules.Strider, Ford)
            .Companion(CharacterRules.Grey, Ford)
            .Companion(Boromir, Ford)
            .Companion(Merry, Ford)
            .FellowshipAt(Ford)
            .Hand(Side.FreePeoples, card);
        board.Act(Side.Shadow);
        board = Attack(board, "orthanc", Ford, random);
        return board.Command(CommandKind.CommitCombatCard, new() { CardId = "" }, random).State;
    }

    // A siege of Lórien, two regions from Moria: the Elves have withdrawn into the stronghold, where the Fellowship
    // shelters, and the besieging army with Elites, Isengard units, a Southron Elite and two Nazgûl attacks them.
    private static MatchState ShadowSiege(string id, IRandomSource random, string card)
    {
        var board = Roll(ScenarioBoard.Setup(id, random))
            .AtWar("sauron", "isengard", "southrons", "elves")
            .Besiege(Lorien, "sauron", 2)
            .Place("sauron", UnitKind.Elite, Lorien)
            .Place("isengard", UnitKind.Regular, Lorien)
            .Place("southrons", UnitKind.Elite, Lorien)
            .Place("sauron", UnitKind.Nazgul, Lorien, 2)
            .FellowshipAt(Lorien)
            .Hand(Side.Shadow, card);
        board.Act(Side.Shadow);
        return Attack(board, Lorien, Lorien, random).State;
    }

    private static ScenarioBoard Roll(ScenarioBoard board) => board
        .Dice(Side.Shadow, DieFace.Army, DieFace.Character, DieFace.Muster, DieFace.Event, DieFace.Army, DieFace.Muster)
        .Hunt(1, 0)
        .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event);

    // The Shadow attacks with every unit of its army, paid with an Army die.
    private static ScenarioBoard Attack(ScenarioBoard board, string from, string to, IRandomSource random)
    {
        var state = board.State;
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available && d.Face == DieFace.Army);
        var units = state.Units.Where(u => u.Location == from && !u.Garrison && BoardRules.Owner(u) == Side.Shadow).Select(u => u.Id).ToList();
        return board.Command(CommandKind.AttackArmy, new()
        {
            DieId = die.Id,
            MovementCost = DieFace.Army,
            Moves = [new() { From = from, To = to, UnitIds = units }]
        }, random);
    }
}
