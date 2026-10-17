using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards02;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.CardPackBattleFixture;

namespace Wotr.Tests;

public class Strategy02CombatTests
{
    [Theory]
    [InlineData(WisdomOfElrond)]
    [InlineData(ThroughDayNight)]
    public void ConfusionCountsNaturalOnesAsOwnHitsAndForbidsTheirReroll(string card)
    {
        var state = Reveal(BeforeCards(card), card, dice: [0, 0, 0, 0, 0, 0, 0, 1]);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(2, BattleRoll.HitCount(battle.Attacker));
        Assert.Equal(0, BattleRoll.HitCount(battle.Defender));
        Assert.Equal(2, battle.Defender.Round.Dice.Count(d => d.CannotReroll));
        Assert.Single(BattleRoll.Failed(battle.Defender));
        Assert.Equal(DecisionKind.BattleReroll, state.Pending!.Kind);
        state = Do(state, CommandKind.RerollBattle);
        Reject(state, CommandKind.RerollBattle, new() { Ids = [battle.Defender.Round.Dice[0].Id] });
        state = Do(state, CommandKind.RerollBattle);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Equal(2, state.Pending.RequiredCount);
        Assert.Equal(8, state.RandomSamples.Count);
    }

    [Fact]
    public void ConfusionIgnoresModifiedOnesAndRerollOnes()
    {
        var catalog = Opponent(new CombatModifierStep(-1, -1), 3);
        var state = Reveal(BeforeCards(WisdomOfElrond, catalog: catalog), WisdomOfElrond, catalog, 0, 0, 0, 0, 0, 1, 1, 1);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(0, BattleRoll.HitCount(battle.Attacker));
        Assert.All(battle.Defender.Round.Dice, d => Assert.False(d.CannotReroll));
        state = Do(state, CommandKind.RerollBattle, catalog: catalog);
        state = Do(state, CommandKind.RerollBattle, new() { Ids = BattleRoll.Failed(battle.Defender) }, new SequenceRandom(0, 0, 0), catalog);
        Assert.Equal(0, BattleRoll.HitCount(BattleRuntime.Active(state)!.Attacker));
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
    }

    [Fact]
    public void CancelledConfusionCannotPreventShadowRerolls()
    {
        var catalog = Opponent(new CancelStep(), 0);
        var state = Reveal(BeforeCards(ThroughDayNight, catalog: catalog), ThroughDayNight, catalog, new int[8]);
        var battle = BattleRuntime.Active(state)!;
        Assert.True(battle.Attacker.Round.CardCancelled);
        Assert.Equal(0, BattleRoll.HitCount(battle.Attacker));
        Assert.Equal(3, BattleRoll.Failed(battle.Defender).Count);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    public void FearShieldWallCancelsOneAtTwoEnemyHits(int hits, int cancelled)
    {
        var dice = new int[8];
        for (var i = 0; i < hits; i++)
            dice[5 + i] = 5;
        var state = NoRerolls(Reveal(BeforeCards(FearFireFoes), FearFireFoes, dice: dice));
        Assert.Equal(cancelled, BattleRuntime.Active(state)!.Attacker.Round.CancelHits);
    }

    [Fact]
    public void ArrowScoutsRequiresDefenderAndRetreatsBeforeAnyRoll()
    {
        var state = BeforeCards(RedArrow);
        Assert.False(EffectCatalog.Base.Combats[RedArrow].Condition.CanPlay(state, BattleRuntime.Active(state)!, Side.FreePeoples));
        state = Reveal(BeforeCards(RedArrow, fpAttacks: false), RedArrow);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [state.Pending!.PrivateChoices[0]] });
        Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
        Assert.Empty(state.RandomSamples);
        state = Do(state, CommandKind.AdvanceBattle);
        var report = BattleArchiveTests.Completed(state, BattleMode.Field);
        Assert.Equal(RedArrow, report.Rounds.Single().Result!.Defender.CardId);
        Assert.Empty(report.Rounds[0].Result!.Attacker.Dice);
        Assert.Contains(report.Steps, s => s.Kind == "BattleRetreat");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HelpNoQuarterNeedsOneActualRollHit(bool hit)
    {
        var state = NoRerolls(Reveal(BeforeCards(HelpUnlookedFor), HelpUnlookedFor, dice: [hit ? 5 : 0, 0, 0, 0, 0, 0, 0, 0]));
        Assert.Equal(hit ? 2 : 0, BattleRoll.HitCount(BattleRuntime.Active(state)!.Attacker));
    }

    [Fact]
    public void WosesSuddenStrikeRequiresLeaderAndAppliesSpecialLossBeforeCombat()
    {
        var state = BeforeCards(PathsOfWoses);
        var battle = BattleRuntime.Active(state)!;
        var condition = EffectCatalog.Base.Combats[PathsOfWoses].Condition;
        var original = battle.Attacker.Participants.ToList();
        battle.Attacker.Participants.RemoveAll(id => state.Characters.Any(c => c.Id == id) || state.Units.Any(u => u.Id == id && u.Kind == UnitKind.Leader));
        Assert.False(condition.CanPlay(state, battle, Side.FreePeoples));
        battle.Attacker.Participants = original;
        state = Reveal(state, PathsOfWoses, dice: [5, 0, 0, 0]);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(1, state.Pending.RequiredCount);
        Assert.Empty(BattleRuntime.Active(state)!.Attacker.Round.Dice);
        Assert.Equal(4, state.RandomSamples.Count);
        var report = BattleArchiveTests.Report(state, battle.Id);
        var special = Assert.Single(report.Latest!.SpecialAttacks);
        Assert.Equal(PathsOfWoses, special.SourceId);
        Assert.True(special.Dice[0].Hit);
        Assert.Empty(report.Latest.Attacker.Dice);
    }

    private sealed record CancelStep : EffectStep
    {
        public override void Apply(EffectContext context) => BattleRuntime.Active(context.State)!.Opponent(context.Owner).Round.CardCancelled = true;
    }
}
