using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsFixture;
using static Wotr.Tests.KingsChieftainBattleFixture;

namespace Wotr.Tests;

public class KingsChieftainCombatTests
{
    [Theory]
    [InlineData(KingsChieftains.Serpent)]
    [InlineData(KingsChieftains.Mirkwood)]
    [InlineData(KingsChieftains.Ugluk)]
    public void ReplacementChoicePrecedesRevealAndDoesNotLeakTheOpposingCard(string chieftain)
    {
        var state = Cards(Ready(chieftain), fpCard: Daring);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Equal("CombatReplacement", state.Pending.Presentation);
        Assert.False(battle.CardsRevealed);
        Assert.Equal(BattleStage.BeforeReveal, battle.Stage);
        Assert.Equal(KingsFixture.SetupSamples, state.RandomSamples.Count);
        Assert.Contains(chieftain, state.Pending.PrivateChoices);
        Assert.Contains("keep", state.Pending.PrivateChoices);
        Assert.DoesNotContain(Daring, Json(state, Side.Shadow));
        Assert.DoesNotContain(KingsCards03.Valinor, Json(state, Side.FreePeoples));
        Assert.Equal(KingsCards03.Valinor, BattleProjection.View(state, Side.Shadow)!.Defender.CardId);
        Assert.True(state.Player(Side.Shadow).CombatCardCommitted);
        Assert.DoesNotContain(KingsCards03.Valinor, state.Player(Side.Shadow).Hand);
    }

