using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.KingsCards02;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsServiceTests
{
    [Theory]
    [InlineData(UnitKind.Regular, false)]
    [InlineData(UnitKind.Elite, false)]
    [InlineData(UnitKind.Regular, true)]
    public void ServiceRecruitsAtSelectedRulerThenRandomlyCleansesUnlessWelcome(UnitKind kind, bool welcome)
    {
        var state = State();
        Tile(state, SovereignCatalog.Dain);
        Tile(state, SovereignCatalog.Dain, KingsTiles.One);
        if (welcome)
            state.Player(Side.Shadow).Table.Add(Welcome);
        state.Units.Single(u => u.Id == SovereignCatalog.Dain).Location = "dale";
        state = Choose(Play(state, Service), SovereignCatalog.Dain);
        var option = new EffectOptions().GetOptions(state, Side.FreePeoples).Single();
        Assert.Equal(new[] { "dale" }, option.Targets);
        var id = option.Recruitment.Single(g => g.Kind == kind).ReserveIds[0];
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Placements = [new() { UnitId = id, RegionId = "erebor" }] });
        state = Do(state, CommandKind.ChooseEffect, new() { Placements = [new() { UnitId = id, RegionId = "dale" }] }, 0);
        Assert.Equal("dale", state.Units.Single(u => u.Id == id).Location);
        Assert.Equal(welcome ? 2 : 1, Sovereign(state, SovereignCatalog.Dain).Tiles.Count);
        Assert.Contains(KingsTiles.One, Sovereign(state, SovereignCatalog.Dain).Tiles);
        Assert.Empty(state.Player(Side.FreePeoples).Hand);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void ServiceStillCleansesWithoutArmyReserves()
    {
        var state = State();
        Tile(state, SovereignCatalog.Dain);
        foreach (var unit in state.Units.Where(u => u.Nation == "dwarves" && BoardRules.IsArmyUnit(u)))
            unit.Location = "casualties";
        state = Choose(Play(state, Service), SovereignCatalog.Dain, 0);
        Assert.Empty(Sovereign(state, SovereignCatalog.Dain).Tiles);
        Assert.DoesNotContain(state.Events, e => e.Kind == "EventRecruited");
    }

    [Fact]
    public void ServiceAllowsAwakenedRulerAndRejectsCorruptedOrRemovedTargets()
    {
        var state = State();
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Dain), "dale");
        Tile(state, SovereignCatalog.Denethor, HuntTests.Three);
        SovereignLifecycle.Remove(state, Sovereign(state, SovereignCatalog.Thranduil));
        state = Play(state, Service);
        Assert.Contains(SovereignCatalog.Dain, state.Pending!.PrivateChoices);
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Denethor] });
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Thranduil] });
        state = Choose(state, SovereignCatalog.Dain);
        Assert.Equal("dale", new EffectOptions().GetOptions(state, Side.FreePeoples).Single().Targets.Single());
    }

    [Fact]
    public void ServiceWithNoSovereignsCompletesWithoutAnotherChoice()
    {
        var state = State();
        foreach (var sovereign in state.Kings!.Sovereigns)
            SovereignLifecycle.Remove(state, sovereign);
        state = Play(state, Service);
        Assert.Empty(state.Resolution.Frames);
        Assert.Empty(state.RandomSamples);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }
}
