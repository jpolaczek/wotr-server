using System.Reflection;
using System.Text.Json;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class FateCardManifestTests
{
    [Fact]
    public void AllSixFateCardsHaveBothRuntimeProgramsExactSourceClausesAndActiveBehaviorTests()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Wotr.slnx")))
            root = root.Parent;
        Assert.NotNull(root);
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "content/fate-card-implementation-manifest.json")));
        using var source = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "docs/sources/fate-index.json")));
        var sources = source.RootElement.GetProperty("cards").EnumerateArray().ToDictionary(c => c.GetProperty("id").GetString()!);
        var cards = manifest.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(6, cards.Length);
        Assert.Equal(CardCatalog.Fate.Keys.Order(), sources.Keys.Order());
        Assert.Equal(sources.Keys.Order(), cards.Select(c => c.GetProperty("id").GetString()).Order());
        Assert.Equal(sources.Keys.Order(), EffectCatalog.All.Events.Keys.Where(id => id.StartsWith("fate.")).Order());
        foreach (var card in cards)
            Check(card, sources[card.GetProperty("id").GetString()!]);
    }

    private static void Check(JsonElement card, JsonElement source)
    {
        var id = card.GetProperty("id").GetString()!;
        Assert.True(card.GetProperty("eventImplemented").GetBoolean());
        Assert.True(card.GetProperty("combatImplemented").GetBoolean());
        Assert.Equal(CardCatalog.Fate[id].Title, source.GetProperty("title").GetString());
        Assert.Equal(CardCatalog.Fate[id].Replaces, source.GetProperty("setup").GetProperty("replaces").GetString());
        Assert.Equal(CardCatalog.Fate[id].Replaces, card.GetProperty("setup").GetProperty("replaces").GetString());
        Assert.Equal(EffectCatalog.All.Events[id].ProgramId, card.GetProperty("programId").GetString());
        Assert.Equal(EffectCatalog.All.Combats[id].Effects.Select(e => e.ProgramId),
            card.GetProperty("combatPrograms").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(EffectCatalog.All.Combats[id].Effects.Select(e => e.Initiative),
            source.GetProperty("initiative").EnumerateArray().Select(e => e.GetInt32()));
        foreach (var section in new[] { "eventRules", "combatRules", "verificationCases" })
        {
            var mappings = card.GetProperty(section).EnumerateArray().ToArray();
            Assert.Equal(source.GetProperty(section).EnumerateArray().Select(e => e.GetString()),
                mappings.Select(e => e.GetProperty("sourceText").GetString()));
            foreach (var mapping in mappings)
                RequireTests(mapping.GetProperty("tests"));
        }
        RequireTests(card.GetProperty("tests"));
    }

    private static void RequireTests(JsonElement tests)
    {
        Assert.NotEqual(0, tests.GetArrayLength());
        foreach (var test in tests.EnumerateArray())
        {
            var parts = test.GetString()!.Split('.');
            var type = typeof(FateCardManifestTests).Assembly.GetType("Wotr.Tests." + parts[0]);
            Assert.NotNull(type);
            var method = type.GetMethod(parts[1], BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);
            var attribute = method.GetCustomAttribute<FactAttribute>();
            Assert.NotNull(attribute);
            Assert.Null(attribute.Skip);
        }
    }
}
