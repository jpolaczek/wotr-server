using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy02Fixture;
using static Wotr.Domain.ShadowStrategyCards02;

namespace Wotr.Tests;

public class ShadowStrategy02EventTests
{
    [Fact]
    public void EveryCardChecksOwnerHandWindowPrintedCostAndSpentDie()
    {
        foreach (var card in Cards)
        {
            var state = State(card);
            var cost = Cost(state, card);
            var command = BattleFixture.Command(state, CommandKind.PlayEvent, cost);
            Assert.Null(Engine().Validate(state, Side.Shadow, command));
            Assert.NotNull(Engine().Validate(state, Side.FreePeoples, command));
            var die = state.Dice.Single(d => d.Id == cost.DieId);
            die.Location = DieLocation.Spent;
            Reject(state, CommandKind.PlayEvent, cost);
            die.Location = DieLocation.Available;
            die.Face = DieFace.Character;
            Reject(state, CommandKind.PlayEvent, cost);
            die.Face = cost.CardCost;
            HandRules.Remove(state.Player(Side.Shadow), card);
            Reject(state, CommandKind.PlayEvent, cost);
            HandRules.Add(state.Player(Side.Shadow), card);
            state.Pending!.Kind = DecisionKind.Reaction;
            Reject(state, CommandKind.PlayEvent, cost);
        }
    }

    [Fact]
    public void PrintedConditionsDoNotSilentlyRequireAnExecutableMovementOrRecruitment()
    {
        foreach (var card in Cards)
        {
            var state = State(card);
            foreach (var unit in state.Units)
                unit.Location = "eliminated";
            state.Control["angmar"] = Side.FreePeoples;
            state = Play(state, card);
            Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
            Assert.Contains(card, state.Player(Side.Shadow).Discard);
            Assert.Single(state.Dice, d => d.Owner == Side.Shadow && d.Location == DieLocation.Spent);
        }
    }

    [Fact]
    public void MovingRageAndCorsairsRejectBeforePrintedNationsAreAtWarAndReturnRequiresWitchKing()
    {
        foreach (var card in new[] { Moving, Rage, Corsairs })
        {
            var state = State(card);
            state.Nations.Single(n => n.Id == (card == Corsairs ? "southrons" : "isengard")).StepsToWar = 1;
            Reject(state, CommandKind.PlayEvent, Cost(state, card));
        }
        var absent = State(Return);
        absent.Characters.Clear();
        Reject(absent, CommandKind.PlayEvent, Cost(absent, Return));
    }

    [Fact]
    public void RageRecruitsBeforeOptionalDirectMovementAndExcludesOtherFigures()
    {
        var state = State(Rage);
        var regular = BattleFixture.Put(state, "isengard", UnitKind.Regular, "north-dunland");
        var elite = BattleFixture.Put(state, "isengard", UnitKind.Elite, "south-dunland");
        var nazgul = BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "south-dunland");
        state = Play(state, Rage);
        Assert.DoesNotContain("orthanc", state.Pending!.PrivateChoices);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["hollin"] });
        Assert.Equal(2, state.Units.Count(u => u.Location == "hollin" && u.Nation == "isengard"));
        Assert.Equal("ArmyFigures", state.Pending!.Presentation);
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [nazgul.Id] });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [regular.Id, elite.Id] });
        Assert.Equal(4, state.Units.Count(u => u.Location == "hollin" && u.Nation == "isengard"));
        Assert.Equal("south-dunland", state.Units.Single(u => u.Id == nazgul.Id).Location);
    }

    [Fact]
    public void RageWithOneReserveRegularStillAllowsMovementAndWithZeroDoesNothing()
    {
        foreach (var reserveCount in new[] { 0, 1 })
        {
            var state = State(Rage);
            var mover = BattleFixture.Put(state, "isengard", UnitKind.Elite, "north-dunland");
            foreach (var unit in state.Units.Where(u => u.Nation == "isengard" && u.Kind == UnitKind.Regular && u.Location == "reserve").Skip(reserveCount))
                unit.Location = "eliminated";
            state = Play(state, Rage);
            if (reserveCount == 0)
            {
                Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
                Assert.Equal("north-dunland", state.Units.Single(u => u.Id == mover.Id).Location);
            }
            else
            {
                state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["hollin"] });
                Assert.Single(state.Units, u => u.Location == "hollin");
                state = Do(state, CommandKind.ChooseEffect, new() { Ids = [mover.Id] });
                Assert.Equal(2, state.Units.Count(u => u.Location == "hollin"));
            }
        }
    }

    [Fact]
    public void RageCanDeclineMovementAndRejectsFiveFiguresDuplicateOrEnemyControlledRecruitment()
    {
        var state = State(Rage);
        for (var i = 0; i < 5; i++)
            BattleFixture.Put(state, "isengard", UnitKind.Regular, "north-dunland");
        state.Control["moria"] = Side.FreePeoples;
        state = Play(state, Rage);
        Assert.DoesNotContain("moria", state.Pending!.PrivateChoices);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["hollin"] });
        var ids = state.Units.Where(u => u.Nation == "isengard" && u.Location == "north-dunland").Select(u => u.Id).ToList();
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ids });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [ids[0], ids[0]] });
        state = Do(state, CommandKind.ChooseEffect, new());
        Assert.Equal(5, state.Units.Count(u => u.Nation == "isengard" && u.Location == "north-dunland"));
    }

    [Fact]
    public void ReturnMovesWitchKingBeforeRecruitmentAndPreservesMoveIfAngmarIsUnavailable()
    {
        foreach (var enemy in new[] { false, true })
        {
            var state = State(Return);
            state.Nations.Single(n => n.Id == "sauron").StepsToWar = 1;
            if (enemy)
                state.Control["angmar"] = Side.FreePeoples;
            state = Play(state, Return);
            Assert.Equal("angmar", CharacterRules.InPlay(state, CharacterRules.WitchKing)!.Location);
            Assert.Equal(enemy ? 0 : 2, state.Units.Count(u => u.Location == "angmar" && u.Kind == UnitKind.Regular));
            Assert.Equal(enemy ? 0 : 1, state.Units.Count(u => u.Location == "angmar" && u.Kind == UnitKind.Elite));
            Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        }
    }

    [Fact]
    public void ReturnRecruitsAvailableRegularEvenWhenNoEliteRemainsAndChecksFinalStack()
    {
        var state = State(Return);
        foreach (var unit in state.Units.Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Elite))
            unit.Location = "eliminated";
        for (var i = 0; i < 9; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, "angmar");
        state = Play(state, Return);
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.Equal(1, state.Pending.RequiredCount);
        Assert.Equal(11, state.Units.Count(u => u.Location == "angmar"));
    }
}
