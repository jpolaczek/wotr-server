using System.Text.Json;
using Wotr.Contracts;

namespace Wotr.Domain.Content;

public static class CardTextCatalog
{
    private sealed class TextEntry
    {
        public string Id { get; set; } = "";
        public string Event { get; set; } = "";
        public string CombatTitle { get; set; } = "";
    }

    private static readonly IReadOnlyDictionary<string, TextEntry> Entries = Load();

    public static string Event(string id, MatchConfiguration configuration)
    {
        var text = Entries.GetValueOrDefault(id)?.Event ?? "";
        return configuration.Kings && configuration.VisibleCorruption && id.StartsWith("kings.", StringComparison.Ordinal)
            ? text.Replace("hidden Corruption tile", "non-Kings Corruption tile", StringComparison.Ordinal) : text;
    }

    // TEST-09: the printed name of the card's combat half, e.g. "It Is a Gift".
    public static string CombatTitle(string id) => Entries.GetValueOrDefault(id)?.CombatTitle ?? "";

    private static IReadOnlyDictionary<string, TextEntry> Load()
    {
        var result = new Dictionary<string, TextEntry>(StringComparer.Ordinal);
        foreach (var module in new[] { "base", "lords", "kings", "fate" })
        {
            using var stream = typeof(CardTextCatalog).Assembly.GetManifestResourceStream($"Wotr.{module}-card-text.json")!;
            foreach (var entry in JsonSerializer.Deserialize<List<TextEntry>>(stream, BoardCatalog.Json)!)
                result.Add(entry.Id, entry);
        }
        return result;
    }
}
