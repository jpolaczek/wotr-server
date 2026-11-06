using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.FateCardsFixture;

namespace Wotr.Tests;

public sealed class FateMonstersBoundaryTests
{
    [Fact]
    public void FateMonstersBoundaryIdentifiesItsParentAndSelectedEastBranch()
    {
        var state = Play(State(FateCards.Monsters), FateCards.Monsters);
        AssertContext(state, "");
        var shadow = GameRuntime.Projection(EffectCatalog.All).For(state, Side.Shadow);
        Assert.Equal(DecisionKind.EffectChoice, shadow.Decision!.Kind);
        Assert.Equal(new[] { FateCards.MonstersWest, FateCards.MonstersEast }, shadow.Decision.Choices);

        state = Choose(state, FateCards.MonstersEast);
        AssertContext(state, FateCards.MonstersEast);
        shadow = GameRuntime.Projection(EffectCatalog.All).For(state, Side.Shadow);
        Assert.Equal(DecisionKind.EffectChoice, shadow.Decision!.Kind);
        Assert.Equal(FateMonstersEliteStep.Regions, shadow.Decision.Choices);

        state = Choose(state, shadow.Decision.Choices[0]);
        Assert.Empty(state.Resolution.Frames);
        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary?.FateMonsters));
    }

    [Theory]
    [InlineData("parent")]
    [InlineData("payment")]
    [InlineData("public-card")]
    public void FateMonstersBoundaryRefusesUnprovenParentOrPayment(string change)
    {
        var state = Play(State(FateCards.Monsters), FateCards.Monsters);
        if (change == "parent")
            state.Resolution.Frames[0].Effect!.SourceId = FateCards.Orcs;
        else if (change == "payment")
            state.Dice.Single(die => die.Id == state.Resolution.PaidDieIds[0]).Location = DieLocation.Spent;
        else
            state.Events.RemoveAll(item => item.Kind == "EventPlayed");

        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary?.FateMonsters));
    }

    [Fact]
    public void FateMonstersEastBoundaryRejectsAContradictoryCommittedBranch()
    {
        var state = Choose(Play(State(FateCards.Monsters), FateCards.Monsters), FateCards.MonstersEast);
        state.Resolution.Frames[0].Effect!.Committed[1] =
            new(Side.Shadow, FateCards.Monsters, FateCards.MonstersWest, "");

        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary?.FateMonsters));
    }

    private static void AssertContext(MatchState state, string branch)
    {
        foreach (var side in Enum.GetValues<Side>())
        {
            var view = GameRuntime.Projection(EffectCatalog.All).For(state, side);
            Assert.Equal(27, view.ActionBoundary?.Version);
            var context = Assert.IsType<FateMonstersEventView>(view.ActionBoundary?.FateMonsters);
            Assert.Equal(FateCards.Monsters, context.CardId);
            Assert.Equal(CardCatalog.All[FateCards.Monsters].EventType, context.CardCost);
            Assert.Equal(branch, context.BranchId);
            Assert.Single(view.ActionBoundary!.PaidDieIds);
        }
    }
}
