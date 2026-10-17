using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.FateCardsFixture;

namespace Wotr.Tests;

public class FateShadowRecruitmentTests
{
    [Theory]
    [InlineData("mount-gundabad")]
    [InlineData("erebor")]
    public void OrcsRecruitsExactlyThreeInDolGuldurAndOneChosenAlternativeBeforeWar(string region)
    {
        var state = State(FateCards.Orcs);
        state.Nations.Single(n => n.Id == "sauron").StepsToWar = 1;
        var before = state.Units.GroupBy(u => u.Location).ToDictionary(g => g.Key, g => g.Count());
        state = Play(state, FateCards.Orcs);
        Assert.Equal(new[] { "erebor", "mount-gundabad" }, Option(state).EligibleIds.Order());
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = ["erebor", "mount-gundabad"] });
        state = Choose(state, region);
        Assert.Equal(before["dol-guldur"] + 3, state.Units.Count(u => u.Location == "dol-guldur"));
        Assert.Equal(before[region] + 3, state.Units.Count(u => u.Location == region));
        var unchanged = region == "erebor" ? "mount-gundabad" : "erebor";
        Assert.Equal(before[unchanged], state.Units.Count(u => u.Location == unchanged));
        Assert.Equal(1, state.Nations.Single(n => n.Id == "sauron").StepsToWar);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void OrcsSharesAScarceReserveWithoutPrioritizingDolGuldurOrMixingBothAlternatives(int dolCount)
    {
        var state = State(FateCards.Orcs);
        Reserve(state, "sauron", UnitKind.Regular, 4);
        state = Choose(Play(state, FateCards.Orcs), "erebor");
        Assert.Equal(4, Option(state).Minimum);
        Assert.Equal(new[] { "dol-guldur", "erebor" }, Option(state).Targets.Order());
        var plan = Plan(state, ("sauron", UnitKind.Regular, "dol-guldur", dolCount),
            ("sauron", UnitKind.Regular, "erebor", 4 - dolCount));
        var illegal = Plan(state, ("sauron", UnitKind.Regular, "dol-guldur", 2),
            ("sauron", UnitKind.Regular, "erebor", 1), ("sauron", UnitKind.Regular, "mount-gundabad", 1));
        LordsFixture.Reject(state, CommandKind.ChooseEffect, illegal);
        LordsFixture.Reject(state, CommandKind.ChooseEffect, Plan(state, ("sauron", UnitKind.Regular, "erebor", 4)));
        state = KingsFixture.Do(state, CommandKind.ChooseEffect, plan);
        Assert.All(plan.Placements, p => Assert.Equal(p.RegionId, state.Units.Single(u => u.Id == p.UnitId).Location));
        Assert.DoesNotContain(state.Units, u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == "reserve");
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrcsKeepsThePossibleInstructionWhenOneOrBothAlternativeRegionsAreBlocked(bool both)
    {
        var state = State(FateCards.Orcs);
        FateFixture.Clear(state, "erebor");
        state.Control["erebor"] = Side.FreePeoples;
        if (both)
        {
            FateFixture.Clear(state, "mount-gundabad");
            state.Control["mount-gundabad"] = Side.FreePeoples;
        }
        var beforeDol = state.Units.Count(u => u.Location == "dol-guldur");
        var beforeMount = state.Units.Count(u => u.Location == "mount-gundabad");
        state = Play(state, FateCards.Orcs);
        Assert.Equal(beforeDol + 3, state.Units.Count(u => u.Location == "dol-guldur"));
        Assert.Equal(beforeMount + (both ? 0 : 3), state.Units.Count(u => u.Location == "mount-gundabad"));
        Assert.DoesNotContain(state.Units, u => u.Location == "erebor");
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData("western-mirkwood")]
    [InlineData("northern-mirkwood")]
    [InlineData("withered-heath")]
    public void MonstersEastKeepsThreeRegularLocationsAndChoosesOneOfThemForItsElite(string eliteRegion)
    {
        var state = State(FateCards.Monsters);
        foreach (var region in FateMonstersEliteStep.Regions)
            FateFixture.Clear(state, region);
        state = Choose(Play(state, FateCards.Monsters), FateCards.MonstersEast);
        Assert.Equal(FateMonstersEliteStep.Regions.Order(), Option(state).EligibleIds.Order());
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = ["trollshaws"] });
        state = Choose(state, eliteRegion);
        foreach (var region in FateMonstersEliteStep.Regions)
        {
            Assert.Single(state.Units, u => u.Location == region && u.Kind == UnitKind.Regular);
            Assert.Equal(region == eliteRegion ? 1 : 0, state.Units.Count(u => u.Location == region && u.Kind == UnitKind.Elite));
        }
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void MonstersWestUsesOnlyItsFourPrintedDestinations()
    {
        var state = State(FateCards.Monsters);
        var regions = new[] { "angmar", "ettenmoors", "weather-hills", "trollshaws" };
        foreach (var region in regions)
            FateFixture.Clear(state, region);
        state = Play(state, FateCards.Monsters);
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [FateCards.MonstersEast, FateCards.MonstersWest] });
        state = Choose(state, FateCards.MonstersWest);
        foreach (var region in regions)
            Assert.Equal(region == "trollshaws" ? UnitKind.Elite : UnitKind.Regular,
                Assert.Single(state.Units, u => u.Location == region).Kind);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(FateCards.MonstersWest, "angmar")]
    [InlineData(FateCards.MonstersEast, "withered-heath")]
    public void MonstersPreservesTheBranchAndAllocationChoiceWithLimitedReserve(string branch, string destination)
    {
        var state = State(FateCards.Monsters);
        Reserve(state, "sauron", UnitKind.Regular, 1);
        Reserve(state, "sauron", UnitKind.Elite, 0);
        state = Play(state, FateCards.Monsters);
        Assert.Equal(new[] { FateCards.MonstersWest, FateCards.MonstersEast }, Option(state).EligibleIds);
        state = Choose(state, branch);
        Assert.Equal(1, Option(state).Minimum);
        var forbidden = branch == FateCards.MonstersWest ? "withered-heath" : "angmar";
        LordsFixture.Reject(state, CommandKind.ChooseEffect, Plan(state, ("sauron", UnitKind.Regular, forbidden, 1)));
        var plan = Plan(state, ("sauron", UnitKind.Regular, destination, 1));
        state = KingsFixture.Do(state, CommandKind.ChooseEffect, plan);
        Assert.Equal(destination, state.Units.Single(u => u.Id == plan.Placements[0].UnitId).Location);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void MonstersBlockedEasternRegionDoesNotPreventRecruitmentElsewhere()
    {
        var state = State(FateCards.Monsters);
        BattleFixture.Put(state, "north", UnitKind.Regular, "western-mirkwood");
        state = Choose(Play(state, FateCards.Monsters), FateCards.MonstersEast);
        Assert.DoesNotContain("western-mirkwood", Option(state).EligibleIds);
        state = Choose(state, "withered-heath");
        Assert.DoesNotContain(state.Units, u => u.Nation == "sauron" && u.Location == "western-mirkwood");
        Assert.Single(state.Units, u => u.Nation == "sauron" && u.Location == "northern-mirkwood");
        Assert.Equal(2, state.Units.Count(u => u.Nation == "sauron" && u.Location == "withered-heath"));
    }
}
