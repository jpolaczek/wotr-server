using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.BattleFixture;

namespace Wotr.Tests;

public class SortieReliefTests
{
    [Fact]
    public void SortieFightsOnFiveAndStoppingKeepsTheRearAndParticipantsInside()
    {
        var state = SiegeTests.SetupSiege(Side.FreePeoples);
        var attacker = Put(state, "gondor", UnitKind.Elite, Origin);
        attacker.Garrison = true;
        var defender = Put(state, "sauron", UnitKind.Elite, Origin);
        var option = new BattleOptions().GetOptions(state, Side.FreePeoples).Single();
        Assert.True(Assert.Single(option.Armies).Garrison);
        Assert.Equal(BattleMode.Sortie, Assert.Single(option.Armies[0].Targets).BattleMode);
        state = Begin(state, [attacker.Id], from: Origin, to: Origin);
        Assert.Equal(BattleMode.Sortie, BattleRuntime.Current(state)!.Mode);
        Rejected(state, Command(state, CommandKind.StopBattle));
        state = NoCards(state, 5, 1);
        Assert.Equal(5, BattleRuntime.Active(state)!.Attacker.Round.Dice[0].HitTarget);
        Assert.Equal(5, BattleRuntime.Active(state)!.Defender.Round.Dice[0].HitTarget);
        state = Losses(state, Loss(defender.Id, BattleLossKind.Downgrade));
        state = Execute(state, CommandKind.StopBattle);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(2, state.Units.Count(u => u.Location == Origin && u.Garrison));
        Assert.Equal(Side.FreePeoples, state.Control[Origin]);
        Assert.True(BoardRules.Besieged(state, Origin, Side.FreePeoples));
        BattleArchiveTests.Completed(state, state.LastBattleRound!.Mode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MutualDestructionDuringSortieKeepsControlAndReleasesAnyRear(bool rear)
    {
        var state = SiegeTests.SetupSiege(Side.FreePeoples);
        var attacker = state.Units.Single(u => u.Garrison);
        if (rear)
            Put(state, "gondor", UnitKind.Regular, Origin).Garrison = true;
        var companion = Character(state, "test.garrison", Side.FreePeoples, Origin);
        companion.Garrison = true;
        Put(state, "sauron", UnitKind.Regular, Origin);
        state = NoCards(Begin(state, [attacker.Id, companion.Id], from: Origin, to: Origin), 6, 5);
        state = Losses(state, Loss(attacker.Id));
        Assert.Equal(Side.FreePeoples, state.Control[Origin]);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        // The eliminated sortie force still deals its simultaneous hit.
        var defender = state.Units.Single(u => u.Location == Origin && !u.Garrison && u.Kind == UnitKind.Regular);
        state = Losses(state, Loss(defender.Id));
        Assert.Equal(Side.FreePeoples, state.Control[Origin]);
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == companion.Id).Location);
        Assert.Equal(rear ? 1 : 0, state.Units.Count(u => u.Location == Origin));
        Assert.DoesNotContain(state.Units, u => u.Location == Origin && u.Garrison);
        BattleArchiveTests.Completed(state, BattleMode.Sortie);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LivingBesiegerCapturesAfterFailedSortieUnlessRearSurvives(bool rear)
    {
        var state = SiegeTests.SetupSiege(Side.FreePeoples);
        var attacker = state.Units.Single(u => u.Garrison);
        if (rear)
            Put(state, "gondor", UnitKind.Regular, Origin).Garrison = true;
        Put(state, "sauron", UnitKind.Regular, Origin);
        state = NoCards(Begin(state, [attacker.Id], from: Origin, to: Origin), 1, 5);
        state = Losses(state, Loss(attacker.Id));
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(rear ? Side.FreePeoples : Side.Shadow, state.Control[Origin]);
        Assert.Equal(rear ? 0 : 2, MilitaryVictory.Points(state, Side.Shadow));
        BattleArchiveTests.Completed(state, BattleMode.Sortie);
    }

    [Fact]
    public void SortieVictoryReleasesWholeGarrisonAndNeverOffersAdvanceOutsideTheRegion()
    {
        var state = SiegeTests.SetupSiege(Side.FreePeoples);
        var attacker = state.Units.Single(u => u.Garrison);
        var rear = Put(state, "gondor", UnitKind.Regular, Origin);
        rear.Garrison = true;
        var defender = Put(state, "sauron", UnitKind.Regular, Origin);
        state = NoCards(Begin(state, [attacker.Id], from: Origin, to: Origin), 5, 1);
        state = Losses(state, Loss(defender.Id));
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        Assert.DoesNotContain(CommandKind.AdvanceBattle, state.Pending.Commands);
        Assert.All(state.Units.Where(u => u.Location == Origin), u => Assert.False(u.Garrison));
        Assert.Equal(Origin, state.Units.Single(u => u.Id == rear.Id).Location);
        var report = BattleArchiveTests.Completed(state, BattleMode.Sortie);
        Assert.DoesNotContain(report.Steps, s => s.Command == CommandKind.AdvanceBattle);
    }

    [Fact]
    public void BesiegerCanRetreatFromSortieAndTheGarrisonReturnsToTheField()
    {
        var state = SiegeTests.SetupSiege(Side.FreePeoples);
        var attacker = state.Units.Single(u => u.Garrison);
        var defender = Put(state, "sauron", UnitKind.Regular, Origin);
        state = NoCards(Begin(state, [attacker.Id], from: Origin, to: Origin), 1, 1);
        state = Execute(state, CommandKind.ContinueBattle);
        state = Execute(state, CommandKind.RetreatBattle, new() { RegionId = "osgiliath" });
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal("osgiliath", state.Units.Single(u => u.Id == defender.Id).Location);
        Assert.False(state.Units.Single(u => u.Id == attacker.Id).Garrison);
        Assert.Equal(Side.FreePeoples, state.Control[Origin]);
    }

    [Fact]
    public void ReliefExcludesGarrisonThenOptionalAdvanceChecksTheCombinedTenUnitLimit()
    {
        var state = State();
        var arrivals = Enumerable.Range(0, 7).Select(_ => Put(state, "gondor", UnitKind.Regular, "osgiliath").Id).ToList();
        var garrison = Enumerable.Range(0, 5).Select(_ => Put(state, "gondor", UnitKind.Regular, Origin)).ToArray();
        foreach (var unit in garrison)
            unit.Garrison = true;
        var besieger = Put(state, "sauron", UnitKind.Regular, Origin);
        Rejected(state, Command(state, CommandKind.AttackArmy, AttackPayload(state, arrivals.Append(garrison[0].Id), "osgiliath", Origin)));
        state = Begin(state, arrivals, from: "osgiliath", to: Origin);
        Assert.Equal(BattleMode.Relief, BattleRuntime.Current(state)!.Mode);
        Assert.Equal(7, BattleRules.Strength(state, BattleRuntime.Current(state)!.Attacker));
        state = NoCards(state, 5, 1, 1, 1, 1, 1);
        state = Losses(state, Loss(besieger.Id));
        Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
        Assert.DoesNotContain(state.Units, u => u.Location == Origin && u.Garrison);
        Assert.All(state.Units.Where(u => arrivals.Contains(u.Id)), u => Assert.Equal("osgiliath", u.Location));
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples, Command(state, CommandKind.AdvanceBattle, new() { Ids = arrivals }));
        Assert.Equal(2, Assert.Single(preview.Excess).Count);
        state = Execute(state, CommandKind.AdvanceBattle, new() { Ids = arrivals });
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        state = Execute(StateCodec.Copy(state), CommandKind.RemoveExcess, new() { Ids = [garrison[0].Id, arrivals[0]] });
        Assert.Equal(10, state.Units.Count(u => u.Location == Origin));
        var report = BattleArchiveTests.Completed(state, BattleMode.Relief);
        Assert.Equal(7, report.Initial!.Attacker.Strength);
        Assert.Equal(10, report.Place!.Units.Count);
        Assert.Contains(report.Steps, s => s.Command == CommandKind.RemoveExcess);
        Assert.Equal(Side.FreePeoples, state.Control[Origin]);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void LosingReliefDoesNotUseGarrisonLeadershipOrEliminateItsFigures()
    {
        var state = SiegeTests.SetupSiege(Side.FreePeoples);
        var garrison = state.Units.Single(u => u.Garrison);
        var leader = Put(state, "gondor", UnitKind.Leader, Origin);
        leader.Garrison = true;
        var attacker = Put(state, "gondor", UnitKind.Regular, "osgiliath");
        Put(state, "sauron", UnitKind.Regular, Origin);
        state = NoCards(Begin(state, [attacker.Id], from: "osgiliath", to: Origin), 1, 5);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        state = Losses(state, Loss(attacker.Id));
        Assert.Empty(state.Resolution.Frames);
        Assert.True(state.Units.Single(u => u.Id == garrison.Id).Garrison);
        Assert.True(state.Units.Single(u => u.Id == leader.Id).Garrison);
        Assert.True(BoardRules.Besieged(state, Origin, Side.FreePeoples));
        BattleArchiveTests.Completed(state, state.LastBattleRound!.Mode);
    }

    [Fact]
    public void EventLossesCanEndSiegeWithoutKillingLoneCharacters()
    {
        var state = SiegeTests.SetupSiege();
        var garrison = state.Units.Single(u => u.Garrison);
        var leader = Put(state, "gondor", UnitKind.Leader, Origin);
        leader.Garrison = true;
        var companion = Character(state, "test.guide", Side.FreePeoples, Origin);
        companion.Garrison = true;
        Put(state, "sauron", UnitKind.Regular, Origin);
        CasualtyRules.Eliminate(state, [garrison.Id], CasualtyCause.Event);
        Assert.Equal(Side.Shadow, state.Control[Origin]);
        Assert.Equal("casualties", leader.Location);
        Assert.Equal(Origin, companion.Location);
        Assert.False(companion.Garrison);
    }
}
