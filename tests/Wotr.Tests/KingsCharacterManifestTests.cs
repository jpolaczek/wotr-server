using System.Reflection;
using System.Text.Json;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class KingsCharacterManifestTests
{
    [Fact]
    public void TenSovereignFrontsMatchReviewedStatsAndMapAllAbilitiesAndCasesToActiveTests() =>
        CheckManifest("kings-sovereign-implementation-manifest.json", 10, false);

    [Fact]
    public void ThreeChieftainFrontsAndFiveTokensMatchReviewedStatsAndMapEveryCaseToActiveTests() =>
        CheckManifest("kings-chieftain-implementation-manifest.json", 3, true);

    private static void CheckManifest(string file, int count, bool chieftains)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Wotr.slnx")))
            root = root.Parent;
        Assert.NotNull(root);
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "content", file)));
        using var source = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, manifest.RootElement.GetProperty("source").GetString()!)));
        var cards = manifest.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        var reviewed = source.RootElement.GetProperty("cards").EnumerateArray()
            .Where(c => (c.GetProperty("role").GetString() == "DarkChieftain") == chieftains).ToDictionary(c => c.GetProperty("id").GetString()!);
        Assert.Equal(count, cards.Length);
        Assert.Equal(reviewed.Keys.Order(), cards.Select(c => c.GetProperty("id").GetString()).Order());
        foreach (var card in cards)
            CheckCard(card, reviewed[card.GetProperty("id").GetString()!]);
        RequireTests(manifest.RootElement.GetProperty("integrationTests"));
        if (chieftains)
            CheckTokens(manifest.RootElement.GetProperty("tokens"), source.RootElement.GetProperty("tokens"));
    }

    private static void CheckCard(JsonElement card, JsonElement source)
    {
        var id = card.GetProperty("id").GetString()!;
        var definition = CharacterCatalog.Kings[id];
        Assert.True(source.GetProperty("implemented").GetBoolean());
        Assert.Equal(source.GetProperty("level").GetInt32(), definition.Level);
        Assert.Equal(source.GetProperty("leadership").GetInt32(), definition.Leadership);
        var nation = source.GetProperty("nationIcon").GetString();
        Assert.Equal(nation == "southrons-easterlings" ? "southrons" : nation, definition.NationIcon);
        Assert.Equal(source.GetProperty("controller").GetString(), definition.Side.ToString());
        Assert.Equal(source.GetProperty("extraActionDice").GetInt32(), definition.ExtraActionDice);
        RequireTests(card.GetProperty("entryTests"));
        RequireTests(card.GetProperty("exitTests"));
        var abilities = card.GetProperty("abilities").EnumerateArray().ToArray();
        Assert.Equal(definition.Abilities.Order(), abilities.Select(a => a.GetProperty("id").GetString()).Order());
        foreach (var ability in abilities)
            RequireTests(ability.GetProperty("tests"));
        RequireCases(card.GetProperty("sourceAbilities"), source.GetProperty("abilities"));
        RequireCases(card.GetProperty("verificationCases"), source.GetProperty("verificationCases"));
    }

    private static void CheckTokens(JsonElement mappings, JsonElement source)
    {
        Assert.Equal(5, mappings.GetArrayLength());
        Assert.Equal(source.EnumerateArray().Select(t => t.GetProperty("id").GetString()),
            mappings.EnumerateArray().Select(t => t.GetProperty("id").GetString()));
        foreach (var token in source.EnumerateArray())
        {
            var sovereign = "kings.sovereign." + token.GetProperty("identity").GetString();
            Assert.Equal(token.GetProperty("id").GetString(), KingsRulerTokens.Id(sovereign));
            Assert.Equal(token.GetProperty("abilityResult").GetString(), KingsRulerTokens.Face(sovereign).ToString());
        }
        foreach (var token in mappings.EnumerateArray())
            RequireTests(token.GetProperty("tests"));
    }

    private static void RequireCases(JsonElement mappings, JsonElement source)
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
            var type = typeof(KingsCharacterManifestTests).Assembly.GetType("Wotr.Tests." + parts[0]);
            Assert.NotNull(type);
            var method = type.GetMethod(parts[1], BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);
            var fact = method.GetCustomAttribute<FactAttribute>();
            Assert.NotNull(fact);
            Assert.Null(fact.Skip);
        }
    }
}
