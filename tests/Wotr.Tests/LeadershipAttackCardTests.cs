using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CardPackBattleFixture;
using static Wotr.Domain.FreePeoplesCharacterCards03;

namespace Wotr.Tests;

public class LeadershipAttackCardTests
{
    [Theory]
    [InlineData(Mirror, true)]
    [InlineData(Challenge, true)]
    [InlineData(Gwaihir, false)]
    [InlineData(Swifter, false)]
    [InlineData(ThereAndBack, false)]
    [InlineData(Eagles, true)]
    public void EveryCombatHalfChecksItsPrintedPresenceCondition(string card, bool leaderEnough)
    {
        var state = BeforeCards(card);
        var battle = BattleRuntime.Active(state)!;
        Assert.True(EffectCatalog.Base.Combats[card].Condition.CanPlay(state, battle, Side.FreePeoples));
        foreach (var c in BattleRules.Characters(state, battle.Attacker))
            c.Location = "casualties";
        Assert.Equal(leaderEnough, EffectCatalog.Base.Combats[card].Condition.CanPlay(state, battle, Side.FreePeoples));
        foreach (var unit in BattleRules.Units(state, battle.Attacker).Where(u => u.Kind == UnitKind.Leader))
            BoardRules.RemoveCasualty(unit);
        CharacterFixture.Reject(state, CommandKind.CommitCombatCard, new() { CardId = card });
    }

    internal static MatchState SuddenLosses() => Reveal(BeforeCards(Challenge), Challenge, dice: [4, 0, 0, 0]);

    [Fact]
    public void SuddenStrikeResolvesImmediateLossesBeforeCombatWithoutSpendingLeadership()
    {
        var state = SuddenLosses();
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        Assert.Equal(1, BattleLosses.Current(state)!.Hits);
        Assert.Equal(4, battle.SpecialAttacks.Single().Dice.Count);
        Assert.Empty(battle.Attacker.Round.Dice);
        Assert.Empty(battle.Attacker.Round.ForfeitedLeadership);
        var loss = BattleRules.Units(state, battle.Defender).First(BoardRules.IsArmyUnit);
        state = CharacterFixture.Do(StateCodec.Copy(state), CommandKind.TakeBattleLosses,
            new() { Losses = [BattleFixture.Loss(loss.Id)] }, new SequenceRandom(new int[7]));
        battle = BattleRuntime.Active(state)!;
        Assert.Equal(2, battle.Defender.Round.Dice.Count);
        Assert.Equal(4, BattleLeadership.Total(state, battle.Attacker));
        Assert.Equal(11, state.RandomSamples.Count);
    }

    [Fact]
    public void SuddenStrikeCanEndTheBattleBeforeOrdinaryRolls()
    {
        var state = Reveal(BeforeCards(Challenge), Challenge, dice: [5, 5, 5, 0]);
        var battle = BattleRuntime.Active(state)!;
        var losses = BattleRules.Units(state, battle.Defender).Where(BoardRules.IsArmyUnit).Select(u => BattleFixture.Loss(u.Id)).ToList();
        state = CharacterFixture.Do(state, CommandKind.TakeBattleLosses, new() { Losses = losses });
        Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
        Assert.DoesNotContain(state.Events, e => e.Kind == "CombatRoll");
        Assert.Equal(4, state.RandomSamples.Count);
    }

    [Theory]
    [InlineData(false, 4)]
    [InlineData(true, 1)]
    public void SpecialAttackIgnoresCombatModifiersAndRerollBanButUsesAvailableLeadership(bool cancelCompanions, int dice)
    {
        var catalog = Opponent(new CancelLeadershipStep(cancelCompanions), 1);
        var state = Reveal(BeforeCards(Challenge, catalog: catalog), Challenge, catalog, new int[20]);
        var battle = BattleRuntime.Active(state)!;
        var special = Assert.Single(battle.SpecialAttacks);
        Assert.Equal(dice, special.Dice.Count);
        Assert.All(special.Dice, d => Assert.Equal(0, d.Modifier));
        Assert.Equal(-2, battle.Attacker.Round.CombatModifier);
        Assert.Empty(battle.Attacker.Round.ForfeitedLeadership);
    }

