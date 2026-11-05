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
        Assert.Equal(15, own.ActionBoundary!.Version);
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

    [Theory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public void DirectOrdinaryBattleLossExposesOnlyItsPaidActionReturn(Side attacker)
    {
        var state = DirectLossDecision(attacker);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(attacker, state.Pending.Owner);
        Assert.Equal("combat", BattleLosses.Current(state)!.SourceId);
        Assert.False(BattleLosses.Current(state)!.Voluntary);

        var own = Projection.For(state, attacker);
        var other = Projection.For(state, TurnFlow.Other(attacker));
        Assert.Equal(15, own.ActionBoundary!.Version);
        Assert.True(own.ActionBoundary.Battle!.CompletesPaidAction);
        Assert.True(other.ActionBoundary!.Battle!.CompletesPaidAction);
        Assert.Equal(JsonSerializer.Serialize(own.ActionBoundary.Battle),
            JsonSerializer.Serialize(other.ActionBoundary.Battle));
        Assert.Equal("DefenderLosses", own.Battle!.Stage);
        Assert.Equal(attacker, own.Battle.LossOwner);
        Assert.Equal(state.Pending.RequiredCount, own.Battle.PendingHits);
        using var boundaryJson = JsonDocument.Parse(JsonSerializer.Serialize(own.ActionBoundary.Battle));
        Assert.Equal("{\"CompletesPaidAction\":true}", boundaryJson.RootElement.GetRawText());

        var nested = StateCodec.Copy(state);
        BattleRuntime.Active(nested)!.Return = EffectReturn.Parent;
        Assert.Null(Projection.For(nested, attacker).ActionBoundary);

        var voluntary = StateCodec.Copy(state);
        BattleLosses.Current(voluntary)!.Voluntary = true;
        Assert.Null(Projection.For(voluntary, attacker).ActionBoundary);

        var unrelatedLoss = StateCodec.Copy(state);
        BattleLosses.Current(unrelatedLoss)!.SourceId = "event-card";
        Assert.Null(Projection.For(unrelatedLoss, attacker).ActionBoundary);
    }

    [Theory]
    [InlineData(Side.FreePeoples, false)]
    [InlineData(Side.FreePeoples, true)]
    [InlineData(Side.Shadow, false)]
    [InlineData(Side.Shadow, true)]
    public void DirectOrdinaryBattleLossInLaterRoundExposesOnlyItsPaidActionReturn(Side attacker, bool attackerHits)
    {
        var state = LaterRoundLossDecision(attacker, attackerHits);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(2, battle.Round);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(attackerHits ? TurnFlow.Other(attacker) : attacker, BattleLosses.Current(state)!.Owner);
        Assert.Equal("combat", BattleLosses.Current(state)!.SourceId);
        Assert.False(BattleLosses.Current(state)!.Voluntary);

        var own = Projection.For(state, state.Pending.Owner);
        var other = Projection.For(state, TurnFlow.Other(state.Pending.Owner));
        Assert.Equal(15, own.ActionBoundary!.Version);
        Assert.True(own.ActionBoundary.Battle!.CompletesPaidAction);
        Assert.True(other.ActionBoundary!.Battle!.CompletesPaidAction);
        Assert.Equal(JsonSerializer.Serialize(own.ActionBoundary.Battle),
            JsonSerializer.Serialize(other.ActionBoundary.Battle));
        Assert.Equal(attackerHits ? "AfterLosses" : "DefenderLosses", own.Battle!.Stage);
        Assert.Equal(state.Pending.Owner, own.Battle.LossOwner);
        Assert.Equal(state.Pending.RequiredCount, own.Battle.PendingHits);
        using var boundaryJson = JsonDocument.Parse(JsonSerializer.Serialize(own.ActionBoundary.Battle));
        Assert.Equal("{\"CompletesPaidAction\":true}", boundaryJson.RootElement.GetRawText());
    }

    [Theory]
    [InlineData(Side.FreePeoples, false)]
    [InlineData(Side.FreePeoples, true)]
    [InlineData(Side.Shadow, false)]
    [InlineData(Side.Shadow, true)]
    public void DirectOrdinaryBattleCardInLaterRoundExposesOnlyItsPaidActionReturn(Side attacker, bool defenderWindow)
    {
        var state = LaterRoundCardDecision(attacker, defenderWindow);
        var actor = state.Pending!.Owner;
        Assert.Equal(attacker, defenderWindow ? TurnFlow.Other(actor) : actor);
        Assert.Equal(DecisionKind.CombatCard, state.Pending.Kind);
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);

        var own = Projection.For(state, actor);
        var other = Projection.For(state, TurnFlow.Other(actor));
        Assert.Equal(15, own.ActionBoundary!.Version);
        Assert.True(own.ActionBoundary.Battle!.CompletesPaidAction);
        Assert.True(other.ActionBoundary!.Battle!.CompletesPaidAction);
        Assert.Equal(JsonSerializer.Serialize(own.ActionBoundary.Battle),
            JsonSerializer.Serialize(other.ActionBoundary.Battle));
        Assert.Equal(defenderWindow ? "DefenderCard" : "AttackerCard", own.Battle!.Stage);
        Assert.Single(own.ActionBoundary.PaidDieIds);
        using var boundaryJson = JsonDocument.Parse(JsonSerializer.Serialize(own.ActionBoundary.Battle));
        Assert.Equal("{\"CompletesPaidAction\":true}", boundaryJson.RootElement.GetRawText());
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

    private static MatchState DirectLossDecision(Side attacker)
    {
        var state = BattleFixture.State(attacker);
        var attackingNation = attacker == Side.Shadow ? "sauron" : "gondor";
        var defendingNation = attacker == Side.Shadow ? "gondor" : "sauron";
        var attackers = Enumerable.Range(0, 3)
            .Select(_ => BattleFixture.Put(state, attackingNation, UnitKind.Regular, BattleFixture.Origin)).ToArray();
        for (var index = 0; index < 3; index++)
            BattleFixture.Put(state, defendingNation, UnitKind.Regular, BattleFixture.Target);
        state = BattleFixture.Begin(state, attackers.Select(unit => unit.Id));
        return BattleFixture.NoCards(state, 6, 6, 6, 6, 6, 6);
    }

    private static MatchState LaterRoundLossDecision(Side attacker, bool attackerHits)
    {
        var state = BattleFixture.State(attacker);
        var attackingNation = attacker == Side.Shadow ? "sauron" : "gondor";
        var defendingNation = attacker == Side.Shadow ? "gondor" : "sauron";
        var attackers = Enumerable.Range(0, 3)
            .Select(_ => BattleFixture.Put(state, attackingNation, UnitKind.Regular, BattleFixture.Origin)).ToArray();
        for (var index = 0; index < 3; index++)
            BattleFixture.Put(state, defendingNation, UnitKind.Regular, BattleFixture.Target);
        state = BattleFixture.Begin(state, attackers.Select(unit => unit.Id));
        state = BattleFixture.NoCards(state, 1, 1, 1, 1, 1, 1);
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
        state = BattleFixture.Execute(state, CommandKind.ContinueBattle);
        if (state.Pending?.Kind == DecisionKind.BattleRetreat)
            state = BattleFixture.Execute(state, CommandKind.HoldBattle);
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);

        return attackerHits
            ? BattleFixture.NoCards(state, 6, 6, 6, 1, 1, 1)
            : BattleFixture.NoCards(state, 1, 1, 1, 6, 6, 6);
    }

    private static MatchState LaterRoundCardDecision(Side attacker, bool defenderWindow)
    {
        var state = BattleFixture.State(attacker);
        var attackingNation = attacker == Side.Shadow ? "sauron" : "gondor";
        var defendingNation = attacker == Side.Shadow ? "gondor" : "sauron";
        var attackers = Enumerable.Range(0, 3)
            .Select(_ => BattleFixture.Put(state, attackingNation, UnitKind.Regular, BattleFixture.Origin)).ToArray();
        for (var index = 0; index < 3; index++)
            BattleFixture.Put(state, defendingNation, UnitKind.Regular, BattleFixture.Target);
        state = BattleFixture.Begin(state, attackers.Select(unit => unit.Id));
        state = BattleFixture.NoCards(state, 1, 1, 1, 1, 1, 1);
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
        state = BattleFixture.Execute(state, CommandKind.ContinueBattle);
        if (state.Pending?.Kind == DecisionKind.BattleRetreat)
            state = BattleFixture.Execute(state, CommandKind.HoldBattle);
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);
        if (defenderWindow)
        {
            Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
            Assert.Equal(attacker, state.Pending.Owner);
            state = BattleFixture.Execute(state, CommandKind.CommitCombatCard);
        }
        return state;
    }
}
