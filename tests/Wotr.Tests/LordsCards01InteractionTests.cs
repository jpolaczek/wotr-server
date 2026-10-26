using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.LordsFreePeoplesCards;
using static Wotr.Tests.LordsCardsFixture;

namespace Wotr.Tests;

public class LordsCards01InteractionTests
{
    [Fact]
    public void ShieldWallDoesNotCancelTheSeparateHitsFromOnslaught()
    {
        var state = Battle(EveryLittle);
        EventTests.GiveCard(state, Side.Shadow, ShadowStrategyCards01.UrukHai);
        state = LordsFixture.Do(state, CommandKind.CommitCombatCard, new() { CardId = EveryLittle });
        state = LordsFixture.Do(state, CommandKind.CommitCombatCard, new() { CardId = ShadowStrategyCards01.UrukHai }, new SequenceRandom(0, 0, 0, 0, 0, 5, 5, 0));
        state = CardPackBattleFixture.NoRerolls(state);
        var fp = BattleRules.Units(state, BattleRuntime.Active(state)!.Attacker).First(BoardRules.IsArmyUnit).Id;
        state = LordsFixture.Do(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(fp)] });
        Assert.True(BattleLosses.Current(state)!.Voluntary);
        var sh = BattleRules.Units(state, BattleRuntime.Active(state)!.Defender).First(BoardRules.IsArmyUnit).Id;
        state = LordsFixture.Do(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(sh)] }, new SequenceRandom(5));
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Equal(1, state.Pending.RequiredCount);
        Assert.Equal(ShadowStrategyCards01.UrukHai, BattleLosses.Current(state)!.SourceId);
        Assert.Equal(1, BattleRuntime.Active(state)!.Attacker.Round.CancelHits);
    }

    [Theory]
    [InlineData(Wind)]
    [InlineData(ThreeRings)]
    public void SwarmCancellationNeverOffersWithdrawalOrCreatesItsObligation(string card)
    {
        var state = Battle(card);
        EventTests.GiveCard(state, Side.Shadow, ShadowStrategyCards02.Moving);
        state = LordsFixture.Do(state, CommandKind.CommitCombatCard, new() { CardId = card });
        state = LordsFixture.Do(state, CommandKind.CommitCombatCard, new() { CardId = ShadowStrategyCards02.Moving }, new SequenceRandom(0, 0, 0, 0, 0, 5, 5, 0));
        state = CardPackBattleFixture.NoRerolls(state);
        Assert.True(BattleRuntime.Active(state)!.Attacker.Round.CardCancelled);
        Assert.False(BattleRuntime.Active(state)!.Attacker.Round.OrderlyWithdrawal);
        Assert.Equal(2, state.Pending!.RequiredCount);
        Assert.Equal(DecisionKind.Losses, state.Pending.Kind);
    }

    [Fact]
    public void MandatoryWithdrawalMayEnterAnAvailableFriendlyStronghold()
    {
        var state = BattleFixture.State(Side.Shadow);
        state.Configuration.Lords = true;
        state.Lords = new();
        var attacker = BattleFixture.Put(state, "sauron", UnitKind.Regular, "osgiliath");
        BattleFixture.Put(state, "gondor", UnitKind.Regular, BattleFixture.Origin);
        BattleFixture.Put(state, "gondor", UnitKind.Leader, BattleFixture.Origin);
        EventTests.GiveCard(state, Side.FreePeoples, Wind);
        state = BattleFixture.Begin(state, [attacker.Id], from: "osgiliath", to: BattleFixture.Origin);
        state = LordsFixture.Do(state, CommandKind.FightFieldBattle);
        state = LordsWithdrawalTests.RevealHits(state, Wind, 0, 1);
        state = Choose(state, OrderlyWithdrawalStep.Cancel);
        Assert.Equal(DecisionKind.BattleRetreat, state.Pending!.Kind);
        Assert.Contains(CommandKind.RetreatIntoSiege, state.Pending.Commands);
        state = LordsFixture.Do(state, CommandKind.RetreatIntoSiege);
        state = LordsFixture.Do(state, CommandKind.AdvanceBattle, new() { Ids = [attacker.Id] });
        Assert.True(BoardRules.Besieged(state, BattleFixture.Origin, Side.FreePeoples));
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void CancellingDuringSortieStopsAfterTheRoundAndPreservesTheGarrison()
    {
        var state = SiegeTests.SetupSiege(Side.FreePeoples);
        state.Configuration.Lords = true;
        state.Lords = new();
        var leader = BattleFixture.Put(state, "gondor", UnitKind.Leader, BattleFixture.Origin);
        leader.Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, BattleFixture.Origin);
        EventTests.GiveCard(state, Side.FreePeoples, Wind);
        state = BattleFixture.Begin(state, state.Units.Where(u => u.Garrison).Select(u => u.Id), from: BattleFixture.Origin, to: BattleFixture.Origin);
        state = LordsWithdrawalTests.RevealHits(state, Wind, 0, 1);
        state = Choose(state, OrderlyWithdrawalStep.Cancel);
        Assert.Empty(state.Resolution.Frames);
        Assert.True(BoardRules.Besieged(state, BattleFixture.Origin, Side.FreePeoples));
        Assert.Equal(2, state.Units.Count(u => u.Garrison));
    }

    [Fact]
    public void EliminatingTheOpposingArmyEndsBattleBeforeAnyRetreatDecision()
    {
        var state = Battle(Wind, false);
        state = LordsWithdrawalTests.RevealHits(state, Wind, 3, 1);
        state = LordsWithdrawalTests.TakeLosses(Choose(state, OrderlyWithdrawalStep.Cancel));
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(3, state.Units.Count(u => u.Location == BattleFixture.Target && BoardRules.IsArmyUnit(u)));
    }

    [Fact]
    public void GreyUsesTheLordsEventDeckAndDefersHandLimitUntilItsOptionalDraw()
    {
        var state = State();
        state.Fellowship.Guide = CharacterRules.Grey;
        foreach (var card in state.Player(Side.FreePeoples).Decks["character"].Take(5).ToArray())
            EventTests.GiveCard(state, Side.FreePeoples, card);
        state = Play(state, EveryLittle);
        Assert.Equal(6, state.Player(Side.FreePeoples).Hand.Count);
        Assert.Equal(DecisionKind.Reaction, state.Pending!.Kind);
        var next = state.Player(Side.FreePeoples).Decks["strategy"][0];
        state = LordsFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.grey-draw" });
        Assert.Contains(next, state.Player(Side.FreePeoples).Hand);
        Assert.Equal(DecisionKind.DiscardCards, state.Pending!.Kind);
        state = LordsFixture.Do(state, CommandKind.DiscardCards, new() { Ids = [next] });
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(6, state.Player(Side.FreePeoples).Hand.Count);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Fact]
    public void ChangingAnUninspectedHandDoesNotChangeFreePeoplesOptionsHistoryOrPreview()
    {
        var state = State();
        EventTests.GiveCard(state, Side.Shadow, "base.sh.character.01");
        state = Play(state, Wind);
        var other = StateCodec.Copy(state);
        // UI-361: a hand card's deck is public (BASE 2.4.1 pp. 14, 29), so the hidden card changes within its deck.
        other.Player(Side.Shadow).Hand[0] = "base.sh.character.24";
        Assert.Equal(Json(state, Side.FreePeoples), Json(other, Side.FreePeoples));
        var command = BattleFixture.Command(state, CommandKind.ChooseEffect, new() { Ids = ["base.sh.character.01"] });
        var previews = new PreviewService(LordsFixture.Engine());
        Assert.Equal(previews.Preview(state, Side.FreePeoples, command).Error, previews.Preview(other, Side.FreePeoples, command).Error);
        Assert.False(previews.Preview(state, Side.FreePeoples, command).Legal);
    }
}
