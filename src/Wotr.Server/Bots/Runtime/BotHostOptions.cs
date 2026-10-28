using Wotr.Contracts;

namespace Wotr.Server;

public sealed record BotHostOptions(bool Enabled, string? AssemblyPath, string? RuntimeVersion,
    int Workers, TimeSpan Lease, TimeSpan Poll, TimeSpan Retry, TimeSpan DecisionTimeout)
{
    public static BotHostOptions Read(IConfiguration configuration)
    {
        var section = configuration.GetSection("Wotr:Bots");
        var options = new BotHostOptions(section.GetValue<bool>("RuntimeEnabled"), section["RuntimeAssembly"],
            section["RuntimeVersion"], section.GetValue("Workers", 2),
            TimeSpan.FromSeconds(section.GetValue("RuntimeLeaseSeconds", 30)),
            TimeSpan.FromMilliseconds(section.GetValue("PollMilliseconds", 500)),
            TimeSpan.FromSeconds(section.GetValue("RetrySeconds", 5)),
            TimeSpan.FromMilliseconds(section.GetValue("DecisionTimeoutMilliseconds", 5000)));
        if (options.Workers is < 1 or > 8 || options.Lease.TotalSeconds is < 5 or > 300 ||
            options.Poll.TotalMilliseconds is < 50 or > 5000 || options.Retry.TotalSeconds is < 1 or > 60 ||
            options.DecisionTimeout.TotalMilliseconds is < 50 or > 5000 ||
            options.Enabled && !Version.TryParse(options.RuntimeVersion, out _))
            throw new InvalidOperationException("Invalid embedded bot host limits or runtime pin.");
        return options;
    }
}

public sealed class BotRuntimeCapabilities(BotHostOptions options, BotRoomOptions rooms,
    BotAccessOptions access, EmbeddedBotRuntime runtime)
{
    public bool Enabled => options.Enabled;
    public string? Version => runtime.Description?.RuntimeVersion;

    public string? Unavailable(MatchConfiguration configuration, string? policyVersion = null, string? runtimeVersion = null)
    {
        var description = runtime.Description;
        if (!Enabled || !rooms.Enabled || !runtime.GameplayReady || description is null)
            return "BotUnavailable";
        if (description.RuntimeVersion != options.RuntimeVersion || runtimeVersion is not null && runtimeVersion != Version ||
            policyVersion is not null && policyVersion != rooms.PolicyVersion ||
            description.Policies.Any(p => p.PolicyVersion != rooms.PolicyVersion))
            return "BotVersionUnsupported";
        if (!access.Supports(configuration) || description.Policies.Any(p =>
                !(access with { Features = p.Features.ToHashSet(StringComparer.Ordinal) }).Supports(configuration)))
            return "BotConfigurationUnsupported";
        return null;
    }
}
