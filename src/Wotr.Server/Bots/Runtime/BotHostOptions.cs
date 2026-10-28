using Wotr.Contracts;

namespace Wotr.Server;

public sealed record BotHostOptions(bool Enabled, string? AssemblyPath, string? RuntimeVersion,
    int Workers, TimeSpan Lease, TimeSpan Poll, TimeSpan Retry, TimeSpan DecisionTimeout)
{
    public string? ManifestPath { get; init; }
    public string? ManifestSha256 { get; init; }
    public bool AllowUnpackaged { get; init; }
    public int MaximumWork { get; init; } = 100_000;
    public long MemoryPressureBytes { get; init; } = 512L * 1024 * 1024;

    public static BotHostOptions Read(IConfiguration configuration)
    {
        var section = configuration.GetSection("Wotr:Bots");
        var memoryMiB = section.GetValue("ManagedMemoryLimitMiB", 512L);
        if (memoryMiB is < 64 or > 16_384)
            throw new InvalidOperationException("Invalid embedded bot memory limit.");
        var options = new BotHostOptions(section.GetValue<bool>("RuntimeEnabled"), section["RuntimeAssembly"],
            section["RuntimeVersion"], section.GetValue("Workers", 2),
            TimeSpan.FromSeconds(section.GetValue("RuntimeLeaseSeconds", 30)),
            TimeSpan.FromMilliseconds(section.GetValue("PollMilliseconds", 500)),
            TimeSpan.FromSeconds(section.GetValue("RetrySeconds", 5)),
            TimeSpan.FromMilliseconds(section.GetValue("DecisionTimeoutMilliseconds", 5000)))
        {
            ManifestPath = section["RuntimeManifest"], ManifestSha256 = section["RuntimeManifestSha256"],
            AllowUnpackaged = section.GetValue<bool>("AllowUnpackagedRuntime"),
            MaximumWork = section.GetValue("MaximumWork", 100_000),
            MemoryPressureBytes = memoryMiB * 1024 * 1024
        };
        if (options.Workers is < 1 or > 8 || options.Lease.TotalSeconds is < 5 or > 300 ||
            options.Poll.TotalMilliseconds is < 50 or > 5000 || options.Retry.TotalSeconds is < 1 or > 60 ||
            options.DecisionTimeout.TotalMilliseconds is < 50 or > 5000 ||
            options.MaximumWork is < 3 or > 100_000 || options.MemoryPressureBytes is < 64L * 1024 * 1024 or > 16L * 1024 * 1024 * 1024 ||
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
    public BotRuntimeIdentity? Compatibility => BotRuntimeIdentity.Of(runtime.Description);

    public string? Unavailable(MatchConfiguration configuration, string? policyVersion = null, string? runtimeVersion = null)
    {
        var description = runtime.Description;
        if (!Enabled || !rooms.Enabled || !runtime.GameplayReady || description is null || Compatibility is null)
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
