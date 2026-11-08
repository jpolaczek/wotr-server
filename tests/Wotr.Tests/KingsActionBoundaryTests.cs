using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsActionBoundaryTests
{
    private static readonly Projection Projection = GameRuntime.Projection(EffectCatalog.All);

    private static string RulerDie(MatchState state) =>
        state.Dice.Single(die => die.Owner == Side.FreePeoples && die.Source == SovereignCatalog.RulerSource).Id;

    internal static MatchState AnarionChoice(int hand = 0)
    {
        var state = KingsAnarionTests.Ready();
        state.Player(Side.FreePeoples).Hand = state.Player(Side.FreePeoples).Decks["character"].Take(hand).ToList();
        foreach (var card in state.Player(Side.FreePeoples).Hand)
            state.Player(Side.FreePeoples).Decks["character"].Remove(card);
        return Do(state, CommandKind.DrawCard, new() { DieId = RulerDie(state), Deck = "strategy" });
    }

    internal static MatchState AnarionDiscard(string choice)
    {
        var state = AnarionChoice(choice == "decline" ? 6 : 5);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [choice] });
        Assert.Equal(DecisionKind.DiscardCards, state.Pending!.Kind);
        Assert.Equal(DiscardContinuation.ResumeEffect, state.AfterDiscard);
        return state;
    }

    [Fact]
    public void TheAnarionDeckChoiceAfterARulerDrawIsMarked()
    {
        var state = AnarionChoice();

        Assert.Equal(SovereignCatalog.Denethor, state.Pending!.SourceId);
        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.False(Projection.For(state, side).ActionBoundary?.AnarionDraw?.Discard));
    }

    [Theory]
    [InlineData("character")]
    [InlineData("decline")]
    public void TheExcessDiscardAfterAnAnarionChoiceIsMarked(string choice)
    {
        var state = AnarionDiscard(choice);

        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.True(Projection.For(state, side).ActionBoundary?.AnarionDraw?.Discard));
        Assert.Equal(choice, Assert.Single(Projection.For(state, Side.FreePeoples).EffectTrail).ChoiceId);
    }

    [Theory]
    [InlineData("window")]
    [InlineData("die")]
    [InlineData("parent")]
    [InlineData("trail")]
    public void TheAnarionDrawRequiresItsRulerDrawAction(string change)
    {
        var state = change == "trail" ? AnarionDiscard("character") : AnarionChoice();
        var execution = state.Resolution.Frames[0].Effect!;
        switch (change)
        {
            case "window": execution.CompletionWindowOffered = false; break;
            case "die": state.Dice.Single(die => die.Id == state.Resolution.PaidDieIds[0]).Source = "standard"; break;
            case "parent": execution.Return = EffectReturn.Parent; break;
            case "trail": execution.Committed.Add(new(Side.FreePeoples, SovereignCatalog.Denethor, "strategy")); break;
        }

        Assert.Null(Projection.For(state, Side.FreePeoples).ActionBoundary);
    }

    [Fact]
    public void AnEventPaidWithTheRulerDieIsNotAnAnarionDraw()
    {
        var state = KingsAnarionTests.EventWindow();

        Assert.Equal(2, state.Resolution.Frames.Count);
        Assert.Null(Projection.For(state, Side.FreePeoples).ActionBoundary?.AnarionDraw);
    }

    [Fact]
    public void TheCorruptionRemovalBeforeAPoliticalActionCompletesIsMarked()
    {
        var state = KingsPoliticsTests.Ready();
        state = Do(state, CommandKind.AdvancePolitics, new() { DieId = Die(state).Id, NationId = "dwarves" });

        Assert.Equal("CorruptionRemoval", state.Pending!.Presentation);
        Assert.All(Enum.GetValues<Side>(), side =>
        {
            var removal = Projection.For(state, side).ActionBoundary?.CorruptionRemoval;
            Assert.Equal(Side.FreePeoples, removal?.Actor);
            Assert.False(removal!.FellowshipMove);
        });
    }

    [Fact]
    public void AShadowActionKeepsItsActorAndQueuedRemovalsInTheMarkedRemoval()
    {
        var state = KingsPoliticsTests.Ready(Side.Shadow);
        DiceRules.Pay(state, Side.Shadow, Die(state, Side.Shadow).Id, DieFace.Muster);
        PoliticalReactions.Add(state, PoliticalTrigger.ArmyAttack, "dwarves", "erebor");
        TurnFlow.FinishAction(state, Side.Shadow);
        new EffectRuntime(EffectCatalog.All).Continue(state, new SequenceRandom());

        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.NotEmpty(state.Kings!.PendingCorruptionRemoval);
        Assert.Equal(Side.Shadow, Projection.For(state, Side.FreePeoples).ActionBoundary?.CorruptionRemoval?.Actor);
        Assert.Equal(Side.Shadow, Projection.For(state, Side.Shadow).ActionBoundary?.CorruptionRemoval?.Actor);
    }

    [Theory]
    [InlineData("interrupt")]
    [InlineData("dice")]
    [InlineData("window")]
    public void TheCorruptionRemovalRequiresTheActionItCloses(string change)
    {
        var state = KingsPoliticsTests.Ready();
        if (change == "interrupt")
        {
            PoliticalRules.Activate(state, "dwarves");
            Assert.True(KingsPolitics.BeginPending(state));
            new EffectRuntime(EffectCatalog.All).Continue(state, new SequenceRandom());
        }
        else
        {
            state = Do(state, CommandKind.AdvancePolitics, new() { DieId = Die(state).Id, NationId = "dwarves" });
            if (change == "dice")
                state.Resolution.PaidDieIds.Clear();
            else
                state.Resolution.Frames[0].Effect!.CompletionWindowOffered = false;
        }

        Assert.Equal("CorruptionRemoval", state.Pending!.Presentation);
        Assert.Null(Projection.For(state, Side.FreePeoples).ActionBoundary?.CorruptionRemoval);
    }
}
