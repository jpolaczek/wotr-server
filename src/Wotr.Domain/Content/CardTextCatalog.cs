using System.Text.Json;
using Wotr.Contracts;

namespace Wotr.Domain.Content;

public static class CardTextCatalog
{
    private sealed class TextEntry
    {
        public string Id { get; set; } = "";
        public string Event { get; set; } = "";
    }

    private static readonly IReadOnlyDictionary<string, string> Events = Load();

    public static string Event(string id, MatchConfiguration configuration)
    {
        var text = Events.GetValueOrDefault(id, "");
        return configuration.Kings && configuration.VisibleCorruption && id.StartsWith("kings.", StringComparison.Ordinal)
            ? text.Replace("hidden Corruption tile", "non-Kings Corruption tile", StringComparison.Ordinal) : text;
    }

    private static IReadOnlyDictionary<string, string> Load()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var module in new[] { "base", "lords", "kings", "fate" })
        {
            using var stream = typeof(CardTextCatalog).Assembly.GetManifestResourceStream($"Wotr.{module}-card-text.json")!;
            foreach (var entry in JsonSerializer.Deserialize<List<TextEntry>>(stream, BoardCatalog.Json)!)
                result.Add(entry.Id, entry.Event);
        }
        return result;
    }
}
