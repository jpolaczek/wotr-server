using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.KingsCards03;
using static Wotr.Tests.KingsCards03Fixture;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsCards03CombatTests
{
    internal static MatchState Combat(string card)
    {
        var state = Battle();
        var battle = BattleRuntime.Active(state)!;
        if (card is Denethor or Threats)
        {
            battle.Mode = BattleMode.Siege;
            var force = battle.Force(Side.Shadow);
            foreach (var unit in BattleRules.Units(state, force))
                unit.Location = "minas-tirith";
            battle.Region = force.Region = "minas-tirith";
            var replaced = state.Units.Single(u => u.Id == force.Participants[0]);
            force.Participants.Remove(replaced.Id);
            BoardRules.ReturnToReserve(replaced);
            force.Participants.Add(BattleFixture.Put(state, "isengard", UnitKind.Regular, force.Region).Id);
        }
        if (card == Palantir)
            state.Units.Single(u => u.Id == SovereignCatalog.Denethor).Location = battle.Region;
        return state;
    }

    [Theory]
    [MemberData(nameof(KingsCards03EventTests.Cards), MemberType = typeof(KingsCards03EventTests))]
    public void EveryCombatValidatesModuleOwnerHandWindowAndHidesCommit(string card)
    {
        var state = Combat(card);
        state.Player(Side.Shadow).Hand = [card];
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, new() { CardId = card });
        state = Do(state, CommandKind.CommitCombatCard, new());
        var command = BattleFixture.Command(state, CommandKind.CommitCombatCard, new() { CardId = card });
        Assert.Null(LordsFixture.Engine().Validate(state, Side.Shadow, command));
        Assert.DoesNotContain(card, Json(state, Side.FreePeoples));
        state.Configuration.Kings = false;
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, command.Payload);
        state.Configuration.Kings = true;
        state.Player(Side.Shadow).Hand.Clear();
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, command.Payload);
        state.Player(Side.Shadow).Hand.Add(card);
        state.Phase = GamePhase.Fellowship;
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, command.Payload);
    }

    [Fact]
    public void DeadlyStrifeAddsTwoToBothRollsForBothArmiesAndResetsNextRound()
    {
        var state = KingsChieftainBattleFixture.Cards(Combat(Valinor), Valinor);
        var battle = BattleRuntime.Active(state)!;
        foreach (var force in new[] { battle.Attacker, battle.Defender })
        {
            Assert.Equal(2, force.Round.CombatModifier);
            Assert.Equal(2, force.Round.RerollModifier);
        }
        state = KingsChieftainBattleFixture.SkipRerolls(state);
        state = Do(state, CommandKind.ContinueBattle);
        if (state.Pending?.Kind == DecisionKind.BattleRetreat)
            state = Do(state, CommandKind.HoldBattle);
        battle = BattleRuntime.Active(state)!;
        Assert.Equal(2, battle.Round);
        Assert.Equal(0, battle.Attacker.Round.CombatModifier);
        Assert.Equal(0, battle.Defender.Round.RerollModifier);
    }

    [Theory]
    [InlineData(Denethor)]
    [InlineData(Threats)]
    public void DevilryRequiresIsengardArmyParticipantAndDefendingStronghold(string card)
    {
        var state = Combat(card);
        var battle = BattleRuntime.Active(state)!;
        var condition = EffectCatalog.All.Combats[card].Condition;
        Assert.True(condition.CanPlay(state, battle, Side.Shadow));
        var isengard = BattleRules.Units(state, battle.Defender).Single(u => u.Nation == "isengard");
        battle.Defender.Participants.Remove(isengard.Id);
        Assert.False(condition.CanPlay(state, battle, Side.Shadow));
        battle.Defender.Participants.Add(isengard.Id);
        battle.Mode = BattleMode.Field;
        Assert.False(condition.CanPlay(state, battle, Side.Shadow));
        state = KingsChieftainBattleFixture.Cards(Combat(card), card);
        battle = BattleRuntime.Active(state)!;
        Assert.Equal(1, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(0, battle.Force(Side.Shadow).Round.RerollModifier);
        Assert.Equal(0, battle.Force(Side.FreePeoples).Round.CombatModifier);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RelentlessCostsZeroToTwoActualHitsBeforeCombat(int paid)
    {
        var state = Combat(King);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        force.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Elite, force.Region).Id);
        state = KingsChieftainBattleFixture.Cards(state, King);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Empty(BattleRuntime.Active(state)!.Attacker.Round.Dice);
        var victims = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.Shadow))
            .Where(u => u.Kind == UnitKind.Regular).Take(paid).Select(u => BattleFixture.Loss(u.Id)).ToList();
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = victims }, new int[5]);
        Assert.Equal(paid, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(0, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.RerollModifier);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GreatHostCountsOnlySurvivorsAfterOrdinaryLosses(bool ordinaryLoss)
    {
        var state = Combat(Stormcrow);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        for (var i = 0; i < 2; i++)
            force.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Regular, force.Region).Id);
        state.Player(Side.Shadow).Hand = [Stormcrow];
        state.Player(Side.FreePeoples).Hand.Clear();
        state = Do(state, CommandKind.CommitCombatCard, new());
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = Stormcrow }, ordinaryLoss ? [5, 0, 0, 0, 0, 0] : new int[6]);
        state = KingsChieftainBattleFixture.SkipRerolls(state);
        if (ordinaryLoss)
        {
            Assert.Equal(Side.Shadow, state.Pending!.Owner);
            var id = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.Shadow)).First().Id;
            state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(id)] });
            Assert.NotEqual(DecisionKind.Losses, state.Pending!.Kind);
        }
        else
        {
            Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
            Assert.Equal(Side.FreePeoples, state.Pending.Owner);
            Assert.Equal(1, state.Pending.RequiredCount);
        }
    }

    [Theory]
    [InlineData(SovereignStatus.Initial, 1)]
    [InlineData(SovereignStatus.Awakened, 1)]
    [InlineData(SovereignStatus.Corrupted, 0)]
    public void PowerUsesSovereignInDefendersRegionWithoutParticipationRequirement(SovereignStatus status, int fpBonus)
    {
        var state = Combat(Palantir);
        var sovereign = Sovereign(state, SovereignCatalog.Denethor);
        var region = BattleRuntime.Active(state)!.Region;
        if (status == SovereignStatus.Awakened)
            SovereignLifecycle.Awaken(state, sovereign, region);
        if (status == SovereignStatus.Corrupted)
        {
            Corrupt(state, sovereign.Id);
            // Place the battle in Denethor's printed corrupted home.
            var battle = BattleRuntime.Active(state)!;
            foreach (var unit in BattleRules.Units(state, battle.Defender).ToArray())
                unit.Location = "minas-tirith";
            battle.Region = battle.Defender.Region = "minas-tirith";
        }
        state = KingsChieftainBattleFixture.Cards(state, Palantir);
        var resolved = BattleRuntime.Active(state)!;
        Assert.Equal(1, resolved.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(1, resolved.Force(Side.Shadow).Round.RerollModifier);
        Assert.Equal(fpBonus, resolved.Force(Side.FreePeoples).Round.CombatModifier);
        Assert.Equal(fpBonus, resolved.Force(Side.FreePeoples).Round.RerollModifier);
    }

    [Fact]
    public void PowerCannotUseSovereignInAttackersOriginAndMixedStatesRequireChoice()
    {
        var state = Battle();
        Assert.False(new HisPowerWaxesCondition().CanPlay(state, BattleRuntime.Active(state)!, Side.Shadow));
        state = MixedPower();
        Assert.Equal("Sovereign", state.Pending!.Presentation);
        Assert.Equal(2, state.Pending.PrivateChoices.Count);
        state = Choose(state, SovereignCatalog.Denethor, 0, 0, 0, 0);
        Assert.Equal(0, BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.CombatModifier);
    }

    internal static MatchState MixedPower()
    {
        var state = Combat(Palantir);
        var battle = BattleRuntime.Active(state)!;
        foreach (var unit in BattleRules.Units(state, battle.Defender).ToArray())
            unit.Location = "minas-tirith";
        battle.Region = battle.Defender.Region = "minas-tirith";
        Corrupt(state, SovereignCatalog.Denethor);
        state.Units.Single(u => u.Id == SovereignCatalog.Theoden).Location = battle.Region;
        return KingsChieftainBattleFixture.Cards(state, Palantir);
    }

    [Fact]
    public void DaringDefianceCancelsPowerBeforeAnyModifierOrSovereignChoice()
    {
        var state = Combat(Palantir);
        KingsCards01CombatTests.AddCompanion(state, Merry);
        state = KingsChieftainBattleFixture.Cards(state, Palantir, KingsChieftainBattleFixture.Daring);
        var battle = BattleRuntime.Active(state)!;
        Assert.True(battle.Force(Side.Shadow).Round.CardCancelled);
        Assert.Equal(0, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.NotEqual("Sovereign", state.Pending!.Presentation);
    }
}