    [Theory]
    [InlineData(KingsCards03.Valinor, 1)]
    [InlineData(ShadowStrategyCards01.Dawn, 2)]
    [InlineData(ShadowStrategyCards02.Rage, 2)]
    [InlineData(KingsCards03.King, 2)]
    public void RedWrathPaysOneHitBeforeRevealAndReplacesOnlyCombatModifiers(string card, int bonus)
    {
        var state = Choose(Cards(Ready(KingsChieftains.Serpent), card), KingsChieftains.Serpent);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(1, state.Pending.RequiredCount);
        Assert.False(battle.CardsRevealed);
        Assert.Equal(0, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(KingsFixture.SetupSamples, state.RandomSamples.Count);
        var replacement = Assert.Single(LordsFixture.Views().For(state, Side.FreePeoples).CombatReplacements);
        Assert.Equal(KingsChieftains.Serpent, replacement.SourceId);
        Assert.Equal(Side.Shadow, replacement.Side);
        Assert.DoesNotContain(card, Json(state, Side.FreePeoples));
        state = PayWrath(StateCodec.Copy(state));
        battle = BattleRuntime.Active(state)!;
        Assert.True(battle.CardsRevealed);
        Assert.Equal(bonus, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(0, battle.Force(Side.Shadow).Round.RerollModifier);
        Assert.Equal(0, battle.Force(Side.FreePeoples).Round.CombatModifier);
        Assert.Equal(0, battle.Force(Side.FreePeoples).Round.RerollModifier);
        Assert.Single(battle.Work, w => w.Owner == Side.Shadow && w.Initiative == 3);
        Assert.Equal(card, battle.Force(Side.Shadow).Round.CardId);
        Assert.Equal(1, BattleRules.Strength(state, battle.Force(Side.Shadow)));
        Assert.Equal(KingsFixture.SetupSamples + 3, state.RandomSamples.Count);
    }

    [Fact]
    public void RedWrathMayDowngradeAnEliteAndNeverForcesAWholeFigureRemoval()
    {
        var state = Ready(KingsChieftains.Serpent);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        var regular = BattleRules.Units(state, force).First();
        BoardRules.ReturnToReserve(regular);
        force.Participants.Remove(regular.Id);
        var elite = BattleFixture.Put(state, "sauron", UnitKind.Elite, force.Region);
        force.Participants.Add(elite.Id);
        state = Choose(Cards(state), KingsChieftains.Serpent);
        state = PayWrath(state, BattleLossKind.Downgrade);
        force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.Equal("reserve", state.Units.Single(u => u.Id == elite.Id).Location);
        Assert.Equal(2, BattleRules.Strength(state, force));
        Assert.All(BattleRules.Units(state, force), u => Assert.Equal(UnitKind.Regular, u.Kind));
        Assert.Equal(KingsFixture.SetupSamples + 4, state.RandomSamples.Count);
    }

    [Theory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public void DaringCancelsRedWrathButDoesNotRefundItsPaidHit(Side attacker)
    {
        var state = Choose(Cards(Ready(KingsChieftains.Serpent, attacker), ShadowStrategyCards01.Dawn, Daring), KingsChieftains.Serpent);
        state = PayWrath(state);
        var battle = BattleRuntime.Active(state)!;
        Assert.True(battle.Force(Side.Shadow).Round.CardCancelled);
        Assert.Equal(1, BattleRules.Strength(state, battle.Force(Side.Shadow)));
        Assert.Equal(0, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.True(state.Player(Side.Shadow).CombatCardCommitted);
    }

    [Theory]
    [InlineData(KingsCards03.Valinor, true)]
    [InlineData(ShadowStrategyCards04.Monsters, false)]
    [InlineData(ShadowStrategyCards04.Musterings, false)]
    [InlineData(ShadowStrategyCards04.Pits, false)]
    public void ICommandReplacesThePrintedCardAndBuffsBothArmiesExceptForDesperateBattle(string card, bool both)
    {
        var state = Choose(Cards(Ready(KingsChieftains.Ugluk), card), KingsChieftains.Ugluk);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(1, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(1, battle.Force(Side.Shadow).Round.RerollModifier);
        Assert.Equal(both ? 1 : 0, battle.Force(Side.FreePeoples).Round.CombatModifier);
        Assert.Equal(both ? 1 : 0, battle.Force(Side.FreePeoples).Round.RerollModifier);
        Assert.Equal(1, BattleLeadership.Total(state, battle.Force(Side.Shadow)));
        Assert.Equal(3, Assert.Single(battle.Work).Initiative);
    }

    [Fact]
    public void PlayerChoosesOneOfSeveralChieftainsOrKeepsTheOriginalCard()
    {
        var state = Ready(KingsChieftains.Ugluk);
        Add(state, KingsChieftains.Serpent);
        Add(state, KingsChieftains.Mirkwood);
        state = Cards(state);
        Assert.Equal(4, state.Pending!.PrivateChoices.Count);
        var kept = Choose(StateCodec.Copy(state), "keep");
        Assert.Equal(2, BattleRuntime.Active(kept)!.Force(Side.Shadow).Round.CombatModifier);
        Assert.Null(BattleRuntime.Active(kept)!.Force(Side.Shadow).Round.ReplacementSource);
        state = Choose(state, KingsChieftains.Ugluk);
        Assert.Single(BattleRuntime.Active(state)!.Work);
        Assert.Equal(2, BattleRules.Strength(state, BattleRuntime.Active(state)!.Force(Side.Shadow)));
        Assert.Equal(4, BattleLeadership.Total(state, BattleRuntime.Active(state)!.Force(Side.Shadow)));
    }

    [Theory]
    [InlineData(KingsChieftains.Serpent)]
    [InlineData(KingsChieftains.Mirkwood)]
    [InlineData(KingsChieftains.Ugluk)]
    public void ReplacementDoesNotBypassTheCommittedCardsPrintedRequirements(string chieftain)
    {
        var state = Ready(chieftain);
        state.Player(Side.Shadow).Hand = [KingsCards03.Denethor];
        state = Do(state, CommandKind.CommitCombatCard);
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, new() { CardId = KingsCards03.Denethor });
        Assert.False(BattleRuntime.Active(state)!.Force(Side.Shadow).Round.CardCommitted);
    }

    [Theory]
    [InlineData("rearguard")]
    [InlineData("cancelled-ability")]
    [InlineData("no-card")]
    public void ReplacementRequiresAnActiveParticipatingChieftainAndAPlayedCard(string reason)
    {
        var state = Ready(KingsChieftains.Ugluk);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        if (reason == "rearguard")
            force.Participants.Remove(KingsChieftains.Ugluk);
        if (reason == "cancelled-ability")
            force.Round.CancelledAbilities.Add(KingsChieftains.Ugluk);
        state = Cards(state, reason == "no-card" ? "" : KingsCards03.Valinor);
        Assert.True(BattleRuntime.Active(state)!.CardsRevealed);
        Assert.NotEqual("CombatReplacement", state.Pending!.Presentation);
    }

    [Fact]
    public void RedWrathKillingItsLastUnitEndsBattleWithoutRollingOrReusingTheCommittedCard()
    {
        var state = Ready(KingsChieftains.Serpent, Side.Shadow);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        var unit = BattleRules.Units(state, force).First();
        BoardRules.ReturnToReserve(unit);
        force.Participants.Remove(unit.Id);
        state = Choose(Cards(state), KingsChieftains.Serpent);
        state = PayWrath(state);
        Assert.Null(BattleRuntime.Active(state));
        Assert.Contains(KingsCards03.Valinor, state.Player(Side.Shadow).Discard);
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == KingsChieftains.Serpent).Location);
        Assert.Equal(KingsFixture.SetupSamples, state.RandomSamples.Count);
    }

    [Fact]
    public void ChieftainReplacementCanBeChosenAgainInTheNextRound()
    {
        var state = SkipRerolls(Choose(Cards(Ready(KingsChieftains.Ugluk)), KingsChieftains.Ugluk));
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
        state = Do(state, CommandKind.ContinueBattle);
        state = Do(state, CommandKind.HoldBattle);
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);
        state = Cards(state, ShadowStrategyCards04.Monsters);
        Assert.Contains(KingsChieftains.Ugluk, state.Pending!.PrivateChoices);
        state = Choose(state, KingsChieftains.Ugluk);
        Assert.Equal(1, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(0, BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.CombatModifier);
    }

    [PostgresFact]
    public Task ChieftainChoiceRestartsBeforeRevealWithBothCardFacesStillPrivate() =>
        LordsPersistenceTests.Restart(Cards(Ready(KingsChieftains.Serpent), fpCard: Daring),
            CommandKind.ChooseEffect, new() { Ids = [KingsChieftains.Serpent] });

    [PostgresFact]
    public Task RedWrathLossRestartsAndRetriesWithoutAnotherHitOrCombatRoll()
    {
        var state = Choose(Cards(Ready(KingsChieftains.Serpent)), KingsChieftains.Serpent);
        var unit = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.Shadow)).First();
        return LordsPersistenceTests.Restart(state, CommandKind.TakeBattleLosses,
            new() { Losses = [BattleFixture.Loss(unit.Id)] }, 0, 0, 0);
    }
}
