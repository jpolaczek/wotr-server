namespace Wotr.Server;

// Host metadata only; the private library exclusively interprets opaque memory.
public sealed record BotRuntimeIdentity(int AbiVersion, int ProtocolVersion, string RulesVersion,
    string ContentVersion, string MemoryFormat)
{
    public static BotRuntimeIdentity? Of(BotRuntimeDescription? description) =>
        description?.MemoryFormat is { Length: > 0 } format
            ? new(description.AbiVersion, description.ProtocolVersion, description.RulesVersion, description.ContentVersion, format) : null;
}
