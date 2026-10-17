using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.BattleFixture;

namespace Wotr.Tests;

public class SiegeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShelterOnlyEnforcesFiveAfterAttackerActuallyAdvances(bool advance)
    {
        var state = State(Side.Shadow);
        var attacker = Put(state, "sauron", UnitKind.Regular, "osgiliath");
        var defenders = Enumerable.Range(0, 6).Select(_ => Put(state, "gondor", UnitKind.Regular, Origin).Id).ToList();
        state = Begin(state, [attacker.Id], from: "osgiliath", to: Origin);
        Assert.Equal(DecisionKind.StrongholdDefense, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        state = Execute(state, CommandKind.RetreatIntoSiege);
        Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
        Assert.All(state.Units.Where(u => defenders.Contains(u.Id)), u => Assert.True(u.Garrison));
        Assert.Equal(6, state.Units.Count(u => u.Location == Origin && u.Garrison));
        Assert.Empty(state.RandomSamples);
        var command = Command(state, CommandKind.AdvanceBattle, new() { Ids = advance ? [attacker.Id] : [] });
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow, command);
        Assert.True(preview.Legal);
        Assert.Equal(advance ? 1 : 0, preview.Excess.Count);
        state = Execute(state, CommandKind.AdvanceBattle, command.Payload);
        if (advance)
        {
            Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
            Assert.Equal(1, state.Pending.RequiredCount);
            Assert.Equal(Side.FreePeoples, state.Pending.Owner);
            Assert.Single(state.Dice, d => d.Location == DieLocation.Resolving);
            state = Execute(StateCodec.Copy(state), CommandKind.RemoveExcess, new() { Ids = [defenders[0]] });
            Assert.Equal(5, state.Units.Count(u => u.Location == Origin && u.Garrison));
            Assert.Equal("reserve", state.Units.Single(u => u.Id == defenders[0]).Location);
            Assert.Equal(Origin, state.Units.Single(u => u.Id == attacker.Id).Location);
        }
        else
        {
            Assert.Equal("osgiliath", state.Units.Single(u => u.Id == attacker.Id).Location);
            Assert.All(state.Units.Where(u => defenders.Contains(u.Id)), u => Assert.False(u.Garrison));
            Assert.Equal(6, state.Units.Count(u => u.Location == Origin));
        }
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(Side.FreePeoples, state.Control[Origin]);
        Assert.Equal(0, MilitaryVictory.Points(state, Side.Shadow));
        Assert.Contains(state.Events, e => e.Kind == "BattleEnded" && e.Text.EndsWith("0 round(s)."));
        var steps = ActionLogFamilyTests.Steps(state);
        Assert.DoesNotContain(steps, s => s.Kind == "BattleRoundCompleted");
        Assert.DoesNotContain(steps, s => s.Facts.Battle?.CardsRevealed == true);
        Assert.Equal(ActionLogStatus.Completed, steps.Last().Status);
        if (advance)
            Assert.Contains(steps, s => s.Command == CommandKind.RemoveExcess && s.Actor == Side.FreePeoples);
    }

    [Fact]
    public void StrongholdFieldBattleHitsOnFiveAndCanShelterAfterFirstRound()
    {
        var state = State(Side.Shadow);
        var attacker = Put(state, "sauron", UnitKind.Regular, "osgiliath");
        var defender = Put(state, "gondor", UnitKind.Elite, Origin);
        state = Begin(state, [attacker.Id], from: "osgiliath", to: Origin);
        state = Execute(state, CommandKind.FightFieldBattle);
        state = NoCards(state, 5, 1);
        Assert.Equal(5, BattleRuntime.Active(state)!.Attacker.Round.Dice[0].HitTarget);
        state = Losses(state, Loss(defender.Id, BattleLossKind.Downgrade));
        state = Execute(state, CommandKind.ContinueBattle);
        Assert.Contains(new StrongholdCommands().GetOptions(state, Side.FreePeoples), o => o.Kind == CommandKind.RetreatIntoSiege);
        state = Execute(state, CommandKind.RetreatIntoSiege);
        state = Execute(state, CommandKind.AdvanceBattle, new() { Ids = [attacker.Id] });
        Assert.Single(state.Units, u => u.Location == Origin && u.Garrison);
        Assert.Contains(state.Events, e => e.Kind == "BattleEnded" && e.Text.EndsWith("1 round(s)."));
    }

    [Fact]
    public void AssaultUsesSixEveryRoundAndEachExtraRoundCostsOneParticipatingElite()
    {
        var state = SetupSiege();
        var first = Put(state, "sauron", UnitKind.Elite, Origin);
        var second = Put(state, "sauron", UnitKind.Elite, Origin);
        var rear = Put(state, "sauron", UnitKind.Elite, Origin);
        state = Begin(state, [first.Id, second.Id], from: Origin, to: Origin);
        Assert.Equal(BattleMode.Siege, BattleRuntime.Current(state)!.Mode);
        state = NoCards(state, 5, 5, 1);
        Assert.Equal(DecisionKind.SiegeContinuation, state.Pending!.Kind);
        Rejected(state, Command(state, CommandKind.RetreatBattle, new() { RegionId = "lossarnach" }), Side.FreePeoples);
        Rejected(state, Command(state, CommandKind.ContinueBattle));
        Rejected(state, Command(state, CommandKind.ExtendSiege, new() { Ids = [rear.Id] }));
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow,
            Command(state, CommandKind.ExtendSiege, new() { Ids = [first.Id] }));
        Assert.Contains(first.Id, preview.RemovedIds);
        Assert.Single(preview.Placements);
        state = Execute(StateCodec.Copy(state), CommandKind.ExtendSiege, new() { Ids = [first.Id] });
        Assert.Equal(2, BattleRuntime.Current(state)!.Round);
        Assert.Equal("reserve", state.Units.Single(u => u.Id == first.Id).Location);
        state = NoCards(state, 5, 5, 1);
        Assert.Equal(DecisionKind.SiegeContinuation, state.Pending!.Kind);
        state = Execute(state, CommandKind.ExtendSiege, new() { Ids = [second.Id] });
        state = NoCards(state, 5, 5, 1);
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Equal(Side.FreePeoples, state.Control[Origin]);
        Assert.Equal(Origin, state.Units.Single(u => u.Id == rear.Id).Location);
        Assert.Single(state.Units, u => u.Location == Origin && u.Garrison);
        var steps = ActionLogFamilyTests.Steps(state);
        Assert.Equal(3, steps.Count(s => s.Kind == "BattleRoundCompleted"));
        var report = BattleArchiveTests.Completed(state, BattleMode.Siege);
        Assert.Equal(3, report.Rounds.Count);
        Assert.Contains(report.Place!.Units, u => u.Id == rear.Id);
        Assert.DoesNotContain(report.Initial!.Attacker.ParticipantIds, id => id == rear.Id);
        Assert.Equal(new[] { first.Id, second.Id }, steps.SelectMany(s => s.Costs).Where(c => c.Kind == ActionCostKind.Figure).Select(c => c.Id));
        Assert.Single(steps.SelectMany(s => s.Costs), c => c.Kind == ActionCostKind.Die);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NoRegularReplacementNeverCreatesAFigureAndCanEndAssault(bool anotherUnit)
    {
        var state = SetupSiege();
        var elite = Put(state, "sauron", UnitKind.Elite, Origin);
        var ids = new List<string> { elite.Id };
        if (anotherUnit)
            ids.Add(Put(state, "sauron", UnitKind.Elite, Origin).Id);
        foreach (var regular in state.Units.Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular))
            regular.Location = "removed";
        state = Begin(state, ids, from: Origin, to: Origin);
        state = NoCards(state, anotherUnit ? [1, 1, 1] : [1, 1]);
        state = Execute(state, CommandKind.ExtendSiege, new() { Ids = [elite.Id] });
        Assert.Equal("reserve", state.Units.Single(u => u.Id == elite.Id).Location);
        var extension = ActionLogFamilyTests.Steps(state).Single(s => s.Command == CommandKind.ExtendSiege);
        Assert.Equal(elite.Id, Assert.Single(extension.Costs).Id);
        Assert.Equal(ActionCostKind.Figure, extension.Costs[0].Kind);
        Assert.Contains(extension.Facts.Figures, f => f.Before?.Id == elite.Id && f.From == Origin && f.To == "reserve");
        Assert.DoesNotContain(state.Units, u => u.Location == Origin && u.Kind == UnitKind.Regular && BoardRules.Owner(u) == Side.Shadow);
        if (anotherUnit)
            Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        else
        {
            Assert.Empty(state.Resolution.Frames);
            Assert.False(state.Units.Single(u => u.Location == Origin).Garrison);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CaptureWaitsForBothLossesAndOnlyAnArmyStillInTheRegionCanCapture(bool attackerDies, bool rear)
    {
        var state = SetupSiege();
        var attacker = Put(state, "sauron", UnitKind.Regular, Origin);
        if (rear)
            Put(state, "sauron", UnitKind.Regular, Origin);
        var defender = state.Units.Single(u => u.Garrison);
        state = NoCards(Begin(state, [attacker.Id], from: Origin, to: Origin), 6, attackerDies ? 6 : 1);
        if (attackerDies)
        {
            state = Losses(state, Loss(attacker.Id));
            Assert.Equal(Side.FreePeoples, state.Control[Origin]);
            Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        }
        state = Losses(state, Loss(defender.Id));
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(attackerDies && !rear ? Side.FreePeoples : Side.Shadow, state.Control[Origin]);
        Assert.Equal(attackerDies && !rear ? 0 : 2, MilitaryVictory.Points(state, Side.Shadow));
        BattleArchiveTests.Completed(state, BattleMode.Siege);
    }

    [Fact]
    public void AssaultCanRecaptureItsOwnStrongholdAndRemoveEnemyPoints()
    {
        var state = State();
        state.Control[Origin] = Side.Shadow;
        var attacker = Put(state, "gondor", UnitKind.Regular, Origin);
        var defender = Put(state, "sauron", UnitKind.Regular, Origin);
        defender.Garrison = true;
        Assert.Equal(2, MilitaryVictory.Points(state, Side.Shadow));
        state = NoCards(Begin(state, [attacker.Id], from: Origin, to: Origin), 6, 1);
        state = Losses(state, Loss(defender.Id));
        Assert.Equal(Side.FreePeoples, state.Control[Origin]);
        Assert.Equal(0, MilitaryVictory.Points(state, Side.Shadow));
        Assert.Equal(0, MilitaryVictory.Points(state, Side.FreePeoples));
    }

    internal static MatchState SetupSiege(Side actor = Side.Shadow)
    {
        var state = State(actor);
        Put(state, "gondor", UnitKind.Regular, Origin).Garrison = true;
        return state;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NextRoundOffersShelterEvenWhenSurroundedAndFieldChoiceNeverNeedsASecondConfirmation(bool surrounded)
    {
        var state = State(Side.Shadow);
        var attacker = Put(state, "sauron", UnitKind.Regular, "osgiliath");
        Put(state, "gondor", UnitKind.Regular, Origin);
        if (surrounded)
        {
            Put(state, "sauron", UnitKind.Regular, "druadan-forest");
            Put(state, "sauron", UnitKind.Regular, "lossarnach");
        }
        state = Begin(state, [attacker.Id], from: "osgiliath", to: Origin);
        state = Execute(state, CommandKind.FightFieldBattle);
        state = NoCards(state, 1, 1);
        state = Execute(state, CommandKind.ContinueBattle);
        Assert.Contains(new StrongholdCommands().GetOptions(state, Side.FreePeoples), o => o.Kind == CommandKind.RetreatIntoSiege);
        Assert.Equal(DecisionKind.BattleRetreat, state.Pending!.Kind);
        state = Execute(state, CommandKind.HoldBattle);
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        Assert.Equal(2, BattleRuntime.Current(state)!.Round);
    }
}
