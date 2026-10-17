using Wotr.Contracts;

namespace Wotr.Releases;

// The client protocols this server build serves. Raise Minimum only together with a client release
// that speaks the newer protocol; older clients then receive ClientUpdateRequired.
public sealed record ProtocolSupport(int Minimum, int Maximum)
{
    public static readonly ProtocolSupport Current = new(2, Protocol.Version);

    public bool Accepts(int protocol) => protocol >= Minimum && protocol <= Maximum;
}
