using System.Reflection;
using System.Text.Json;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class LordsCharacterManifestTests
{
    [Fact]
    public void AllEightCardsHaveEntryExitAbilitiesAndEveryReviewedSourceCaseMappedToBehaviorTests() =>
        CheckManifest("lords-character-implementation-manifest.json", null, 8);

    [Fact]
    public void AllSixCouncilCardsHaveAbilitiesAndEveryReviewedSourceCaseMappedToBehaviorTests() =>
        CheckManifest("council-character-implementation-manifest.json", "Council", 6);

    private static void CheckManifest(string file, string? variant, int expectedCount)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Wotr.slnx")))
            root = root.Parent;
        Assert.NotNull(root);
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "content", file)));
        using var sources = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, manifest.RootElement.GetProperty("source").GetString()!)));
        var cards = manifest.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        var reviewed = sources.RootElement.GetProperty("cards").EnumerateArray()
            .Where(c => c.GetProperty("requiredVariant").GetString() == variant)
            .ToDictionary(c => c.GetProperty("id").GetString()!);
        Assert.Equal(expectedCount, cards.Length);
        Assert.Equal(reviewed.Keys.Order(), cards.Select(c => c.GetProperty("id").GetString()).Order());
        foreach (var card in cards)
        {
            var id = card.GetProperty("id").GetString()!;
            Assert.True(reviewed[id].GetProperty("implemented").GetBoolean());
            RequireTests(card.GetProperty("entryTests"));
            RequireTests(card.GetProperty("exitTests"));
            var abilities = card.GetProperty("abilities").EnumerateArray().ToArray();
            Assert.Equal(CharacterCatalog.Lords[id].Abilities.Order(), abilities.Select(a => a.GetProperty("id").GetString()).Order());
            foreach (var ability in abilities)
                RequireTests(ability.GetProperty("tests"));
            RequireSourceCases(card.GetProperty("sourceAbilities"), reviewed[id].GetProperty("abilities"));
            RequireSourceCases(card.GetProperty("verificationCases"), reviewed[id].GetProperty("verificationCases"));
        }
        RequireTests(manifest.RootElement.GetProperty("integrationTests"));
    }

    private static void RequireSourceCases(JsonElement mappings, JsonElement source)
    {
        var cases = mappings.EnumerateArray().ToArray();
        Assert.Equal(source.EnumerateArray().Select(c => c.GetString()), cases.Select(c => c.GetProperty("sourceText").GetString()));
        foreach (var item in cases)
            RequireTests(item.GetProperty("tests"));
    }

    private static void RequireTests(JsonElement tests)
    {
        Assert.NotEqual(0, tests.GetArrayLength());
        foreach (var test in tests.EnumerateArray())
        {
            var parts = test.GetString()!.Split('.');
            Assert.Equal(2, parts.Length);
            var type = typeof(LordsCharacterManifestTests).Assembly.GetType("Wotr.Tests." + parts[0]);
            Assert.NotNull(type);
            var method = type.GetMethod(parts[1], BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);
            Assert.NotNull(method.GetCustomAttribute<FactAttribute>());
        }
    }
}
