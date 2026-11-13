using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// A separately installed public ABI fixture, never the private implementation.
internal sealed class BotPackageFixture : IDisposable
{
    public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "wotr-runtime-package-" + Guid.NewGuid().ToString("N"));
    public string ManifestPath => Path.Combine(DirectoryPath, "runtime-manifest.json");
    public string Library => Path.Combine(DirectoryPath, "Wotr.RuntimeFixture.dll");
    public string Hash => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(ManifestPath)));
    public BotRuntimePackage Package { get; private set; }

    public BotPackageFixture(bool upgrade = false)
    {
        Directory.CreateDirectory(DirectoryPath);
        File.Copy(upgrade ? Path.Combine(AppContext.BaseDirectory, "Fixtures/RuntimeUpgrade/Wotr.RuntimeFixture.dll") : BotHostFixture.Library, Library);
        using var runtime = EmbeddedBotRuntime.Load(Library);
        Assert.Equal("Loaded", runtime.Status);
        Package = new(1, Path.GetFileName(Library), runtime.Description!, new(new string('a', 40), new string('b', 40), new string('c', 64)),
            [new(Path.GetFileName(Library), new FileInfo(Library).Length, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Library))))]);
        Rewrite(Package);
    }

    public void Rewrite(BotRuntimePackage package)
    {
        Package = package;
        File.WriteAllText(ManifestPath, JsonSerializer.Serialize(package, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    public Dictionary<string, string?> Settings(IReadOnlyDictionary<string, string?>? original = null)
    {
        var result = new Dictionary<string, string?>(original ?? new Dictionary<string, string?>())
        {
            ["Wotr:Bots:RuntimeEnabled"] = "true", ["Wotr:Bots:RuntimeAssembly"] = null,
            ["Wotr:Bots:RuntimeVersion"] = Package.Description.RuntimeVersion,
            ["Wotr:Bots:RuntimeManifest"] = ManifestPath, ["Wotr:Bots:RuntimeManifestSha256"] = Hash,
            ["Wotr:Bots:AllowUnpackagedRuntime"] = "false"
        };
        return result;
    }

    public BotHostOptions Options(IReadOnlyDictionary<string, string?>? original = null) =>
        BotHostOptions.Read(new ConfigurationBuilder().AddInMemoryCollection(Settings(original)).Build());

    public async Task<(int ExitCode, string Output)> Operator(string connection, params string[] arguments)
    {
        var info = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            RedirectStandardOutput = true, RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location)!
        };
        info.ArgumentList.Add(typeof(Program).Assembly.Location);
        info.ArgumentList.Add("--bot-runtime");
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);
        info.Environment["WOTR_DB"] = connection;
        info.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        info.Environment["Logging__LogLevel__Default"] = "Warning";
        foreach (var setting in Settings())
            info.Environment[setting.Key.Replace(":", "__", StringComparison.Ordinal)] = setting.Value;
        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        try { await process.WaitForExitAsync(timeout.Token); }
        finally
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        Assert.True(string.IsNullOrEmpty(await error), await error);
        return (process.ExitCode, await output);
    }

    public void Dispose() => Directory.Delete(DirectoryPath, recursive: true);
}
