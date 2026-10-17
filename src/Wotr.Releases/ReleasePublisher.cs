using Wotr.Contracts;

namespace Wotr.Releases;

// Operator commands. Each verifies before it changes anything and ends with one atomic catalog write.
public sealed class ReleasePublisher(DistributionLayout layout, ReleaseTrust trust, TimeProvider clock)
{
    // DS21: clients get at least this long to download a scheduled minimum before it applies.
    public static readonly TimeSpan MinimumLead = TimeSpan.FromMinutes(5);

    public ReleaseCatalog Publish(string version, bool makeMinimum, DateTimeOffset? effectiveAt = null)
    {
        RequireVersion(version);
        if (effectiveAt is not null && !makeMinimum)
            throw new ReleaseRejected("A minimum time needs --minimum.");
        using var _ = CatalogLock.Acquire(layout);
        var catalog = Read();
        if (catalog.Find(version) is not null)
            throw new ReleaseRejected($"Release {version} is already in the catalog. Releases are immutable; publish a higher version.");
        var verified = MoveVerifiedUpload(version);
        catalog.Releases.Add(new CatalogRelease
        {
            Version = version,
            ProtocolVersion = verified.ProtocolVersion,
            PublishedAt = clock.GetUtcNow(),
            Manifests = ClientPlatforms.All.ToDictionary(platform => platform, platform => verified.Platforms[platform].ManifestSha256)
        });
        if (makeMinimum)
            ApplyMinimum(catalog, version, effectiveAt);
        return Save(catalog);
    }

    // DS12: the minimum is always an announced release whose packages for both platforms are intact.
    // DS21: with a time it is scheduled instead, so clients can download it before it applies.
    public ReleaseCatalog SetMinimum(string version, DateTimeOffset? effectiveAt = null)
    {
        RequireVersion(version);
        using var _ = CatalogLock.Acquire(layout);
        var catalog = Read();
        var release = catalog.Find(version);
        if (release is null || release.Withdrawn)
            throw new ReleaseRejected($"The minimum must be an announced release; {version} is not announced.");
        ReleaseVerifier.VerifyPublished(layout, release, trust, PackageCheck.Full);
        ApplyMinimum(catalog, version, effectiveAt);
        return Save(catalog);
    }

    public ReleaseCatalog CancelScheduledMinimum()
    {
        using var _ = CatalogLock.Acquire(layout);
        var catalog = Read();
        if (catalog.ScheduledMinimum is null)
            throw new ReleaseRejected("No minimum is scheduled.");
        catalog.ScheduledMinimum = null;
        return Save(catalog);
    }

    public ReleaseCatalog Withdraw(string version)
    {
        RequireVersion(version);
        using var _ = CatalogLock.Acquire(layout);
        var catalog = Read();
        var release = catalog.Find(version);
        if (release is null || release.Withdrawn)
            throw new ReleaseRejected($"Release {version} is not announced.");
        if (catalog.MinimumVersion == version)
            throw new ReleaseRejected($"Release {version} is the minimum; move the minimum to another announced release first.");
        if (catalog.ScheduledMinimum?.Version == version)
            throw new ReleaseRejected($"Release {version} is the scheduled minimum; cancel it first.");
        release.Withdrawn = true;
        return Save(catalog);
    }

    public ReleaseCatalog Status() => CatalogFile.Read(layout);

    public ReleaseCatalog Verify()
    {
        var catalog = CatalogFile.Read(layout);
        foreach (var release in catalog.Announced)
            ReleaseVerifier.VerifyPublished(layout, release, trust, PackageCheck.Full);
        return catalog;
    }

    private VerifiedRelease MoveVerifiedUpload(string version)
    {
        var incoming = layout.Incoming(version);
        var published = layout.Published(version);
        if (!Directory.Exists(published))
        {
            var verified = ReleaseVerifier.VerifyUpload(incoming, version, trust);
            Directory.CreateDirectory(Path.GetDirectoryName(published)!);
            Directory.Move(incoming, published);
            return verified;
        }
        if (Directory.Exists(incoming))
            throw new ReleaseRejected($"Release {version} exists in both incoming and public; remove the stale upload.");
        // Resumes a publish interrupted after the move. The files are verified again before they are announced.
        return ReleaseVerifier.VerifyUpload(published, version, trust);
    }

    private void ApplyMinimum(ReleaseCatalog catalog, string version, DateTimeOffset? effectiveAt)
    {
        if (effectiveAt is not { } at)
        {
            catalog.MinimumVersion = version;
            if (catalog.ScheduledMinimum is { } scheduled && !IsHigher(scheduled.Version, version))
                catalog.ScheduledMinimum = null;
            return;
        }
        if (at - clock.GetUtcNow() < MinimumLead)
            throw new ReleaseRejected($"A scheduled minimum must start at least {MinimumLead.TotalMinutes:0} minutes from now.");
        if (catalog.MinimumVersion.Length > 0 && !IsHigher(version, catalog.MinimumVersion))
            throw new ReleaseRejected($"The scheduled minimum must be higher than the current minimum {catalog.MinimumVersion}.");
        catalog.ScheduledMinimum = new ScheduledMinimum { Version = version, EffectiveAt = at.ToUniversalTime() };
    }

    private static bool IsHigher(string version, string than) =>
        ClientVersion.Parse(version).CompareTo(ClientVersion.Parse(than)) > 0;

    // Every command first folds a scheduled minimum whose time has passed into the plain minimum.
    private ReleaseCatalog Read()
    {
        var catalog = CatalogFile.Read(layout);
        catalog.Settle(clock.GetUtcNow());
        return catalog;
    }

    private ReleaseCatalog Save(ReleaseCatalog catalog)
    {
        catalog.Revision++;
        CatalogFile.Write(layout, catalog);
        return catalog;
    }

    private static void RequireVersion(string version)
    {
        if (!ClientVersion.TryParse(version, out _))
            throw new ReleaseRejected($"'{version}' is not a client version (MAJOR.MINOR.PATCH[-pre-release]).");
    }
}
