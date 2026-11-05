using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards01;
using static Wotr.Tests.BattleFixture;

namespace Wotr.Tests;

// EXP-20: withdrawing into a stronghold is a recorded Retreat from the field onto the walls of the same region, whichever
// decision offered it, and both sides read the same record of it.
public class ActionLogShelterFactsTests
{
    private const string Besieger = "osgiliath";
    private static readonly Side[] Viewers = [Side.FreePeoples, Side.Shadow];

    [Fact]
    public void WithdrawingBeforeTheBattleRecordsARetreatOntoTheWalls()
    {
        var state = State(Side.Shadow);
        var attacker = Put(state, "sauron", UnitKind.Regular, Besieger);
        var defenders = new[] { Put(state, "gondor", UnitKind.Regular, Origin).Id, Put(state, "gondor", UnitKind.Elite, Origin).Id };
        var captain = Character(state, CharacterRules.Aragorn, Side.FreePeoples, Origin);
        state = Begin(state, [attacker.Id], from: Besieger, to: Origin);
        Assert.Contains(captain.Id, BattleRuntime.Active(state)!.Defender.Participants);
        state = Execute(state, CommandKind.RetreatIntoSiege);
        var move = AssertShelter(state, Origin, [.. defenders, captain.Id]);
        Assert.Contains(move.Participants, p => p.Id == captain.Id && p.DefinitionId == CharacterRules.Aragorn && p.UnitKind is null);
        var decision = ActionLogFamilyTests.Steps(state).Single(s => s.Command == CommandKind.RetreatIntoSiege);
        Assert.Equal(Side.FreePeoples, decision.Actor);
        Assert.Empty(decision.Movements);
        var shelter = ActionLogFamilyTests.Steps(state).Single(s => s.Kind == "StrongholdShelter");
        Assert.Equal(decision.ActionId, shelter.ActionId);
        AssertSameForBothSides(state, decision.ActionId);
    }

    [Fact]
    public void WithdrawingBetweenRoundsMovesOnlyTheFiguresStillInTheBattle()
    {
        var state = State(Side.Shadow);
        var attacker = Put(state, "sauron", UnitKind.Regular, Besieger);
        var elite = Put(state, "gondor", UnitKind.Elite, Origin);
        var regular = Put(state, "gondor", UnitKind.Regular, Origin);
        state = Begin(state, [attacker.Id], from: Besieger, to: Origin);
        state = Execute(state, CommandKind.FightFieldBattle);
        state = NoCards(state, 5, 1, 1);
        state = Losses(state, Loss(regular.Id));
        state = Execute(state, CommandKind.ContinueBattle);
        Assert.Equal(DecisionKind.BattleRetreat, state.Pending!.Kind);
        state = Execute(state, CommandKind.RetreatIntoSiege);
        AssertShelter(state, Origin, [elite.Id]);
    }

    [Fact]
    public void ScoutsWithdrawalRecordsTheSameRetreatWithoutNamingACard()
    {
        var state = State(Side.Shadow);
        var enemy = Put(state, "isengard", UnitKind.Regular, "westemnet");
        var defender = Put(state, "rohan", UnitKind.Regular, "helms-deep");
        EventTests.GiveCard(state, Side.FreePeoples, SpiritOfMordor);
        EventTests.GiveCard(state, Side.FreePeoples, TomBombadil);
        state = Begin(state, [enemy.Id], from: "westemnet", to: "helms-deep");
        state = CharacterFixture.Do(state, CommandKind.FightFieldBattle);
        state = CardPackBattleFixture.Reveal(state, SpiritOfMordor);
        state = CharacterFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = [ScoutsStep.Shelter] });
        AssertShelter(state, "helms-deep", [defender.Id]);
        var shelter = ActionLogFamilyTests.Steps(state, Side.Shadow).Single(s => s.Kind == "StrongholdShelter");
        Assert.DoesNotContain(TomBombadil, ActionLogFamilyTests.Json(ActionLogFixture.Action(state, shelter.ActionId, Side.Shadow).Steps));
    }

    private static ActionMovement AssertShelter(MatchState state, string region, string[] participants)
    {
        var moves = Viewers.Select(viewer => AssertShelterSeenBy(state, viewer, region, participants)).ToArray();
        return moves[0];
    }

    private static ActionMovement AssertShelterSeenBy(MatchState state, Side viewer, string region, string[] participants)
    {
        var step = ActionLogFamilyTests.Steps(state, viewer).Single(s => s.Kind == "StrongholdShelter");
        var move = Assert.Single(step.Movements);
        Assert.Equal(ActionMovementKind.Retreat, move.Kind);
        Assert.Equal((region, region), (move.From, move.To));
        Assert.Equal([region], move.Route);
        Assert.Equal(participants.Order(), move.Participants.Select(p => p.Id).Order());
        Assert.All(move.Participants, p => Assert.True(p.Garrison));
        Assert.All(move.Participants, p => Assert.Equal(Side.FreePeoples, p.Side));
        Assert.Equal([region], step.Context.Regions);
        Assert.Equal(BattleRuntime.Active(state)!.Id, step.Context.BattleId);
        Assert.True(step.Facts.Battle!.DefenderSheltered);
        return move;
    }

    private static void AssertSameForBothSides(MatchState state, string actionId)
    {
        var records = Viewers.Select(viewer => ActionLogFamilyTests.Json(
            ActionLogFixture.Action(state, actionId, viewer).Steps)).ToArray();
        Assert.Equal(records[0], records[1]);
    }
}
