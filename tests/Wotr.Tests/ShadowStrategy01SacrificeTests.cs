using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards01;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy01Fixture;

namespace Wotr.Tests;

public class ShadowStrategy01SacrificeTests
{
    [Theory]
    [InlineData(UrukHai, 4)]
    [InlineData(Dawn, 2)]
    public void VoluntaryLossesExposeZeroToLimitAndRejectInvalidFiguresWithoutMutation(string card, int maximum)
    {
        var state = Combat(card, own: 5);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        var rear = BattleFixture.Put(state, "sauron", UnitKind.Regular, force.Region);
        var nazgul = BattleFixture.Put(state, "sauron", UnitKind.Nazgul, force.Region);
        force.Participants.Add(nazgul.Id);
        state = Shadow01Fixture.Reveal(state, card, dice: new int[8]);
        state = CardPackBattleFixture.NoRerolls(state);
        Assert.Equal("VoluntaryLosses", state.Pending!.Presentation);
        var options = new BattleOptions().GetOptions(state, Side.Shadow).Single();
        Assert.Equal(0, options.Minimum);
        Assert.Equal(maximum, options.Maximum);
        Assert.DoesNotContain(rear.Id, options.EligibleIds);
        Assert.DoesNotContain(nazgul.Id, options.EligibleIds);
        Reject(state, CommandKind.TakeBattleLosses, Sacrifice(state, maximum + 1));
        Reject(state, CommandKind.TakeBattleLosses, Losses(rear.Id));
        Reject(state, CommandKind.TakeBattleLosses, Losses(nazgul.Id));
        Reject(state, CommandKind.TakeBattleLosses, Losses("unknown"));
        var own = options.EligibleIds[0];
        Reject(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(own), BattleFixture.Loss(own)] });
        Reject(state, CommandKind.TakeBattleLosses, Losses(own, BattleLossKind.Downgrade));
        Reject(state, CommandKind.TakeBattleLosses, Losses(own, (BattleLossKind)999));
        var opponent = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.FreePeoples)).First();
        Reject(state, CommandKind.TakeBattleLosses, Losses(opponent.Id));
    }

    [Theory]
    [InlineData(UrukHai, false)]
    [InlineData(UrukHai, true)]
    [InlineData(Dawn, false)]
    [InlineData(Dawn, true)]
    public void ZeroSacrificeIsExplicitAndDoesNotRemoveFiguresOrAddBonus(string card, bool shadowAttacks)
    {
        var state = Shadow01Fixture.Reveal(Combat(card, shadowAttacks), card, dice: new int[6]);
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow,
            BattleFixture.Command(state, CommandKind.TakeBattleLosses, new()));
        Assert.True(preview.Legal);
        Assert.Equal(before, StateCodec.Write(state));
        var random = new SequenceRandom(new int[6]);
        state = Do(StateCodec.Copy(state), CommandKind.TakeBattleLosses, random: random);
        Assert.Equal(card == Dawn ? 6 : 0, random.Calls);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(3, BattleRules.Strength(state, battle.Force(Side.Shadow)));
        Assert.Equal(0, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.Empty(battle.SpecialAttacks);
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OnslaughtWaitsForBothOrdinaryLossesBeforeOfferingSacrifice(bool shadowAttacks)
    {
        var state = Shadow01Fixture.Reveal(Combat(UrukHai, shadowAttacks), UrukHai, dice: [5, 0, 0, 5, 0, 0]);
        for (var i = 0; i < 2; i++)
        {
            Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
            Assert.Equal("combat", BattleLosses.Current(state)!.SourceId);
            Assert.False(BattleLosses.Current(state)!.Voluntary);
            var force = BattleRuntime.Active(state)!.Force(state.Pending.Owner);
            var victim = BattleRules.Units(state, force).First(BoardRules.IsArmyUnit);
            state = Do(state, CommandKind.TakeBattleLosses, Losses(victim.Id));
        }
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Equal("VoluntaryLosses", state.Pending.Presentation);
        Assert.Equal(2, BattleRules.Strength(state, BattleRuntime.Active(state)!.Force(Side.Shadow)));
        Assert.Equal(2, BattleRules.Strength(state, BattleRuntime.Active(state)!.Force(Side.FreePeoples)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OnslaughtStillRollsAfterOwnEliminationAndMutualDestructionDoesNotCapture(bool shadowAttacks)
    {
        var state = Shadow01Fixture.Reveal(Combat(UrukHai, shadowAttacks, own: 1, enemy: 1), UrukHai, dice: [0, 0]);
        var control = state.Control[BattleFixture.Target];
        var random = new SequenceRandom(3);
        state = Do(state, CommandKind.TakeBattleLosses, Sacrifice(state, 1), random);
        Assert.Equal(1, random.Calls);
        var battle = BattleRuntime.Active(state)!;
        Assert.False(BattleRules.Alive(state, battle.Force(Side.Shadow)));
        var attack = Assert.Single(battle.SpecialAttacks);
        Assert.True(Assert.Single(attack.Dice).Hit);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Equal(UrukHai, BattleLosses.Current(state)!.SourceId);
        var enemy = BattleRules.Units(state, battle.Force(Side.FreePeoples)).Single();
        state = Do(state, CommandKind.TakeBattleLosses, Losses(enemy.Id));
        Assert.Null(BattleRuntime.Active(state));
        Assert.Equal(control, state.Control[BattleFixture.Target]);
        Assert.DoesNotContain(state.Units, unit => BoardRules.IsArmyUnit(unit) &&
            unit.Location is BattleFixture.Origin or BattleFixture.Target);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
    }

    [Fact]
    public void OnslaughtDoesNotStartIfNormalCasualtiesEliminatedOwnArmy()
    {
        var state = Shadow01Fixture.Reveal(Combat(UrukHai, own: 1, enemy: 1), UrukHai, dice: [0, 5]);
        Assert.Equal("combat", BattleLosses.Current(state)!.SourceId);
        var victim = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.Shadow)).Single();
        state = Do(state, CommandKind.TakeBattleLosses, Losses(victim.Id));
        Assert.Null(BattleRuntime.Active(state));
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        Assert.DoesNotContain(state.Events, e => e.Kind == "SpecialAttack");
        Assert.Equal(2, state.RandomSamples.Count);
    }

    [Fact]
    public void OnslaughtRollsOneUnmodifiedDiePerPaidHitWithFourPlusTargetAndNoReroll()
    {
        var state = Shadow01Fixture.Reveal(Combat(UrukHai, own: 5), UrukHai, dice: new int[8]);
        var battle = BattleRuntime.Active(state)!;
        battle.Force(Side.Shadow).Round.CombatModifier = 4;
        battle.Force(Side.Shadow).Round.RerollModifier = 4;
        battle.Force(Side.FreePeoples).Round.CancelHits = 5;
        var random = new SequenceRandom(2, 3, 0, 5);
        state = Do(state, CommandKind.TakeBattleLosses, Sacrifice(state, 4), random);
        Assert.Equal(4, random.Calls);
        var attack = Assert.Single(BattleRuntime.Active(state)!.SpecialAttacks);
        Assert.Equal(4, attack.Dice.Count);
        Assert.Equal(2, attack.Dice.Count(die => die.Hit));
        Assert.All(attack.Dice, die =>
        {
            Assert.Equal(0, die.Modifier);
            Assert.Equal(4, die.HitTarget);
            Assert.Null(die.Reroll);
        });
        Assert.Equal(2, state.Pending!.RequiredCount);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RelentlessAppliesLossesBeforeCombatAndModifiesOnlyCombat(bool shadowAttacks)
    {
        var state = Combat(Dawn, shadowAttacks, own: 5);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        var nazgul = BattleFixture.Put(state, "sauron", UnitKind.Nazgul, force.Region);
        force.Participants.Add(nazgul.Id);
        state = Shadow01Fixture.Reveal(state, Dawn);
        Assert.Empty(state.RandomSamples);
        state = Do(state, CommandKind.TakeBattleLosses, Sacrifice(state, 2), new SequenceRandom(new int[6]));
        force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.Equal(3, force.Round.Dice.Count);
        Assert.Equal(2, force.Round.CombatModifier);
        Assert.Equal(0, force.Round.RerollModifier);
        Assert.All(force.Round.Dice, die => Assert.False(die.Hit));
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        state = Do(state, CommandKind.RerollBattle, new() { Ids = [force.Round.Dice[0].Id] }, new SequenceRandom(3));
        force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.Equal(0, force.Round.Dice[0].RerollModifier);
        Assert.False(force.Round.Dice[0].RerollHit);
    }

    [Fact]
    public void RelentlessStillPaysTwoHitsWhenAdvantageousPositionReducesItsBonus()
    {
        const string position = FreePeoplesStrategyCards01.TomBombadil;
        var state = Shadow01Fixture.BeforeCombat(Dawn, true, fpCard: position);
        state = Shadow01Fixture.Reveal(state, Dawn, position);
        Assert.Equal("VoluntaryLosses", state.Pending!.Presentation);
        state = Do(state, CommandKind.TakeBattleLosses, Sacrifice(state, 2), new SequenceRandom(new int[6]));
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.Equal(1, BattleRules.Strength(state, force));
        Assert.Equal(1, force.Round.CombatModifier);
        Assert.Equal(0, force.Round.RerollModifier);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RelentlessEliminatingOwnArmyEndsBattleBeforeAnyCombatRoll(bool shadowAttacks)
    {
        var state = Shadow01Fixture.Reveal(Combat(Dawn, shadowAttacks, own: 1), Dawn);
        var random = new SequenceRandom();
        state = Do(state, CommandKind.TakeBattleLosses, Sacrifice(state, 1), random);
        Assert.Equal(0, random.Calls);
        Assert.Empty(state.RandomSamples);
        Assert.DoesNotContain(state.Events, e => e.Kind is "CombatRoll" or "SpecialAttack");
        Assert.Equal(shadowAttacks ? DecisionKind.Action : DecisionKind.BattleAdvance, state.Pending!.Kind);
    }

    [Theory]
    [InlineData(UrukHai, BattleLossKind.Downgrade, 1)]
    [InlineData(UrukHai, BattleLossKind.Remove, 2)]
    [InlineData(Dawn, BattleLossKind.Downgrade, 1)]
    [InlineData(Dawn, BattleLossKind.Remove, 2)]
    public void EliteSacrificePaysChosenHitsAndReplacementParticipates(string card, BattleLossKind kind, int hits)
    {
        var state = Shadow01Fixture.Reveal(Combat(card, own: 3, elite: true), card);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        var elite = BattleRules.Units(state, force).Single(u => u.Kind == UnitKind.Elite);
        var beforeIds = force.Participants.ToHashSet();
        state = Do(state, CommandKind.TakeBattleLosses, Losses(elite.Id, kind), new SequenceRandom(new int[6]));
        var battle = BattleRuntime.Active(state)!;
        force = battle.Force(Side.Shadow);
        Assert.Equal(kind == BattleLossKind.Downgrade ? 3 : 2, BattleRules.Strength(state, force));
        Assert.Equal(card == Dawn ? hits : 0, force.Round.CombatModifier);
        if (card == UrukHai)
            Assert.Equal(hits, Assert.Single(battle.SpecialAttacks).Dice.Count);
        if (kind == BattleLossKind.Downgrade)
        {
            var replacement = BattleRules.Units(state, force).Single(u => !beforeIds.Contains(u.Id));
            Assert.Equal(UnitKind.Regular, replacement.Kind);
        }
    }

    [Theory]
    [InlineData(UrukHai)]
    [InlineData(Dawn)]
    public void EliteDowngradeWithoutReplacementStillPaysExactlyOneHit(string card)
    {
        var state = Combat(card, own: 2, elite: true);
        foreach (var regular in state.Units.Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == "reserve"))
            regular.Location = "gorgoroth";
        state = Shadow01Fixture.Reveal(state, card);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        var elite = BattleRules.Units(state, force).Single(u => u.Kind == UnitKind.Elite);
        state = Do(state, CommandKind.TakeBattleLosses, Losses(elite.Id, BattleLossKind.Downgrade), new SequenceRandom(new int[4]));
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(1, BattleRules.Strength(state, battle.Force(Side.Shadow)));
        if (card == UrukHai)
            Assert.Single(Assert.Single(battle.SpecialAttacks).Dice);
        else
            Assert.Equal(1, battle.Force(Side.Shadow).Round.CombatModifier);
    }

    private static CommandPayload Losses(string id, BattleLossKind kind = BattleLossKind.Remove) =>
        new() { Losses = [BattleFixture.Loss(id, kind)] };
}
