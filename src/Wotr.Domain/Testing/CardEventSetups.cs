using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

// TEST-09: what the board needs so a card's event can be played, in one sentence and as changes of the standard setup.
public sealed record CardEventSetup(string Description, Action<ScenarioBoard> Arrange);

// TEST-09: the event scenario of a card. The owner acts on turn 1 with a die of the card's event type and an Event die;
// the opponent keeps the results of the dice scenarios. Cards without a condition need nothing else.
public static class CardEventSetups
{
    private static readonly CardEventSetup Ready = new("Nothing else is needed to play it.", _ => { });

    public static CardEventSetup For(string cardId) =>
        FreePeoplesCardEvents.Setups.GetValueOrDefault(cardId) ?? ShadowCardEvents.Setups.GetValueOrDefault(cardId) ?? Ready;

    public static MatchState Build(string id, IRandomSource random, CardDefinition card, CardEventSetup setup) =>
        Build(ScenarioBoard.Setup(id, random), card, setup);

    // UI-266: the same event scenario on a board set up with expansion modules.
    public static MatchState Build(ScenarioBoard board, CardDefinition card, CardEventSetup setup)
    {
        Roll(board, card);
        board.Hand(card.Side, card.Id);
        setup.Arrange(board);
        return board.Act(card.Side);
    }

    // The Free Peoples have four results and the Shadow six with one die in the Hunt Box, as in the dice scenarios.
    private static void Roll(ScenarioBoard board, CardDefinition card)
    {
        var others = new[] { DieFace.Character, DieFace.Army, DieFace.Muster }.Where(face => face != card.EventType).ToArray();
        if (card.Side == Side.FreePeoples)
        {
            board.Dice(Side.FreePeoples, card.EventType, DieFace.Event, others[0], others[1])
                .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster)
                .Hunt(1, 1);
            return;
        }
        board.Dice(Side.Shadow, card.EventType, DieFace.Event, others[0], others[1], DieFace.Army, DieFace.Character)
            .Hunt(1, 0)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event);
    }
}
