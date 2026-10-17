using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards03;
using static Wotr.Tests.CardPackBattleFixture;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Strategy03Fixture;

namespace Wotr.Tests;

public class Strategy03CombatTests
{
    [Theory]
    [InlineData(CirdansShips)]
    [InlineData(GuardsOfCitadel)]
    public void ChargeRequiresParticipatingEliteAndAppliesSpecialDamageBeforeCombat(string card)
    {
        var state = Charge(card);
        var battle = BattleRuntime.Active(state)!;
        var condition = EffectCatalog.Base.Combats[card].Condition;
        var participants = battle.Attacker.Participants.ToList();
        battle.Attacker.Participants.RemoveAll(id => state.Units.Any(u => u.Id == id && u.Kind == UnitKind.Elite));
        Assert.False(condition.CanPlay(state, battle, Side.FreePeoples));
        battle.Attacker.Participants = participants;
        state = Reveal(state, card, dice: [5, 0]);
        battle = BattleRuntime.Active(state)!;
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        Assert.Empty(battle.Defender.Round.Dice);
        Assert.Equal(2, state.RandomSamples.Count);
        var victim = BattleRules.Units(state, battle.Defender).First(BoardRules.IsArmyUnit).Id;
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(victim)] }, new SequenceRandom(new int[7]));
        Assert.Equal(2, BattleRuntime.Active(state)!.Defender.Round.Dice.Count);
    }

    [Theory]
    [InlineData(true, BattleMode.Field, 1, 6)]
    [InlineData(true, BattleMode.Field, 2, 5)]
    [InlineData(true, BattleMode.Siege, 2, 6)]
    [InlineData(false, BattleMode.Field, 1, 5)]
    public void ChargeUsesUnmodifiedCombatOddsAndCapsAtFive(bool attacking, BattleMode mode, int round, int target)
    {
        var catalog = Opponent(new CombatModifierStep(3, 3, true), 1);
        var state = Charge(elites: 6, fpAttacks: attacking, catalog: catalog);
        var battle = BattleRuntime.Active(state)!;
        battle.Mode = mode;
        battle.Round = round;
        battle.Region = "pelargir";
        state = Reveal(state, CirdansShips, catalog, Enumerable.Repeat(4, 13).ToArray());
        var attack = Assert.Single(BattleRuntime.Active(state)!.SpecialAttacks);
        Assert.Equal(5, attack.Dice.Count);
        Assert.All(attack.Dice, d =>
        {
            Assert.Equal(0, d.Modifier);
            Assert.Equal(target, d.HitTarget);
            Assert.Equal(target == 5, d.Hit);
        });
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 0)]
    public void ChargeCountsElitesRemainingAfterEarlierDefenderSpecialAttack(int hits, int remaining)
    {
        var catalog = Opponent(new SpecialStep(hits), 2);
        var state = Reveal(Charge(catalog: catalog), CirdansShips, catalog, Enumerable.Repeat(5, hits).ToArray());
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        var force = BattleRuntime.Active(state)!.Attacker;
        var losses = BattleRules.Units(state, force).Where(u => u.Kind == UnitKind.Elite).Take(hits)
            .Select(u => BattleFixture.Loss(u.Id, BattleLossKind.Downgrade)).ToList();
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = losses }, new SequenceRandom(new int[9]), catalog);
        var charge = BattleRuntime.Active(state)!.SpecialAttacks.Single(a => a.Owner == Side.FreePeoples);
        Assert.Equal(remaining, charge.Dice.Count);
    }

    [Fact]
    public void DefenderChargeRunsFirstAtTiedInitiativeAndArmyDestructionStopsLaterAttack()
    {
        var catalog = Opponent(new SpecialStep(3), 2);
        var state = Reveal(Charge(fpAttacks: false, catalog: catalog), CirdansShips, catalog, 0, 0, 5, 5, 5);
        Assert.Equal(Side.FreePeoples, BattleRuntime.Active(state)!.SpecialAttacks[0].Owner);
        state = Reveal(Charge(elites: 1, catalog: catalog), CirdansShips, catalog, 5, 5, 5);
        var force = BattleRuntime.Active(state)!.Attacker;
        var losses = BattleRules.Units(state, force).Where(BoardRules.IsArmyUnit).Select(u => BattleFixture.Loss(u.Id)).ToList();
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = losses }, catalog: catalog);
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Single(state.Events, e => e.Kind == "SpecialAttack");
    }

    [Fact]
    public void CancelledChargeDoesNotRoll()
    {
        var catalog = Opponent(new CancelStep(), 0);
        var state = Reveal(Charge(catalog: catalog), CirdansShips, catalog, new int[8]);
        Assert.Empty(BattleRuntime.Active(state)!.SpecialAttacks);
        Assert.True(BattleRuntime.Active(state)!.Attacker.Round.CardCancelled);
    }

    [Theory]
    [InlineData(CelebornsGaladhrim)]
    [InlineData(RidersOfTheoden)]
    public void DaylightLimitsEnemyCombatRollToThree(string card)
    {
        var state = BeforeCards(card);
        var force = BattleRuntime.Active(state)!.Defender;
        for (var i = 0; i < 3; i++)
            force.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Regular, force.Region).Id);
        state = Reveal(state, card);
        Assert.Equal(3, BattleRuntime.Active(state)!.Defender.Round.Dice.Count);
    }

    [Fact]
    public void GrimbeornScoutsRequiresDefenderAndRetreatsBeforeRoll()
    {
        var state = BeforeCards(Grimbeorn);
        Assert.False(EffectCatalog.Base.Combats[Grimbeorn].Condition.CanPlay(state, BattleRuntime.Active(state)!, Side.FreePeoples));
        state = Reveal(BeforeCards(Grimbeorn, fpAttacks: false), Grimbeorn);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [state.Pending!.PrivateChoices[0]] });
        Assert.Empty(state.RandomSamples);
        Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    public void ImrahilShieldWallNeedsTwoOpponentHits(int hits, int cancelled)
    {
        var dice = new int[8];
        for (var i = 0; i < hits; i++)
            dice[5 + i] = 5;
        var state = NoRerolls(Reveal(BeforeCards(Imrahil), Imrahil, dice: dice));
        Assert.Equal(cancelled, BattleRuntime.Active(state)!.Attacker.Round.CancelHits);
    }

    private sealed record SpecialStep(int Dice) : EffectStep
    {
        public override void Apply(EffectContext context) => BattleRoll.Special(context.State,
            BattleRuntime.Active(context.State)!, context.Owner, context.Execution.SourceId, Dice, context.Random);
    }

    private sealed record CancelStep : EffectStep
    {
        public override void Apply(EffectContext context) => BattleRuntime.Active(context.State)!.Opponent(context.Owner).Round.CardCancelled = true;
    }
}
