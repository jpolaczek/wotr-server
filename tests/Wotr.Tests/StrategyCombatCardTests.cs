using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards01;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.CardPackBattleFixture;

namespace Wotr.Tests;

public class StrategyCombatCardTests
{
    [Fact]
    public void DaylightCapsShadowCombatRollAtThree()
    {
        var state = BeforeCards(LastBattle);
        var battle = BattleRuntime.Active(state)!;
        for (var i = 0; i < 3; i++)
            battle.Defender.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Regular, battle.Region).Id);
        state = Reveal(state, LastBattle, dice: new int[8]);
        Assert.Equal(3, BattleRuntime.Active(state)!.Defender.Round.Dice.Count);
        Assert.Equal(5, BattleRuntime.Active(state)!.Attacker.Round.Dice.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NoQuarterRequiresActualCombatOrRerollHit(bool hit)
    {
        var state = Reveal(BeforeCards(PowerTooGreat), PowerTooGreat, dice: [hit ? 5 : 0, 0, 0, 0, 0, 0, 0, 0]);
        state = NoRerolls(state);
        Assert.Equal(hit ? 2 : 0, BattleRoll.HitCount(BattleRuntime.Active(state)!.Attacker));
    }

    [Theory]
    [InlineData(TomBombadil)]
    [InlineData(BookOfMazarbul)]
    public void AdvantageousPositionUsesBordersAndModifiesOnlyShadowCombatWithNaturalSixHit(string card)
    {
        var state = BeforeCards(card);
        var battle = BattleRuntime.Active(state)!;
        var condition = EffectCatalog.Base.Combats[card].Condition;
        Assert.True(condition.CanPlay(state, battle, Side.FreePeoples));
        battle.Region = "fangorn";
        Assert.False(condition.CanPlay(state, battle, Side.FreePeoples));
        battle.Region = BattleFixture.Target;
        state = Reveal(state, card, dice: [0, 0, 0, 0, 0, 4, 5, 3]);
        var shadow = BattleRuntime.Active(state)!.Defender;
        Assert.Equal(-1, shadow.Round.CombatModifier);
        Assert.Equal(0, shadow.Round.RerollModifier);
        Assert.Equal(new[] { false, true, false }, shadow.Round.Dice.Select(d => d.Hit));
        Assert.Equal(0, BattleRuntime.Active(state)!.Attacker.Round.RerollModifier);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    public void ShieldWallCancelsOneOnlyAtTwoNormalHitsAndPreservesDice(int hits, int cancelled)
    {
        var dice = new int[8];
        for (var i = 0; i < hits; i++)
            dice[5 + i] = 5;
        var state = NoRerolls(Reveal(BeforeCards(FaramirsRangers), FaramirsRangers, dice: dice));
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(cancelled, battle.Attacker.Round.CancelHits);
        Assert.Equal(hits, BattleRoll.HitCount(battle.Defender));
        Assert.Equal(hits, battle.Defender.Round.Dice.Count(d => d.Hit));
    }

    internal static MatchState ScoutsChoice()
    {
        var state = BeforeCards(SpiritOfMordor, fpAttacks: false);
        return Reveal(state, SpiritOfMordor);
    }

    [Fact]
    public void ScoutsRetreatsBeforeRollAndDoesNotActivateNation()
    {
        var state = ScoutsChoice();
        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        Assert.Equal("RegionOrShelter", state.Pending.Presentation);
        Assert.Empty(state.RandomSamples);
        state.Nations.Single(n => n.Id == "gondor").Active = false;
        var target = state.Pending.PrivateChoices.First(r => r != ScoutsStep.Shelter);
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["mount-doom"] });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [target] });
        Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
        Assert.False(state.Nations.Single(n => n.Id == "gondor").Active);
        Assert.Empty(state.RandomSamples);
        Assert.All(state.Characters.Where(c => c.Side == Side.FreePeoples), c => Assert.Equal(target, c.Location));
    }

    [Fact]
    public void ScoutsCannotBePlayedAttackingOrInSiegeButAllowsReliefDefense()
    {
        var state = BeforeCards(SpiritOfMordor);
        var battle = BattleRuntime.Active(state)!;
        var condition = new ScoutsCondition();
        Assert.False(condition.CanPlay(state, battle, Side.FreePeoples));
        state = BeforeCards(SpiritOfMordor, fpAttacks: false);
        battle = BattleRuntime.Active(state)!;
        foreach (var mode in Enum.GetValues<BattleMode>())
        {
            battle.Mode = mode;
            Assert.Equal(mode is BattleMode.Field or BattleMode.Relief, condition.CanPlay(state, battle, Side.FreePeoples));
        }
    }

    [Fact]
    public void ScoutsCanWithdrawIntoStrongholdAndAttackerChoosesWhetherToAdvance()
    {
        var state = BattleFixture.State(Side.Shadow);
        var enemy = BattleFixture.Put(state, "isengard", UnitKind.Regular, "westemnet");
        BattleFixture.Put(state, "rohan", UnitKind.Regular, "helms-deep");
        EventTests.GiveCard(state, Side.FreePeoples, SpiritOfMordor);
        state = BattleFixture.Begin(state, [enemy.Id], from: "westemnet", to: "helms-deep");
        state = Do(state, CommandKind.FightFieldBattle);
        state = Reveal(state, SpiritOfMordor);
        Assert.Contains(ScoutsStep.Shelter, state.Pending!.PrivateChoices);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [ScoutsStep.Shelter] });
        Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
        Assert.True(BattleRuntime.Active(state)!.DefenderSheltered);
        Assert.Empty(state.RandomSamples);
        state = Do(state, CommandKind.AdvanceBattle, new() { Ids = [enemy.Id] });
        Assert.True(BoardRules.Besieged(state, "helms-deep", Side.FreePeoples));
    }

    [Fact]
    public void ScoutsOverstackingResolvesBeforeAttackerAdvance()
    {
        var state = ScoutsChoice();
        for (var i = 0; i < 10; i++)
            BattleFixture.Put(state, "gondor", UnitKind.Regular, "pelargir");
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["pelargir"] });
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        var units = state.Units.Where(u => u.Location == "pelargir" && BoardRules.IsArmyUnit(u)).Take(3).Select(u => u.Id).ToList();
        state = Do(state, CommandKind.RemoveExcess, new() { Ids = units });
        Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
        Assert.All(units, id => Assert.Equal("reserve", state.Units.Single(u => u.Id == id).Location));
        Assert.Empty(state.RandomSamples);
    }

    [Fact]
    public void InitiativeZeroCancellationPreventsScoutsButScoutsStopsLaterEffects()
    {
        var catalog = Opponent(new CancelOpposingStep(), 0);
        var state = Reveal(BeforeCards(SpiritOfMordor, fpAttacks: false, catalog: catalog), SpiritOfMordor, catalog, new int[8]);
        Assert.Equal(DecisionKind.BattleReroll, state.Pending!.Kind);
        Assert.True(BattleRuntime.Active(state)!.Defender.Round.CardCancelled);
        Assert.NotEmpty(state.RandomSamples);
        catalog = Opponent(new LaterAttackStep(), 2);
        state = Reveal(BeforeCards(SpiritOfMordor, fpAttacks: false, catalog: catalog), SpiritOfMordor, catalog);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [state.Pending!.PrivateChoices[0]] }, catalog: catalog);
        Assert.DoesNotContain(state.Events, e => e.Kind == "LaterAttack");
        Assert.Empty(state.RandomSamples);
    }

    [Fact]
    public void NoQuarterMayTriggerFromLeaderRerollAloneAndShieldWallDoesNotEraseIt()
    {
        var state = Reveal(BeforeCards(PowerTooGreat), PowerTooGreat, dice: new int[8]);
        var die = BattleRuntime.Active(state)!.Attacker.Round.Dice[0].Id;
        state = Do(state, CommandKind.RerollBattle, new() { Ids = [die] });
        state = Do(state, CommandKind.RerollBattle, random: new SequenceRandom(5));
        Assert.Equal(2, BattleRoll.HitCount(BattleRuntime.Active(state)!.Attacker));
        state = Reveal(BeforeCards(FaramirsRangers), FaramirsRangers, dice: new int[8]);
        state = Do(state, CommandKind.RerollBattle);
        state = Do(state, CommandKind.RerollBattle, new() { Ids = BattleRuntime.Active(state)!.Defender.Round.Dice.Take(2).Select(d => d.Id).ToList() }, new SequenceRandom(5, 5));
        Assert.Equal(2, BattleRuntime.Active(state)!.Defender.Round.Dice.Count(d => d.RerollHit));
        Assert.Equal(1, BattleRuntime.Active(state)!.Attacker.Round.CancelHits);
    }

    private sealed record CancelOpposingStep : EffectStep
    {
        public override void Apply(EffectContext context) => BattleRuntime.Active(context.State)!.Opponent(context.Owner).Round.CardCancelled = true;
    }

    private sealed record LaterAttackStep : EffectStep
    {
        public override void Apply(EffectContext context) => context.Record("LaterAttack", "This later initiative must not run after Scouts.");
    }
}
