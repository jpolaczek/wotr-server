using System.Reflection;
using System.Text.Json;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class LordsCardManifestTests
{
    [Fact]
    public void EachImplementedLordsCardMatchesItsSourceRulesRuntimeAndBehaviorTests()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Wotr.slnx")))
            root = root.Parent;
        Assert.NotNull(root);
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "content/lords-card-implementation-manifest.json")));
        using var source = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "docs/sources/lords-card-index.json")));
        var reviewed = source.RootElement.GetProperty("cards").EnumerateArray().ToDictionary(c => c.GetProperty("id").GetString()!);
        var cards = manifest.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(14, cards.Length);
        Assert.Equal(reviewed.Keys.Order(), cards.Select(c => c.GetProperty("id").GetString()).Order());
        Assert.Single(cards, c => c.GetProperty("kind").GetString() == "SpecialEvent");
        Assert.Equal(EffectCatalog.All.Events.Keys.Where(id => id.StartsWith("lords.")).Order(),
            cards.Where(c => c.GetProperty("kind").GetString() == "Event").Select(c => c.GetProperty("id").GetString()).Order());
        Assert.Equal(14, CardCatalog.Lords.Count);
        Assert.Equal(96, CardCatalog.Base.Count);
        foreach (var card in cards)
            CheckCard(card, reviewed[card.GetProperty("id").GetString()!]);
    }

    private static void CheckCard(JsonElement card, JsonElement source)
    {
        var id = card.GetProperty("id").GetString()!;
        Assert.True(source.GetProperty("implemented").GetBoolean());
        Assert.True(card.GetProperty("eventImplemented").GetBoolean());
        Assert.Equal(CardCatalog.Lords[id].Title, source.GetProperty("title").GetString());
        Assert.Equal(source.GetProperty("kind").GetString(), card.GetProperty("kind").GetString());
        if (card.GetProperty("kind").GetString() == "SpecialEvent")
            CheckSpecial(card, id);
        else
        {
            Assert.True(card.GetProperty("combatImplemented").GetBoolean());
            Assert.Equal(EffectCatalog.All.Events[id].ProgramId, card.GetProperty("programId").GetString());
            Assert.Equal(EffectCatalog.All.Combats[id].Effects.Select(e => e.ProgramId), card.GetProperty("combatPrograms").EnumerateArray().Select(e => e.GetString()));
            Assert.Equal(EffectCatalog.All.Combats[id].Effects.Select(e => e.Initiative), source.GetProperty("initiative").EnumerateArray().Select(e => e.GetInt32()));
        }
        foreach (var section in new[] { "eventRules", "combatRules", "verificationCases" })
        {
            var mappings = card.GetProperty(section).EnumerateArray().ToArray();
            Assert.Equal(source.GetProperty(section).EnumerateArray().Select(e => e.GetString()), mappings.Select(e => e.GetProperty("sourceText").GetString()));
            foreach (var mapping in mappings)
                RequireTests(mapping.GetProperty("tests"));
        }
        RequireTests(card.GetProperty("tests"));
    }

    private static void CheckSpecial(JsonElement card, string id)
    {
        Assert.Equal(SmeagolRules.WeShallGetIt, id);
        Assert.Equal("special", CardCatalog.Lords[id].Deck);
        Assert.False(card.GetProperty("combatImplemented").GetBoolean());
        Assert.Empty(card.GetProperty("combatPrograms").EnumerateArray());
        Assert.Empty(card.GetProperty("combatInitiatives").EnumerateArray());
        Assert.DoesNotContain(id, EffectCatalog.All.Events.Keys);
        Assert.DoesNotContain(id, EffectCatalog.All.Combats.Keys);
        var reaction = EffectCatalog.All.Reactions[card.GetProperty("reactionId").GetString()!];
        Assert.Equal(id, reaction.RequiredTableCard);
        Assert.Equal(WeShallGetIt.Window, reaction.Window);
        Assert.Equal(reaction.ProgramId, card.GetProperty("programId").GetString());
        Assert.Equal("SmeagolRules.Discard", card.GetProperty("automaticEntry").GetString());
    }

    private static void RequireTests(JsonElement tests)
    {
        Assert.NotEqual(0, tests.GetArrayLength());
        foreach (var test in tests.EnumerateArray())
        {
            var parts = test.GetString()!.Split('.');
            var type = typeof(LordsCardManifestTests).Assembly.GetType("Wotr.Tests." + parts[0]);
            Assert.NotNull(type);
            var method = type.GetMethod(parts[1], BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);
            var attribute = method.GetCustomAttribute<FactAttribute>();
            Assert.NotNull(attribute);
            Assert.Null(attribute.Skip);
        }
    }
}
