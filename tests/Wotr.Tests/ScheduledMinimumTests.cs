using System.Text;
using Wotr.Contracts;
using Wotr.Releases;
using Xunit;

namespace Wotr.Tests;

// DS21 (DIST-09): a minimum announced for a future time, and catalogs and manifests from before it.
public class ScheduledMinimumTests
{
    private static readonly TimeSpan Lead = ReleasePublisher.MinimumLead;

    [Fact]
    public void AScheduledMinimumAppliesFromItsTimeAndSettlesOnTheNextWrite()
    {
        using var releases = new ReleaseFixture();
        releases.Publish("0.3.0", minimum: true);
        releases.Publish("0.4.0");
        var at = releases.Clock.GetUtcNow() + TimeSpan.FromMinutes(10);

        var catalog = releases.Publisher.SetMinimum("0.4.0", at);
        Assert.Equal("0.3.0", catalog.MinimumVersion);
        Assert.Equal(("0.4.0", at), (catalog.ScheduledMinimum!.Version, catalog.ScheduledMinimum.EffectiveAt));
        Assert.Equal("0.3.0", catalog.EffectiveMinimum(at - TimeSpan.FromTicks(1)));
        Assert.Equal("0.4.0", catalog.EffectiveMinimum(at));

        releases.Clock.Advance(TimeSpan.FromMinutes(10));
        releases.Publish("0.5.0");
        var settled = CatalogFile.Read(releases.Layout);
        Assert.Equal("0.4.0", settled.MinimumVersion);
        Assert.Null(settled.ScheduledMinimum);
    }

    [Fact]
    public void SchedulesAreRefusedWhenTooSoonNotHigherOrNotAnnouncedWithoutChanges()
    {
        using var releases = new ReleaseFixture();
        releases.Publish("0.3.0", minimum: true);
        releases.Publish("0.4.0");
        var now = releases.Clock.GetUtcNow();

        AssertRefused(releases, () => releases.Publisher.SetMinimum("0.4.0", now + Lead - TimeSpan.FromSeconds(1)), "at least 5 minutes");
        AssertRefused(releases, () => releases.Publisher.SetMinimum("0.4.0", now - TimeSpan.FromHours(1)), "at least 5 minutes");
        AssertRefused(releases, () => releases.Publisher.SetMinimum("0.3.0", now + Lead), "higher than the current minimum");
        AssertRefused(releases, () => releases.Publisher.SetMinimum("0.9.0", now + Lead), "not announced");
        AssertRefused(releases, () => releases.Publisher.CancelScheduledMinimum(), "No minimum is scheduled");
        releases.Upload("0.5.0");
        AssertRefused(releases, () => releases.Publisher.Publish("0.5.0", makeMinimum: false, now + Lead), "needs --minimum");
        Assert.False(Directory.Exists(releases.Layout.Published("0.5.0")));
    }

    [Fact]
    public void TheScheduledReleaseCannotBeWithdrawnUntilTheScheduleIsCancelled()
    {
        using var releases = new ReleaseFixture();
        releases.Publish("0.3.0", minimum: true);
        releases.Publish("0.4.0");
        releases.Publisher.SetMinimum("0.4.0", releases.Clock.GetUtcNow() + Lead);

        AssertRefused(releases, () => releases.Publisher.Withdraw("0.4.0"), "scheduled minimum");
        Assert.Null(releases.Publisher.CancelScheduledMinimum().ScheduledMinimum);
        Assert.True(releases.Publisher.Withdraw("0.4.0").Find("0.4.0")!.Withdrawn);
    }

    [Fact]
    public void AnImmediateMinimumReplacesALowerOrEqualScheduleAndKeepsAHigherOne()
    {
        using var releases = new ReleaseFixture();
        releases.Publish("0.3.0", minimum: true);
        releases.Publish("0.4.0");
        releases.Upload("0.5.0");
        var at = releases.Clock.GetUtcNow() + TimeSpan.FromHours(1);

        // Publishing straight to a scheduled minimum announces the release first, then schedules it.
        var scheduled = releases.Publisher.Publish("0.5.0", makeMinimum: true, at);
        Assert.Equal(("0.3.0", "0.5.0"), (scheduled.MinimumVersion, scheduled.ScheduledMinimum!.Version));

        var lower = releases.Publisher.SetMinimum("0.4.0");
        Assert.Equal(("0.4.0", "0.5.0"), (lower.MinimumVersion, lower.ScheduledMinimum!.Version));

        var equal = releases.Publisher.SetMinimum("0.5.0");
        Assert.Equal("0.5.0", equal.MinimumVersion);
        Assert.Null(equal.ScheduledMinimum);
    }

    [Fact]
    public void ASchemaOneCatalogStaysReadableAndIsWrittenAsSchemaTwo()
    {
        using var releases = new ReleaseFixture();
        releases.Publish("0.3.0", minimum: true);
        var schemaOne = File.ReadAllText(releases.Layout.CatalogPath)
            .Replace("\"schema\": 2", "\"schema\": 1")
            .Replace("  \"scheduledMinimum\": null,\n", "");
        Assert.DoesNotContain("scheduledMinimum", schemaOne);
        File.WriteAllText(releases.Layout.CatalogPath, schemaOne);

        Assert.Equal(ReleaseCatalog.FirstSchema, CatalogFile.Read(releases.Layout).Schema);
        releases.Publish("0.4.0");
        Assert.Equal(ReleaseCatalog.CurrentSchema, CatalogFile.Read(releases.Layout).Schema);
    }

