using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class FateSetupTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CompleteRegionDiffMatchesPrintedRulesAndPreservesTheGraph(bool fate, bool cities)
    {
        var config = new MatchConfiguration { Fate = fate, NewCities = cities };
        var changes = new Dictionary<string, (string? Nation, Side? Owner, string Settlement, int Points)>();
        if (fate)
        {
            changes.Add("erebor", ("sauron", Side.Shadow, "Stronghold", 2));
            changes.Add("dale", (null, null, "Fortification", 0));
            changes.Add("iron-hills", ("dwarves", Side.FreePeoples, "City", 1));
        }
        if (cities)
        {
            changes.Add("ered-luin", ("dwarves", Side.FreePeoples, "City", 1));
            changes.Add("south-rhun", ("southrons", Side.Shadow, "City", 1));
        }
        var state = MatchFactory.Create("fate-test", new SequenceRandom(new int[92]), config);
        foreach (var original in BoardCatalog.LoadBase().Regions)
        {
            var actual = BoardRules.Region(state, original.Id);
            var expected = changes.GetValueOrDefault(original.Id,
                (original.Nation, original.OriginalOwner, original.Settlement!, original.VictoryPoints));
            Assert.Equal(expected, (actual.Nation, actual.OriginalOwner, actual.Settlement!, actual.VictoryPoints));
            Assert.Equal(original.Name, actual.Name);
            Assert.Equal(original.Coastal, actual.Coastal);
            Assert.Equal(original.Neighbors, actual.Neighbors);
            Assert.Equal(expected.Item2, state.Control[actual.Id]);
        }
        Assert.Equal(changes.Keys.Order(), BoardProjection.Overrides(state).Select(r => r.Id).Order());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryPhysicalFigureMatchesFinalSetupTotalsAndRemovedFiguresReturnToReserve(bool cities)
    {
        var original = BoardCatalog.LoadBase();
        var state = FateFixture.Setup(cities);
        var expected = original.Units.Where(u => u.Location != "reserve").GroupBy(u => (u.Location, u.Nation, u.Kind))
            .ToDictionary(g => g.Key, g => g.Count());
        var totals = new (string Region, string Nation, int Regular, int Elite, int Leader)[]
        {
            ("erebor", "dwarves", 0, 0, 0), ("erebor", "sauron", 2, 0, 0),
            ("dale", "north", 0, 0, 0), ("carrock", "north", 2, 0, 1),
            ("ered-luin", "dwarves", 2, 0, 0), ("iron-hills", "dwarves", 2, 0, 1)
        };
        foreach (var total in totals)
        {
            var counts = new[] { total.Regular, total.Elite, total.Leader, 0 };
            foreach (var kind in Enum.GetValues<UnitKind>())
            {
                var key = (total.Region, total.Nation, kind);
                expected.Remove(key);
                if (counts[(int)kind] > 0)
                    expected[key] = counts[(int)kind];
            }
        }
        var actual = state.Units.Where(u => u.Location != "reserve").GroupBy(u => (u.Location, u.Nation, u.Kind))
            .ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(expected.OrderBy(p => p.Key), actual.OrderBy(p => p.Key));
        Assert.Equal(original.Units.Select(u => (u.Id, u.Nation, u.Kind)), state.Units.Select(u => (u.Id, u.Nation, u.Kind)));
        Assert.All(state.Units, u => Assert.False(u.Garrison));
        Assert.DoesNotContain(state.Units, u => u.Location is "casualties" or "removed");
        foreach (var nation in original.Nations)
        {
            var changed = state.Nations.Single(n => n.Id == nation.Id);
            Assert.Equal(nation.StepsToWar, changed.StepsToWar);
            Assert.Equal(nation.Id == "dwarves" || nation.Active, changed.Active);
        }
    }

    [Fact]
    public void BaseSetupRemainsByteIdenticalAndNewCitiesWithoutFateFailsBeforeRandomness()
    {
        var original = MatchFactory.Create("base", new SequenceRandom(new int[92]));
        var explicitBase = MatchFactory.Create("base", new SequenceRandom(new int[92]), new() { Fate = false, NewCities = false });
        Assert.Equal(StateCodec.Write(original), StateCodec.Write(explicitBase));
        var random = new SequenceRandom();
        Assert.False(ConfigurationRules.IsValid(new() { NewCities = true }));
        Assert.Throws<ArgumentException>(() => MatchFactory.Create("bad", random, new() { NewCities = true }));
        Assert.Equal(0, random.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetupAndBothPublicViewsAreDeterministicDetachedAndStableAcrossReload(bool cities)
    {
        var state = FateFixture.Setup(cities);
        Assert.Equal(StateCodec.Write(state), StateCodec.Write(FateFixture.Setup(cities)));
        var restored = StateCodec.Copy(state);
        foreach (var side in Enum.GetValues<Side>())
        {
            var view = LordsFixture.Views().For(state, side);
            Assert.Equal(JsonSerializer.Serialize(view), JsonSerializer.Serialize(LordsFixture.Views().For(restored, side)));
            Assert.Equal(cities ? 5 : 3, view.RegionOverrides.Count);
            Assert.All(view.Players, p => Assert.Equal(0, p.VictoryPoints));
            var erebor = view.RegionOverrides.Single(r => r.Id == "erebor");
            erebor.Neighbors.Clear();
            erebor.Nation = "north";
            Assert.Equal("sauron", BoardRules.Region(state, "erebor").Nation);
            Assert.Equal(3, BoardRules.Region(restored, "erebor").Neighbors.Count);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void KingsSetupExplicitlyOmitsBothAbsentSovereignsWithoutChangingOrdinaryFateFigures(bool cities)
    {
        var state = FateFixture.Setup(cities, kings: true);
        var ordinary = state.Units.Select(u => (u.Id, u.Location)).ToArray();
        KingsSetup.Initialize(state);
        Assert.Equal(3, state.Kings!.Sovereigns.Count);
        Assert.DoesNotContain(state.Kings.Sovereigns, s => s.Id is SovereignCatalog.Dain or SovereignCatalog.Brand);
        Assert.DoesNotContain(state.Units, u => u.Id is SovereignCatalog.Dain or SovereignCatalog.Brand);
        Assert.Equal(ordinary, state.Units.Where(u => u.Id.StartsWith("base.", StringComparison.Ordinal)).Select(u => (u.Id, u.Location)));
    }
}
