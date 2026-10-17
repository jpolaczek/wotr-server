using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class BoardTests
{
    [Fact]
    public void CoastalRegionsFollowPrintedSeaBorderRatherThanNearbyRivers()
    {
        var expected = new[] { "andrast", "anfalas", "arnor", "dol-amroth", "druwaith-iaur", "enedwaith", "evendim",
            "forlindon", "grey-havens", "harlindon", "lamedon", "minhiriath", "north-ered-luin", "pelargir", "south-ered-luin", "umbar", "west-harondor" };
        Assert.Equal(expected, BoardCatalog.LoadBase().Regions.Where(r => r.Coastal).Select(r => r.Id).Order());
    }

    [Fact]
    public void CompleteBoardMatchesIndependentInventory()
    {
        var b = BoardCatalog.LoadBase();
        Assert.Equal(105, b.Regions.Count);
        Assert.Equal(232, b.Regions.Sum(r => r.Neighbors.Count) / 2);
        Assert.Equal(16, b.Regions.Count(r => r.Settlement == "Stronghold"));
        Assert.Equal(6, b.Regions.Count(r => r.Settlement == "City"));
        var inventory = new Dictionary<string, int[]>
        {
            ["dwarves"] = [5, 5, 4, 0],
            ["elves"] = [5, 10, 4, 0],
            ["gondor"] = [15, 5, 4, 0],
            ["north"] = [10, 5, 4, 0],
            ["rohan"] = [10, 5, 4, 0],
            ["isengard"] = [12, 6, 0, 0],
            ["sauron"] = [36, 6, 0, 8],
            ["southrons"] = [24, 6, 0, 0]
        };
        foreach (var (nation, counts) in inventory)
            foreach (var kind in Enum.GetValues<UnitKind>())
                Assert.Equal(counts[(int)kind], b.Units.Count(u => u.Nation == nation && u.Kind == kind));
        Assert.Equal(35, b.Units.Where(u => u.Location != "reserve").Select(u => u.Location).Distinct().Count());
        Assert.Equal(16, b.HuntTiles.Count(t => t.Pool == "standard"));
    }
    [Theory]
    [InlineData("angmar", "north-downs", false)]
    [InlineData("northern-rhovanion", "southern-dorwinion", false)]
    [InlineData("high-pass", "old-ford", false)]
    [InlineData("high-pass", "goblins-gate", true)]
    [InlineData("moria", "dimrill-dale", true)]
    public void PrintedBordersDetermineMovement(string a, string b, bool allowed)
        => Assert.Equal(allowed, BoardCatalog.LoadBase().Regions.Single(r => r.Id == a).Neighbors.Contains(b));

    [Fact]
    public void OriginalNationIsIndependentOfStartingArmy()
    {
        var b = BoardCatalog.LoadBase();
        Assert.Null(b.Regions.Single(r => r.Id == "osgiliath").Nation);
        Assert.Equal(2, b.Units.Count(u => u.Location == "osgiliath" && u.Nation == "gondor"));
        Assert.Equal(Side.FreePeoples, b.Regions.Single(r => r.Id == "erebor").OriginalOwner);
        Assert.Equal(new[] { "gorgoroth" }, b.Regions.Single(r => r.Id == "barad-dur").Neighbors);
    }
    [Fact]
    public void CorruptDataIsRejected()
    {
        var b = BoardCatalog.LoadBase();
        b.Regions[0].Neighbors.Add("unknown");
        Assert.Throws<InvalidDataException>(() => BoardCatalog.Load(JsonSerializer.Serialize(b, BoardCatalog.Json)));
    }
    [Fact]
    public void PoliticsAndReserveTotalsMatchSetup()
    {
        var b = BoardCatalog.LoadBase();
        Assert.Equal(new[] { "elves", "isengard", "sauron", "southrons" }, b.Nations.Where(n => n.Active).Select(n => n.Id).Order());
        Assert.Equal(2, b.Nations.Single(n => n.Id == "gondor").StepsToWar);
        Assert.Equal(3, b.Nations.Single(n => n.Id == "north").StepsToWar);
        Assert.Equal(4, b.Units.Count(u => u.Nation == "sauron" && u.Kind == UnitKind.Nazgul && u.Location == "reserve"));
        Assert.DoesNotContain(b.Units, u => u.Nation == "elves" && u.Kind == UnitKind.Leader && u.Location == "reserve");
    }
}