    [Fact]
    public void CatalogRulesRejectInconsistentSchedules()
    {
        using var releases = new ReleaseFixture();
        releases.Publish("0.3.0", minimum: true);
        releases.Publish("0.4.0");
        var catalog = CatalogFile.Read(releases.Layout);
        var at = releases.Clock.GetUtcNow() + TimeSpan.FromHours(1);

        catalog.ScheduledMinimum = new ScheduledMinimum { Version = "0.3.0", EffectiveAt = at };
        Assert.Contains("higher than the current minimum", Assert.Throws<ReleaseRejected>(() => CatalogRules.Check(catalog)).Message);
        catalog.ScheduledMinimum = new ScheduledMinimum { Version = "0.9.0", EffectiveAt = at };
        Assert.Contains("announced release", Assert.Throws<ReleaseRejected>(() => CatalogRules.Check(catalog)).Message);
        catalog.ScheduledMinimum = new ScheduledMinimum { Version = "0.4.0", EffectiveAt = at.ToOffset(TimeSpan.FromHours(2)) };
        Assert.Contains("UTC", Assert.Throws<ReleaseRejected>(() => CatalogRules.Check(catalog)).Message);
        catalog.ScheduledMinimum = new ScheduledMinimum { Version = "0.4.0", EffectiveAt = at };
        catalog.Schema = ReleaseCatalog.FirstSchema;
        Assert.Contains("schema 2", Assert.Throws<ReleaseRejected>(() => CatalogRules.Check(catalog)).Message);
    }

    // DIST-05 uploads had no installer; their signed manifests stay valid after the schema change.
    [Fact]
    public void ASchemaOneManifestWithoutAnInstallerIsStillPublished()
    {
        using var releases = new ReleaseFixture();
        foreach (var platform in ClientPlatforms.All)
            UploadSchemaOne(releases, "0.3.0", platform);

        Assert.Equal("0.3.0", releases.Publisher.Publish("0.3.0", makeMinimum: true).MinimumVersion);
        releases.Publisher.Verify();

        var manifest = ReleaseJson.Parse<ReleaseManifest>(File.ReadAllBytes(releases.Published("0.3.0", ClientPlatforms.Windows,
            ReleaseManifest.FileName)), "test");
        Assert.Equal((ReleaseManifest.FirstSchema, (ReleasePackage?)null), (manifest.Schema, manifest.Installer));
    }

    [Fact]
    public void AnUploadWhoseInstallerIsMissingAlteredOrStrayIsRefused()
    {
        using var releases = new ReleaseFixture();
        releases.Upload("0.3.0");
        var installer = releases.Incoming("0.3.0", ClientPlatforms.MacOS, ReleaseFixture.InstallerName("0.3.0", ClientPlatforms.MacOS));
        var bytes = File.ReadAllBytes(installer);

        File.WriteAllBytes(installer, bytes.Reverse().ToArray());
        AssertRefused(releases, () => releases.Publisher.Publish("0.3.0", makeMinimum: false), "installer SHA-256");
        File.Delete(installer);
        AssertRefused(releases, () => releases.Publisher.Publish("0.3.0", makeMinimum: false), "only regular files");
        File.WriteAllBytes(installer, bytes);
        Assert.Single(releases.Publisher.Publish("0.3.0", makeMinimum: false).Announced);
    }

    private static void UploadSchemaOne(ReleaseFixture releases, string version, string platform)
    {
        var folder = releases.Incoming(version, platform);
        Directory.CreateDirectory(folder);
        var name = ReleaseFixture.PackageName(version, platform);
        var package = Path.Combine(folder, name);
        File.WriteAllText(package, $"schema one {version} {platform}");
        var manifest = new ReleaseManifest
        {
            Schema = ReleaseManifest.FirstSchema,
            Version = version,
            Platform = platform,
            ProtocolVersion = Protocol.Version,
            SourceCommit = ReleaseFixture.Commit,
            Package = new ReleasePackage
            {
                FileName = name,
                Url = $"{ReleaseFixture.DownloadsUrl}releases/{version}/{platform}/{name}",
                Size = new FileInfo(package).Length,
                Sha256 = ReleaseJson.Sha256(package)
            },
            Notes = "Before installers.",
            CreatedAt = releases.Clock.GetUtcNow()
        };
        var bytes = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(ReleaseJson.Serialize(manifest)).Replace("  \"installer\": null,\n", ""));
        File.WriteAllBytes(Path.Combine(folder, ReleaseManifest.FileName), bytes);
        File.WriteAllBytes(Path.Combine(folder, ReleaseManifest.SignatureFileName), ManifestSignature.Create(bytes, releases.Key));
    }

    private static void AssertRefused(ReleaseFixture releases, Action command, string reason)
    {
        var before = File.Exists(releases.Layout.CatalogPath) ? File.ReadAllBytes(releases.Layout.CatalogPath) : null;
        Assert.Contains(reason, Assert.Throws<ReleaseRejected>(command).Message);
        Assert.Equal(before, File.Exists(releases.Layout.CatalogPath) ? File.ReadAllBytes(releases.Layout.CatalogPath) : null);
    }
}
