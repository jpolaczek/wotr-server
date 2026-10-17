using System.Security.Cryptography;
using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Server;

public static class CommandFingerprint
{
    private const string Version = "2:";
    private static readonly HashSet<string> Envelope = ["matchId", "commandId", "expectedRevision", "decisionId", "kind", "payload"];

    public static string Create(GameCommand command)
    {
        var value = JsonSerializer.SerializeToElement(command, BoardCatalog.Json);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
            Write(writer, value, typeof(GameCommand), root: true);
        return Version + Hash(stream.ToArray());
    }

    public static bool Matches(string saved, GameCommand command, string current) =>
        saved.StartsWith(Version, StringComparison.Ordinal) ? saved == current
            : saved.Length == 64 && LegacyCommandFingerprint.Matches(saved, command);

    internal static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static void Write(Utf8JsonWriter writer, JsonElement value, Type type, bool root = false)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();
            foreach (var item in value.EnumerateArray())
                Write(writer, item, type.GetGenericArguments()[0]);
            writer.WriteEndArray();
        }
        else if (value.ValueKind == JsonValueKind.Object)
        {
            var shape = CommandJsonShape.For(type);
            writer.WriteStartObject();
            foreach (var property in value.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
            {
                if ((!root || !Envelope.Contains(property.Name)) && shape.IsDefault(property))
                    continue;
                writer.WritePropertyName(property.Name);
                Write(writer, property.Value, shape.Fields[property.Name]);
            }
            writer.WriteEndObject();
        }
        else
            value.WriteTo(writer);
    }
}