    [Fact]
    public void EqualInitiativeUsesDefenderFirstWhenCancellingSuddenStrikeLeadership()
    {
        var catalog = Opponent(new CancelLeadershipStep(true), 2);
        foreach (var fpAttacks in new[] { true, false })
        {
            var state = Reveal(BeforeCards(Challenge, fpAttacks: fpAttacks, catalog: catalog), Challenge, catalog, new int[20]);
            Assert.Equal(fpAttacks ? 1 : 4, BattleRuntime.Active(state)!.SpecialAttacks.Single().Dice.Count);
        }
    }

    [Fact]
    public void SpecialAttackCapsItsPoolAtFive()
    {
        var state = BeforeCards(Challenge);
        var force = BattleRuntime.Active(state)!.Attacker;
        for (var i = 0; i < 3; i++)
            force.Participants.Add(BattleFixture.Put(state, "gondor", UnitKind.Leader, force.Region).Id);
        state = Reveal(state, Challenge, dice: new int[20]);
        Assert.Equal(5, Assert.Single(BattleRuntime.Active(state)!.SpecialAttacks).Dice.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SpecialAttackUsesSixAgainstFortificationOrSiege(bool siege)
    {
        var state = BattleFixture.State();
        var from = siege ? "minas-morgul" : "minas-tirith";
        var to = siege ? from : "osgiliath";
        var army = BattleFixture.Put(state, "gondor", UnitKind.Regular, from);
        var leader = BattleFixture.Put(state, "gondor", UnitKind.Leader, from);
        BattleFixture.Put(state, "sauron", UnitKind.Regular, to).Garrison = siege;
        EventTests.GiveCard(state, Side.FreePeoples, Challenge);
        state = BattleFixture.Begin(state, [army.Id, leader.Id], from: from, to: to);
        state = Reveal(state, Challenge, dice: [4, 0, 0]);
        var die = Assert.Single(Assert.Single(BattleRuntime.Active(state)!.SpecialAttacks).Dice);
        Assert.Equal(6, die.HitTarget);
        Assert.False(die.Hit);
        Assert.Equal(DecisionKind.BattleReroll, state.Pending!.Kind);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BraveStandCountsCompanionFiguresEvenWithCancelledLeadership(bool white)
    {
        var catalog = Opponent(new CancelLeadershipStep(true), 1);
        var state = BeforeCards(ThereAndBack, white ? CharacterRules.White : CharacterRules.Aragorn, catalog: catalog);
        BattleRuntime.Active(state)!.WhiteRiderUsed = white;
        state = Reveal(state, ThereAndBack, catalog, new int[8]);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(2, battle.Defender.Round.CombatDicePenalty);
        Assert.Single(battle.Defender.Round.Dice);
        Assert.Equal(1, BattleLeadership.Total(state, battle.Attacker));
    }

    [Fact]
    public void BraveStandCountsSurvivingCompanionsAtExecutionAndDoesNotAffectEarlierSpecialAttack()
    {
        var catalog = Opponent(new StrikeThenRemoveCompanionStep(), 2);
        var state = Reveal(BeforeCards(ThereAndBack, catalog: catalog), ThereAndBack, catalog, new int[20]);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(3, battle.SpecialAttacks.Single().Dice.Count);
        Assert.Equal(1, battle.Defender.Round.CombatDicePenalty);
        Assert.Equal(2, battle.Defender.Round.Dice.Count);
    }

    private sealed record CancelLeadershipStep(bool Cancel) : EffectStep
    {
        public override void Apply(EffectContext context)
        {
            var force = BattleRuntime.Active(context.State)!.Opponent(context.Owner);
            force.Round.CombatModifier = -2;
            force.Round.RerollLimit = 0;
            if (Cancel)
                force.Round.CancelledLeadership.UnionWith(BattleRules.Characters(context.State, force).Select(c => c.Id));
        }
    }

    private sealed record StrikeThenRemoveCompanionStep : EffectStep
    {
        public override void Apply(EffectContext context)
        {
            var battle = BattleRuntime.Active(context.State)!;
            BattleRoll.Special(context.State, battle, context.Owner, "test", 3, context.Random);
            CharacterLifecycle.Eliminate(context.State, BattleRules.Characters(context.State, battle.Opponent(context.Owner)).First());
        }
    }
}
