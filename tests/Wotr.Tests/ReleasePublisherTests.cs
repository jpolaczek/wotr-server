using System.Text;
using Wotr.Contracts;
using Wotr.Releases;
using Xunit;

namespace Wotr.Tests;

public class ReleasePublisherTests
{
    private const string Windows = ClientPlatforms.Windows;
    private const string Mac = ClientPlatforms.MacOS;

    [Fact]
    public void CompleteSignedUploadIsMovedAndAnnouncedAtomically()
    {
        using var releases = new ReleaseFixture();
        releases.Upload("0.3.0");
        var catalog = releases.Publisher.Publish("0.3.0", makeMinimum: true);
        Assert.Equal(1, catalog.Revision);
        Assert.Equal("0.3.0", catalog.MinimumVersion);
        Assert.False(Directory.Exists(releases.Layout.Incoming("0.3.0")));
        foreach (var platform in ClientPlatforms.All)
            Assert.True(File.Exists(releases.Published("0.3.0", platform, ReleaseFixture.PackageName("0.3.0", platform))));
        Assert.DoesNotContain("announced", File.ReadAllText(releases.Layout.CatalogPath));
        var stored = CatalogFile.Read(releases.Layout);
        Assert.Equal(ClientPlatforms.All.Order(), stored.Releases.Single().Manifests.Keys.Order());
        Assert.Equal(Protocol.Version, stored.Releases.Single().ProtocolVersion);
        releases.Publisher.Verify();
    }

    [Fact]
    public void IncompleteUploadMissingPlatformAndExtraFilesAreRefusedWithoutChanges()
    {
        using var releases = new ReleaseFixture();
        releases.Upload("0.3.0");
        var package = releases.Incoming("0.3.0", Windows, ReleaseFixture.PackageName("0.3.0", Windows));
        var complete = File.ReadAllBytes(package);
        File.WriteAllBytes(package, complete[..^4]);
        AssertRefused(releases, "0.3.0", "incomplete");
        File.WriteAllBytes(package, complete);
        File.WriteAllText(releases.Incoming("0.3.0", Windows, "notes.txt"), "stray");
        AssertRefused(releases, "0.3.0", "only regular files");
        File.Delete(releases.Incoming("0.3.0", Windows, "notes.txt"));

        releases.Upload("0.4.0", platforms: Windows);
        AssertRefused(releases, "0.4.0", "exactly the folders");
        AssertRefused(releases, "0.5.0", "no uploaded folder");
        releases.Publisher.Publish("0.3.0", makeMinimum: false);
    }

    [Fact]
    public void AlteredManifestOtherPlatformHashForeignKeyAndPathAreRefused()
    {
        using var releases = new ReleaseFixture();
        releases.Upload("0.3.0");
        var manifest = releases.Incoming("0.3.0", Windows, ReleaseManifest.FileName);
        var original = File.ReadAllBytes(manifest);
        var altered = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(original).Replace("Changes in", "Changed in"));
        File.WriteAllBytes(manifest, altered);
        AssertRefused(releases, "0.3.0", "signature does not match");
        File.WriteAllBytes(manifest, original);

        // A validly signed manifest of the other platform cannot stand in for this one.
        File.Copy(releases.Incoming("0.3.0", Mac, ReleaseManifest.FileName), manifest, overwrite: true);
        File.Copy(releases.Incoming("0.3.0", Mac, ReleaseManifest.SignatureFileName), releases.Incoming("0.3.0", Windows, ReleaseManifest.SignatureFileName), overwrite: true);
        AssertRefused(releases, "0.3.0", "another platform");

        releases.Upload("0.4.0");
        var package = releases.Incoming("0.4.0", Mac, ReleaseFixture.PackageName("0.4.0", Mac));
        var bytes = File.ReadAllBytes(package);
        bytes[0] ^= 1;
        File.WriteAllBytes(package, bytes);
        AssertRefused(releases, "0.4.0", "SHA-256 differs");

