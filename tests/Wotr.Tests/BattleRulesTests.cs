using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.BattleFixture;

namespace Wotr.Tests;

public class BattleRulesTests
{
    [Fact]
    public void AttackKeepsParticipantsAtOriginAndAdvancesDefendingNationsOnlyOnce()
    {
        var state = State();
        var attacker = Put(state, "gondor", UnitKind.Regular, Origin);
        var defender = Put(state, "sauron", UnitKind.Regular, Target);
        var nation = state.Nations.Single(n => n.Id == "sauron");
        nation.Active = false;
        nation.StepsToWar = 2;
        state = Begin(state, [attacker.Id]);
        Assert.Equal(Origin, state.Units.Single(u => u.Id == attacker.Id).Location);
        Assert.Equal(Target, state.Units.Single(u => u.Id == defender.Id).Location);
        Assert.Equal(2, state.Nations.Single(n => n.Id == "sauron").StepsToWar);
        Assert.False(state.Nations.Single(n => n.Id == "sauron").Active);
        Assert.Equal(DieLocation.Resolving, state.Dice[0].Location);
        state = NoCards(state, 1, 1);
        state = Execute(state, CommandKind.ContinueBattle);
        state = Execute(state, CommandKind.HoldBattle);
        Assert.Equal(2, BattleRuntime.Current(state)!.Round);
        Assert.Equal(2, state.Nations.Single(n => n.Id == "sauron").StepsToWar);
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        state = NoCards(state, 1, 1);
        state = Execute(state, CommandKind.StopBattle);
        Assert.Equal(1, state.Nations.Single(n => n.Id == "sauron").StepsToWar);
        Assert.True(state.Nations.Single(n => n.Id == "sauron").Active);
        Assert.Empty(state.PoliticalReactions);
    }

    [Theory]
    [InlineData("not-at-war")]
    [InlineData("peaceful-leader")]
    [InlineData("no-rearguard-army")]
    [InlineData("only-character-rear")]
    [InlineData("duplicate")]
    [InlineData("enemy-participant")]
    [InlineData("nonadjacent")]
    [InlineData("garrison")]
    [InlineData("stronghold")]
    [InlineData("two-attacks")]
    [InlineData("wrong-die")]
    [InlineData("no-commander")]
    public void IllegalAttackIsAtomic(string reason)
    {
        var state = State();
        var unit = Put(state, "gondor", UnitKind.Regular, Origin);
        var defender = Put(state, "sauron", UnitKind.Regular, Target);
        var payload = AttackPayload(state, [unit.Id]);
        switch (reason)
        {
            case "not-at-war":
                state.Nations.Single(n => n.Id == "gondor").StepsToWar = 1;
                break;
            case "peaceful-leader":
                var leader = Put(state, "north", UnitKind.Leader, Origin);
                state.Nations.Single(n => n.Id == "north").StepsToWar = 1;
                payload.Moves[0].UnitIds.Add(leader.Id);
                break;
            case "no-rearguard-army":
                Put(state, "gondor", UnitKind.Leader, Origin);
                break;
            case "only-character-rear":
                Character(state, "test.rear", Side.FreePeoples, Origin);
                break;
            case "duplicate":
                payload.Moves[0].UnitIds.Add(unit.Id);
                break;
            case "enemy-participant":
                payload.Moves[0].UnitIds.Add(defender.Id);
                break;
            case "nonadjacent":
                payload.Moves[0].To = "dale";
                break;
            case "garrison":
                unit.Garrison = true;
                break;
            case "stronghold":
                defender.Location = payload.Moves[0].To = "dol-amroth";
                break;
            case "two-attacks":
                payload.Moves.Add(payload.Moves[0]);
                break;
            case "wrong-die":
                state.Dice[0].Face = DieFace.Event;
                break;
            case "no-commander":
                state.Dice[0].Face = payload.MovementCost = DieFace.Character;
                break;
        }
        Rejected(state, Command(state, CommandKind.AttackArmy, payload));
    }

    [Fact]
    public void CharacterCostRequiresParticipatingCommanderAndRearCannotTakeLosses()
    {
        var state = State();
        var first = Put(state, "gondor", UnitKind.Regular, Origin);
        var rear = Put(state, "gondor", UnitKind.Regular, Origin);
        var leader = Put(state, "gondor", UnitKind.Leader, Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        state.Dice[0].Face = DieFace.Character;
        Rejected(state, Command(state, CommandKind.AttackArmy, AttackPayload(state, [first.Id], cost: DieFace.Character)));
        state = Execute(state, CommandKind.AttackArmy, AttackPayload(state, [first.Id, leader.Id], cost: DieFace.Character));
        state = NoCards(state, 6, 6);
        Rejected(state, Command(state, CommandKind.TakeBattleLosses, new() { Losses = [Loss(rear.Id)] }));
        state = Losses(state, Loss(first.Id));
        Assert.Equal("casualties", state.Units.Single(u => u.Id == leader.Id).Location);
        Assert.Equal(Origin, state.Units.Single(u => u.Id == rear.Id).Location);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
    }

    [Fact]
    public void RearWithPeacefulLeaderNeedsAnArmyAndOptionsFindThatSplitWithoutEnumeratingSubsets()
    {
        var state = State();
        var first = Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "north", UnitKind.Leader, Origin);
        state.Nations.Single(n => n.Id == "north").StepsToWar = 1;
        Put(state, "sauron", UnitKind.Regular, Target);
        var option = Assert.Single(new BattleOptions().GetOptions(state, Side.FreePeoples));
        Assert.True(option.Enabled);
        Assert.Contains(option.Armies.Single().Targets, t => t.RegionId == Target);
        Assert.DoesNotContain(option.EligibleIds, id => state.Units.Single(u => u.Id == id).Nation == "north");
        state = Begin(state, [first.Id]);
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
    }

    [Fact]
    public void DecisionOwnershipIdAndRevisionProtectEveryBattleStep()
    {
        var state = State();
        var unit = Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        state = Begin(state, [unit.Id]);
        var command = Command(state, CommandKind.CommitCombatCard);
        Rejected(state, command, Side.Shadow);
        command.DecisionId = "expired";
        Rejected(state, command);
        command = Command(state, CommandKind.CommitCombatCard);
        command.ExpectedRevision--;
        Rejected(state, command);
    }

    [Fact]
    public void NullTargetsAndUnknownCardsAreRejectedWithoutExceptions()
    {
        var state = State();
        var unit = Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        var invalid = AttackPayload(state, [unit.Id]);
        invalid.Moves[0].From = null!;
        Rejected(state, Command(state, CommandKind.AttackArmy, invalid));
        state = Begin(state, [unit.Id]);
        Rejected(state, Command(state, CommandKind.CommitCombatCard, new() { CardId = null! }));
        Rejected(state, Command(state, CommandKind.CommitCombatCard, new() { CardId = "unknown-card" }));
        Rejected(state, Command(state, CommandKind.CommitCombatCard, new() { CardId = FpCard }));
    }
}
