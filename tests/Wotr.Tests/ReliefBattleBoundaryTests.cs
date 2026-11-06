using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards02;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Strategy01Fixture;
using static Wotr.Tests.Strategy02Fixture;

namespace Wotr.Tests;

public sealed class ReliefBattleBoundaryTests
{
    [Fact]
    public void HelpUnlookedForBattleExposesOnlyItsPublicParentAndPayment()
    {
        var state = BeginReliefBattle();
        var projection = GameRuntime.Projection(EffectCatalog.All);
        var freePeoples = projection.For(state, Side.FreePeoples);
        var shadow = projection.For(state, Side.Shadow);

        foreach (var view in new[] { freePeoples, shadow })
        {
            Assert.Equal(28, view.ActionBoundary?.Version);
            var context = Assert.IsType<BattleContextView>(view.ActionBoundary?.Battle);
            Assert.False(context.CompletesPaidAction);
            Assert.False(context.SpecialAttackLoss);
            Assert.Equal(HelpUnlookedFor, context.ReliefEvent?.CardId);
            Assert.Equal(DieFace.Army, context.ReliefEvent?.CardCost);
            Assert.Equal(BattleMode.Relief, view.Battle?.Mode);
            Assert.Equal(Side.FreePeoples, view.Battle?.Attacker.Side);
            Assert.Equal(Side.Shadow, view.Battle?.Defender.Side);
            Assert.Equal(2, view.Battle?.Defender.CombatDiceReduction);
            Assert.Single(view.ActionBoundary!.PaidDieIds);
        }

        Assert.Contains(HelpUnlookedFor, shadow.ActionBoundary!.Players
            .Single(player => player.Side == Side.FreePeoples).PublicDiscards);
        Assert.DoesNotContain(shadow.EffectTrail, item => item.SourceId == HelpUnlookedFor);
        Assert.Contains(freePeoples.EffectTrail, item => item.SourceId == HelpUnlookedFor);
    }

    [Theory]
    [InlineData("parent")]
    [InlineData("reduction")]
    [InlineData("public-card")]
    public void ReliefBoundaryRefusesAnUnprovenParentOrDiceReduction(string change)
    {
        var state = BeginReliefBattle();
        if (change == "parent")
            state.Resolution.Frames[0].Effect!.SourceId = FreePeoplesStrategyCards02.PathsOfWoses;
        else if (change == "reduction")
            state.Resolution.Frames[1].Battle!.Defender.CombatDiceReduction++;
        else
            state.Events.RemoveAll(item => item.Kind == "EventPlayed");

        Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, Side.FreePeoples).ActionBoundary);
        Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, Side.Shadow).ActionBoundary);
    }

    [Fact]
    public void ReliefBoundaryRetainsEventParentThroughLossRetreatAndAdvance()
    {
        var state = BeginReliefBattle();
        state = BattleFixture.NoCards(state, 6, 1, 1, 1, 1, 1);
        state = CardPackBattleFixture.NoRerolls(state);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        AssertReliefContext(state);

        var battle = BattleRuntime.Active(state)!;
        var shadowUnitId = battle.Defender.Participants.First(id => state.Units.Any(candidate => candidate.Id == id));
        state = BattleFixture.Losses(state, BattleFixture.Loss(shadowUnitId));
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
        state = CharacterFixture.Do(state, CommandKind.ContinueBattle);
        Assert.Equal(DecisionKind.BattleRetreat, state.Pending!.Kind);
        AssertReliefContext(state);

        var retreat = BattleMovement.Retreats(state, BattleRuntime.Current(state)!).First();
        state = CharacterFixture.Do(state, CommandKind.RetreatBattle, new() { RegionId = retreat });
        Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
        AssertReliefContext(state);
    }

    private static void AssertReliefContext(MatchState state)
    {
        foreach (var side in Enum.GetValues<Side>())
        {
            var view = GameRuntime.Projection(EffectCatalog.All).For(state, side);
            Assert.Equal(28, view.ActionBoundary?.Version);
            Assert.Equal(HelpUnlookedFor, view.ActionBoundary?.Battle?.ReliefEvent?.CardId);
            Assert.Equal(2, view.Battle?.Defender.CombatDiceReduction);
        }
    }

    private static MatchState BeginReliefBattle()
    {
        var state = Relief(garrison: 2, shadow: 5);
        state.Nations.Single(nation => nation.Id == "gondor").StepsToWar = 0;
        state = Play(state, HelpUnlookedFor);
        return Do(state, CommandKind.AttackArmy, Attack(state));
    }
}
