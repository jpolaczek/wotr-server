using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.BattleFixture;
using static Wotr.Tests.ActionLogFamilyTests;

namespace Wotr.Tests;

public class BattleArchiveTests
{
    [Fact]
    public void TwoRoundsAndTwoBattlesAtSameRegionKeepIndependentHistoricalSnapshots()
    {
        var state = State();
        var attacker = Put(state, "gondor", UnitKind.Regular, Origin);
        var defender = Put(state, "sauron", UnitKind.Regular, Target);
        var nation = state.Nations.Single(n => n.Id == "sauron");
        nation.Active = false;
        nation.StepsToWar = 2;
        state = Begin(state, [attacker.Id]);
        var firstId = BattleRuntime.Active(state)!.Id;
        Assert.Empty(Report(state, firstId).Rounds);
        state = NoCards(state, 1, 2);
        state = Execute(state, CommandKind.ContinueBattle);
        state = Execute(state, CommandKind.HoldBattle);
        Assert.Single(Report(state, firstId).Rounds);
        Assert.DoesNotContain(Report(state, firstId).Steps, s => s.Facts.Nations.Any(n => n.Id == "sauron" && n.Active));
        state = NoCards(state, 3, 4);
        state = Execute(state, CommandKind.StopBattle);
        var first = Report(StateCodec.Copy(state), firstId);
        Assert.Equal(BattleReportStatus.Completed, first.Status);
        Assert.Equal(new[] { 1, 2 }, first.Rounds.Select(r => r.Number));
        Assert.All(first.Rounds, r => Assert.True(r.Completed));
        Assert.Equal(1, first.Rounds[0].Result!.Attacker.Dice[0].Result);
        Assert.Equal(3, first.Rounds[1].Result!.Attacker.Dice[0].Result);
        Assert.Single(first.Costs, c => c.Kind == ActionCostKind.Die);
        Assert.Contains(first.Steps, s => s.Facts.Nations.Any(n => n.Id == "sauron" && n.Active && n.StepsToWar == 1));
        var saved = Json(first);

        // Shadow discards a die; the second real attack has the same origin, target and participants.
        state = Discard(state);
        state = Begin(state, [attacker.Id]);
        var secondId = BattleRuntime.Active(state)!.Id;
        Assert.NotEqual(firstId, secondId);
        state = NoCards(state, 6, 1);
        state = Losses(state, Loss(defender.Id));
        Assert.Equal(BattleReportStatus.InProgress, Report(state, secondId).Status);
        state = Execute(state, CommandKind.AdvanceBattle, new() { Ids = [attacker.Id] });
        Assert.Equal(saved, Json(Report(StateCodec.Copy(state), firstId)));
        Assert.Equal(Target, Report(state, secondId).Place!.Units.Single(u => u.Id == attacker.Id).Location);
        Assert.Null(BattleReportProjection.Create(state.Id, "unknown", ActionLogFixture.Records(state, Side.FreePeoples)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnrolledShelterAndStayNeverInventRoundOrCapture(bool advance)
    {
        var state = State(Side.Shadow);
        var attacker = Put(state, "sauron", UnitKind.Regular, "osgiliath");
        Put(state, "gondor", UnitKind.Regular, Origin);
        state = Begin(state, [attacker.Id], from: "osgiliath", to: Origin);
        var id = BattleRuntime.Active(state)!.Id;
        state = Execute(state, CommandKind.RetreatIntoSiege);
        Assert.Equal(BattleReportStatus.InProgress, Report(state, id).Status);
        state = Execute(state, CommandKind.AdvanceBattle, new() { Ids = advance ? [attacker.Id] : [] });
        var report = Completed(state, BattleMode.Field);
        Assert.Empty(report.Rounds);
        Assert.Equal(Side.FreePeoples, report.Place!.Controller);
        Assert.Equal(0, report.Outcome!.ShadowPoints);
        Assert.Empty(state.RandomSamples);
        Assert.Equal(advance ? Origin : "osgiliath", report.Place!.Units.Single(u => u.Id == attacker.Id).Location);
        Assert.Equal(advance, report.Place.Units.Single(u => u.Side == Side.FreePeoples).Garrison);
    }

    [Fact]
    public void BothSidesSeeIdenticalReportWhileOriginalCommittedCardAndRerollStayPrivate()
    {
        var state = State();
        var unit = Put(state, "gondor", UnitKind.Regular, Origin);
        var leader = Put(state, "gondor", UnitKind.Leader, Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        Put(state, "sauron", UnitKind.Nazgul, Target);
        EventTests.GiveCard(state, Side.FreePeoples, FpCard);
        state = Begin(state, [unit.Id, leader.Id]);
        var id = BattleRuntime.Active(state)!.Id;
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = FpCard });
        var hidden = Report(state, id);
        Assert.DoesNotContain(FpCard, Json(hidden));
        state = Execute(state, CommandKind.CommitCombatCard, random: new SequenceRandom(0, 0));
        var before = Json(Report(state, id));
        var die = BattleRuntime.Active(state)!.Attacker.Round.Dice[0].Id;
        state = Execute(state, CommandKind.RerollBattle, new() { Ids = [die] });
        Assert.Equal(before, Json(Report(state, id)));
        state = Execute(state, CommandKind.RerollBattle, random: new SequenceRandom(5));
        var report = Report(state, id);
        Assert.Equal(6, report.Latest!.Attacker.Dice[0].Reroll);
        Assert.DoesNotContain(FpCard, Json(hidden));
        Assert.Contains(report.Steps, s => s.Facts.Cards.Any(c => c.Id == FpCard));
        Assert.All(report.Steps.Select(s => s.Facts.Battle).OfType<BattleView>(), b =>
        {
            Assert.Empty(b.Attacker.OwnRerollSelection);
            Assert.Empty(b.Defender.OwnRerollSelection);
            Assert.Empty(b.OwnTableCombatCards);
        });
    }

    [Fact]
    public void LegacyRoundSurvivesFurtherPlayWithoutClaimingUnknownHistoryOrCosts()
    {
        var state = State();
        var unit = Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        state = NoCards(Begin(state, [unit.Id]), 1, 1);
        state = Execute(state, CommandKind.StopBattle);
        var id = state.LastBattleRound!.Id;
        foreach (var item in state.Events)
            item.Activity = null;
        var legacy = Report(state, id);
        Assert.True(legacy.Legacy);
        Assert.True(legacy.PartialHistory);
        Assert.Null(legacy.Initial);
        Assert.Null(legacy.Turn);
        Assert.Empty(legacy.Costs);
        var snapshot = Json(legacy.Rounds[0].Result);
        var samples = state.RandomSamples.Count;
        state = Discard(state);
        Assert.Equal(samples, state.RandomSamples.Count);
        state = NoCards(Begin(state, [unit.Id]), 2, 2);
        state = Execute(state, CommandKind.StopBattle);
        var preserved = Report(StateCodec.Copy(state), id);
        Assert.True(preserved.Legacy);
        Assert.Null(preserved.Turn);
        Assert.Equal(BattleReportStatus.Unknown, preserved.Status);
        Assert.Empty(preserved.Costs);
        Assert.Equal(snapshot, Json(preserved.Rounds.Single().Result));
        Assert.Single(state.Events, e => e.Kind == "LegacyBattleRound");
    }

    internal static BattleReportView Report(MatchState state, string id)
    {
        var fp = BattleReportProjection.Create(state.Id, id, ActionLogFixture.Records(state, Side.FreePeoples), state.LastBattleRound)!;
        var sh = BattleReportProjection.Create(state.Id, id, ActionLogFixture.Records(state, Side.Shadow), state.LastBattleRound)!;
        Assert.NotNull(fp);
        Assert.Equal(Json(fp), Json(sh));
        return fp;
    }

    private static MatchState Discard(MatchState state) => Execute(state, CommandKind.DiscardDie,
        new() { DieId = state.Dice.First(d => d.Owner == state.Pending!.Owner && d.Location == DieLocation.Available).Id });

    internal static BattleReportView Completed(MatchState state, BattleMode mode)
    {
        var id = state.Events.Last(e => e.Kind == "BattleEnded").Activity!.Context.BattleId;
        var report = Report(StateCodec.Copy(state), id);
        Assert.Equal(mode, report.Initial!.Mode);
        Assert.Equal(mode, report.Latest!.Mode);
        Assert.False(report.PartialHistory);
        Assert.Equal(BattleReportStatus.Completed, report.Status);
        Assert.Equal(state.Control.GetValueOrDefault(report.Place!.RegionId), report.Place.Controller);
        Assert.Equal(MilitaryVictory.Points(state, Side.FreePeoples), report.Outcome!.FreePeoplesPoints);
        Assert.Equal(MilitaryVictory.Points(state, Side.Shadow), report.Outcome.ShadowPoints);
        Assert.Equal(state.Winner, report.Outcome.Winner);
        Assert.Equal(report.Steps.Count, report.Steps.Select(s => s.Id).Distinct().Count());
        return report;
    }
}
