using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards02;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy02Fixture;
using static Wotr.Tests.CardPackPersistenceTests;

namespace Wotr.Tests;

public class ShadowStrategy02PersistenceTests
{
    [PostgresFact]
    public async Task EveryEventRestoresItsPaidCardAndContinuationWithoutASecondCost()
    {
        foreach (var card in Cards)
        {
            var state = State(card);
            var cost = Cost(state, card);
            state = await RestartDecision(state, CommandKind.PlayEvent, cost, new SequenceRandom());
            Assert.Single(state.Events, e => e.Kind == "EventPlayed" && e.Text.Contains(Wotr.Domain.Content.CardCatalog.Title(card), StringComparison.Ordinal));
            Assert.Single(state.Player(Side.Shadow).Discard, id => id == card);
            Assert.NotEqual(DieLocation.Available, state.Dice.Single(d => d.Id == cost.DieId).Location);
        }
    }

    [PostgresFact]
    public async Task GatherRestoresChosenRouteAndThenExcessWithoutRepeatingAnyMovement()
    {
        var state = State(Gather);
        for (var i = 0; i < 9; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, "south-dunland");
        state = Play(state, Gather);
        var moved = Move(state, "moria", "hollin", "north-dunland", "south-dunland");
        state = await RestartDecision(state, CommandKind.MoveArmies, new() { Moves = [moved] }, new SequenceRandom());
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.Single(state.Events, e => e.Kind == "ArmyMoved");
        var excess = state.Pending.PrivateChoices[0];
        state = await RestartDecision(state, CommandKind.RemoveExcess, new() { Ids = [excess] }, new SequenceRandom());
        Assert.Equal(10, state.Units.Count(u => u.Location == "south-dunland"));
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Events, e => e.Kind == "ArmyMoved");
    }

    [PostgresFact]
    public async Task TwoAndFourArmyPlansRestoreAllOriginalGroupsInOneCommit()
    {
        foreach (var card in new[] { Lengthens, Moving })
        {
            var state = State(card);
            var moves = new List<ArmyMoveChoice>();
            if (card == Lengthens)
            {
                moves.Add(Move(state, "moria", "north-dunland", "south-dunland"));
                moves.Add(Move(state, "south-dunland", "north-dunland", "moria"));
            }
            else
            {
                BattleFixture.Put(state, "sauron", UnitKind.Regular, "hollin");
                BattleFixture.Put(state, "sauron", UnitKind.Regular, "north-dunland");
                moves.Add(Move(state, "moria", "north-dunland"));
                moves.Add(Move(state, "north-dunland", "hollin"));
                moves.Add(Move(state, "hollin", "moria"));
                moves.Add(Move(state, "south-dunland", "north-dunland"));
            }
            state = Play(state, card);
            state = await RestartDecision(state, CommandKind.MoveArmies, new() { Moves = moves }, new SequenceRandom());
            foreach (var move in moves)
                Assert.All(move.UnitIds, id => Assert.Equal(move.To, state.Units.Single(u => u.Id == id).Location));
            Assert.Equal(moves.Count, state.Events.Count(e => e.Kind == "ArmyMoved"));
        }
    }

    [PostgresFact]
    public async Task RageRestoresRegionRecruitmentAndOptionalMovementWithoutReusingReserves()
    {
        var state = State(Rage);
        var mover = BattleFixture.Put(state, "isengard", UnitKind.Elite, "north-dunland");
        state = Play(state, Rage);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ["hollin"] }, new SequenceRandom());
        Assert.Equal("ArmyFigures", state.Pending!.Presentation);
        Assert.Equal(2, state.Units.Count(u => u.Location == "hollin"));
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = [mover.Id] }, new SequenceRandom());
        Assert.Equal(3, state.Units.Count(u => u.Location == "hollin"));
        Assert.Equal(2, state.Events.Count(e => e.Kind == "EventRecruited"));
    }

    [PostgresFact]
    public async Task ReturnRestoresFinalExcessAndKeepsWitchKingInAngmar()
    {
        var state = State(Return);
        for (var i = 0; i < 9; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, "angmar");
        state = Play(state, Return);
        Assert.Equal(2, state.Pending!.RequiredCount);
        state = await RestartDecision(state, CommandKind.RemoveExcess,
            new() { Ids = state.Pending.PrivateChoices.Take(2).ToList() }, new SequenceRandom());
        Assert.Equal("angmar", CharacterRules.InPlay(state, CharacterRules.WitchKing)!.Location);
        Assert.Equal(10, state.Units.Count(u => u.Location == "angmar"));
        Assert.Single(state.Events, e => e.Kind == "CharacterMoved");
    }
}
