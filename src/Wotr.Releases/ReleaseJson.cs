using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wotr.Releases;

// Release files are strict: an unknown field is a different schema, not something to ignore.
public static class ReleaseJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() }
    };

    public static byte[] Serialize<T>(T value) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, Options) + "\n");

    public static T Parse<T>(byte[] bytes, string description)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(bytes, Options) ?? throw new ReleaseRejected(description + " is empty.");
        }
        catch (JsonException)
        {
            throw new ReleaseRejected(description + " does not match its schema.");
        }
    }

    public static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    public static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    public static bool IsSha256(string value) =>
        value.Length == 64 && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
}
