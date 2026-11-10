using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Wotr.Server;

// Optional operator-only library. Loading it never readies a room or starts work.
public sealed class EmbeddedBotRuntime : IDisposable
{
    private readonly object gate = new();
    private BotRuntimeLoadContext? context;
    private Func<string, CancellationToken, string>? decide;
    private readonly BotRuntimeDescription? description;
    public string Status { get; }
    public bool GameplayReady => decide is not null && description?.GameplayReady is true;
    public BotRuntimeDescription? Description => description is null ? null :
        description with { Profiles = [.. description.Profiles], Modules = [.. description.Modules],
            Policies = description.Policies.Select(p => p with { Features = [.. p.Features] }).ToArray() };

    private EmbeddedBotRuntime(string status, BotRuntimeLoadContext? context = null,
        BotRuntimeDescription? description = null, Func<string, CancellationToken, string>? decide = null)
    {
        Status = status;
        this.context = context;
        this.description = description;
        this.decide = decide;
    }

    public static EmbeddedBotRuntime Load(string? assemblyPath, string? expectedVersion = null)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
            return new("NotConfigured");
        if (!Path.IsPathFullyQualified(assemblyPath) || !File.Exists(assemblyPath))
            return new("Unavailable");
        BotRuntimeLoadContext? context = null;
        try
        {
            context = new(assemblyPath);
            var assembly = context.LoadFromAssemblyPath(assemblyPath);
            var type = assembly.GetType("Wotr.Bot.Runtime.RuntimeEntryPoint", throwOnError: true)!;
            var entry = Activator.CreateInstance(type)!;
            var describe = type.GetMethod("Describe", BindingFlags.Public | BindingFlags.Instance, [])!;
            var decide = type.GetMethod("Decide", BindingFlags.Public | BindingFlags.Instance, [typeof(string), typeof(CancellationToken)])!;
            var describeCall = describe.CreateDelegate<Func<string>>(entry);
            var decideCall = decide.CreateDelegate<Func<string, CancellationToken, string>>(entry);
            var json = describeCall();
            var description = json is null || Encoding.UTF8.GetByteCount(json) > 16 * 1024 ? null :
                JsonSerializer.Deserialize<BotRuntimeDescription>(json, BotRuntimeProtocol.Json);
            if (!BotRuntimeProtocol.Compatible(description) || expectedVersion is not null && expectedVersion != description!.RuntimeVersion)
            {
                context.Unload();
                return new("Incompatible");
            }
            return new("Loaded", context, description, decideCall);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            context?.Unload();
            return new("LoadFailed");
        }
    }

    public static EmbeddedBotRuntime LoadConfigured(BotHostOptions options)
    {
        if (options.ManifestPath is null)
            return options.Enabled && !options.AllowUnpackaged ? new("PackageRequired") : Load(options.AssemblyPath, options.RuntimeVersion);
        try
        {
            var package = BotRuntimePackage.Read(options.ManifestPath, options.ManifestSha256 ?? "");
            var entry = Path.Combine(Path.GetDirectoryName(options.ManifestPath)!, package.EntryAssembly);
            if (options.AssemblyPath is not null && Path.GetFullPath(options.AssemblyPath) != entry)
                return new("PackageInvalid");
            var runtime = Load(entry, options.RuntimeVersion);
            if (runtime.Status != "Loaded")
                return runtime;
            if (JsonSerializer.Serialize(runtime.Description, BotRuntimeProtocol.Json) !=
                JsonSerializer.Serialize(package.Description, BotRuntimeProtocol.Json))
            {
                runtime.Dispose();
                return new("PackageInvalid");
            }
            return runtime;
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            return new("PackageInvalid");
        }
    }

    public BotRuntimeResponse Decide(BotRuntimeRequest request, CancellationToken cancellation = default)
    {
        // One evaluation per loaded instance. BOT-18 owns the bounded host queue;
        // the lock is independent of room/match locks and fences disposal.
        lock (gate)
        {
            BotRuntimeResponse Refused(string reason) => new(BotRuntimeProtocol.Version, request.InvocationId, "Refused", reason, []);
            if (cancellation.IsCancellationRequested)
                return Refused("Cancelled");
            if (decide is null || description is null)
                return Refused("RuntimeUnavailable");
            if (!BotRuntimeProtocol.Valid(request, description))
                return Refused("InvalidInput");
            try
            {
                var input = JsonSerializer.Serialize(request, BotRuntimeProtocol.Json);
                if (Encoding.UTF8.GetByteCount(input) > BotRuntimeProtocol.MaximumInputBytes)
                    return Refused("InvalidInput");
                var json = decide(input, cancellation);
                if (cancellation.IsCancellationRequested)
                    return Refused("Cancelled");
                if (json is null || Encoding.UTF8.GetByteCount(json) > BotRuntimeProtocol.MaximumOutputBytes)
                    return Refused("InvalidOutput");
                var response = JsonSerializer.Deserialize<BotRuntimeResponse>(json, BotRuntimeProtocol.Json);
                if (BotRuntimeProtocol.Valid(response, request))
                    return response!;
                // The broken rule follows the reason for the host's log; a reason holds at most 64 characters.
                var invalid = "InvalidOutput:" + BotRuntimeProtocol.Invalidity(response, request);
                return Refused(invalid[..Math.Min(64, invalid.Length)]);
            }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                // Exceptions may contain private input. Do not expose their text.
                return Refused(cancellation.IsCancellationRequested ? "Cancelled" : "RuntimeFailure");
            }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            decide = null;
            context?.Unload();
            context = null;
        }
    }
}
