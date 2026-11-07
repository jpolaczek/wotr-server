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
        Assert.Equal(40, own.ActionBoundary!.Version);
        Assert.True(own.ActionBoundary.Battle!.CompletesPaidAction);
        Assert.Equal(own.ActionBoundary.Battle.CompletesPaidAction,
            other.ActionBoundary!.Battle!.CompletesPaidAction);
        Assert.Single(own.ActionBoundary.PaidDieIds);
        Assert.Equal(attacker, state.Dice.Single(d => d.Location == DieLocation.Resolving).Owner);
        Assert.Null(other.Decision);
        Assert.Empty(other.Options);

        using var boundaryJson = JsonDocument.Parse(JsonSerializer.Serialize(own.ActionBoundary.Battle));
        Assert.True(boundaryJson.RootElement.GetProperty("CompletesPaidAction").GetBoolean());
        Assert.False(boundaryJson.RootElement.GetProperty("SpecialAttackLoss").GetBoolean());

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
        Assert.Equal(40, own.ActionBoundary!.Version);
        Assert.True(own.ActionBoundary.Battle!.CompletesPaidAction);
        Assert.True(other.ActionBoundary!.Battle!.CompletesPaidAction);
        Assert.Equal(JsonSerializer.Serialize(own.ActionBoundary.Battle),
            JsonSerializer.Serialize(other.ActionBoundary.Battle));
        Assert.Equal("DefenderLosses", own.Battle!.Stage);
        Assert.Equal(attacker, own.Battle.LossOwner);
        Assert.Equal(state.Pending.RequiredCount, own.Battle.PendingHits);
        using var boundaryJson = JsonDocument.Parse(JsonSerializer.Serialize(own.ActionBoundary.Battle));
        Assert.Equal("{\"CompletesPaidAction\":true,\"SpecialAttackLoss\":false,\"ReliefEvent\":null,\"AttackerCancelledHits\":0,\"DefenderCancelledHits\":0}", boundaryJson.RootElement.GetRawText());

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
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public void RelentlessAssaultVoluntaryLossExposesOnlyItsPaidActionReturn(Side attacker)
    {
        var state = BeginRelentlessAssaultVoluntaryLosses(attacker);
        var own = Projection.For(state, Side.Shadow);
        var other = Projection.For(state, Side.FreePeoples);
        Assert.Equal(40, own.ActionBoundary!.Version);
        Assert.True(own.ActionBoundary.Battle!.CompletesPaidAction);
        Assert.False(own.ActionBoundary.Battle.SpecialAttackLoss);
        Assert.Equal(JsonSerializer.Serialize(own.ActionBoundary.Battle),
            JsonSerializer.Serialize(other.ActionBoundary!.Battle));
        Assert.Equal(nameof(BattleStage.BeforeRoll), own.Battle!.Stage);
        Assert.True(own.Battle.VoluntaryLosses);
        Assert.Equal(ShadowStrategyCards01.Dawn, own.Battle.LossSource);
        Assert.Equal(2, own.Battle.PendingHits);
        Assert.Null(own.Battle.Attacker.VoluntaryLossHits);
        Assert.Null(own.Battle.Defender.VoluntaryLossHits);
        Assert.Equal("{\"CompletesPaidAction\":true,\"SpecialAttackLoss\":false,\"ReliefEvent\":null,\"AttackerCancelledHits\":0,\"DefenderCancelledHits\":0}",
            JsonSerializer.Serialize(own.ActionBoundary.Battle));

        var wrongCursor = StateCodec.Copy(state);
        wrongCursor.Resolution.Frames[1].NextStep = 0;
        Assert.Null(Projection.For(wrongCursor, Side.Shadow).ActionBoundary);

        var wrongLoss = StateCodec.Copy(state);
        BattleLosses.Current(wrongLoss)!.Hits++;
        Assert.Null(Projection.For(wrongLoss, Side.Shadow).ActionBoundary);
    }

    [Theory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public void OnslaughtVoluntaryLossExposesOnlyItsPaidActionReturn(Side attacker)
    {
        var state = BeginOnslaughtVoluntaryLosses(attacker);
        var own = Projection.For(state, Side.Shadow);
        var other = Projection.For(state, Side.FreePeoples);
        Assert.Equal(40, own.ActionBoundary!.Version);
        Assert.True(own.ActionBoundary.Battle!.CompletesPaidAction);
        Assert.False(own.ActionBoundary.Battle.SpecialAttackLoss);
        Assert.Equal(JsonSerializer.Serialize(own.ActionBoundary.Battle),
            JsonSerializer.Serialize(other.ActionBoundary!.Battle));
        Assert.Equal(nameof(BattleStage.AfterLosses), own.Battle!.Stage);
        Assert.True(own.Battle.VoluntaryLosses);
        Assert.Equal(ShadowStrategyCards01.UrukHai, own.Battle.LossSource);
        Assert.Equal(4, own.Battle.PendingHits);
        Assert.Null(own.Battle.Attacker.VoluntaryLossHits);
        Assert.Null(own.Battle.Defender.VoluntaryLossHits);
        Assert.Equal("{\"CompletesPaidAction\":true,\"SpecialAttackLoss\":false,\"ReliefEvent\":null,\"AttackerCancelledHits\":0,\"DefenderCancelledHits\":0}",
            JsonSerializer.Serialize(own.ActionBoundary.Battle));

        var wrongCursor = StateCodec.Copy(state);
        wrongCursor.Resolution.Frames[1].NextStep = 0;
        Assert.Null(Projection.For(wrongCursor, Side.Shadow).ActionBoundary);

        var wrongLossKind = StateCodec.Copy(state);
        BattleLosses.Current(wrongLossKind)!.AdditionalAttack = false;
        Assert.Null(Projection.For(wrongLossKind, Side.Shadow).ActionBoundary);
    }

    [Theory]
    [InlineData(Side.FreePeoples, FreePeoplesStrategyCards03.CirdansShips)]
    [InlineData(Side.Shadow, FreePeoplesStrategyCards03.CirdansShips)]
    [InlineData(Side.FreePeoples, FreePeoplesStrategyCards03.GuardsOfCitadel)]
    [InlineData(Side.Shadow, FreePeoplesStrategyCards03.GuardsOfCitadel)]
    public void FirstChargeSpecialAttackLossExposesOnlyTheReviewedResumeMarker(Side attacker, string cardId)
    {
        var state = ChargeSpecialAttackLoss(attacker, cardId);
        var owner = state.Pending!.Owner;
        Assert.Equal(DecisionKind.Losses, state.Pending.Kind);
        Assert.Equal(cardId, state.Pending.SourceId);
        Assert.Equal(cardId, BattleLosses.Current(state)!.SourceId);

        var own = Projection.For(state, owner);
        var other = Projection.For(state, TurnFlow.Other(owner));
        Assert.Equal(40, own.ActionBoundary!.Version);
        Assert.False(own.ActionBoundary.Battle!.CompletesPaidAction);
        Assert.True(own.ActionBoundary.Battle.SpecialAttackLoss);
        Assert.Equal(JsonSerializer.Serialize(own.ActionBoundary.Battle),
            JsonSerializer.Serialize(other.ActionBoundary!.Battle));
        Assert.True(own.Battle!.CardsRevealed);
        Assert.Equal(nameof(BattleStage.BeforeRoll), own.Battle.Stage);
        Assert.Equal(cardId, Assert.Single(own.Battle.SpecialAttacks).SourceId);
        Assert.Equal(own.Battle.PendingHits, state.Pending.RequiredCount);
        Assert.Single(own.ActionBoundary.PaidDieIds);

        using var boundaryJson = JsonDocument.Parse(JsonSerializer.Serialize(own.ActionBoundary.Battle));
        Assert.Equal("{\"CompletesPaidAction\":false,\"SpecialAttackLoss\":true,\"ReliefEvent\":null,\"AttackerCancelledHits\":0,\"DefenderCancelledHits\":0}",
            boundaryJson.RootElement.GetRawText());
        Assert.DoesNotContain("ResolutionFrame", JsonSerializer.Serialize(own.ActionBoundary));
        Assert.DoesNotContain("NextStep", JsonSerializer.Serialize(own.ActionBoundary));

        var nested = StateCodec.Copy(state);
        nested.Resolution.Frames[1].NextStep = 0;
        Assert.Null(Projection.For(nested, owner).ActionBoundary);

        var mismatchedSource = StateCodec.Copy(state);
        BattleLosses.Current(mismatchedSource)!.SourceId = "combat";
        Assert.Null(Projection.For(mismatchedSource, owner).ActionBoundary);
    }

    [Theory]
    [InlineData(ShadowStrategyCards01.UrukHai)]
    [InlineData(ShadowStrategyCards04.MistyMountains)]
    [InlineData(ShadowStrategyCards04.OrcsMultiplying)]
    public void EveryOnslaughtCardExposesItsVoluntaryLoss(string card)
    {
        var state = BeginOnslaughtVoluntaryLosses(Side.Shadow, card);

        Assert.True(ReviewedCombatCards.IsOnslaught(card));
        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.True(Projection.For(state, side).ActionBoundary?.Battle?.CompletesPaidAction));
    }

    [Theory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public void PostCombatOnslaughtLossExposesOnlyTheReviewedResumeMarker(Side attacker)
    {
        var state = OnslaughtSpecialAttackLoss(attacker);
        var owner = state.Pending!.Owner;
        var loss = BattleLosses.Current(state)!;
        Assert.Equal(Side.FreePeoples, owner);
        Assert.False(loss.Voluntary);
        Assert.False(loss.AdditionalAttack);
        Assert.Equal(1, loss.Hits);
        Assert.Equal(ShadowStrategyCards01.UrukHai, loss.SourceId);

        var own = Projection.For(state, owner);
        var other = Projection.For(state, TurnFlow.Other(owner));
        Assert.Equal(40, own.ActionBoundary!.Version);
        Assert.False(own.ActionBoundary.Battle!.CompletesPaidAction);
        Assert.True(own.ActionBoundary.Battle.SpecialAttackLoss);
        Assert.Equal(JsonSerializer.Serialize(own.ActionBoundary.Battle),
            JsonSerializer.Serialize(other.ActionBoundary!.Battle));
        Assert.Equal(nameof(BattleStage.AfterLosses), own.Battle!.Stage);
        Assert.Equal(ShadowStrategyCards01.UrukHai, own.Battle.LossSource);
        Assert.Equal(1, own.Battle.PendingHits);
        var attack = Assert.Single(own.Battle.SpecialAttacks);
        Assert.Equal(Side.Shadow, attack.Side);
        Assert.Equal(ShadowStrategyCards01.UrukHai, attack.SourceId);
        var die = Assert.Single(attack.Dice);
        Assert.Equal(4, die.HitTarget);
        Assert.True(die.Hit);

        var wrongCursor = StateCodec.Copy(state);
        wrongCursor.Resolution.Frames[1].NextStep = 1;
        Assert.Null(Projection.For(wrongCursor, owner).ActionBoundary);

        var wrongAppliedHits = StateCodec.Copy(state);
        wrongAppliedHits.Resolution.Frames[1].Effect!.AppliedHits = 0;
        Assert.Null(Projection.For(wrongAppliedHits, owner).ActionBoundary);

        var wrongLossKind = StateCodec.Copy(state);
        BattleLosses.Current(wrongLossKind)!.Voluntary = true;
        Assert.Null(Projection.For(wrongLossKind, owner).ActionBoundary);
    }

    [Theory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public void ZeroOnslaughtSacrificeDoesNotExposeAnAttackLossMarker(Side attacker)
    {
        var state = BeginOnslaughtVoluntaryLosses(attacker);
        state = BattleFixture.Losses(state);

        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
        Assert.Empty(BattleRuntime.Active(state)!.SpecialAttacks);
        Assert.False(Projection.For(state, state.Pending.Owner).ActionBoundary?.Battle?.SpecialAttackLoss ?? false);
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
        Assert.Equal(40, own.ActionBoundary!.Version);
        Assert.True(own.ActionBoundary.Battle!.CompletesPaidAction);
        Assert.True(other.ActionBoundary!.Battle!.CompletesPaidAction);
        Assert.Equal(JsonSerializer.Serialize(own.ActionBoundary.Battle),
            JsonSerializer.Serialize(other.ActionBoundary.Battle));
        Assert.Equal(attackerHits ? "AfterLosses" : "DefenderLosses", own.Battle!.Stage);
        Assert.Equal(state.Pending.Owner, own.Battle.LossOwner);
        Assert.Equal(state.Pending.RequiredCount, own.Battle.PendingHits);
        using var boundaryJson = JsonDocument.Parse(JsonSerializer.Serialize(own.ActionBoundary.Battle));
        Assert.Equal("{\"CompletesPaidAction\":true,\"SpecialAttackLoss\":false,\"ReliefEvent\":null,\"AttackerCancelledHits\":0,\"DefenderCancelledHits\":0}", boundaryJson.RootElement.GetRawText());
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
        Assert.Equal(40, own.ActionBoundary!.Version);
        Assert.True(own.ActionBoundary.Battle!.CompletesPaidAction);
        Assert.True(other.ActionBoundary!.Battle!.CompletesPaidAction);
        Assert.Equal(JsonSerializer.Serialize(own.ActionBoundary.Battle),
            JsonSerializer.Serialize(other.ActionBoundary.Battle));
        Assert.Equal(defenderWindow ? "DefenderCard" : "AttackerCard", own.Battle!.Stage);
        Assert.Single(own.ActionBoundary.PaidDieIds);
        using var boundaryJson = JsonDocument.Parse(JsonSerializer.Serialize(own.ActionBoundary.Battle));
        Assert.Equal("{\"CompletesPaidAction\":true,\"SpecialAttackLoss\":false,\"ReliefEvent\":null,\"AttackerCancelledHits\":0,\"DefenderCancelledHits\":0}", boundaryJson.RootElement.GetRawText());
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

    private static MatchState ChargeSpecialAttackLoss(Side attacker, string cardId)
    {
        var state = BattleFixture.State(attacker);
        var attackingNation = attacker == Side.Shadow ? "sauron" : "gondor";
        var defendingNation = attacker == Side.Shadow ? "gondor" : "sauron";
        var attackingFigures = Enumerable.Range(0, 3)
            .Select(_ => BattleFixture.Put(state, attackingNation, UnitKind.Regular, BattleFixture.Origin)).ToArray();
        for (var index = 0; index < 3; index++)
            BattleFixture.Put(state, defendingNation, UnitKind.Regular, BattleFixture.Target);
        var elfRegion = attacker == Side.FreePeoples ? BattleFixture.Origin : BattleFixture.Target;
        var elfElite = BattleFixture.Put(state, "elves", UnitKind.Elite, elfRegion);
        state.Player(Side.FreePeoples).Hand.Add(cardId);
        var attackers = attackingFigures.Select(unit => unit.Id)
            .Concat(attacker == Side.FreePeoples ? [elfElite.Id] : []);
        state = BattleFixture.Begin(state, attackers);

        while (state.Pending!.Kind == DecisionKind.CombatCard)
        {
            var actor = state.Pending.Owner;
            var selected = actor == Side.FreePeoples ? cardId : "";
            state = BattleFixture.Execute(state, CommandKind.CommitCombatCard, new() { CardId = selected },
                new SequenceRandom(5));
        }

        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(cardId, state.Pending.SourceId);
        Assert.Equal(1, BattleRuntime.Active(state)!.NextWork);
        return state;
    }

    private static MatchState OnslaughtSpecialAttackLoss(Side attacker)
    {
        var state = BeginOnslaughtVoluntaryLosses(attacker);
        var battle = BattleRuntime.Active(state)!;
        var regular = BattleRules.Units(state, battle.Force(Side.Shadow))
            .First(unit => unit.Kind == UnitKind.Regular && BoardRules.IsArmyUnit(unit));
        state = BattleFixture.Execute(state, CommandKind.TakeBattleLosses,
            new() { Losses = [BattleFixture.Loss(regular.Id)] }, new SequenceRandom(5));
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        Assert.Equal(ShadowStrategyCards01.UrukHai, BattleLosses.Current(state)!.SourceId);
        return state;
    }

    private static MatchState BeginOnslaughtVoluntaryLosses(Side attacker, string card = ShadowStrategyCards01.UrukHai)
    {
        var state = BattleFixture.State(attacker);
        var attackingNation = attacker == Side.Shadow ? "sauron" : "gondor";
        var defendingNation = attacker == Side.Shadow ? "gondor" : "sauron";
        var attackers = Enumerable.Range(0, 3)
            .Select(_ => BattleFixture.Put(state, attackingNation, UnitKind.Regular, BattleFixture.Origin)).ToArray();
        for (var index = 0; index < 3; index++)
            BattleFixture.Put(state, defendingNation, UnitKind.Regular, BattleFixture.Target);
        state.Player(Side.Shadow).Hand.Add(card);
        state = BattleFixture.Begin(state, attackers.Select(unit => unit.Id));

        while (state.Pending!.Kind == DecisionKind.CombatCard)
        {
            var actor = state.Pending.Owner;
            var cardId = actor == Side.Shadow ? card : "";
            state = BattleFixture.Execute(state, CommandKind.CommitCombatCard,
                new() { CardId = cardId }, new SequenceRandom(new int[64]));
        }

        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        Assert.True(BattleLosses.Current(state)!.Voluntary);
        Assert.True(BattleLosses.Current(state)!.AdditionalAttack);
        Assert.Equal(card, BattleLosses.Current(state)!.SourceId);
        return state;
    }

    private static MatchState BeginRelentlessAssaultVoluntaryLosses(Side attacker)
    {
        var state = BattleFixture.State(attacker);
        var attackingNation = attacker == Side.Shadow ? "sauron" : "gondor";
        var defendingNation = attacker == Side.Shadow ? "gondor" : "sauron";
        var attackers = Enumerable.Range(0, 3)
            .Select(_ => BattleFixture.Put(state, attackingNation, UnitKind.Regular, BattleFixture.Origin)).ToArray();
        for (var index = 0; index < 3; index++)
            BattleFixture.Put(state, defendingNation, UnitKind.Regular, BattleFixture.Target);
        state.Player(Side.Shadow).Hand.Add(ShadowStrategyCards01.Dawn);
        state = BattleFixture.Begin(state, attackers.Select(unit => unit.Id));

        while (state.Pending!.Kind == DecisionKind.CombatCard)
        {
            var actor = state.Pending.Owner;
            var cardId = actor == Side.Shadow ? ShadowStrategyCards01.Dawn : "";
            state = BattleFixture.Execute(state, CommandKind.CommitCombatCard,
                new() { CardId = cardId }, new SequenceRandom(new int[64]));
        }

        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        Assert.True(BattleLosses.Current(state)!.Voluntary);
        Assert.False(BattleLosses.Current(state)!.AdditionalAttack);
        Assert.Equal(ShadowStrategyCards01.Dawn, BattleLosses.Current(state)!.SourceId);
        return state;
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
