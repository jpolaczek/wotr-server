using Microsoft.Extensions.Configuration;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public sealed class BotRuntimePackageTests
{
    [Theory]
    [InlineData(false, "0.2.0")]
    [InlineData(true, "0.2.1")]
    public void VerifiedPackageLoadsItsActualVersion(bool upgrade, string version)
    {
        using var package = new BotPackageFixture(upgrade);
        using var runtime = EmbeddedBotRuntime.LoadConfigured(package.Options());
        Assert.Equal("Loaded", runtime.Status);
        Assert.Equal(version, runtime.Description!.RuntimeVersion);
        Assert.Equal("fixture.memory/1", runtime.Description.MemoryFormat);
    }

    [Theory]
    [InlineData("digest")]
    [InlineData("library")]
    [InlineData("missing")]
    [InlineData("symbols")]
    [InlineData("extra")]
    [InlineData("path")]
    [InlineData("metadata")]
    [InlineData("memory")]
    [InlineData("symlink")]
    public void DamagedOrUnreviewedPackageCannotReadyAnyRoom(string fault)
    {
        using var package = new BotPackageFixture();
        var options = package.Options();
        switch (fault)
        {
            case "digest": options = options with { ManifestSha256 = new string('0', 64) }; break;
            case "library": File.AppendAllText(package.Library, "changed"); break;
            case "missing": File.Delete(package.Library); break;
            case "symbols": File.WriteAllText(Path.Combine(package.DirectoryPath, "Wotr.Bot.Runtime.pdb"), "private"); break;
            case "extra": File.WriteAllText(Path.Combine(package.DirectoryPath, "unexpected.dll"), "private"); break;
            case "path": package.Rewrite(package.Package with { EntryAssembly = "../outside.dll" }); options = package.Options(); break;
            case "metadata": package.Rewrite(package.Package with { Description = package.Package.Description with { MemoryFormat = "fixture.memory/2" } }); options = package.Options(); break;
            case "memory": package.Rewrite(package.Package with { Description = package.Package.Description with { MemoryFormat = null } }); options = package.Options(); break;
            case "symlink": File.Delete(package.Library); File.CreateSymbolicLink(package.Library, BotHostFixture.Library); break;
        }
        using var runtime = EmbeddedBotRuntime.LoadConfigured(options);
        Assert.Equal("PackageInvalid", runtime.Status);
        Assert.False(runtime.GameplayReady);
        Assert.Null(runtime.Description);
    }

    [Fact]
    public void ProductionLoadingRequiresPackageAndExplicitPinWhileDevelopmentOptInIsSeparate()
    {
        using var package = new BotPackageFixture();
        var options = package.Options() with { ManifestPath = null, AssemblyPath = BotHostFixture.Library };
        using var refused = EmbeddedBotRuntime.LoadConfigured(options);
        Assert.Equal("PackageRequired", refused.Status);
        using var development = EmbeddedBotRuntime.LoadConfigured(options with { AllowUnpackaged = true });
        Assert.Equal("Loaded", development.Status);
        using var wrongVersion = EmbeddedBotRuntime.LoadConfigured(package.Options() with { RuntimeVersion = "0.3.0" });
        Assert.Equal("Incompatible", wrongVersion.Status);
    }

    [Theory]
    [InlineData("Workers", "9")]
    [InlineData("MaximumWork", "2")]
    [InlineData("MaximumWork", "100001")]
    [InlineData("ManagedMemoryLimitMiB", "63")]
    [InlineData("ManagedMemoryLimitMiB", "9223372036854775807")]
    [InlineData("DecisionTimeoutMilliseconds", "5001")]
    public void UnsafeHostLimitsAreRejectedBeforeScheduling(string name, string value)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Wotr:Bots:" + name] = value }).Build();
        Assert.Throws<InvalidOperationException>(() => BotHostOptions.Read(configuration));
    }

    [Fact]
    public void MetricsHaveFixedLabelsAndDoNotExposeArbitraryRuntimeReasons()
    {
        using var package = new BotPackageFixture();
        using var metrics = new BotHostMetrics(package.Options());
        metrics.Outcome("secret-hand-or-account");
        Assert.Equal(new Dictionary<string, long> { ["RuntimeRefused"] = 1 }, metrics.Snapshot().Outcomes);
    }
}
