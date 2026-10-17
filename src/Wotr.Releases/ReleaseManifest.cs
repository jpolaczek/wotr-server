using System.Text.RegularExpressions;
using Wotr.Contracts;

namespace Wotr.Releases;

// DS12: the signed description of one full client package. The signature covers these exact bytes.
// Schema 2 (DIST-09) adds the platform installer for first installs; schema 1 manifests stay readable.
public sealed class ReleaseManifest
{
    public const int CurrentSchema = 2;
    public const int FirstSchema = 1;
    public const string StableChannel = "stable";
    public const string FileName = "manifest.json";
    public const string SignatureFileName = "manifest.json.sig";
    public const int MaximumNotesLength = 4000;

    public int Schema { get; set; } = CurrentSchema;
    public string Channel { get; set; } = StableChannel;
    public string Version { get; set; } = "";
    public string Platform { get; set; } = "";
    public int ProtocolVersion { get; set; }
    public string SourceCommit { get; set; } = "";
    public ReleasePackage Package { get; set; } = new();
    public ReleasePackage? Installer { get; set; }
    public string Notes { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ReleasePackage
{
    public string FileName { get; set; } = "";
    public string Url { get; set; } = "";
    public long Size { get; set; }
    public string Sha256 { get; set; } = "";
}

public static partial class ManifestRules
{
    [GeneratedRegex(@"\A[A-Za-z0-9][A-Za-z0-9._-]{0,127}\z")]
    private static partial Regex PackageName();

    [GeneratedRegex(@"\A[0-9a-f]{40}\z")]
    private static partial Regex Commit();

    public static bool IsPackageName(string name) =>
        PackageName().IsMatch(name) && name is not ReleaseManifest.FileName and not ReleaseManifest.SignatureFileName;

    public static bool IsCommit(string commit) => Commit().IsMatch(commit);

    public static void Check(ReleaseManifest manifest, string version, string platform, DownloadsLocation downloads)
    {
        Require(manifest.Schema is ReleaseManifest.FirstSchema or ReleaseManifest.CurrentSchema &&
            manifest.Channel == ReleaseManifest.StableChannel, "uses an unknown schema or channel");
        Require(manifest.Version == version && ClientVersion.TryParse(manifest.Version, out _), "names another version");
        Require(manifest.Platform == platform && ClientPlatforms.IsKnown(platform), "names another platform");
        Require(manifest.ProtocolVersion > 0 && IsCommit(manifest.SourceCommit), "lacks a protocol or source commit");
        var notes = manifest.Notes.Trim();
        Require(notes.Length > 0 && manifest.Notes.Length <= ReleaseManifest.MaximumNotesLength, "lacks release notes");
        CheckFile(manifest.Package, "package");
        if (manifest.Schema == ReleaseManifest.FirstSchema)
            Require(manifest.Installer is null, "names an installer in a schema without one");
        else
            CheckInstaller(manifest.Installer);

        void CheckInstaller(ReleasePackage? installer)
        {
            Require(installer is not null, "lacks its installer");
            Require(installer!.FileName != manifest.Package.FileName, "uses one file as package and installer");
            CheckFile(installer, "installer");
        }

        void CheckFile(ReleasePackage file, string role)
        {
            Require(IsPackageName(file.FileName), $"names an unsafe {role} file");
            Require(file.Url == downloads.FileUrl(version, platform, file.FileName), $"points its {role} outside the approved downloads path");
            Require(file.Size > 0 && ReleaseJson.IsSha256(file.Sha256), $"lacks the {role} size or SHA-256");
        }

        void Require(bool condition, string problem)
        {
            if (!condition)
                throw new ReleaseRejected($"Manifest {platform} {version} {problem}.");
        }
    }
}
