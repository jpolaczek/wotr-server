using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.BattleFixture;

namespace Wotr.Tests;

public class BattleMovementTests
{
    [Fact]
    public void AttackerMustContinueBeforeDefenderRetreatsAndEnemySettlementIsNotFree()
    {
        var state = State();
        var unit = Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        state = NoCards(Begin(state, [unit.Id]), 1, 1);
        Rejected(state, Command(state, CommandKind.RetreatBattle, new() { RegionId = "osgiliath" }), Side.Shadow);
        state = Execute(state, CommandKind.ContinueBattle);
        var choices = new BattleOptions().GetOptions(state, Side.Shadow).Single(o => o.Kind == CommandKind.RetreatBattle);
        Assert.Equal(new[] { "osgiliath" }, choices.Targets);
        Rejected(state, Command(state, CommandKind.RetreatBattle, new() { RegionId = "pelargir" }));
        Rejected(state, Command(state, CommandKind.RetreatBattle, new() { RegionId = Origin }));
        state = Execute(state, CommandKind.RetreatBattle, new() { RegionId = "osgiliath" });
        Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
        Assert.Equal(Origin, state.Units.Single(u => u.Id == unit.Id).Location);
        state = Execute(state, CommandKind.AdvanceBattle);
        Assert.Equal(Origin, state.Units.Single(u => u.Id == unit.Id).Location);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void NoFreeRetreatSkipsImpossibleDecisionAndStartsNextRound()
    {
        var state = State();
        var unit = Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "gondor", UnitKind.Regular, "osgiliath");
        Put(state, "sauron", UnitKind.Regular, Target);
        state = NoCards(Begin(state, [unit.Id]), 1, 1);
        state = Execute(state, CommandKind.ContinueBattle);
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        Assert.Equal(2, BattleRuntime.Current(state)!.Round);
    }

    [Fact]
    public void RetreatIgnoresNationalBordersButLevelZeroCharacterStays()
    {
        var state = State(Side.Shadow);
        var attacker = Put(state, "sauron", UnitKind.Regular, "bree");
        var defender = Put(state, "gondor", UnitKind.Regular, "south-downs");
        var fixedCharacter = Character(state, "test.fixed", Side.FreePeoples, "south-downs", 0, 0);
        state.Nations.Single(n => n.Id == "gondor").StepsToWar = 3;
        state.Control["north-dunland"] = Side.FreePeoples;
        state = NoCards(Begin(state, [attacker.Id], from: "bree", to: "south-downs"), 1, 1);
        Assert.Equal(3, state.Nations.Single(n => n.Id == "gondor").StepsToWar);
        state = Execute(state, CommandKind.ContinueBattle);
        state = Execute(state, CommandKind.RetreatBattle, new() { RegionId = "north-dunland" });
        Assert.Equal("north-dunland", state.Units.Single(u => u.Id == defender.Id).Location);
        Assert.Equal("south-downs", state.Characters.Single(c => c.Id == fixedCharacter.Id).Location);
    }

    [Fact]
    public void RetreatStackLimitSuspendsBattleAndResumesAdvanceWithoutFinishingCostEarly()
    {
        var state = State(Side.Shadow);
        var attacker = Put(state, "sauron", UnitKind.Regular, "osgiliath");
        var defenders = Enumerable.Range(0, 2).Select(_ => Put(state, "gondor", UnitKind.Regular, Target).Id).ToArray();
        for (var i = 0; i < 10; i++)
            Put(state, "gondor", UnitKind.Regular, Origin);
        state = NoCards(Begin(state, [attacker.Id], from: "osgiliath"), 1, 1, 1);
        state = Execute(state, CommandKind.ContinueBattle);
        state = Execute(state, CommandKind.RetreatBattle, new() { RegionId = Origin });
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.Equal(2, state.Pending.RequiredCount);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Resolving);
        state = Execute(StateCodec.Copy(state), CommandKind.RemoveExcess, new() { Ids = [.. defenders] });
        Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        state = Execute(state, CommandKind.AdvanceBattle, new() { Ids = [attacker.Id] });
        Assert.Equal(Target, state.Units.Single(u => u.Id == attacker.Id).Location);
        Assert.Equal(Side.Shadow, state.Control[Target]);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void AdvanceCanSplitSurvivorsButCannotMoveRearOrStrandFreePeoplesLeader()
    {
        var state = State();
        var attacker = Put(state, "gondor", UnitKind.Regular, Origin);
        var second = Put(state, "gondor", UnitKind.Regular, Origin);
        var rear = Put(state, "north", UnitKind.Regular, Origin);
        var companion = Character(state, "test.rear", Side.FreePeoples, Origin);
        var leader = Put(state, "gondor", UnitKind.Leader, Origin);
        var defender = Put(state, "sauron", UnitKind.Regular, Target);
        state.Control[Target] = Side.Shadow;
        state = NoCards(Begin(state, [attacker.Id, second.Id, leader.Id]), 6, 6, 1);
        state = Losses(state, Loss(defender.Id));
        Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Control[Target]);
        Rejected(state, Command(state, CommandKind.AdvanceBattle, new() { Ids = [attacker.Id, rear.Id] }));
        Rejected(state, Command(state, CommandKind.AdvanceBattle, new() { Ids = [attacker.Id, companion.Id] }));
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples,
            Command(state, CommandKind.AdvanceBattle, new() { Ids = [attacker.Id] }));
        Assert.Equal(Side.FreePeoples, preview.ControlChanges[Target]);
        state = Execute(state, CommandKind.AdvanceBattle, new() { Ids = [attacker.Id] });
        Assert.Equal(Target, state.Units.Single(u => u.Id == attacker.Id).Location);
        Assert.Equal(Origin, state.Units.Single(u => u.Id == leader.Id).Location);
        Assert.Equal(Origin, state.Units.Single(u => u.Id == rear.Id).Location);
        Assert.Equal(Side.FreePeoples, state.Control[Target]);
    }

    [Fact]
    public void FullAdvanceMustTakeItsFreePeoplesLeaderWhenNoArmyRemains()
    {
        var state = State();
        var attacker = Put(state, "gondor", UnitKind.Regular, Origin);
        var leader = Put(state, "gondor", UnitKind.Leader, Origin);
        var defender = Put(state, "sauron", UnitKind.Regular, Target);
        state = NoCards(Begin(state, [attacker.Id, leader.Id]), 6, 1);
        state = Losses(state, Loss(defender.Id));
        Rejected(state, Command(state, CommandKind.AdvanceBattle, new() { Ids = [attacker.Id] }));
        state = Execute(state, CommandKind.AdvanceBattle, new() { Ids = [attacker.Id, leader.Id] });
        Assert.Equal(Target, state.Units.Single(u => u.Id == leader.Id).Location);
    }
}
