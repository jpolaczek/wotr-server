using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsCardsFixture;

namespace Wotr.Tests;

public class LordsRangersTests
{
    internal static MatchState LossWindow()
    {
        var state = State();
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "bree");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "bree");
        return Play(state, LordsFreePeoplesCards.Rangers, 4, 0, 3);
    }

    // The lone army in Bree is attacked without a choice; the boundary names each region the
    // later steps read.
    [Fact]
    public void EveryRangersDecisionIsMarkedWithTheRegionItReads()
    {
        var losses = LossWindow();
        var victim = EventLosses.Current(losses)!.Target.Units(losses).First().Id;
        var region = LordsFixture.Do(StateCodec.Copy(losses), CommandKind.TakeEventLosses,
            new() { Losses = [BattleFixture.Loss(victim)] });
        var figures = Choose(StateCodec.Copy(region), "buckland");

        Assert.Equal(DecisionKind.Losses, losses.Pending!.Kind);
        Assert.Equal("Region", region.Pending!.Presentation);
        Assert.Equal("ReserveFigures", figures.Pending!.Presentation);
        foreach (var (state, regionId) in new[] { (losses, "bree"), (region, "bree"), (figures, "buckland") })
            Assert.All(Enum.GetValues<Side>(), side =>
            {
                var choice = LordsFixture.Views().For(state, side).ActionBoundary?.DirectEventChoice;
                Assert.Equal(LordsFreePeoplesCards.Rangers, choice?.CardId);
                Assert.Equal(regionId, choice!.RegionId);
            });
        losses.Resolution.Frames[0].Effect!.RegionId = "arnor";
        Assert.Null(LordsFixture.Views().For(losses, Side.Shadow).ActionBoundary?.DirectEventChoice);
    }

    [Fact]
    public void OnlyArmiesInOrAdjacentToNorthAreEligibleAndDamagePrecedesRecruitment()
    {
        var state = State();
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "bree");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "arnor");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "lossarnach");
        BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "the-shire");
        state = Play(state, LordsFreePeoplesCards.Rangers);
        Assert.Equal(new[] { "arnor", "bree" }, state.Pending!.PrivateChoices.Order());
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = ["lossarnach"] });
        state = LordsFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = ["arnor"] }, new SequenceRandom(4, 3, 5));
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Equal(2, EventLosses.Current(state)!.Hits);
        Assert.DoesNotContain(state.Events, e => e.Kind == "EventRecruited");
    }

    [Theory]
    [InlineData(UnitKind.Regular)]
    [InlineData(UnitKind.Elite)]
    public void ShadowChoosesEventLossesThenFreePeoplesChoosesNorthRegionAndUnitKind(UnitKind kind)
    {
        var state = LossWindow();
        var victim = EventLosses.Current(state)!.Target.Units(state).First().Id;
        state = LordsFixture.Do(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(victim)] });
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.All(state.Pending.PrivateChoices, id => Assert.Equal("north", BoardRules.Regions[id].Nation));
        Assert.DoesNotContain("bree", state.Pending.PrivateChoices);
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = ["the-shire"] });
        state = Choose(state, "buckland");
        var unit = state.Units.Single(u => state.Pending!.PrivateChoices.Contains(u.Id) && u.Kind == kind);
        state = Choose(state, unit.Id);
        Assert.Equal("buckland", state.Units.Single(u => u.Id == unit.Id).Location);
        Assert.Single(state.Units, u => u.Location == "buckland" && u.Kind == UnitKind.Leader);
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void DestroyingArmyLeavesNazgulAndRecruitmentStillUsesOriginalTargetRegion()
    {
        var state = State();
        var victim = BattleFixture.Put(state, "sauron", UnitKind.Regular, "bree").Id;
        var nazgul = BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "bree").Id;
        state = Play(state, LordsFreePeoplesCards.Rangers, 5, 0, 0);
        state = LordsFixture.Do(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(victim)] });
        Assert.Equal("bree", state.Units.Single(u => u.Id == nazgul).Location);
        Assert.Equal("bree", EffectRuntime.Current(state)!.Effect!.RegionId);
        Assert.Contains("buckland", state.Pending!.PrivateChoices);
    }

    [Fact]
    public void NoFreeNorthNeighborSkipsRecruitmentWithoutUsingTargetOrForeignRegion()
    {
        var state = State();
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "arnor");
        foreach (var id in BoardRules.Regions["arnor"].Neighbors.Where(id => BoardRules.Regions[id].Nation == "north"))
            BattleFixture.Put(state, "sauron", UnitKind.Regular, id);
        state = Play(state, LordsFreePeoplesCards.Rangers);
        state = LordsFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = ["arnor"] }, new SequenceRandom(0, 0, 0));
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.Events, e => e.Kind == "EventRecruited");
    }

    [Theory]
    [InlineData(UnitKind.Regular)]
    [InlineData(UnitKind.Leader)]
    public void MissingArmyUnitOrLeaderReserveExecutesOnlyAvailablePart(UnitKind missing)
    {
        var state = State();
        foreach (var unit in state.Units.Where(u => u.Nation == "north" &&
            (missing == UnitKind.Leader ? u.Kind == UnitKind.Leader : BoardRules.IsArmyUnit(u))))
            unit.Location = "casualties";
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "bree");
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "buckland");
        state = Play(state, LordsFreePeoplesCards.Rangers, 0, 0, 0);
        state = Choose(state, "buckland");
        if (missing == UnitKind.Leader)
            state = Choose(state, state.Pending!.PrivateChoices[0]);
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Events, e => e.Kind == "EventRecruited");
        Assert.Single(state.Units, u => u.Location == "buckland" && u.Nation == "north");
    }
}
