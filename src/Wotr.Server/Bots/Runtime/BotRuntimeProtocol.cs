using System.Text.Json;
using System.Text.Json.Serialization;
using Wotr.Contracts;

namespace Wotr.Server;

// Server-only ABI envelopes. No private policy assembly is a public build input.
public sealed record BotRuntimeDescription(int AbiVersion, string RuntimeVersion, int ProtocolVersion,
    string RulesVersion, string ContentVersion, bool GameplayReady, string[] Profiles, string[] Modules);

public sealed record BotRuntimeRequest(int AbiVersion, string InvocationId, string RuntimeVersion,
    int ProtocolVersion, string RulesVersion, string ContentVersion, string MatchId, Side Side,
    PlayerView Player, ActionLogStep[] History, BattleReportView[] Reports, ActionPreview[] Previews,
    string? Memory, int SampleSeed, int MaximumWork)
{
    public override string ToString() => "BotRuntimeRequest [private player information]";
}

public sealed record BotRuntimeResponse(int AbiVersion, string InvocationId, string Status, string Reason,
    string[] Modules, string? PolicyVersion = null, GameCommand? Command = null, string? ProposedMemory = null)
{
    public override string ToString() => "BotRuntimeResponse [private]";
}

internal static class BotRuntimeProtocol
{
    internal const int Version = 1;
    internal const int MaximumInputBytes = 16 * 1024 * 1024;
    internal const int MaximumOutputBytes = 4 * 1024 * 1024;
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = 64,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    internal static bool Compatible(BotRuntimeDescription? description) => description is not null &&
        description.AbiVersion == Version && System.Version.TryParse(description.RuntimeVersion, out _) &&
        description.ProtocolVersion == Protocol.Version && description.RulesVersion == Protocol.RulesVersion &&
        description.ContentVersion == Protocol.ContentVersion && description.Profiles is { Length: 2 } &&
        description.Profiles.Order(StringComparer.Ordinal).SequenceEqual(new[] { "free-peoples-strong", "shadow-strong" }) &&
        description.Modules is { Length: > 0 and <= 10 } && description.Modules.Contains("base") &&
        description.Modules.Distinct().Count() == description.Modules.Length &&
        description.Modules.All(BotAccessOptions.KnownFeatures.Contains);

    internal static bool Valid(BotRuntimeRequest request, BotRuntimeDescription description) =>
        request.AbiVersion == Version && Guid.TryParseExact(request.InvocationId, "N", out _) &&
        request.RuntimeVersion == description.RuntimeVersion && request.ProtocolVersion == Protocol.Version &&
        request.RulesVersion == Protocol.RulesVersion && request.ContentVersion == Protocol.ContentVersion &&
        request.Player is not null && request.Player.MatchId == request.MatchId && request.Player.You == request.Side &&
        Enum.IsDefined(request.Side) && request.Player.Revision >= 0 && request.Player.Configuration is not null &&
        request.History is { Length: <= 4096 } && request.Reports is { Length: <= 128 } &&
        request.Previews is { Length: <= 256 } && request.MaximumWork is >= 1 and <= 100_000;

    internal static bool Valid(BotRuntimeResponse? response, BotRuntimeRequest request) =>
        response is not null && response.AbiVersion == Version && response.InvocationId == request.InvocationId &&
        response.Modules is { Length: <= 10 } && response.Modules.All(BotAccessOptions.KnownFeatures.Contains) &&
        response.Reason is { Length: <= 64 } &&
        (response.Status == "Proposal" ? Proposal(response, request) :
            response.Status is "NoDecision" or "Refused" && response.Command is null && response.ProposedMemory is null);

    private static bool Proposal(BotRuntimeResponse response, BotRuntimeRequest request) =>
        response.PolicyVersion is { Length: > 0 and <= 64 } && response.ProposedMemory is { Length: > 0 and <= 2_000_000 } &&
        response.Command is { Payload: not null } command && command.CommandId is { Length: > 0 and <= 128 } &&
        command.MatchId == request.MatchId && command.ExpectedRevision == request.Player.Revision &&
        request.Player.Winner is null && request.Player.DecisionOwner == request.Side && request.Player.Decision is not null &&
        command.DecisionId == request.Player.Decision.Id &&
        request.Player.Options?.Any(option => option.Enabled && option.Kind == command.Kind) is true;
}
