using System.Net;
using System.Net.Http.Json;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Wotr.Releases;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// DIST-09 on the running server: the scheduled minimum by the clock, the repair offer and the installers.
public class ClientReleaseScheduleTests
{
    [PostgresFact]
    public async Task TheGateAndTheCatalogSwitchToAScheduledMinimumExactlyAtItsTime()
    {
        using var releases = new ReleaseFixture();
        releases.Publish("0.3.0", minimum: true);
        releases.Publish("0.4.0");
        var at = releases.Clock.GetUtcNow() + TimeSpan.FromMinutes(10);
        releases.Publisher.SetMinimum("0.4.0", at);
        await using var host = new TestHost(settings: releases.ServerSettings(), clock: releases.Clock);
        using var old = ReleaseFixture.Versioned(host.Client(), "0.3.0");

        var before = await Read(old, "platform=windows-x64&version=0.3.0&protocol=2");
        Assert.Equal(("0.3.0", ClientCompatibilityStatus.Compatible), (before.MinimumVersion, before.Status));
        Assert.Equal(("0.4.0", at), (before.ScheduledMinimum!.Version, before.ScheduledMinimum.EffectiveAt));
        Assert.Equal(releases.Clock.GetUtcNow(), before.ServerTime);
        Assert.Equal(HttpStatusCode.Unauthorized, (await old.GetAsync("/api/rooms")).StatusCode);

        releases.Clock.Advance(TimeSpan.FromMinutes(10) - TimeSpan.FromSeconds(1));
        Assert.Equal(HttpStatusCode.Unauthorized, (await old.GetAsync("/api/rooms")).StatusCode);

        releases.Clock.Advance(TimeSpan.FromSeconds(1));
        using var refused = await old.GetAsync("/api/rooms");
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal(ClientCompatibility.ClientUpdateRequired, (await refused.Content.ReadFromJsonAsync<ApiError>())!.Error);
        var after = await Read(old, "platform=windows-x64&version=0.3.0&protocol=2");
        Assert.Equal(("0.4.0", ClientCompatibilityStatus.ClientUpdateRequired), (after.MinimumVersion, after.Status));
        Assert.Null(after.ScheduledMinimum);
        using var current = ReleaseFixture.Versioned(host.Client(), "0.4.0");
        Assert.Equal(HttpStatusCode.Unauthorized, (await current.GetAsync("/api/rooms")).StatusCode);
    }

    [PostgresFact]
    public async Task TheInstalledOfferNamesTheDeclaredReleaseUntilItIsWithdrawn()
    {
        using var releases = new ReleaseFixture();
        releases.Publish("0.3.0", minimum: true);
        releases.Publish("0.4.0");
        await using var host = new TestHost(settings: releases.ServerSettings(), clock: releases.Clock);
        using var anonymous = host.Client();

        var installed = (await Read(anonymous, "platform=macos-universal&version=0.4.0&protocol=2")).Installed!;
        Assert.Equal("0.4.0", installed.Version);
        Assert.Equal($"{ReleaseFixture.DownloadsUrl}releases/0.4.0/macos-universal/manifest.json", installed.ManifestUrl);
        Assert.Equal("0.3.0", (await Read(anonymous, "platform=macos-universal&version=0.3.0&protocol=2")).Installed!.Version);
        Assert.Null((await Read(anonymous, "platform=macos-universal&version=0.9.0&protocol=2")).Installed);
        Assert.Null((await Read(anonymous, "platform=macos-universal")).Installed);

        releases.Publisher.Withdraw("0.4.0");
        Assert.Null((await Read(anonymous, "platform=macos-universal&version=0.4.0&protocol=2")).Installed);
    }

    [PostgresFact]
    public async Task TheGamePageListsInstallersOfTheNewestReleaseWithoutClientHeaders()
    {
        using var releases = new ReleaseFixture();
        await using var host = new TestHost(settings: releases.ServerSettings(), clock: releases.Clock);
        using var browser = host.Client();

        using var empty = await browser.GetAsync(ClientReleaseEndpoints.DownloadsPath);
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        Assert.Equal("no-store", empty.Headers.CacheControl!.ToString());
        Assert.Null((await empty.Content.ReadFromJsonAsync<ClientDownloadsView>(BoardCatalog.Json))!.Latest);

        releases.Publish("0.3.0", minimum: true);
        releases.Publish("0.4.0");
        var latest = (await browser.GetFromJsonAsync<ClientDownloadsView>(ClientReleaseEndpoints.DownloadsPath, BoardCatalog.Json))!.Latest!;
        Assert.Equal("0.4.0", latest.Version);
        Assert.Equal(ClientPlatforms.All.Order(), latest.Installers.Select(installer => installer.Platform).Order());
        foreach (var installer in latest.Installers)
        {
            var path = releases.Published("0.4.0", installer.Platform, installer.FileName);
            Assert.Equal(ReleaseFixture.InstallerName("0.4.0", installer.Platform), installer.FileName);
            Assert.Equal($"{ReleaseFixture.DownloadsUrl}releases/0.4.0/{installer.Platform}/{installer.FileName}", installer.Url);
            Assert.Equal((new FileInfo(path).Length, ReleaseJson.Sha256(path)), (installer.Size, installer.Sha256));
        }

        releases.Publisher.Withdraw("0.4.0");
        Assert.Equal("0.3.0", (await browser.GetFromJsonAsync<ClientDownloadsView>(ClientReleaseEndpoints.DownloadsPath, BoardCatalog.Json))!
            .Latest!.Version);
    }

    // DIST-15: the game site's What's new page reads the notes signed into the announced releases' manifests, newest
    // first, without client version headers; a withdrawn release leaves the page.
    [PostgresFact]
    public async Task TheWhatsNewPageListsTheSignedNotesNewestFirst()
    {
        using var releases = new ReleaseFixture();
        await using var host = new TestHost(settings: releases.ServerSettings(), clock: releases.Clock);
        using var browser = host.Client();

        using var empty = await browser.GetAsync(ClientReleaseEndpoints.NotesPath);
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        Assert.Equal("no-cache", empty.Headers.CacheControl!.ToString());
        Assert.Empty((await empty.Content.ReadFromJsonAsync<ClientNotesView>(BoardCatalog.Json))!.Releases);

        releases.Publish("0.3.0", minimum: true);
        releases.Publish("0.4.0");
        var notes = await Notes(browser);
        Assert.Equal(["0.4.0", "0.3.0"], notes.Select(note => note.Version));
        Assert.Equal(["Changes in 0.4.0.", "Changes in 0.3.0."], notes.Select(note => note.Notes));
        Assert.All(notes, note => Assert.Equal(releases.Clock.GetUtcNow(), note.PublishedAt));

        releases.Publisher.Withdraw("0.4.0");
        Assert.Equal(["0.3.0"], (await Notes(browser)).Select(note => note.Version));
    }

    private static async Task<IReadOnlyList<ClientReleaseNote>> Notes(HttpClient client) =>
        (await client.GetFromJsonAsync<ClientNotesView>(ClientReleaseEndpoints.NotesPath, BoardCatalog.Json))!.Releases;

    private static async Task<ClientReleaseView> Read(HttpClient client, string query) =>
        (await client.GetFromJsonAsync<ClientReleaseView>(ClientReleaseEndpoints.Path + "?" + query, BoardCatalog.Json))!;
}
