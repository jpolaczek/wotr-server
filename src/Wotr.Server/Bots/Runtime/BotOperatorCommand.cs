using System.Text.Json;
using Npgsql;

namespace Wotr.Server;

// Local operator CLI only. Never register these operations as HTTP endpoints.
public sealed record BotOperatorCommand(string Operation, long Epoch = 0, string? SourceVersion = null)
{
    public static BotOperatorCommand? Parse(string[] args)
    {
        if (!args.Contains("--bot-runtime", StringComparer.Ordinal))
            return null;
        if (args is ["--bot-runtime", "inspect" or "status" or "drain"])
            return new(args[1]);
        if (args.Length is 3 or 4 && args[0] == "--bot-runtime" &&
            long.TryParse(args[2], out var epoch) && epoch >= 0)
        {
            if (args is [_, "resume", _])
                return new("resume", epoch);
            if (args is [_, "upgrade", _, var source] && Version.TryParse(source, out _))
                return new("upgrade", epoch, source);
        }
        throw new ArgumentException("Use --bot-runtime inspect|status|drain, resume <epoch>, or upgrade <epoch> <source-version>.");
    }

    public async Task<int> Run(IServiceProvider services, CancellationToken cancellation = default)
    {
        void Write(object value) => Console.WriteLine(JsonSerializer.Serialize(value, BotRuntimeProtocol.Json));
        try
        {
            if (Operation == "status")
            {
                Write(await services.GetRequiredService<BotRuntimeControl>().Status(cancellation));
                return 0;
            }
            if (Operation == "drain")
            {
                Write(new { draining = true, epoch = await services.GetRequiredService<BotRuntimeControl>().Drain(cancellation) });
                return 0;
            }
            var options = services.GetRequiredService<BotHostOptions>();
            var runtime = services.GetRequiredService<EmbeddedBotRuntime>();
            if (options.ManifestPath is null || runtime.Status != "Loaded" || runtime.Description is not { } description)
            {
                Write(new { error = "VerifiedPackageRequired", runtime.Status });
                return 1;
            }
            if (Operation == "inspect")
            {
                Write(new { runtime.Status, Description = description });
                return 0;
            }
            var control = services.GetRequiredService<BotRuntimeControl>();
            if (Operation == "resume")
            {
                var resumed = await control.Resume(Epoch, description.RuntimeVersion, options.ManifestSha256!.ToUpperInvariant(), cancellation);
                Write(new { resumed, error = resumed ? "" : "ControlVersionMismatch" });
                return resumed ? 0 : 1;
            }
            var result = await control.Upgrade(Epoch, SourceVersion!, description, options.ManifestSha256!, cancellation);
            Write(result);
            return result.Applied ? 0 : 1;
        }
        catch (Exception error) when (error is NpgsqlException or InvalidOperationException or JsonException)
        {
            Write(new { error = "OperatorOperationFailed" });
            return 1;
        }
    }
}
