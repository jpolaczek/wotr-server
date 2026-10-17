using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.CorsairFixture;
using static Wotr.Tests.CardPackPersistenceTests;

namespace Wotr.Tests;

public class CorsairPersistenceTests
{
    [PostgresFact]
    public async Task PaymentAndLandingRestoreTheirDecisionAndNeverRepeatMovement()
    {
        var state = State();
        state = await RestartDecision(state, CommandKind.PlayEvent, EventCost(state), new SequenceRandom());
        Assert.Equal(DecisionKind.ArmyEvent, state.Pending!.Kind);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Resolving);
        state = await RestartDecision(state, CommandKind.MoveArmies, Move(state, "lamedon"), new SequenceRandom());
        Assert.Equal(2, state.Units.Count(u => u.Location == "lamedon" && BoardRules.IsArmyUnit(u)));
        Assert.Single(state.Events, e => e.Kind == "ArmyMoved");
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Empty(state.Resolution.Frames);
        Assert.Empty(state.RandomSamples);
    }

    [PostgresFact]
    public async Task ArrivalRestoresStrongholdDefenseAndShelterWithoutAdvanceChoice()
    {
        var state = Play(State(target: Stronghold, defenders: 2));
        state = await RestartDecision(state, CommandKind.MoveArmies, Move(state, Stronghold), new SequenceRandom());
        Assert.Equal(DecisionKind.StrongholdDefense, state.Pending!.Kind);
        Assert.True(BattleRuntime.Active(state)!.ArrivalBeforeBattle);
        state = await RestartDecision(state, CommandKind.RetreatIntoSiege, new(), new SequenceRandom());
        Assert.Empty(state.Resolution.Frames);
        Assert.True(BoardRules.Besieged(state, Stronghold, Side.FreePeoples));
        Assert.Empty(state.RandomSamples);
    }

    [PostgresFact]
    public async Task ImmediateMergeLimitRestoresBeforeTheFirstSiegeCard()
    {
        var state = State(8, Stronghold, 1);
        state.Units.Single(u => u.Location == Stronghold).Garrison = true;
        for (var i = 0; i < 4; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, Stronghold);
        state = Play(state);
        state = await RestartDecision(state, CommandKind.MoveArmies, Move(state, Stronghold), new SequenceRandom());
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.Null(BattleRuntime.Active(state));
        var excess = state.Units.Where(u => u.Location == Stronghold && !u.Garrison).Take(2).Select(u => u.Id).ToList();
        state = await RestartDecision(state, CommandKind.RemoveExcess, new() { Ids = excess }, new SequenceRandom());
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        Assert.Equal(BattleMode.Siege, BattleRuntime.Active(state)!.Mode);
        Assert.Equal(10, BattleRules.Strength(state, BattleRuntime.Active(state)!.Attacker));
        Assert.Single(state.Events, e => e.Kind == "BattleStarted");
    }

    [PostgresFact]
    public async Task CombatRerollLossesAndForcedContinuationRestoreUntilDefenderRetreats()
    {
        var state = State(defenders: 2);
        BattleFixture.Put(state, "sauron", UnitKind.Nazgul, CorsairRules.Origin);
        state = Land(state);
        state = await RestartDecision(state, CommandKind.CommitCombatCard, new(), new SequenceRandom());
        state = await RestartDecision(state, CommandKind.CommitCombatCard, new(), new SequenceRandom(0, 5, 5, 0));
        Assert.Equal(DecisionKind.BattleReroll, state.Pending!.Kind);
        var failedDie = BattleRoll.Failed(BattleRuntime.Active(state)!.Attacker).Single();
        state = await RestartDecision(state, CommandKind.RerollBattle, new() { Ids = [failedDie] }, new SequenceRandom(0));
        foreach (var side in new[] { Side.Shadow, Side.FreePeoples })
        {
            Assert.Equal(side, state.Pending!.Owner);
            var casualty = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(side)).First(BoardRules.IsArmyUnit);
            state = await RestartDecision(state, CommandKind.TakeBattleLosses,
                new() { Losses = [BattleFixture.Loss(casualty.Id)] }, new SequenceRandom());
        }
        Assert.Equal(DecisionKind.BattleRetreat, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        state = await RestartDecision(state, CommandKind.HoldBattle, new(), new SequenceRandom());
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);
        state = await RestartDecision(state, CommandKind.CommitCombatCard, new(), new SequenceRandom());
        state = await RestartDecision(state, CommandKind.CommitCombatCard, new(), new SequenceRandom(0, 0));
        state = await RestartDecision(state, CommandKind.RerollBattle, new(), new SequenceRandom());
        state = await RestartDecision(state, CommandKind.RetreatBattle, new() { RegionId = "lossarnach" }, new SequenceRandom());
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(Side.Shadow, state.Control["pelargir"]);
        Assert.Equal(7, state.RandomSamples.Count);
    }

    [PostgresFact]
    public async Task ThreeStackReliefRestoresFinalMergeBeforePalantirDraw()
    {
        var state = CorsairBattleTests.Relief(7, 5);
        Put(state, CharacterRules.Saruman, "orthanc");
        Shadow04Fixture.Table(state, ShadowCharacterCards04.Palantir);
        state = Play(state, DieFace.Event);
        state = await RestartDecision(state, CommandKind.MoveArmies, Move(state, Stronghold), new SequenceRandom());
        Assert.Equal(BattleMode.Relief, BattleRuntime.Active(state)!.Mode);
        Assert.Equal(7, BattleRules.Strength(state, BattleRuntime.Active(state)!.Attacker));
        state = await RestartDecision(state, CommandKind.CommitCombatCard, new(), new SequenceRandom());
        state = await RestartDecision(state, CommandKind.CommitCombatCard, new(), new SequenceRandom(new int[6]));
        state = await RestartDecision(state, CommandKind.RetreatBattle, new() { RegionId = "erech" }, new SequenceRandom());
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.NotEqual("Deck", state.Pending.Presentation);
        var excess = state.Units.Where(u => u.Location == Stronghold && BoardRules.IsArmyUnit(u)).Take(2).Select(u => u.Id).ToList();
        state = await RestartDecision(state, CommandKind.RemoveExcess, new() { Ids = excess }, new SequenceRandom());
        Assert.Null(BattleRuntime.Active(state));
        Assert.Equal("Deck", state.Pending!.Presentation);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Resolving);
        var cards = state.Player(Side.Shadow).Hand.Count;
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ["strategy"] }, new SequenceRandom());
        Assert.Equal(cards + 1, state.Player(Side.Shadow).Hand.Count);
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Single(state.Events, e => e.Kind == "BattleStarted");
        Assert.Equal(6, state.RandomSamples.Count);
    }
}
