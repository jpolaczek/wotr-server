using System.Net;
using System.Net.Http.Json;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Wotr.Releases;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// DS12: the public catalog before login, per platform, reloaded without a redeploy.
public class ClientReleaseCatalogTests
{
    [PostgresFact]
    public async Task CatalogIsPublicPerPlatformAndReportsCompatibility()
    {
        using var releases = new ReleaseFixture();
        await using var host = new TestHost(settings: releases.ServerSettings());
        using var anonymous = host.Client();

        var empty = await Read(anonymous, "platform=windows-x64");
        Assert.Null(empty.Latest);
        Assert.Equal("", empty.MinimumVersion);
        Assert.Equal(ClientCompatibilityStatus.Unknown, empty.Status);
        Assert.Equal((2, Protocol.Version), (empty.MinimumProtocolVersion, empty.MaximumProtocolVersion));

        releases.Publish("0.3.0", minimum: true);
        foreach (var platform in ClientPlatforms.All)
        {
            using var response = await anonymous.GetAsync($"/api/client/releases?platform={platform}");
            Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
            var view = (await response.Content.ReadFromJsonAsync<ClientReleaseView>(BoardCatalog.Json))!;
            Assert.Equal(platform, view.Platform);
            Assert.Equal("0.3.0", view.MinimumVersion);
            Assert.Equal("0.3.0", view.Latest!.Version);
            Assert.Equal($"{ReleaseFixture.DownloadsUrl}releases/0.3.0/{platform}/manifest.json", view.Latest.ManifestUrl);
            Assert.Equal($"{ReleaseFixture.DownloadsUrl}releases/0.3.0/{platform}/manifest.json.sig", view.Latest.SignatureUrl);
        }
        Assert.Equal(ClientCompatibilityStatus.ClientUpdateRequired, (await Read(anonymous, "platform=macos-universal&version=0.2.0&protocol=2")).Status);
        Assert.Equal(ClientCompatibilityStatus.ServerUpdateRequired, (await Read(anonymous, "platform=macos-universal&version=0.3.0&protocol=3")).Status);
        Assert.Equal(ClientCompatibilityStatus.Compatible, (await Read(anonymous, "platform=macos-universal&version=0.3.0&protocol=2")).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.GetAsync("/api/client/releases?platform=linux-arm64")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.GetAsync("/api/client/releases?platform=windows-x64&version=latest&protocol=2")).StatusCode);
    }

    [PostgresFact]
    public async Task NewAndWithdrawnReleasesApplyWithoutRestartAndCarryNoSessionData()
    {
        using var releases = new ReleaseFixture();
        releases.Publish("0.3.0", minimum: true);
        // A frozen clock keeps serverTime equal, so both responses can be compared byte for byte.
        await using var host = new TestHost(settings: releases.ServerSettings(), clock: releases.Clock);
        using var anonymous = host.Client();
        using var alice = host.Client("alice");
        Assert.Equal("0.3.0", (await Read(anonymous, "platform=windows-x64")).Latest!.Version);

        releases.Publish("0.4.0");
        Assert.Equal("0.4.0", (await Read(anonymous, "platform=windows-x64")).Latest!.Version);
        var signedIn = await alice.GetAsync("/api/client/releases?platform=windows-x64");
        var raw = await signedIn.Content.ReadAsStringAsync();
        Assert.Equal(await (await anonymous.GetAsync("/api/client/releases?platform=windows-x64")).Content.ReadAsStringAsync(), raw);
        Assert.DoesNotContain("alice", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", raw, StringComparison.OrdinalIgnoreCase);
        Assert.False(signedIn.Headers.Contains("Set-Cookie"));
        // Every announced file lives on the separate downloads origin, so fetching it never needs the game session.
        Assert.All(new[] { (await Read(anonymous, "platform=windows-x64")).Latest!.ManifestUrl },
            url => Assert.StartsWith(ReleaseFixture.DownloadsUrl, url));

        releases.Publisher.Withdraw("0.4.0");
        Assert.Equal("0.3.0", (await Read(anonymous, "platform=windows-x64")).Latest!.Version);
    }

    [PostgresFact]
    public async Task ACatalogThatFailsVerificationNeverReplacesTheLastValidOne()
    {
        using var releases = new ReleaseFixture();
        releases.Publish("0.3.0", minimum: true);
        await using var host = new TestHost(settings: releases.ServerSettings());
        using var anonymous = host.Client();
        Assert.Equal("0.3.0", (await Read(anonymous, "platform=windows-x64")).Latest!.Version);

        releases.Upload("0.4.0");
        File.Delete(releases.Incoming("0.4.0", ClientPlatforms.MacOS, ReleaseFixture.PackageName("0.4.0", ClientPlatforms.MacOS)));
        Assert.Throws<ReleaseRejected>(() => releases.Publisher.Publish("0.4.0", makeMinimum: true));
        Assert.Equal("0.3.0", (await Read(anonymous, "platform=windows-x64")).Latest!.Version);

        // A hand-edited catalog that announces a manifest whose bytes changed after publication is ignored.
        releases.Publish("0.5.0");
        var manifest = releases.Published("0.5.0", ClientPlatforms.Windows, ReleaseManifest.FileName);
        File.AppendAllText(manifest, " ");
        var catalog = CatalogFile.Read(releases.Layout);
        catalog.Revision++;
        CatalogFile.Write(releases.Layout, catalog);
        var view = await Read(anonymous, "platform=windows-x64");
        Assert.Equal("0.3.0", view.Latest!.Version);
        Assert.Equal("0.3.0", view.MinimumVersion);
    }

    // LNX-01 (D18): Linux is a full platform — a release without its AppImage or package is not announced.
    [Fact]
    public void AReleaseWithoutItsLinuxFilesIsRefused()
    {
        using var releases = new ReleaseFixture();
        releases.Upload("0.4.0");
        File.Delete(releases.Incoming("0.4.0", ClientPlatforms.Linux, ReleaseFixture.InstallerName("0.4.0", ClientPlatforms.Linux)));
        Assert.Throws<ReleaseRejected>(() => releases.Publisher.Publish("0.4.0", makeMinimum: false));
        releases.Upload("0.5.0");
        Assert.Equal(3, releases.Publisher.Publish("0.5.0", makeMinimum: false).Releases.Single(r => r.Version == "0.5.0").Manifests.Count);
    }

    private static async Task<ClientReleaseView> Read(HttpClient client, string query) =>
        (await client.GetFromJsonAsync<ClientReleaseView>("/api/client/releases?" + query, BoardCatalog.Json))!;
}
