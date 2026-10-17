using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.BattleFixture;

namespace Wotr.Tests;

public class BattleLossTests
{
    [Theory]
    [InlineData("casualties", "casualties")]
    [InlineData("reserve", "casualties")]
    [InlineData("none", "casualties")]
    public void EliteDowngradeUsesSameNationCasualtiesBeforeReserveOrDiesForOneHit(string regularPool, string expectedElitePool)
    {
        var state = State();
        var elite = Put(state, "gondor", UnitKind.Elite, Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        var regulars = state.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular).ToArray();
        foreach (var regular in regulars)
            regular.Location = regularPool == "none" ? "removed" : "reserve";
        var chosen = regulars.OrderBy(u => u.Id, StringComparer.Ordinal).First();
        if (regularPool == "casualties")
        {
            chosen = regulars.OrderBy(u => u.Id, StringComparer.Ordinal).Last();
            chosen.Location = "casualties";
        }
        state = NoCards(Begin(state, [elite.Id]), 1, 6);
        var command = Command(state, CommandKind.TakeBattleLosses, new() { Losses = [Loss(elite.Id, BattleLossKind.Downgrade)] });
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples, command);
        Assert.True(preview.Legal);
        Assert.Equal(before, StateCodec.Write(state));
        Assert.Contains(elite.Id, preview.RemovedIds);
        Assert.NotNull(preview.Losses);
        Assert.Equal(1, preview.Losses.AppliedHits);
        var operation = Assert.Single(preview.Losses.Operations);
        Assert.Equal(BattleLossKind.Downgrade, operation.Operation);
        Assert.Equal(1, operation.Hits);
        Assert.Equal(regularPool == "none" ? "" : regularPool, operation.ReplacementFrom);
        Assert.Equal(regularPool == "none" ? "" : chosen.Id, operation.ReplacementId);
        state = Losses(state, Loss(elite.Id, BattleLossKind.Downgrade));
        Assert.Equal(expectedElitePool, state.Units.Single(u => u.Id == elite.Id).Location);
        if (regularPool == "none")
        {
            Assert.DoesNotContain(state.Units, u => u.Location == Origin);
            Assert.Null(BattleRuntime.Active(state));
        }
        else
        {
            Assert.Equal(Origin, state.Units.Single(u => u.Id == chosen.Id).Location);
            Assert.Contains(chosen.Id, BattleRuntime.Active(state)!.Attacker.Participants);
            Assert.Equal(chosen.Id, Assert.Single(preview.Placements).UnitId);
        }
    }

    [Fact]
    public void RemovingEliteNeedsTwoHitsAndRegularLossCanSupplyLaterDowngrade()
    {
        var state = State();
        var elite = Put(state, "gondor", UnitKind.Elite, Origin);
        var regular = Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        Put(state, "sauron", UnitKind.Regular, Target);
        foreach (var reserve in state.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular && u.Location == "reserve"))
            reserve.Location = "removed";
        state = NoCards(Begin(state, [elite.Id, regular.Id]), 1, 1, 6, 6);
        Rejected(state, Command(state, CommandKind.TakeBattleLosses, new() { Losses = [Loss(regular.Id)] }));
        Rejected(state, Command(state, CommandKind.TakeBattleLosses, new() { Losses = [Loss(regular.Id), Loss(elite.Id)] }));
        var remove = Execute(StateCodec.Copy(state), CommandKind.TakeBattleLosses, new() { Losses = [Loss(elite.Id)] });
        Assert.Equal(Origin, remove.Units.Single(u => u.Id == regular.Id).Location);
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples,
            Command(state, CommandKind.TakeBattleLosses, new() { Losses = [Loss(regular.Id), Loss(elite.Id, BattleLossKind.Downgrade)] }));
        Assert.True(preview.Legal);
        Assert.Equal(new[] { regular.Id, elite.Id }, preview.Losses!.Operations.Select(o => o.Figure.Id));
        Assert.Equal(regular.Id, preview.Losses.Operations[1].ReplacementId);
        Assert.Equal("casualties", preview.Losses.Operations[1].ReplacementFrom);
        Assert.Empty(preview.Placements); // The reused regular finishes where it began.
        state = Losses(state, Loss(regular.Id), Loss(elite.Id, BattleLossKind.Downgrade));
        Assert.Equal(Origin, state.Units.Single(u => u.Id == regular.Id).Location);
        Assert.Equal("casualties", state.Units.Single(u => u.Id == elite.Id).Location);
        Assert.Equal(1, BattleRules.Strength(state, BattleRuntime.Current(state)!.Attacker));
    }

    [Fact]
    public void OneHitCannotRemoveAnEliteForTwoOrDowngradeARegular()
    {
        var state = State();
        var elite = Put(state, "gondor", UnitKind.Elite, Origin);
        var regular = Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        state = NoCards(Begin(state, [elite.Id, regular.Id]), 1, 1, 6);
        Rejected(state, Command(state, CommandKind.TakeBattleLosses, new() { Losses = [Loss(elite.Id)] }));
        Rejected(state, Command(state, CommandKind.TakeBattleLosses, new() { Losses = [Loss(regular.Id, BattleLossKind.Downgrade)] }));
        Rejected(state, Command(state, CommandKind.TakeBattleLosses, new() { Losses = [Loss(regular.Id), Loss(regular.Id)] }));
    }

    [Fact]
    public void AttackerDiesFirstButDefenderStillTakesCommittedHitsAndBothCommandersDie()
    {
        var state = State();
        var attacker = Put(state, "gondor", UnitKind.Regular, Origin);
        var defender = Put(state, "sauron", UnitKind.Regular, Target);
        var leader = Put(state, "gondor", UnitKind.Leader, Origin);
        var nazgul = Put(state, "sauron", UnitKind.Nazgul, Target);
        var companion = Character(state, "test.companion", Side.FreePeoples, Origin);
        var minion = Character(state, "test.minion", Side.Shadow, Target);
        state = NoCards(Begin(state, [attacker.Id, leader.Id, companion.Id]), 6, 6);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        state = Losses(state, Loss(attacker.Id));
        Assert.Equal(DieLocation.Resolving, state.Dice[0].Location);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Equal(1, state.Pending.RequiredCount);
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == companion.Id).Location);
        state = Losses(StateCodec.Copy(state), Loss(defender.Id));
        Assert.Equal("reserve", state.Units.Single(u => u.Id == nazgul.Id).Location);
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == minion.Id).Location);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(DieLocation.Spent, state.Dice[0].Location);
    }

    [Fact]
    public void ExcessHitsDisappearOnlyAfterLastArmyUnitIsRemoved()
    {
        var state = State();
        var attacker = Put(state, "gondor", UnitKind.Regular, Origin);
        for (var i = 0; i < 5; i++)
            Put(state, "sauron", UnitKind.Regular, Target);
        state = NoCards(Begin(state, [attacker.Id]), 1, 6, 6, 6, 6, 6);
        Assert.Equal(5, state.Pending!.RequiredCount);
        state = Losses(state, Loss(attacker.Id));
        Assert.Empty(state.Resolution.Frames);
    }
}
