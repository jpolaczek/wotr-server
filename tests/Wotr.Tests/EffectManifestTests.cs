using System.Reflection;
using System.Text.Json;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class EffectManifestTests
{
    [Fact]
    public void ManifestMatchesImplementedProgramsAndExistingBehaviorTests()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Wotr.slnx")))
            root = root.Parent;
        Assert.NotNull(root);
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "content/implementation-manifest.json")));
        var cards = json.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(96, cards.Length);
        Assert.Equal(CardCatalog.Base.Keys.Order(), cards.Select(c => c.GetProperty("id").GetString()).Order());
        Assert.Equal(EffectCatalog.Base.Events.Keys.Order(), cards.Where(c => c.GetProperty("eventImplemented").GetBoolean()).Select(c => c.GetProperty("id").GetString()).Order());
        Assert.Equal(EffectCatalog.Base.Combats.Keys.Order(), cards.Where(c => c.GetProperty("combatImplemented").GetBoolean()).Select(c => c.GetProperty("id").GetString()).Order());
        Assert.Equal(96, CardCatalog.Base.Count);
        Assert.Equal(CardCatalog.Base.Keys.Order(), BoardCatalog.LoadBase().Cards.Order());
        foreach (var card in cards)
        {
            var id = card.GetProperty("id").GetString()!;
            Assert.True(card.GetProperty("eventImplemented").GetBoolean());
            Assert.True(card.GetProperty("combatImplemented").GetBoolean());
            var combat = EffectCatalog.Base.Combats[id];
            Assert.Equal(combat.Effects.Select(e => e.ProgramId), card.GetProperty("combatPrograms").EnumerateArray().Select(e => e.GetString()));
            Assert.Equal(combat.Effects.Select(e => e.Initiative), card.GetProperty("combatInitiatives").EnumerateArray().Select(e => e.GetInt32()));
            Assert.Equal(EffectCatalog.Base.Events[id].ProgramId, card.GetProperty("programId").GetString());
            Assert.True(File.Exists(Path.Combine(root.FullName, card.GetProperty("source").GetString()!)));
            var tests = card.GetProperty("tests").EnumerateArray().ToArray();
            Assert.NotEmpty(tests);
            foreach (var test in tests)
                RequireBehaviorTest(test.GetString()!);
        }
        foreach (var test in json.RootElement.GetProperty("infrastructureTests").EnumerateArray())
            RequireBehaviorTest(test.GetString()!);
    }

    private static void RequireBehaviorTest(string name)
    {
        var parts = name.Split('.');
        var type = typeof(EffectManifestTests).Assembly.GetType($"Wotr.Tests.{parts[0]}");
        Assert.NotNull(type);
        var method = type.GetMethod(parts[1], BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var fact = method.GetCustomAttribute<FactAttribute>();
        Assert.NotNull(fact);
        Assert.Null(fact.Skip);
    }
}
