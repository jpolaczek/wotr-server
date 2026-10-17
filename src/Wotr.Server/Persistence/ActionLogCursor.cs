using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Wotr.Contracts;

namespace Wotr.Server;

internal sealed record ActionLogCursor(int Version, string Match, Side Viewer, ActionLogFilter Filter, string Event)
{
    public static string Encode(string match, Side viewer, ActionLogFilter filter, string eventId) =>
        WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new ActionLogCursor(1, match, viewer, filter, eventId)));

    public static string Decode(string cursor, string match, Side viewer, ActionLogFilter filter)
    {
        try
        {
            if (cursor.Length > 512)
                throw new ArgumentException("InvalidPage");
            var value = JsonSerializer.Deserialize<ActionLogCursor>(WebEncoders.Base64UrlDecode(cursor));
            if (value is null || value.Version != 1 || value.Match != match || value.Viewer != viewer || value.Filter != filter ||
                value.Event is null || value.Event.Length != 0 && !ValidId(value.Event))
                throw new ArgumentException("InvalidPage");
            return value.Event;
        }
        catch (Exception error) when (error is JsonException or FormatException)
        {
            throw new ArgumentException("InvalidPage");
        }
    }

    public static bool ValidId(string id) => id.Length == 64 && id.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
