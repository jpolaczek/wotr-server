using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsCardsFixture;
using static Wotr.Domain.LordsFreePeoplesCards;

namespace Wotr.Tests;

public class LordsWithdrawalTests
{
    internal static MatchState Window(bool fpAttacks = true, string card = Wind, int shadowHits = 2)
    {
        var state = Battle(card, fpAttacks);
        return RevealHits(state, card, 1, shadowHits);
    }

    internal static MatchState RevealHits(MatchState state, string card, int fpHits, int shadowHits)
    {
        var battle = BattleRuntime.Active(state)!;
        var samples = new[] { battle.Attacker, battle.Defender }.SelectMany(force =>
            Enumerable.Range(0, BattleRoll.DiceCount(state, force)).Select(i => i < (force.Side == Side.FreePeoples ? fpHits : shadowHits) ? 5 : 0)).ToArray();
        state = CardPackBattleFixture.Reveal(state, card, dice: samples);
        return CardPackBattleFixture.NoRerolls(state);
    }

    internal static MatchState TakeLosses(MatchState state)
    {
        while (state.Pending?.Kind == DecisionKind.Losses)
        {
            var battle = BattleRuntime.Active(state)!;
            var losses = BattleRules.Units(state, battle.Force(state.Pending.Owner)).Where(BoardRules.IsArmyUnit)
                .Take(state.Pending.RequiredCount).Select(u => BattleFixture.Loss(u.Id)).ToList();
            state = LordsFixture.Do(state, CommandKind.TakeBattleLosses, new() { Losses = losses });
        }
        return state;
    }

    [Theory]
    [InlineData(Wind, true)]
    [InlineData(Wind, false)]
    [InlineData(ThreeRings, true)]
    [InlineData(ThreeRings, false)]
    public void CancellingHitResolvesBothArmiesLossesThenForcesStopOrRetreat(string card, bool fpAttacks)
    {
        var state = Window(fpAttacks, card);
        state = Choose(state, OrderlyWithdrawalStep.Cancel);
        Assert.Equal(1, BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.CancelHits);
        state = TakeLosses(state);
        Assert.Equal(2, state.Units.Count(u => u.Location == (fpAttacks ? BattleFixture.Origin : BattleFixture.Target) && BoardRules.IsArmyUnit(u)));
        if (fpAttacks)
        {
            Assert.Null(BattleRuntime.Active(state));
            Assert.Equal(Side.Shadow, state.Pending!.Owner);
        }
        else
        {
            Assert.Equal(DecisionKind.BattleRetreat, state.Pending!.Kind);
            Assert.Equal(Side.FreePeoples, state.Pending.Owner);
            Assert.DoesNotContain(CommandKind.HoldBattle, state.Pending.Commands);
            Assert.DoesNotContain(LordsFixture.Views().For(state, Side.FreePeoples).Options, o => o.Kind == CommandKind.HoldBattle);
            LordsFixture.Reject(state, CommandKind.HoldBattle, new());
            state = LordsFixture.Do(state, CommandKind.RetreatBattle, new() { RegionId = "pelargir" });
            Assert.True(BattleRuntime.Active(state)!.DefenderRetreated);
        }
    }

    [Fact]
    public void DecliningDoesNotForceWithdrawalAndNoHitsOffersNoCancellation()
    {
        var state = Choose(Window(), OrderlyWithdrawalStep.Decline);
        state = TakeLosses(state);
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
        Assert.False(BattleRuntime.Active(state)!.Attacker.Round.OrderlyWithdrawal);
        state = Window(shadowHits: 0);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        Assert.False(BattleRuntime.Active(state)!.Attacker.Round.OrderlyWithdrawal);
    }

    [Fact]
    public void NoLegalRetreatKeepsTheNormalContinuationAndFlagExpiresNextRound()
    {
        var state = Window(false);
        foreach (var id in BoardRules.Regions[BattleFixture.Target].Neighbors)
            if (!BoardRules.HasEnemyArmy(state, id, Side.FreePeoples))
                BattleFixture.Put(state, "sauron", UnitKind.Regular, id);
        state = TakeLosses(Choose(state, OrderlyWithdrawalStep.Cancel));
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
        state = LordsFixture.Do(state, CommandKind.ContinueBattle);
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);
        Assert.False(BattleRuntime.Active(state)!.Defender.Round.OrderlyWithdrawal);
    }

    [Theory]
    [InlineData(BattleMode.Field, true)]
    [InlineData(BattleMode.Relief, true)]
    [InlineData(BattleMode.Sortie, true)]
    [InlineData(BattleMode.Siege, false)]
    public void ConditionRequiresFieldOrSortieAndAParticipatingLeaderOrCompanion(BattleMode mode, bool legal)
    {
        var state = Battle(Wind);
        var battle = BattleRuntime.Active(state)!;
        battle.Mode = mode;
        var cards = new BattleCards(EffectCatalog.All);
        Assert.Equal(legal, cards.CanCommit(state, battle, Side.FreePeoples, Wind));
        battle.Attacker.Participants.RemoveAll(id => state.Characters.Any(c => c.Id == id) || state.Units.Any(u => u.Id == id && u.Kind == UnitKind.Leader));
        Assert.False(cards.CanCommit(state, battle, Side.FreePeoples, Wind));
    }
}
