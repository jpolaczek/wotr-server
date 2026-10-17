using System.Reflection;
using System.Text.Json;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class CharacterManifestTests
{
    [Fact]
    public void EveryReviewedCharacterEntryAndPrintedAbilityHasAnExistingBehaviorTest()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Wotr.slnx")))
            root = root.Parent;
        Assert.NotNull(root);
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "content/character-implementation-manifest.json")));
        using var sources = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, manifest.RootElement.GetProperty("source").GetString()!)));
        var cards = manifest.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        var reviewed = sources.RootElement.GetProperty("cards").EnumerateArray().ToDictionary(c => c.GetProperty("id").GetString()!);
        Assert.Equal(14, cards.Length);
        Assert.Equal(CharacterCatalog.Base.Keys.Order(), cards.Select(c => c.GetProperty("id").GetString()).Order());
        foreach (var card in cards)
        {
            var id = card.GetProperty("id").GetString()!;
            RequireTests(card.GetProperty("entryTests"));
            var abilities = card.GetProperty("abilities").EnumerateArray().ToArray();
            var names = abilities.Select(a => a.GetProperty("id").GetString()).Order();
            Assert.Equal(CharacterCatalog.Base[id].Abilities.Order(), names);
            Assert.Equal(reviewed[id].GetProperty("abilities").EnumerateArray().Select(a => a.GetString()).Order(), names);
            foreach (var ability in abilities)
                RequireTests(ability.GetProperty("tests"));
        }
        RequireTests(manifest.RootElement.GetProperty("integrationTests"));
    }

    private static void RequireTests(JsonElement tests)
    {
        Assert.NotEqual(0, tests.GetArrayLength());
        foreach (var test in tests.EnumerateArray())
        {
            var parts = test.GetString()!.Split('.');
            Assert.Equal(2, parts.Length);
            var type = typeof(CharacterManifestTests).Assembly.GetType("Wotr.Tests." + parts[0]);
            Assert.NotNull(type);
            var method = type.GetMethod(parts[1], BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);
            Assert.NotNull(method.GetCustomAttribute<FactAttribute>());
        }
    }
}
