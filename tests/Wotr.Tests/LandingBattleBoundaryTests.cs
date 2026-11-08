using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.CorsairFixture;

namespace Wotr.Tests;

public sealed class LandingBattleBoundaryTests
{
    private static readonly Projection Projection = GameRuntime.Projection(EffectCatalog.All);

    [Fact]
    public void CorsairsBattleExposesOnlyItsPublicParentAndPayment()
    {
        var state = Land(State(defenders: 2));
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);

        var shadow = Projection.For(state, Side.Shadow);
        var freePeoples = Projection.For(state, Side.FreePeoples);
        foreach (var view in new[] { shadow, freePeoples })
        {
            Assert.Equal(52, view.ActionBoundary?.Version);
            var context = Assert.IsType<BattleContextView>(view.ActionBoundary?.Battle);
            Assert.False(context.CompletesPaidAction);
            Assert.False(context.SpecialAttackLoss);
            Assert.Null(context.ReliefEvent);
            Assert.Equal(Card, context.LandingEvent?.CardId);
            Assert.Equal(DieFace.Army, context.LandingEvent?.CardCost);
            Assert.True(view.Battle?.ArrivalBeforeBattle);
            Assert.Equal("pelargir", view.Battle?.RegionId);
            Assert.Equal("pelargir", view.Battle?.Attacker.RegionId);
            Assert.Single(view.ActionBoundary!.PaidDieIds);
        }

        Assert.Contains(Card, freePeoples.ActionBoundary!.Players.Single(player => player.Side == Side.Shadow).PublicDiscards);
        Assert.Contains(shadow.EffectTrail, item => item.SourceId == Card);
        Assert.DoesNotContain(freePeoples.EffectTrail, item => item.SourceId == Card);
    }

    [Theory]
    [InlineData("parent")]
    [InlineData("region")]
    [InlineData("palantir")]
    [InlineData("public-card")]
    [InlineData("arrival")]
    public void LandingBoundaryRefusesAnUnprovenParentOrArrival(string change)
    {
        var state = Land(State(defenders: 2));
        var execution = state.Resolution.Frames[0].Effect!;
        if (change == "parent")
            execution.SourceId = ShadowStrategyCards02.Rage;
        else if (change == "region")
            execution.RegionId = "lossarnach";
        else if (change == "palantir")
            execution.PalantirDrawPending = true;
        else if (change == "public-card")
            state.Events.RemoveAll(item => item.Kind == "EventPlayed");
        else
            state.Resolution.Frames[1].Battle!.ArrivalBeforeBattle = false;

        Assert.Null(Projection.For(state, Side.Shadow).ActionBoundary?.Battle);
        Assert.Null(Projection.For(state, Side.FreePeoples).ActionBoundary?.Battle);
    }

    // The arrival forbids the attacker to stop, so the round goes straight to the defender's retreat.
    [Fact]
    public void LandingBoundaryRetainsEventParentThroughCardsAndRetreat()
    {
        var state = Land(State(defenders: 2));
        AssertLandingContext(state);
        state = Do(state, CommandKind.CommitCombatCard);
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        AssertLandingContext(state);

        state = Round(Land(State(defenders: 2)));
        Assert.Equal(DecisionKind.BattleRetreat, state.Pending!.Kind);
        AssertLandingContext(state);
    }

    // At Dol Amroth the defender first chooses between the field and the stronghold.
    [Fact]
    public void LandingBoundaryMarksTheStrongholdDefenseChoice()
    {
        var state = Land(State(target: Stronghold, defenders: 2), Stronghold);
        Assert.Equal(DecisionKind.StrongholdDefense, state.Pending!.Kind);
        AssertLandingContext(state);
    }

    private static void AssertLandingContext(MatchState state)
    {
        foreach (var side in new[] { Side.Shadow, Side.FreePeoples })
            Assert.Equal(Card, Projection.For(state, side).ActionBoundary?.Battle?.LandingEvent?.CardId);
    }
}
