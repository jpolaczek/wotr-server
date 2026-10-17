using Wotr.Contracts;

namespace Wotr.Releases;

// Runs on the release machine: turns a built update package, its installer and build-info.json
// into a signed upload folder.
public static class ReleaseSigner
{
    public static string Sign(ClientBuildInfo build, string packagePath, string installerPath, string notes,
        DownloadsLocation downloads, ReleasePrivateKey key, string outputRoot, DateTimeOffset now)
    {
        CheckBuild(build);
        var manifest = new ReleaseManifest
        {
            Version = build.Version,
            Platform = build.Platform,
            ProtocolVersion = build.ProtocolVersion,
            SourceCommit = build.SourceCommit,
            Package = Describe(packagePath, "Package", build, downloads),
            Installer = Describe(installerPath, "Installer", build, downloads),
            Notes = notes.Trim(),
            CreatedAt = now
        };
        ManifestRules.Check(manifest, build.Version, build.Platform, downloads);
        var folder = Path.Combine(outputRoot, build.Version, build.Platform);
        if (Directory.Exists(folder))
            throw new ReleaseRejected($"{folder} already exists. Signed releases are immutable.");
        Directory.CreateDirectory(folder);
        try
        {
            WriteSignedFolder(folder, packagePath, installerPath, manifest, key);
        }
        catch
        {
            Directory.Delete(folder, recursive: true);
            throw;
        }
        return folder;
    }

    public static void CheckBuild(ClientBuildInfo build)
    {
        if (build.Schema != ClientBuildInfo.CurrentSchema || build.Development)
            throw new ReleaseRejected("Only a non-development build with a known build-info schema can be released.");
        if (!ClientVersion.TryParse(build.Version, out _) || !ClientPlatforms.IsKnown(build.Platform))
            throw new ReleaseRejected("The build info lacks a client version or a known platform.");
        if (!ManifestRules.IsCommit(build.SourceCommit) || build.ProtocolVersion <= 0)
            throw new ReleaseRejected("The build info lacks its source commit or protocol.");
    }

    private static ReleasePackage Describe(string path, string role, ClientBuildInfo build, DownloadsLocation downloads)
    {
        var fileName = Path.GetFileName(path);
        if (!ManifestRules.IsPackageName(fileName) || !File.Exists(path))
            throw new ReleaseRejected($"{role} '{path}' is missing or has an unsafe file name.");
        return new ReleasePackage
        {
            FileName = fileName,
            Url = downloads.FileUrl(build.Version, build.Platform, fileName),
            Size = new FileInfo(path).Length,
            Sha256 = ReleaseJson.Sha256(path)
        };
    }

    private static void WriteSignedFolder(string folder, string packagePath, string installerPath, ReleaseManifest manifest,
        ReleasePrivateKey key)
    {
        CopyUnchanged(packagePath, folder, manifest.Package);
        CopyUnchanged(installerPath, folder, manifest.Installer!);
        var bytes = ReleaseJson.Serialize(manifest);
        File.WriteAllBytes(Path.Combine(folder, ReleaseManifest.FileName), bytes);
        File.WriteAllBytes(Path.Combine(folder, ReleaseManifest.SignatureFileName), ManifestSignature.Create(bytes, key));
    }

    private static void CopyUnchanged(string source, string folder, ReleasePackage expected)
    {
        var target = Path.Combine(folder, expected.FileName);
        File.Copy(source, target);
        if (new FileInfo(target).Length != expected.Size || ReleaseJson.Sha256(target) != expected.Sha256)
            throw new ReleaseRejected($"{expected.FileName} changed while it was copied; sign it again.");
    }
}
