using Wotr.Contracts;

namespace Wotr.Releases;

public enum PackageCheck
{
    // Existence and size: cheap enough for every catalog reload on the server.
    Size,
    // Size and SHA-256 of the whole package: operator commands before anything is announced.
    Full
}

public sealed class ReleaseTrust(ReleasePublicKey key, DownloadsLocation downloads, ProtocolSupport protocols)
{
    public ReleasePublicKey Key { get; } = key;
    public DownloadsLocation Downloads { get; } = downloads;
    public ProtocolSupport Protocols { get; } = protocols;
}

public sealed record VerifiedPlatform(ReleaseManifest Manifest, string ManifestSha256);

public sealed record VerifiedRelease(string Version, int ProtocolVersion, IReadOnlyDictionary<string, VerifiedPlatform> Platforms);

public static class ReleaseVerifier
{
    // A complete upload holds one folder per platform, each with only its manifest, signature, package and installer.
    public static VerifiedRelease VerifyUpload(string folder, string version, ReleaseTrust trust)
    {
        if (!Directory.Exists(folder))
            throw new ReleaseRejected($"Release {version} has no uploaded folder at {folder}.");
        var platforms = Directory.GetDirectories(folder).Select(Path.GetFileName).Order(StringComparer.Ordinal).ToList();
        var expected = ClientPlatforms.All.Order(StringComparer.Ordinal).ToList();
        if (Directory.GetFiles(folder).Length > 0 || !platforms.SequenceEqual(expected))
            throw new ReleaseRejected($"Release {version} must contain exactly the folders {string.Join(" and ", expected)}; " +
                $"found {(platforms.Count == 0 ? "none" : string.Join(", ", platforms))}.");
        var verified = ClientPlatforms.All.ToDictionary(platform => platform,
            platform => VerifyPlatform(Path.Combine(folder, platform), version, platform, trust, PackageCheck.Full, null, onlyReleaseFiles: true));
        return Combine(version, verified, trust);
    }

    // A published release must still match the manifest bytes the catalog announced.
    public static VerifiedRelease VerifyPublished(DistributionLayout layout, CatalogRelease release, ReleaseTrust trust, PackageCheck check)
    {
        var folder = layout.Published(release.Version);
        var verified = ClientPlatforms.All.ToDictionary(platform => platform,
            platform => VerifyPlatform(Path.Combine(folder, platform), release.Version, platform, trust, check, release.Manifests[platform], onlyReleaseFiles: false));
        var result = Combine(release.Version, verified, trust);
        if (result.ProtocolVersion != release.ProtocolVersion)
            throw new ReleaseRejected($"Release {release.Version} manifests name protocol {result.ProtocolVersion}, the catalog {release.ProtocolVersion}.");
        return result;
    }

    private static VerifiedRelease Combine(string version, Dictionary<string, VerifiedPlatform> platforms, ReleaseTrust trust)
    {
        var manifests = platforms.Values.Select(platform => platform.Manifest).ToList();
        if (manifests.Select(manifest => manifest.ProtocolVersion).Distinct().Count() != 1)
            throw new ReleaseRejected($"Release {version} platforms speak different protocols.");
        if (manifests.Select(manifest => manifest.SourceCommit).Distinct().Count() != 1)
            throw new ReleaseRejected($"Release {version} platforms were built from different commits.");
        var protocol = manifests[0].ProtocolVersion;
        if (!trust.Protocols.Accepts(protocol))
            throw new ReleaseRejected($"Release {version} speaks protocol {protocol}; this server supports " +
                $"{trust.Protocols.Minimum}–{trust.Protocols.Maximum}.");
        return new VerifiedRelease(version, protocol, platforms);
    }

    private static VerifiedPlatform VerifyPlatform(string folder, string version, string platform, ReleaseTrust trust,
        PackageCheck check, string? announcedSha256, bool onlyReleaseFiles)
    {
        var description = $"Release {version} {platform}";
        var manifestPath = Path.Combine(folder, ReleaseManifest.FileName);
        var signaturePath = Path.Combine(folder, ReleaseManifest.SignatureFileName);
        if (!File.Exists(manifestPath) || !File.Exists(signaturePath))
            throw new ReleaseRejected(description + " lacks its manifest or signature.");
        var bytes = File.ReadAllBytes(manifestPath);
        var sha256 = ReleaseJson.Sha256(bytes);
        if (announcedSha256 is not null && sha256 != announcedSha256)
            throw new ReleaseRejected(description + " manifest differs from the bytes the catalog announced.");
        ManifestSignature.Verify(bytes, File.ReadAllBytes(signaturePath), trust.Key, description + " manifest");
        var manifest = ReleaseJson.Parse<ReleaseManifest>(bytes, description + " manifest");
        ManifestRules.Check(manifest, version, platform, trust.Downloads);
        var files = manifest.Installer is null ? [manifest.Package] : new[] { manifest.Package, manifest.Installer };
        if (onlyReleaseFiles)
            RequireOnlyReleaseFiles(folder, files.Select(file => file.FileName), description);
        foreach (var file in files)
            CheckFile(folder, file, check, description + (file == manifest.Package ? " package" : " installer"));
        return new VerifiedPlatform(manifest, sha256);
    }

    private static void CheckFile(string folder, ReleasePackage expected, PackageCheck check, string description)
    {
        var path = Path.Combine(folder, expected.FileName);
        var file = new FileInfo(path);
        if (!file.Exists || file.Length != expected.Size)
            throw new ReleaseRejected(description + " is missing or incomplete: its size differs from the manifest.");
        if (check == PackageCheck.Full && ReleaseJson.Sha256(path) != expected.Sha256)
            throw new ReleaseRejected(description + " SHA-256 differs from its manifest.");
    }

    private static void RequireOnlyReleaseFiles(string folder, IEnumerable<string> releaseFiles, string description)
    {
        var entries = Directory.GetFileSystemEntries(folder);
        var names = entries.Select(Path.GetFileName).Order(StringComparer.Ordinal);
        var expected = releaseFiles.Append(ReleaseManifest.FileName).Append(ReleaseManifest.SignatureFileName).Order(StringComparer.Ordinal);
        if (!names.SequenceEqual(expected) || entries.Any(entry => File.GetAttributes(entry).HasFlag(FileAttributes.ReparsePoint)))
            throw new ReleaseRejected(description + " must hold only regular files: its manifest, signature, package and installer.");
    }
}
