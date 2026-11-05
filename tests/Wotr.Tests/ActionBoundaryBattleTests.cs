using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public sealed class ActionBoundaryBattleTests
{
    private static readonly Projection Projection = GameRuntime.Projection(EffectCatalog.All);

    [Theory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public void DirectPaidAttackExposesOnlyItsSemanticReturnToBothSeats(Side attacker)
    {
        var state = StartBattle(attacker);
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        Assert.Equal(attacker, state.Pending.Owner);

        var own = Projection.For(state, attacker);
        var other = Projection.For(state, TurnFlow.Other(attacker));
        Assert.Equal(12, own.ActionBoundary!.Version);
        Assert.True(own.ActionBoundary.Battle!.CompletesPaidAction);
        Assert.Equal(own.ActionBoundary.Battle.CompletesPaidAction,
            other.ActionBoundary!.Battle!.CompletesPaidAction);
        Assert.Single(own.ActionBoundary.PaidDieIds);
        Assert.Equal(attacker, state.Dice.Single(d => d.Location == DieLocation.Resolving).Owner);
        Assert.Null(other.Decision);
        Assert.Empty(other.Options);

        using var boundaryJson = JsonDocument.Parse(JsonSerializer.Serialize(own.ActionBoundary.Battle));
        var returnFacts = Assert.Single(boundaryJson.RootElement.EnumerateObject());
        Assert.True(returnFacts.Value.GetBoolean());

        var parentReturn = StateCodec.Copy(state);
        BattleRuntime.Active(parentReturn)!.Return = EffectReturn.Parent;
        Assert.Null(Projection.For(parentReturn, attacker).ActionBoundary);

        var wrongStage = StateCodec.Copy(state);
        wrongStage.Pending!.Kind = DecisionKind.BattleContinue;
        Assert.Null(Projection.For(wrongStage, attacker).ActionBoundary);

        var missingPayment = StateCodec.Copy(state);
        missingPayment.Resolution.PaidDieIds.Clear();
        Assert.Null(Projection.For(missingPayment, attacker).ActionBoundary);

        var relief = StateCodec.Copy(state);
        BattleRuntime.Active(relief)!.Mode = BattleMode.Relief;
        Assert.Null(Projection.For(relief, attacker).ActionBoundary);
    }

    [Fact]
    public void EmptyAttackCardPassKeepsTheDirectReturnWithoutInventingACard()
    {
        var state = StartBattle(Side.Shadow);
        state = BattleFixture.Execute(state, CommandKind.CommitCombatCard);

        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        var view = Projection.For(state, Side.FreePeoples);
        Assert.True(view.ActionBoundary!.Battle!.CompletesPaidAction);
        Assert.Equal("DefenderCard", view.Battle!.Stage);
        Assert.True(view.Battle.Attacker.CardCommitted);
        Assert.Null(view.Battle.Attacker.CardId);
        Assert.Equal("", view.Battle.Attacker.CardBack);
    }

    [Fact]
    public void OpposingFaceDownCombatCardPermutationsProduceTheSameView()
    {
        var first = OpponentCardView(BattleFixture.ShCard);
        var second = OpponentCardView("base.sh.strategy.02");

        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.True(first.Battle!.Attacker.CardCommitted);
        Assert.Null(first.Battle.Attacker.CardId);
        Assert.Equal("strategy", first.Battle.Attacker.CardBack);
        Assert.DoesNotContain(BattleFixture.ShCard, JsonSerializer.Serialize(first));
        Assert.DoesNotContain("base.sh.strategy.02", JsonSerializer.Serialize(first));
    }

    private static PlayerView OpponentCardView(string cardId)
    {
        var state = BattleFixture.State(Side.Shadow);
        state.Player(Side.Shadow).Hand.Add(cardId);
        state = StartBattle(state, Side.Shadow);
        state = BattleFixture.Execute(state, CommandKind.CommitCombatCard, new() { CardId = cardId });
        return Projection.For(state, Side.FreePeoples);
    }

    private static MatchState StartBattle(Side attacker) => StartBattle(BattleFixture.State(attacker), attacker);

    private static MatchState StartBattle(MatchState state, Side attacker)
    {
        var attackingNation = attacker == Side.Shadow ? "sauron" : "gondor";
        var defendingNation = attacker == Side.Shadow ? "gondor" : "sauron";
        var army = BattleFixture.Put(state, attackingNation, UnitKind.Regular, BattleFixture.Origin);
        BattleFixture.Put(state, defendingNation, UnitKind.Regular, BattleFixture.Target);
        return BattleFixture.Begin(state, [army.Id]);
    }
}
