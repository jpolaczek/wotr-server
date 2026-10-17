using System.Text.Json.Serialization;
using Wotr.Contracts;

namespace Wotr.Releases;

// catalog.json: what the server announces. Packages live in public/releases; withdrawn ones stay on disk.
// Schema 2 (DIST-09) adds a minimum scheduled for a future time; schema 1 files stay readable.
public sealed class ReleaseCatalog
{
    public const int CurrentSchema = 2;
    public const int FirstSchema = 1;

    public int Schema { get; set; } = CurrentSchema;
    public string Channel { get; set; } = ReleaseManifest.StableChannel;
    public int Revision { get; set; }
    public string MinimumVersion { get; set; } = "";
    public ScheduledMinimum? ScheduledMinimum { get; set; }
    public List<CatalogRelease> Releases { get; set; } = [];

    [JsonIgnore]
    public IEnumerable<CatalogRelease> Announced => Releases.Where(release => !release.Withdrawn);

    public CatalogRelease? Find(string version) => Releases.FirstOrDefault(release => release.Version == version);

    // The minimum the server enforces at a moment: the scheduled one once its time has come.
    public string EffectiveMinimum(DateTimeOffset now) =>
        ScheduledMinimum is { } scheduled && now >= scheduled.EffectiveAt ? scheduled.Version : MinimumVersion;

    // A scheduled minimum whose time has passed becomes the plain minimum on the next write.
    public void Settle(DateTimeOffset now)
    {
        if (ScheduledMinimum is not { } scheduled || now < scheduled.EffectiveAt)
            return;
        MinimumVersion = scheduled.Version;
        ScheduledMinimum = null;
    }
}

public sealed class ScheduledMinimum
{
    public string Version { get; set; } = "";
    public DateTimeOffset EffectiveAt { get; set; }
}

public sealed class CatalogRelease
{
    public string Version { get; set; } = "";
    public int ProtocolVersion { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public bool Withdrawn { get; set; }
    // Platform → SHA-256 of its manifest.json, binding the catalog to the exact signed bytes.
    public Dictionary<string, string> Manifests { get; set; } = [];
}

public static class CatalogRules
{
    public static void Check(ReleaseCatalog catalog)
    {
        Require(catalog.Schema is ReleaseCatalog.FirstSchema or ReleaseCatalog.CurrentSchema &&
            catalog.Channel == ReleaseManifest.StableChannel, "The catalog uses an unknown schema or channel.");
        Require(catalog.Revision >= 0, "The catalog revision is negative.");
        var versions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var release in catalog.Releases)
        {
            Require(ClientVersion.TryParse(release.Version, out _) && versions.Add(release.Version),
                $"The catalog lists an invalid or repeated version '{release.Version}'.");
            Require(release.ProtocolVersion > 0, $"Release {release.Version} lacks its protocol.");
            Require(release.Manifests.Count == ClientPlatforms.All.Count &&
                ClientPlatforms.All.All(platform => release.Manifests.TryGetValue(platform, out var sha) && ReleaseJson.IsSha256(sha)),
                $"Release {release.Version} does not bind exactly one manifest per platform.");
        }
        Require(catalog.MinimumVersion.Length == 0 || IsAnnounced(catalog, catalog.MinimumVersion),
            "The minimum version must be an announced release.");
        if (catalog.ScheduledMinimum is { } scheduled)
            CheckScheduled(catalog, scheduled);
    }

    private static void CheckScheduled(ReleaseCatalog catalog, ScheduledMinimum scheduled)
    {
        Require(catalog.Schema == ReleaseCatalog.CurrentSchema, "A scheduled minimum needs catalog schema 2.");
        Require(IsAnnounced(catalog, scheduled.Version), "The scheduled minimum must be an announced release.");
        Require(scheduled.EffectiveAt.Offset == TimeSpan.Zero, "The scheduled minimum time must be in UTC.");
        Require(catalog.MinimumVersion.Length == 0 ||
            ClientVersion.Parse(scheduled.Version).CompareTo(ClientVersion.Parse(catalog.MinimumVersion)) > 0,
            "The scheduled minimum must be higher than the current minimum.");
    }

    private static bool IsAnnounced(ReleaseCatalog catalog, string version) =>
        catalog.Announced.Any(release => release.Version == version);

    private static void Require(bool condition, string problem)
    {
        if (!condition)
            throw new ReleaseRejected(problem);
    }
}
