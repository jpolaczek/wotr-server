using System.Reflection;
using System.Text.Json;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class FateGimliManifestTests
{
    [Fact]
    public void BothFrontsMatchRuntimeStatsAbilitiesSourceClausesAndActiveTests()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Wotr.slnx")))
            root = root.Parent;
        Assert.NotNull(root);
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "content/fate-character-implementation-manifest.json")));
        using var source = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "docs/sources/fate-index.json")));
        var reviewed = source.RootElement.GetProperty("characters").EnumerateArray().ToDictionary(c => c.GetProperty("id").GetString()!);
        var cards = manifest.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(2, cards.Length);
        Assert.Equal(CharacterCatalog.Fate.Keys.Order(), reviewed.Keys.Order());
        Assert.Equal(reviewed.Keys.Order(), cards.Select(c => c.GetProperty("id").GetString()).Order());
        foreach (var card in cards)
            Check(card, reviewed[card.GetProperty("id").GetString()!]);
        var reaction = EffectCatalog.All.Reactions[FateGimli.GuideReaction];
        Assert.Equal(FateGimli.Alternate, reaction.RequiredGuideId);
        Assert.Equal("council.gimli-guide", reaction.ProgramId);
    }

    private static void Check(JsonElement card, JsonElement source)
    {
        var definition = CharacterCatalog.Fate[card.GetProperty("id").GetString()!];
        Assert.Equal(source.GetProperty("identity").GetString(), definition.Identity);
        Assert.Equal(source.GetProperty("level").GetInt32(), definition.Level);
        Assert.Equal(source.GetProperty("leadership").GetInt32(), definition.Leadership);
        Assert.Equal(source.GetProperty("nationIcon").GetString(), definition.NationIcon);
        Assert.Equal(0, definition.ExtraActionDice);
        Assert.Equal("Companion", definition.Role);
        Assert.Equal(source.GetProperty("setup").GetProperty("replaces").GetString(), card.GetProperty("setup").GetProperty("replaces").GetString());
        Assert.Equal(definition.Abilities, card.GetProperty("abilities").EnumerateArray().Select(a => a.GetProperty("id").GetString()));
        foreach (var (section, sourceSection) in new[] { ("sourceAbilities", "abilities"), ("verificationCases", "verificationCases") })
            Assert.Equal(source.GetProperty(sourceSection).EnumerateArray().Select(a => a.GetString()),
                card.GetProperty(section).EnumerateArray().Select(a => a.GetProperty("sourceText").GetString()));
        foreach (var section in new[] { "abilities", "sourceAbilities", "verificationCases" })
            foreach (var item in card.GetProperty(section).EnumerateArray())
                RequireTests(item.GetProperty("tests"));
        foreach (var section in new[] { "entryTests", "exitTests", "tests" })
            RequireTests(card.GetProperty(section));
    }

    private static void RequireTests(JsonElement tests)
    {
        Assert.NotEqual(0, tests.GetArrayLength());
        foreach (var test in tests.EnumerateArray())
        {
            var parts = test.GetString()!.Split('.');
            var type = typeof(FateGimliManifestTests).Assembly.GetType("Wotr.Tests." + parts[0]);
            Assert.NotNull(type);
            var method = type.GetMethod(parts[1], BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);
            var fact = method.GetCustomAttribute<FactAttribute>();
            Assert.NotNull(fact);
            Assert.Null(fact.Skip);
        }
    }
}
