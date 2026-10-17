using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards02;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Strategy01Fixture;
using static Wotr.Tests.Strategy02Fixture;

namespace Wotr.Tests;

public class HelpUnlookedForTests
{
    [Theory]
    [InlineData(2, 8, 3)]
    [InlineData(5, 8, 1)]
    [InlineData(2, 2, 1)]
    public void ReliefReducesAfterFiveDiceCapAndPersistsForEveryRound(int garrison, int shadow, int dice)
    {
        var state = Relief(garrison, shadow);
        state.Nations.Single(n => n.Id == "gondor").StepsToWar = 0;
        state = Play(state, HelpUnlookedFor);
        state = Do(state, CommandKind.AttackArmy, Attack(state));
        var battle = BattleRuntime.Current(state)!;
        Assert.Equal(BattleMode.Relief, battle.Mode);
        Assert.Equal(dice, BattleRoll.DiceCount(state, battle.Defender));
        Assert.Equal(garrison, battle.Defender.CombatDiceReduction);
        Assert.DoesNotContain(battle.Attacker.Participants, id => state.Units.Any(u => u.Id == id && u.Garrison));
        state = BattleFixture.NoCards(state, Enumerable.Repeat(1, 3 + dice).ToArray());
        state = CardPackBattleFixture.NoRerolls(state);
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
        state = Do(state, CommandKind.ContinueBattle);
        state = Do(state, CommandKind.HoldBattle);
        Assert.Equal(2, BattleRuntime.Current(state)!.Round);
        Assert.Equal(dice, BattleRoll.DiceCount(state, BattleRuntime.Current(state)!.Defender));
        state = BattleFixture.NoCards(state, Enumerable.Repeat(1, 3 + dice).ToArray());
        state = CardPackBattleFixture.NoRerolls(state);
        state = Do(state, CommandKind.StopBattle);
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Fact]
    public void ReliefRequiresLegalAtWarAttackerAndRejectsOtherBattleOrExtraDie()
    {
        var state = Relief();
        state.Nations.Single(n => n.Id == "gondor").StepsToWar = 1;
        Assert.False(EffectCatalog.Base.Events[HelpUnlookedFor].Condition.CanPlay(state, Side.FreePeoples));
        state.Nations.Single(n => n.Id == "gondor").StepsToWar = 0;
        state = Play(state, HelpUnlookedFor);
        var payload = Attack(state);
        payload.DieId = state.Dice.First().Id;
        Reject(state, CommandKind.AttackArmy, payload);
        payload.DieId = "";
        payload.Moves[0].To = "north-ithilien";
        Reject(state, CommandKind.AttackArmy, payload);
        Reject(state, CommandKind.MoveArmies, Move(state, "osgiliath", "north-ithilien"));
    }

    [Fact]
    public void GreyGuideDrawWaitsForWholeReliefAndRetreatAdvance()
    {
        var state = Relief();
        state.Fellowship.Guide = CharacterRules.Grey;
        EventTests.GiveCard(state, Side.FreePeoples, HelpUnlookedFor);
        var payload = Cost(state, DieFace.Event);
        payload.CardId = HelpUnlookedFor;
        payload.CardCost = DieFace.Event;
        state = Do(state, CommandKind.PlayEvent, payload);
        state = Do(state, CommandKind.AttackArmy, Attack(state));
        state = CardPackBattleFixture.NoRerolls(BattleFixture.NoCards(state, 1, 1, 1, 1, 1, 1));
        state = Do(state, CommandKind.ContinueBattle);
        state = Do(state, CommandKind.RetreatBattle, new() { RegionId = "druadan-forest" });
        Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
        var ids = BattleRuntime.Current(state)!.Attacker.Participants;
        state = Do(state, CommandKind.AdvanceBattle, new() { Ids = ids });
        Assert.Equal(DecisionKind.Reaction, state.Pending!.Kind);
        Assert.Equal("event.completed", state.Pending.Window);
        Assert.Null(BattleRuntime.Active(state));
        Assert.False(BoardRules.Besieged(state, "minas-tirith", Side.FreePeoples));
        state = Do(state, CommandKind.DeclineReaction);
        Assert.Empty(state.Resolution.Frames);
    }
}
