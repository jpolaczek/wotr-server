using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Wotr.Domain.BotProtocol;

// BOT-225: compact JSON as sent (one line, fields in the order they were written) and the decision's fingerprint (SPEC §4:
// 64-bit FNV-1a over "SIDE\nKIND\nN\n" and each option's compact JSON without its "text", each followed by "\n").
public static class ProtocolJson
{
    public static readonly JsonSerializerOptions Compact = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Write(JsonNode node) => node.ToJsonString(Compact);

    public static string Fingerprint(string side, string kind, IReadOnlyList<JsonObject> options)
    {
        var text = new StringBuilder().Append(side).Append('\n').Append(kind).Append('\n').Append(options.Count).Append('\n');
        foreach (var option in options)
        {
            var copy = new JsonObject();
            foreach (var (key, value) in option)
                if (key != "text")
                    copy[key] = value?.DeepClone();
            text.Append(Write(copy)).Append('\n');
        }
        return Fnv1a(Encoding.UTF8.GetBytes(text.ToString())).ToString("x16");
    }

    public static ulong Fnv1a(ReadOnlySpan<byte> bytes)
    {
        var hash = 0xcbf29ce484222325UL;
        foreach (var b in bytes)
        {
            hash ^= b;
            hash = unchecked(hash * 0x100000001b3UL);
        }
        return hash;
    }
}