        releases.Upload("0.5.0", key: ReleasePrivateKey.Generate());
        AssertRefused(releases, "0.5.0", "another key");
        releases.Upload("0.6.0", downloadsUrl: "https://mirror.example.test/");
        AssertRefused(releases, "0.6.0", "outside the approved downloads path");
        releases.Upload("0.7.0", protocol: Protocol.Version + 1);
        AssertRefused(releases, "0.7.0", "this server supports");
    }

    [Fact]
    public void MinimumNeverOutrunsAnnouncedIntactPackagesAndWithdrawalKeepsFiles()
    {
        using var releases = new ReleaseFixture();
        releases.Publish("0.3.0", minimum: true);
        releases.Publish("0.4.0");
        Assert.Contains("already in the catalog", Assert.Throws<ReleaseRejected>(() => releases.Publish("0.4.0")).Message);
        Assert.Contains("not announced", Assert.Throws<ReleaseRejected>(() => releases.Publisher.SetMinimum("0.5.0")).Message);
        Assert.Contains("is the minimum", Assert.Throws<ReleaseRejected>(() => releases.Publisher.Withdraw("0.3.0")).Message);

        var package = releases.Published("0.4.0", Mac, ReleaseFixture.PackageName("0.4.0", Mac));
        var bytes = File.ReadAllBytes(package);
        File.Delete(package);
        Assert.Contains("incomplete", Assert.Throws<ReleaseRejected>(() => releases.Publisher.SetMinimum("0.4.0")).Message);
        Assert.Equal("0.3.0", CatalogFile.Read(releases.Layout).MinimumVersion);
        File.WriteAllBytes(package, bytes);
        Assert.Equal("0.4.0", releases.Publisher.SetMinimum("0.4.0").MinimumVersion);
        Assert.Equal("0.3.0", releases.Publisher.SetMinimum("0.3.0").MinimumVersion);

        var withdrawn = releases.Publisher.Withdraw("0.4.0");
        Assert.Equal(["0.3.0"], withdrawn.Announced.Select(release => release.Version));
        Assert.True(File.Exists(package));
        Assert.Contains("not announced", Assert.Throws<ReleaseRejected>(() => releases.Publisher.SetMinimum("0.4.0")).Message);
        Assert.Contains("not announced", Assert.Throws<ReleaseRejected>(() => releases.Publisher.Withdraw("0.4.0")).Message);
        Assert.Equal(5, CatalogFile.Read(releases.Layout).Revision);
    }

    [Fact]
    public void InterruptedPublishResumesAfterTheMoveAndAWriterHoldsTheLock()
    {
        using var releases = new ReleaseFixture();
        releases.Upload("0.3.0");
        Directory.CreateDirectory(Path.GetDirectoryName(releases.Layout.Published("0.3.0"))!);
        Directory.Move(releases.Layout.Incoming("0.3.0"), releases.Layout.Published("0.3.0"));
        Assert.Equal("0.3.0", releases.Publisher.Publish("0.3.0", makeMinimum: false).Releases.Single().Version);

        releases.Upload("0.4.0");
        using (CatalogLock.Acquire(releases.Layout))
            Assert.Contains("Another release command", Assert.Throws<ReleaseRejected>(() => releases.Publisher.Publish("0.4.0", false)).Message);
        Assert.True(Directory.Exists(releases.Layout.Incoming("0.4.0")));
        releases.Publisher.Publish("0.4.0", false);
    }

    [Fact]
    public async Task ConcurrentReadersOnlyEverSeeACompleteCatalog()
    {
        using var releases = new ReleaseFixture();
        string[] versions = ["0.3.0", "0.4.0", "0.5.0", "0.6.0", "0.7.0", "0.8.0"];
        foreach (var version in versions)
            releases.Upload(version);
        using var stop = new CancellationTokenSource();
        var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(() => Read(releases.Layout, stop.Token))).ToList();
        foreach (var version in versions)
            releases.Publisher.Publish(version, makeMinimum: true);
        await stop.CancelAsync();
        foreach (var observed in await Task.WhenAll(readers))
        {
            Assert.NotEmpty(observed);
            Assert.Equal(observed.Order(), observed);
        }
        Assert.Equal(versions.Length, CatalogFile.Read(releases.Layout).Revision);
    }

    // Returns the catalog revisions one reader saw, in order; any partial or invalid file fails the test.
    private static List<int> Read(DistributionLayout layout, CancellationToken stop)
    {
        var observed = new List<int>();
        while (!stop.IsCancellationRequested || observed.Count == 0)
        {
            if (!File.Exists(layout.CatalogPath))
                continue;
            observed.Add(CatalogFile.Parse(File.ReadAllBytes(layout.CatalogPath)).Revision);
        }
        return observed;
    }

    private static void AssertRefused(ReleaseFixture releases, string version, string reason)
    {
        var before = File.Exists(releases.Layout.CatalogPath) ? File.ReadAllBytes(releases.Layout.CatalogPath) : null;
        var error = Assert.Throws<ReleaseRejected>(() => releases.Publisher.Publish(version, makeMinimum: true));
        Assert.Contains(reason, error.Message);
        Assert.Equal(before, File.Exists(releases.Layout.CatalogPath) ? File.ReadAllBytes(releases.Layout.CatalogPath) : null);
        Assert.False(Directory.Exists(releases.Layout.Published(version)));
    }
}
