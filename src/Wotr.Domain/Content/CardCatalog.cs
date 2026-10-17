using System.Text.Json;
using Wotr.Contracts;

namespace Wotr.Domain.Content;

public sealed class CardDefinition
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public Side Side { get; set; }
    public string Deck { get; set; } = "";
    public DieFace EventType { get; set; }
    public string Source { get; set; } = "";
    public string Replaces { get; set; } = "";
    public bool ExcludedWithFate { get; set; }
}

public static class CardCatalog
{
    public static IReadOnlyDictionary<string, CardDefinition> Base { get; } = Load("base", 96);
    public static IReadOnlyDictionary<string, CardDefinition> Lords { get; } = Load("lords", 14);
    public static IReadOnlyDictionary<string, CardDefinition> Kings { get; } = Load("kings", 19);
    public static IReadOnlyDictionary<string, CardDefinition> Fate { get; } = Load("fate", 6);
    public static IReadOnlyDictionary<string, CardDefinition> All { get; } = Base.Values.Concat(Lords.Values).Concat(Kings.Values)
        .Concat(Fate.Values).ToDictionary(c => c.Id);
    private static readonly IReadOnlySet<string> LordsReplacements = Lords.Values
        .Where(c => c.Replaces.Length > 0).Select(c => c.Replaces).ToHashSet(StringComparer.Ordinal);
    private static readonly IReadOnlyDictionary<string, CardDefinition> KingsReplacements = Kings.Values
        .Where(c => c.Replaces.Length > 0).ToDictionary(c => c.Replaces);
    private static readonly IReadOnlySet<string> FateReplacements = Fate.Values.Select(c => c.Replaces).ToHashSet(StringComparer.Ordinal);

    // UI-198: the title a player reads in the history; an unknown id stands for itself.
    public static string Title(string id) => All.TryGetValue(id, out var card) ? card.Title : id;

    public static bool Enabled(MatchState state, string id) => Enabled(state.Configuration, id);

    public static bool Enabled(MatchConfiguration configuration, string id)
    {
        if (Fate.ContainsKey(id))
            return configuration.Fate;
        if (Kings.TryGetValue(id, out var kings))
            return configuration.Kings && !(configuration.Fate && kings.ExcludedWithFate);
        if (Base.ContainsKey(id))
            return !(configuration.Fate && FateReplacements.Contains(id)) &&
                !(configuration.Lords && LordsReplacements.Contains(id)) &&
                !(configuration.Kings && KingsReplacements.TryGetValue(id, out var replacement) &&
                    !(configuration.Fate && replacement.ExcludedWithFate));
        return configuration.Lords && Lords.ContainsKey(id);
    }

    // Resolve module replacements before shuffling, with explicit Fate exclusions.
    public static IEnumerable<CardDefinition> ConfiguredDeck(MatchConfiguration configuration) =>
        All.Values.Where(c => c.Deck != "special" && Enabled(configuration, c.Id));

    private static IReadOnlyDictionary<string, CardDefinition> Load(string module, int expectedCount)
    {
        using var stream = typeof(CardCatalog).Assembly.GetManifestResourceStream($"Wotr.{module}-cards.json")!;
        var cards = JsonSerializer.Deserialize<List<CardDefinition>>(stream, BoardCatalog.Json)
            ?? throw new InvalidDataException("Missing card metadata.");
        if (cards.Count != expectedCount || cards.Any(c => c.EventType is not (DieFace.Character or DieFace.Muster or DieFace.Army)))
            throw new InvalidDataException($"Invalid {module} card metadata.");
        return cards.ToDictionary(c => c.Id);
    }
}
