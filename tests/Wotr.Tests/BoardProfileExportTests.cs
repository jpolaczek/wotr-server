using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public sealed class BoardProfileExportTests
{
    [Fact]
    public void TenWorkloadsUseNormalSetupAndActualBattleAndHuntDecisions()
    {
        var inputs = Inputs(new CountingRandom());
        var views = BoardProfileScenario.ProfileViews(inputs);
        Assert.Equal(new[] { "characters-fp", "characters-sh", "homelands-fp", "homelands-sh", "menu-battle",
            "menu-hunt", "setup-council-fp", "setup-fate-fp", "setup-lords-fp", "setup-waiting-sh" },
            views.Keys.OrderBy(key => key).ToArray());
        foreach (var key in new[] { "setup-lords-fp", "setup-council-fp", "setup-fate-fp" })
        {
            var view = views[key];
            Assert.Equal(DecisionKind.CouncilVersions, view.Decision!.Kind);
            Assert.Equal(Side.FreePeoples, view.DecisionOwner);
            var option = Assert.Single(view.Options, option => option.Kind == CommandKind.ChooseCompanionVersions);
            Assert.Equal(key == "setup-lords-fp" ? 1 : 7, option.CompanionVersions.Count);
            Assert.Equal(0, view.Turn);
            Assert.Empty(view.Hand);
            if (key == "setup-fate-fp")
            {
                Assert.True(view.Configuration.NewCities);
                var gimli = Assert.Single(option.CompanionVersions, group => group.FigureId == "base.character.gimli");
                Assert.Equal(FateGimli.Standard, gimli.BaseId);
                Assert.Equal(FateGimli.Alternate, gimli.AlternateId);
            }
        }
        Assert.Equal(DecisionKind.CouncilHomelands, views["homelands-fp"].Decision!.Kind);
        Assert.Equal(5, Assert.Single(views["homelands-fp"].Options,
            option => option.Kind == CommandKind.ChooseCouncilHomelands).CharacterActions.Count);
        Assert.NotNull(views["menu-battle"].Battle);
        Assert.Equal(DecisionKind.Losses, views["menu-battle"].Decision!.Kind);
        Assert.Equal(Side.FreePeoples, views["menu-battle"].DecisionOwner);
        Assert.NotNull(views["menu-hunt"].Hunt);
        Assert.Equal(DecisionKind.HuntReroll, views["menu-hunt"].Decision!.Kind);
        Assert.Equal(Side.Shadow, views["menu-hunt"].DecisionOwner);
        foreach (var key in new[] { "characters-fp", "characters-sh" })
        {
            var view = views[key];
            Assert.True(view.OwnCharacterRoster.Count >= (view.You == Side.FreePeoples ? 13 : 7));
            Assert.All(view.OwnCharacterRoster, row => Assert.Equal(view.You, row.Side));
            Assert.Equal(view.OwnCharacterRoster.Count, view.OwnCharacterRoster.Select(row => row.FigureId).Distinct().Count());
        }
    }

    [Fact]
    public void ExportRoundTripsWithoutMutatingStatesRandomnessOrLeakingPrivateSetup()
    {
        var random = new CountingRandom();
        var inputs = Inputs(random);
        var before = inputs.ToDictionary(pair => pair.Key, pair => StateCodec.Write(pair.Value));
        var calls = random.Calls;
        var json = JsonSerializer.Serialize(BoardProfileScenario.ProfileViews(inputs));
        var restored = JsonSerializer.Deserialize<Dictionary<string, PlayerView>>(json)!;
        Assert.Equal(10, restored.Count);
        Assert.Equal(calls, random.Calls);
        foreach (var pair in inputs)
            Assert.Equal(before[pair.Key], StateCodec.Write(pair.Value));
        foreach (var key in new[] { "setup-waiting-sh", "homelands-sh" })
        {
            var view = restored[key];
            Assert.Equal(Side.Shadow, view.You);
            Assert.Null(view.Decision);
            Assert.Empty(view.Options);
            Assert.Empty(view.OwnCompanions);
            Assert.Empty(view.OwnCompanionVersions);
            Assert.DoesNotContain(view.OwnCharacterRoster, row => row.Side == Side.FreePeoples);
        }
        using var document = JsonDocument.Parse(json);
        foreach (var item in document.RootElement.EnumerateObject())
        {
            Assert.False(item.Value.TryGetProperty("Lords", out _));
            Assert.False(item.Value.TryGetProperty("Resolution", out _));
            Assert.False(item.Value.TryGetProperty("RandomSamples", out _));
            foreach (var player in item.Value.GetProperty("Players").EnumerateArray())
            {
                Assert.False(player.TryGetProperty("Hand", out _));
                Assert.False(player.TryGetProperty("Decks", out _));
                Assert.False(player.TryGetProperty("PrivateCompanionVersions", out _));
            }
        }
        var path = Environment.GetEnvironmentVariable("WOTR_UI72_PROFILE_EXPORT");
        if (!string.IsNullOrEmpty(path))
            File.WriteAllText(path, json);
    }

    [Fact]
    public void PreparationRejectsAPreAttackPositionInsteadOfCallingItABattle()
    {
        var random = new CountingRandom();
        var inputs = Inputs(random);
        var field = BattleScenario.Field(random);
        Assert.Throws<InvalidOperationException>(() => BoardProfileScenario.Inputs(random,
            inputs["characters-fp"], inputs["homelands-fp"], field, inputs["menu-hunt"]));
    }

    private static Dictionary<string, MatchState> Inputs(IRandomSource random) => BoardProfileScenario.Inputs(random,
        CharacterRosterScenario.Create(random)["ui66-full"], CouncilHomelandScenario.Create(random)["ui69-layout"],
        BattleScenario.Create(random)["no-regular"], FellowshipScenario.Hunt(random));

    private sealed class CountingRandom : IRandomSource
    {
        public int Calls { get; private set; }
        public int Next(int upperBound)
        {
            Calls++;
            return 0;
        }
    }
}
