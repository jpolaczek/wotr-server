using Microsoft.Extensions.Primitives;
using Wotr.Contracts;

namespace Wotr.Server;

public static class BotCompatibility
{
    public const int Version = 1;
    public const string ApplicationHeader = "X-Wotr-Bot-Version";
    public const string ProtocolHeader = "X-Wotr-Bot-Protocol";
    public const string RulesHeader = "X-Wotr-Rules-Version";
    public const string ContentHeader = "X-Wotr-Content-Version";

    // Called only after a credential established BotIdentity; headers alone cannot select this policy.
    public static bool Compatible(HttpContext context, BotAccessOptions options)
    {
        var headers = context.Request.Headers;
        return ClientVersion.TryParse(Single(headers[ApplicationHeader]), out var version) &&
            version!.CompareTo(options.MinimumVersion) >= 0 && Single(headers[ProtocolHeader]) == Version.ToString() &&
            Single(headers[ClientCompatibility.ProtocolVersionHeader]) == Protocol.Version.ToString() &&
            Single(headers[RulesHeader]) == Protocol.RulesVersion && Single(headers[ContentHeader]) == Protocol.ContentVersion;
    }

    private static string? Single(StringValues values) => values.Count == 1 ? values[0] : null;
}
