using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.KingsFreePeoplesCards01;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsRecruitCardsTests
{
    [Theory]
    [InlineData("edoras", UnitKind.Regular)]
    [InlineData("eastemnet", UnitKind.Elite)]
    public void RidersRecruitsBothFiguresTogetherInOneEligibleRohanRegion(string region, UnitKind kind)
    {
        var state = State();
        Companion(state, Gimli, "eastemnet");
        Companion(state, Boromir, "pelargir");
        state = Choose(Play(state, Riders), "recruit");
        Assert.Equal("RecruitmentPlan", state.Pending!.Presentation);
        var option = new EffectOptions().GetOptions(state, Side.FreePeoples).Single();
        Assert.Equal(new[] { "eastemnet", "edoras" }, option.Targets.Order());
        Assert.Equal(1, option.EventRecruitment!.MaximumRegions);
        var unit = state.Units.First(u => u.Nation == "rohan" && u.Kind == kind && u.Location == "reserve").Id;
        var leader = state.Units.First(u => u.Nation == "rohan" && u.Kind == UnitKind.Leader && u.Location == "reserve").Id;
        var payload = new CommandPayload { Placements = [new() { UnitId = unit, RegionId = region }, new() { UnitId = leader, RegionId = region }] };
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Placements = [payload.Placements[0]] });
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Placements =
            [new() { UnitId = unit, RegionId = "eastemnet" }, new() { UnitId = leader, RegionId = "edoras" }] });
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.FreePeoples, BattleFixture.Command(state, CommandKind.ChooseEffect, payload));
        Assert.True(preview.Legal);
        Assert.Equal(2, preview.Placements.Count);
        state = Do(state, CommandKind.ChooseEffect, payload);
        Assert.Equal(region, state.Units.Single(u => u.Id == unit).Location);
        Assert.Equal(region, state.Units.Single(u => u.Id == leader).Location);
        Assert.Single(state.Player(Side.FreePeoples).Hand);
        Assert.False(state.Nations.Single(n => n.Id == "rohan").Active);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void RidersCanRecruitIntoBesiegedHelmsDeepWithACompanion()
    {
        var state = State();
        Companion(state, Gimli, "helms-deep").Garrison = true;
        BattleFixture.Put(state, "rohan", UnitKind.Regular, "helms-deep").Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "helms-deep");
        state.Control["edoras"] = Side.Shadow;
        state = Choose(Play(state, Riders), "recruit");
        var option = new EffectOptions().GetOptions(state, Side.FreePeoples).Single();
        Assert.Equal(new[] { "helms-deep" }, option.Targets);
        var ids = option.EligibleIds.Where(id => state.Units.Single(u => u.Id == id).Kind != UnitKind.Elite).ToArray();
        state = Do(state, CommandKind.ChooseEffect, new() { Placements = ids.Select(id => new UnitPlacementChoice { UnitId = id, RegionId = "helms-deep" }).ToList() });
        Assert.All(ids, id => Assert.True(state.Units.Single(u => u.Id == id).Garrison));
        Assert.Single(state.Player(Side.FreePeoples).Hand);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void BrandRecruitsOnlyAvailableRegularsThenDraws(int available)
    {
        var state = State();
        foreach (var unit in state.Units.Where(u => u.Nation == "north" && u.Kind == UnitKind.Regular).Skip(available))
            unit.Location = "casualties";
        state = Choose(Play(state, Brand), "recruit");
        Assert.Equal(available, state.Units.Count(u => u.Location == "dale" && u.Kind == UnitKind.Regular));
        Assert.Single(state.Player(Side.FreePeoples).Hand);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(Stewards, "pelargir", 2)]
    [InlineData(Riders, "edoras", 1)]
    [InlineData(Brand, "dale", 1)]
    public void CapturedDestinationDoesNotRecruitOrSilentlySwitchToCorruption(string card, string region, int draw)
    {
        var state = State();
        Companion(state, Boromir, "pelargir");
        state.Control[region] = Side.Shadow;
        Tile(state, SovereignCatalog.Denethor);
        state = Choose(Play(state, card), "recruit");
        Assert.DoesNotContain(state.Events, e => e.Kind == "EventRecruited");
        Assert.Equal(draw, state.Player(Side.FreePeoples).Hand.Count);
        Assert.Single(Sovereign(state, SovereignCatalog.Denethor).Tiles);
        Assert.Empty(state.RandomSamples);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void RidersWithoutArmyReservesStillRecruitsTheLeaderWithAnExistingArmy()
    {
        var state = State();
        foreach (var unit in state.Units.Where(u => u.Nation == "rohan" && BoardRules.IsArmyUnit(u)))
            unit.Location = "casualties";
        BattleFixture.Put(state, "elves", UnitKind.Regular, "edoras");
        state = Choose(Play(state, Riders), "recruit");
        Assert.Single(state.Events, e => e.Kind == "EventRecruited");
        Assert.Equal(2, state.Units.Count(u => u.Location == "edoras" && u.Kind == UnitKind.Leader));
        Assert.Single(state.Player(Side.FreePeoples).Hand);
    }

    // UI-278 (EXP-15 D15): the client lights the branch's object — the nation's Reserve figures, the sovereign.
    [Theory]
    [InlineData(Stewards, SovereignCatalog.Denethor, "gondor")]
    [InlineData(Riders, SovereignCatalog.Theoden, "rohan")]
    [InlineData(Brand, SovereignCatalog.Brand, "north")]
    [InlineData(KingsCards02.Dain, SovereignCatalog.Dain, "dwarves")]
    [InlineData(KingsCards02.Thranduil, SovereignCatalog.Thranduil, "elves")]
    public void EachBranchNamesItsObject(string card, string sovereign, string nation)
    {
        var state = State();
        Companion(state, Boromir, "pelargir");
        state = Play(state, card);
        Assert.Equal("SovereignCardBranch", state.Pending!.Presentation);
        var choices = new EffectOptions().GetOptions(state, Side.FreePeoples).Single().EffectChoices.ToDictionary(c => c.Id);
        Assert.Equal(nation, choices["recruit"].TargetId);
        Assert.Equal(sovereign, choices["corruption"].TargetId);
    }

    [Theory]
    [InlineData(Stewards, SovereignCatalog.Denethor, 2)]
    [InlineData(Riders, SovereignCatalog.Theoden, 1)]
    [InlineData(Brand, SovereignCatalog.Brand, 1)]
    public void WelcomeBlocksRandomRemovalButNotTheFollowingStrategyDraw(string card, string sovereign, int draw)
    {
        var state = State();
        Companion(state, Boromir, "pelargir");
        Tile(state, sovereign);
        state.Player(Side.Shadow).Table.Add(KingsCardRestrictions.Welcome);
        state = Choose(Play(state, card), "corruption");
        Assert.Single(Sovereign(state, sovereign).Tiles);
        Assert.Empty(state.RandomSamples);
        Assert.Equal(draw, state.Player(Side.FreePeoples).Hand.Count);
    }
}
