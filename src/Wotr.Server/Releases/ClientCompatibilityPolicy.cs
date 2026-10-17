using System.Globalization;
using Wotr.Contracts;
using Wotr.Releases;

namespace Wotr.Server;

public enum CompatibilityVerdict
{
    Compatible,
    ClientUpdateRequired,
    ServerUpdateRequired,
    Invalid
}

// DS11: whether this server serves a client build, from its declared version and protocol.
public sealed class ClientCompatibilityPolicy(ClientReleaseOptions options, TimeProvider clock)
{
    public ProtocolSupport Protocols { get; } = ProtocolSupport.Current;

    public CompatibilityVerdict CheckRequest(string? version, string? protocol, ReleaseSnapshot catalog)
    {
        if (string.IsNullOrEmpty(version) && string.IsNullOrEmpty(protocol))
            return options.AcceptUnversioned ? CompatibilityVerdict.Compatible : CompatibilityVerdict.ClientUpdateRequired;
        return CheckDeclared(version, protocol, catalog);
    }

    public CompatibilityVerdict CheckDeclared(string? version, string? protocol, ReleaseSnapshot catalog)
    {
        if (!ClientVersion.TryParse(version, out var client) || !TryParseProtocol(protocol, out var clientProtocol))
            return CompatibilityVerdict.Invalid;
        if (clientProtocol > Protocols.Maximum)
            return CompatibilityVerdict.ServerUpdateRequired;
        if (clientProtocol < Protocols.Minimum || catalog.Minimum(clock.GetUtcNow()) is { } minimum && client.CompareTo(minimum) < 0)
            return CompatibilityVerdict.ClientUpdateRequired;
        return CompatibilityVerdict.Compatible;
    }

    public static string ErrorCode(CompatibilityVerdict verdict) => verdict switch
    {
        CompatibilityVerdict.ClientUpdateRequired => ClientCompatibility.ClientUpdateRequired,
        CompatibilityVerdict.ServerUpdateRequired => ClientCompatibility.ServerUpdateRequired,
        _ => ClientCompatibility.InvalidClientVersion
    };

    private static bool TryParseProtocol(string? text, out int protocol) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out protocol) && protocol > 0;
}
