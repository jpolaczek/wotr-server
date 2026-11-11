using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

public sealed class EventCompletionBoundaryTests
{
    private static readonly Projection Projection = GameRuntime.Projection(EffectCatalog.All);
    private const string Celeborn = "base.fp.strategy.15";

    // Celeborn's Galadhrim paid with an Event die while Gandalf the Grey guides: after its choice
    // the completion window offers his draw, with a hand of six.
    private static MatchState CompletionWindow()
    {
        var state = TurnTests.Actions();
        HandRules.Add(state.Player(Side.FreePeoples), Celeborn);
        foreach (var id in state.Player(Side.FreePeoples).Decks["character"].Take(5).ToArray())
        {
            state.Player(Side.FreePeoples).Decks["character"].Remove(id);
            HandRules.Add(state.Player(Side.FreePeoples), id);
        }
        var cost = Cost(state, DieFace.Event);
        cost.CardId = Celeborn;
        state = Do(state, CommandKind.PlayEvent, cost);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [state.Pending!.PrivateChoices[0]] });
        Assert.Equal("event.completed", state.Pending!.Window);
        return state;
    }

    private static void AssertMarked(MatchState state, string card)
    {
        foreach (var side in Enum.GetValues<Side>())
            Assert.Equal(card, Projection.For(state, side).ActionBoundary?.DirectEventChoice?.CardId);
    }

    // A political consequence a separation left waits, public, until the action completes.
    [Fact]
    public void TheCompletionWindowIsMarkedWhileAPoliticalConsequenceWaits()
    {
        var state = CompletionWindow();
        state.PoliticalReactions.Add(new(PoliticalTrigger.CompanionArrival, "gondor", "minas-tirith"));

        AssertMarked(state, Celeborn);
        Assert.Equal("gondor", Assert.Single(Projection.For(state, Side.Shadow).ActionBoundary!.PendingPolitics).NationId);
    }

    // Gandalf's draw takes the hand to seven; the excess discard resumes the window.
    [Theory]
    [InlineData("")]
    [InlineData("offered")]
    [InlineData("guide")]
    public void TheDiscardAfterGandalfsDrawIsMarkedWithTheEvent(string change)
    {
        var state = Do(CompletionWindow(), CommandKind.RespondEffect, new() { ReactionId = "character.grey-draw" });
        Assert.Equal(DecisionKind.DiscardCards, state.Pending!.Kind);
        switch (change)
        {
            case "offered": state.Resolution.Frames[1].Effect!.OfferedReactions.Clear(); break;
            case "guide": state.Fellowship.Guide = CharacterRules.Strider; break;
        }

        if (change.Length == 0)
            AssertMarked(state, Celeborn);
        else
            Assert.Null(Projection.For(state, Side.FreePeoples).ActionBoundary?.DirectEventChoice);
    }

    // The Three Rings roll 5, 6 and 1: two successes recover the ring out of play and one Shadow holds.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheRingChoiceAfterTheRecoveryRollIsMarkedWithItsRolls(bool tampered)
    {
        var state = LordsRingRecoveryTests.Choice();
        var recovery = state.Resolution.Frames[0].Effect!.RingRecovery!;
        Assert.Equal("ElvenRings", state.Pending!.Presentation);
        if (tampered)
            recovery.Successes++;

        foreach (var side in Enum.GetValues<Side>())
        {
            var facts = Projection.For(state, side).ActionBoundary?.DirectEventChoice;
            if (tampered)
                Assert.Null(facts);
            else
            {
                Assert.Equal(LordsFreePeoplesCards.ThreeRings, facts?.CardId);
                Assert.Equal(recovery.Rolls, facts!.RingRecoveryRolls);
            }
        }
    }

    // The Wind from the West finds no Shadow table card, so the Free Peoples inspect Shadow's hand;
    // the choice that follows lists it in full, to the Free Peoples alone.
    [Fact]
    public void TheWindsHandInspectionIsMarkedForBothSeats()
    {
        var state = LordsCardsFixture.State();
        state.Player(Side.Shadow).Table.Clear();
        if (state.Player(Side.Shadow).Hand.Count == 0)
            CardFlow.Draw(state, Side.Shadow, "character");
        state = LordsCardsFixture.Play(state, LordsFreePeoplesCards.Wind);
        state = LordsCardsFixture.Choose(state, WindFromWestStep.Inspect);
        Assert.Equal("InspectedHand", state.Pending!.Presentation);
        Assert.Equal(state.Player(Side.Shadow).Hand, state.Pending.PrivateChoices);

        AssertMarked(state, LordsFreePeoplesCards.Wind);
        Assert.Null(Projection.For(state, Side.Shadow).Decision);
        state.Resolution.Frames[0].Effect!.Committed.RemoveAt(1);
        Assert.Null(Projection.For(state, Side.FreePeoples).ActionBoundary?.DirectEventChoice);
    }
}
