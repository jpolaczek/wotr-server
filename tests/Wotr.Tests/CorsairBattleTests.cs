using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.CorsairFixture;

namespace Wotr.Tests;

public class CorsairBattleTests
{
    [Fact]
    public void ShelterRemovesExcessGarrisonBeforeCompletingTheLandingEvent()
    {
        var state = Land(State(target: Stronghold, defenders: 6), Stronghold);
        state = Do(state, CommandKind.RetreatIntoSiege);
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        var excess = state.Units.First(u => u.Location == Stronghold && u.Garrison);
        state = Do(state, CommandKind.RemoveExcess, new() { Ids = [excess.Id] });
        Assert.Equal(5, state.Units.Count(u => u.Location == Stronghold && u.Garrison));
        Assert.Equal(2, state.Units.Count(u => u.Location == Stronghold && !u.Garrison));
        Assert.Empty(state.Resolution.Frames);
        Assert.Empty(state.RandomSamples);
    }

    [Fact]
    public void DefenderChoosesShelterAfterArrivalAndShadowCannotDeclineOccupation()
    {
        var state = Land(State(target: Stronghold, defenders: 2), Stronghold);
        Assert.Equal(DecisionKind.StrongholdDefense, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        Assert.All(state.Units.Where(u => BoardRules.Owner(u) == Side.Shadow && BoardRules.IsArmyUnit(u) && u.Location != "reserve"),
            u => Assert.Equal(Stronghold, u.Location));
        Assert.True(new Projection().For(state, Side.Shadow).Battle!.ArrivalBeforeBattle);
        state = Do(state, CommandKind.RetreatIntoSiege);
        Assert.Null(BattleRuntime.Active(state));
        Assert.Empty(state.Resolution.Frames);
        Assert.Empty(state.RandomSamples);
        Assert.True(BoardRules.Besieged(state, Stronghold, Side.FreePeoples));
        Assert.Equal(Side.FreePeoples, state.Control[Stronghold]);
        Assert.DoesNotContain(state.Events, e => e.Kind == "BattleAdvance");
    }

    [Fact]
    public void NonSiegeRoundsMustContinueWhileDefenderRetainsRetreatChoice()
    {
        var state = Land(State(defenders: 2));
        state = Round(state);
        Assert.Equal(DecisionKind.BattleRetreat, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        Assert.DoesNotContain(CommandKind.StopBattle, state.Pending.Commands);
        Reject(state, CommandKind.StopBattle, new());
        state = Do(state, CommandKind.HoldBattle);
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);
        state = Round(state);
        Assert.Equal(DecisionKind.BattleRetreat, state.Pending!.Kind);
        state = Do(state, CommandKind.RetreatBattle, new() { RegionId = "lossarnach" });
        Assert.Equal(Side.Shadow, state.Control["pelargir"]);
        Assert.Equal(2, state.Units.Count(u => u.Location == "pelargir" && BoardRules.Owner(u) == Side.Shadow));
        Assert.Equal(2, state.Units.Count(u => u.Location == "lossarnach" && BoardRules.Owner(u) == Side.FreePeoples));
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.Events, e => e.Kind == "BattleAdvance");
    }

