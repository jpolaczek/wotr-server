using Wotr.Contracts;
using Wotr.Releases;

namespace Wotr.Server;

// One announced release this server can serve: its offers per platform, from schema 2 its installers, and the notes
// of its signed manifests (DIST-15).
public sealed record SnapshotRelease(ClientVersion Version, DateTimeOffset PublishedAt,
    IReadOnlyDictionary<string, ClientReleaseOffer> Offers, IReadOnlyList<ClientInstallerOffer> Installers, string Notes = "");

// DIST-15: one release on the game site's What's new page (GET /api/client/notes).
public sealed record ClientReleaseNote(string Version, DateTimeOffset PublishedAt, string Notes);

public sealed record ClientNotesView(IReadOnlyList<ClientReleaseNote> Releases);

public sealed record SnapshotSchedule(ClientVersion Version, DateTimeOffset EffectiveAt);

// What the current catalog announces to clients this server can serve.
public sealed class ReleaseSnapshot(int revision, ClientVersion? minimum, SnapshotSchedule? scheduled, IReadOnlyList<SnapshotRelease> newestFirst)
{
    public static readonly ReleaseSnapshot Empty = new(0, null, null, []);

    public int Revision { get; } = revision;

    // DS21: the scheduled minimum applies from its time without waiting for the catalog file to change.
    public ClientVersion? Minimum(DateTimeOffset now) => scheduled is { } next && now >= next.EffectiveAt ? next.Version : minimum;

    public SnapshotSchedule? Upcoming(DateTimeOffset now) => scheduled is { } next && now < next.EffectiveAt ? next : null;

    public ClientReleaseOffer? Latest(string platform) => newestFirst.Count == 0 ? null : newestFirst[0].Offers[platform];

    public ClientReleaseOffer? Release(string platform, ClientVersion version) =>
        newestFirst.FirstOrDefault(release => release.Version.CompareTo(version) == 0)?.Offers[platform];

    // The game page offers the newest release that has installers; schema 1 releases have none.
    public ClientDownloadsRelease? LatestInstallers() => newestFirst
        .Where(release => release.Installers.Count == ClientPlatforms.All.Count)
        .Select(release => new ClientDownloadsRelease
        {
            Version = release.Version.Text,
            PublishedAt = release.PublishedAt,
            Installers = release.Installers.ToList()
        })
        .FirstOrDefault();

    // DIST-15: the notes of the newest announced releases, newest first, for the What's new page.
    public IReadOnlyList<ClientReleaseNote> Notes(int limit) => newestFirst
        .Take(limit)
        .Select(release => new ClientReleaseNote(release.Version.Text, release.PublishedAt, release.Notes))
        .ToList();
}

// DS12: reloads catalog.json when its bytes change, at most once per refresh interval, without a redeploy.
// A catalog that fails verification never replaces the last valid one.
public sealed class ReleaseCatalogStore(ClientReleaseOptions options, TimeProvider clock, ILogger<ReleaseCatalogStore> logger)
{
    private readonly Lock gate = new();
    private ReleaseSnapshot current = ReleaseSnapshot.Empty;
    private string checkedSha256 = "";
    private long nextCheck = long.MinValue;

    public ReleaseSnapshot Current()
    {
        if (options.Layout is not { } layout || options.Trust is not { } trust)
            return ReleaseSnapshot.Empty;
        if (clock.GetTimestamp() < Volatile.Read(ref nextCheck))
            return Volatile.Read(ref current);
        lock (gate)
        {
            if (clock.GetTimestamp() >= nextCheck)
            {
                Reload(layout, trust);
                Volatile.Write(ref nextCheck, clock.GetTimestamp() + (long)(options.Refresh.TotalSeconds * clock.TimestampFrequency));
            }
            return current;
        }
    }

    private void Reload(DistributionLayout layout, ReleaseTrust trust)
    {
        byte[] bytes;
        try
        {
            bytes = File.Exists(layout.CatalogPath) ? File.ReadAllBytes(layout.CatalogPath) : [];
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("Client release catalog unreadable; keeping revision {Revision}.", current.Revision);
            return;
        }
        var sha256 = ReleaseJson.Sha256(bytes);
        if (sha256 == checkedSha256)
            return;
        checkedSha256 = sha256;
        try
        {
            Volatile.Write(ref current, bytes.Length == 0 ? ReleaseSnapshot.Empty : Load(layout, trust, CatalogFile.Parse(bytes)));
            logger.LogInformation("Client release catalog revision {Revision} loaded.", current.Revision);
        }
        catch (Exception error) when (error is ReleaseRejected or IOException or UnauthorizedAccessException)
        {
            logger.LogError("Client release catalog rejected; keeping revision {Revision}: {Reason}", current.Revision, error.Message);
        }
    }

    private static ReleaseSnapshot Load(DistributionLayout layout, ReleaseTrust trust, ReleaseCatalog catalog)
    {
        var offered = new List<SnapshotRelease>();
        foreach (var release in catalog.Announced.Where(release => trust.Protocols.Accepts(release.ProtocolVersion)))
        {
            var verified = ReleaseVerifier.VerifyPublished(layout, release, trust, PackageCheck.Size);
            offered.Add(new SnapshotRelease(ClientVersion.Parse(release.Version), release.PublishedAt,
                Offers(release, trust.Downloads), Installers(verified), Notes(verified)));
        }
        var minimum = catalog.MinimumVersion.Length == 0 ? null : ClientVersion.Parse(catalog.MinimumVersion);
        var scheduled = catalog.ScheduledMinimum is { } next
            ? new SnapshotSchedule(ClientVersion.Parse(next.Version), next.EffectiveAt)
            : null;
        return new ReleaseSnapshot(catalog.Revision, minimum, scheduled,
            offered.OrderByDescending(release => release.Version).ToList());
    }

    // Every platform's manifest is signed with the same notes (release/notes/<version>.md); the first one is read.
    private static string Notes(VerifiedRelease release) => release.Platforms[ClientPlatforms.All[0]].Manifest.Notes;

    private static IReadOnlyList<ClientInstallerOffer> Installers(VerifiedRelease release) => ClientPlatforms.All
        .Select(platform => (Platform: platform, File: release.Platforms[platform].Manifest.Installer))
        .Where(entry => entry.File is not null)
        .Select(entry => new ClientInstallerOffer
        {
            Platform = entry.Platform,
            FileName = entry.File!.FileName,
            Url = entry.File.Url,
            Size = entry.File.Size,
            Sha256 = entry.File.Sha256
        })
        .ToList();

    private static IReadOnlyDictionary<string, ClientReleaseOffer> Offers(CatalogRelease release, DownloadsLocation downloads) =>
        ClientPlatforms.All.ToDictionary(platform => platform, platform => new ClientReleaseOffer
        {
            Version = release.Version,
            ProtocolVersion = release.ProtocolVersion,
            ManifestUrl = downloads.FileUrl(release.Version, platform, ReleaseManifest.FileName),
            SignatureUrl = downloads.FileUrl(release.Version, platform, ReleaseManifest.SignatureFileName),
            PublishedAt = release.PublishedAt
        });
}
