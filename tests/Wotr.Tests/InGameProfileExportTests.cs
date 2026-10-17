using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public sealed class InGameProfileExportTests
{
    [Fact]
    public void EightPresentationWorkloadsPreserveCanonicalSourcesBothRolesAndPhysicalResources()
    {
        var state = InGameProfileScenario.Dense(new ZeroRandom());
        var views = InGameProfileScenario.ProfileViews(state);
        Assert.Equal(new[] { "dense-fp", "dense-sh", "effects", "help", "resources-fp", "resources-sh", "sovereigns", "victory" },
            views.Keys.OrderBy(id => id).ToArray());
        foreach (var pair in views)
        {
            var expected = pair.Key is "help" or "resources-sh" or "dense-sh" ? Side.Shadow : Side.FreePeoples;
            var view = pair.Value;
            Assert.Equal(expected, view.You);
            Assert.Equal(state.Id, view.MatchId);
            Assert.Equal(5, view.Sovereigns.Count);
            Assert.All(view.Sovereigns, sovereign => Assert.Equal(SovereignStatus.Awakened, sovereign.Status));
            Assert.Equal(5, view.RulerTokens.Count);
            Assert.All(view.RulerTokens, token => Assert.Equal(pair.Key == "resources-sh" ? "dismay" : "free-peoples", token.Location));
            Assert.Equal(2, view.CouncilTokens.Count);
            Assert.Equal(state.Dice.Count, view.Dice.Count);
            Assert.Equal(state.Rings.Count, view.Rings.Count);
            Assert.Equal(6, view.Effects.Select(effect => effect.SourceId).Distinct().Count());
            Assert.All(view.Effects, effect => Assert.Equal(CardCatalog.All[effect.SourceId].Title, effect.Title));
            Assert.Equal(6, view.Effects.SelectMany(effect => effect.Targets).Count(target => target.Kind == "Region"));
            Assert.Contains(view.Effects, effect => effect.Id == "table:" + FreePeoplesCharacterCards01.Mithril);
            Assert.Equal(state.Player(expected).Hand, view.Hand);
            Assert.All(view.OwnCharacterRoster, row => Assert.Equal(expected, row.Side));
        }
    }

    [Fact]
    public void ExportIsPureRoundTripsAndNeverIncludesOpponentsPrivateCompanionVersionsOrDecks()
    {
        var state = InGameProfileScenario.Dense(new ZeroRandom());
        var before = JsonSerializer.Serialize(state);
        var exported = InGameProfileScenario.ProfileViews(state);
        Assert.Equal(before, JsonSerializer.Serialize(state));
        var json = JsonSerializer.Serialize(exported);
        var restored = JsonSerializer.Deserialize<Dictionary<string, PlayerView>>(json)!;
        Assert.Equal(8, restored.Count);
        foreach (var view in restored.Values.Where(view => view.You == Side.Shadow))
        {
            Assert.Empty(view.OwnCompanionVersions);
            Assert.Empty(view.OwnCompanions);
            Assert.DoesNotContain(view.OwnCharacterRoster, row => row.Side == Side.FreePeoples);
        }
        using var document = JsonDocument.Parse(json);
        foreach (var view in document.RootElement.EnumerateObject())
        {
            Assert.False(view.Value.TryGetProperty("Lords", out _));
            Assert.False(view.Value.TryGetProperty("Random", out _));
            foreach (var player in view.Value.GetProperty("Players").EnumerateArray())
            {
                Assert.False(player.TryGetProperty("Hand", out _));
                Assert.False(player.TryGetProperty("Decks", out _));
                Assert.False(player.TryGetProperty("PrivateCompanionVersions", out _));
            }
        }
        var path = Environment.GetEnvironmentVariable("WOTR_UI63_PROFILE_EXPORT");
        if (!string.IsNullOrEmpty(path))
            File.WriteAllText(path, json);
    }

    [Fact]
    public void VictoryWorkloadUsesRealCapturedCitiesAndStrongholdsAndDoesNotDeclareAnEarlyWinner()
    {
        var state = InGameProfileScenario.Dense(new ZeroRandom());
        var before = StateCodec.Write(state);
        var views = InGameProfileScenario.ProfileViews(state);
        Assert.Equal(before, StateCodec.Write(state));
        var victory = views["victory"];
        Assert.Null(victory.Winner);
        Assert.Null(victory.Victory);
        foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
        {
            var score = Assert.Single(victory.MilitaryVictory, score => score.Side == side);
            Assert.Equal(6, score.Points);
            Assert.Equal(side == Side.FreePeoples, score.ThresholdReached);
            var expected = side == Side.FreePeoples ? new[] { "angmar", "far-harad", "mount-gundabad", "umbar" }
                : new[] { "dol-amroth", "edoras", "helms-deep", "pelargir" };
            Assert.Equal(expected, score.Regions.Select(region => region.RegionId).OrderBy(id => id).ToArray());
            Assert.All(score.Regions, region =>
            {
                Assert.Equal(side, victory.Control[region.RegionId]);
                Assert.Equal(BoardRules.Regions[region.RegionId].VictoryPoints, region.Points);
            });
        }
        Assert.All(views["dense-fp"].MilitaryVictory, score => Assert.Empty(score.Regions));
    }

    private sealed class ZeroRandom : IRandomSource
    {
        public int Next(int upperBound) => 0;
    }
}