    [Fact]
    public void NoRetreatDestinationAutomaticallyBeginsAnotherRound()
    {
        var state = State(defenders: 1);
        foreach (var neighbor in BoardRules.Regions["pelargir"].Neighbors)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, neighbor);
        state = Round(Land(state));
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);
    }

    [Fact]
    public void EliminationAutomaticallyCapturesTheLandingRegion()
    {
        var state = Land(State(1, defenders: 1));
        state = Round(state, 5, 0);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        var casualty = state.Units.Single(u => u.Location == "pelargir" && BoardRules.Owner(u) == Side.FreePeoples);
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(casualty.Id)] });
        Assert.Equal(Side.Shadow, state.Control["pelargir"]);
        Assert.Null(BattleRuntime.Active(state));
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.Events, e => e.Kind == "BattleAdvance");
    }

    [Fact]
    public void MutualEliminationDoesNotCaptureTheLandingSettlement()
    {
        var state = Land(State(1, defenders: 1));
        state = Round(state, 5, 5);
        var attacker = state.Units.Single(u => u.Location == "pelargir" && BoardRules.Owner(u) == Side.Shadow);
        var defender = state.Units.Single(u => u.Location == "pelargir" && BoardRules.Owner(u) == Side.FreePeoples);
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(attacker.Id)] });
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(defender.Id)] });
        Assert.Equal(Side.FreePeoples, state.Control["pelargir"]);
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.Units, u => u.Location == "pelargir" && BoardRules.IsArmyUnit(u));
    }

    [Fact]
    public void ExistingSiegeMergesAndRemovesExcessBeforeCombatThenAllowsNormalSiegeExtension()
    {
        var state = State(8, Stronghold, 2);
        foreach (var unit in state.Units.Where(u => u.Location == Stronghold))
            unit.Garrison = true;
        for (var i = 0; i < 3; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, Stronghold);
        BattleFixture.Put(state, "sauron", UnitKind.Elite, Stronghold);
        state = Land(state, Stronghold);
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.Null(BattleRuntime.Active(state));
        Assert.Equal(12, state.Units.Count(u => u.Location == Stronghold && !u.Garrison && BoardRules.IsArmyUnit(u)));
        var excess = state.Units.Where(u => u.Location == Stronghold && u.Kind == UnitKind.Regular && !u.Garrison).Take(2).Select(u => u.Id).ToList();
        state = Do(state, CommandKind.RemoveExcess, new() { Ids = excess });
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(BattleMode.Siege, battle.Mode);
        Assert.Equal(10, BattleRules.Strength(state, battle.Attacker));
        state = Round(state);
        Assert.Equal(DecisionKind.SiegeContinuation, state.Pending!.Kind);
        Assert.Contains(CommandKind.StopBattle, state.Pending.Commands);
        var elite = BattleRules.Units(state, BattleRuntime.Active(state)!.Attacker).Single(u => u.Kind == UnitKind.Elite);
        state = Do(state, CommandKind.ExtendSiege, new() { Ids = [elite.Id] });
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);
        state = Round(state);
        Assert.Empty(state.Resolution.Frames);
        Assert.True(BoardRules.Besieged(state, Stronghold, Side.FreePeoples));
    }

    [Fact]
    public void ThreeStackReliefKeepsGarrisonOutOfCombatThenMergesAfterDefenderRetreat()
    {
        var state = Relief(7, 5);
        var garrisonIds = state.Units.Where(u => u.Location == Stronghold && u.Garrison).Select(u => u.Id).ToArray();
        state = Land(state, Stronghold);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(BattleMode.Relief, battle.Mode);
        Assert.Equal(7, BattleRules.Strength(state, battle.Attacker));
        Assert.DoesNotContain(battle.Attacker.Participants, garrisonIds.Contains);
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        state = Round(state);
        state = Do(state, CommandKind.RetreatBattle, new() { RegionId = "erech" });
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.All(garrisonIds, id => Assert.False(state.Units.Single(u => u.Id == id).Garrison));
        var excess = state.Units.Where(u => u.Location == Stronghold && BoardRules.IsArmyUnit(u)).Take(2).Select(u => u.Id).ToList();
        state = Do(state, CommandKind.RemoveExcess, new() { Ids = excess });
        Assert.Equal(10, state.Units.Count(u => u.Location == Stronghold && BoardRules.IsArmyUnit(u)));
        Assert.Equal(Side.Shadow, state.Control[Stronghold]);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void LosingTheArrivingArmyLeavesOriginalShadowGarrisonUnderSiege()
    {
        var state = Land(Relief(1, 3), Stronghold);
        var battle = BattleRuntime.Active(state)!;
        var attacker = BattleRules.Units(state, battle.Attacker).Single();
        state = Round(state, 0, 5);
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(attacker.Id)] });
        Assert.Empty(state.Resolution.Frames);
        Assert.True(BoardRules.Besieged(state, Stronghold, Side.Shadow));
        Assert.Equal(3, state.Units.Count(u => u.Location == Stronghold && u.Garrison && BoardRules.Owner(u) == Side.Shadow));
        Assert.Equal(Side.Shadow, state.Control[Stronghold]);
    }

    [Fact]
    public void MutualReliefEliminationFreesTheSurvivingGarrison()
    {
        var state = Land(Relief(1, 3), Stronghold);
        var battle = BattleRuntime.Active(state)!;
        var attacker = BattleRules.Units(state, battle.Attacker).Single();
        var defender = BattleRules.Units(state, battle.Defender).Single();
        state = Round(state, 5, 5);
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(attacker.Id)] });
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(defender.Id)] });
        Assert.Empty(state.Resolution.Frames);
        Assert.False(BoardRules.Besieged(state, Stronghold, Side.Shadow));
        Assert.Equal(3, state.Units.Count(u => u.Location == Stronghold && !u.Garrison && BoardRules.Owner(u) == Side.Shadow));
    }

    [Fact]
    public void ScoutsCanWithdrawBeforeRollWithoutSendingShadowBackToUmbar()
    {
        var state = State(defenders: 1);
        EventTests.GiveCard(state, Side.FreePeoples, FreePeoplesStrategyCards01.SpiritOfMordor);
        state = Land(state);
        state = Do(state, CommandKind.CommitCombatCard);
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = FreePeoplesStrategyCards01.SpiritOfMordor });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["lossarnach"] });
        Assert.Empty(state.RandomSamples);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(Side.Shadow, state.Control["pelargir"]);
        Assert.Equal(2, state.Units.Count(u => u.Location == "pelargir" && BoardRules.Owner(u) == Side.Shadow));
    }

    internal static MatchState Relief(int arrivals, int garrison)
    {
        var state = State(arrivals, Stronghold, 1);
        state.Control[Stronghold] = Side.Shadow;
        for (var i = 0; i < garrison; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, Stronghold).Garrison = true;
        return state;
    }
}
