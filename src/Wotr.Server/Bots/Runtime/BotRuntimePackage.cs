using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Wotr.Server;

public sealed record BotPackageFile(string Path, long Size, string Sha256);
public sealed record BotPackageProvenance(string SourceCommit, string RulesCommit, string RulesArchiveSha256);
public sealed record BotRuntimePackage(int FormatVersion, string EntryAssembly, BotRuntimeDescription Description,
    BotPackageProvenance Provenance, BotPackageFile[] Files)
{
    public static BotRuntimePackage Read(string manifestPath, string expectedHash)
    {
        if (!Path.IsPathFullyQualified(manifestPath) || !Hash(expectedHash) || !File.Exists(manifestPath))
            throw new InvalidDataException("Invalid runtime package.");
        var manifest = new FileInfo(manifestPath);
        var directory = manifest.Directory!;
        if (manifest.LinkTarget is not null || directory.LinkTarget is not null || manifest.Length > 128 * 1024)
            throw new InvalidDataException("Invalid runtime package.");
        var bytes = File.ReadAllBytes(manifestPath);
        if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Invalid runtime package.");
        var package = JsonSerializer.Deserialize<BotRuntimePackage>(bytes, BotRuntimeProtocol.Json);
        if (package is null || package.FormatVersion != 1 || !BotRuntimeProtocol.Compatible(package.Description) ||
            BotRuntimeIdentity.Of(package.Description) is null || package.Files is not { Length: > 0 and <= 32 } ||
            !Name(package.EntryAssembly) || !package.EntryAssembly.EndsWith(".dll", StringComparison.Ordinal) ||
            package.Files.Any(file => file is null || !Name(file.Path) || file.Path.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) ||
                file.Path.Equals(manifest.Name, StringComparison.OrdinalIgnoreCase) ||
                !Hash(file.Sha256) || file.Size is < 1 or > 64 * 1024 * 1024) ||
            package.Files.Sum(file => file.Size) > 64 * 1024 * 1024 ||
            package.Files.Select(file => file.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != package.Files.Length ||
            !package.Files.Any(file => file.Path == package.EntryAssembly) ||
            package.Provenance is null || !Commit(package.Provenance.SourceCommit) ||
            !Commit(package.Provenance.RulesCommit) || !Hash(package.Provenance.RulesArchiveSha256))
            throw new InvalidDataException("Invalid runtime package.");
        var expected = package.Files.Select(file => file.Path).Append(manifest.Name).ToHashSet(StringComparer.Ordinal);
        if (directory.EnumerateFileSystemInfos().Any(file => !expected.Contains(file.Name) || file.LinkTarget is not null || file is DirectoryInfo))
            throw new InvalidDataException("Invalid runtime package.");
        foreach (var file in package.Files)
        {
            var actual = new FileInfo(Path.Combine(directory.FullName, file.Path));
            if (!actual.Exists || actual.Length != file.Size || actual.LinkTarget is not null)
                throw new InvalidDataException("Invalid runtime package.");
            using var input = actual.OpenRead();
            if (!Convert.ToHexString(SHA256.HashData(input)).Equals(file.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Invalid runtime package.");
        }
        return package;
    }

    private static bool Name(string? value) => value is not null && Regex.IsMatch(value, "^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$");
    private static bool Hash(string? value) => value is not null && Regex.IsMatch(value, "^[a-fA-F0-9]{64}$");
    private static bool Commit(string? value) => value is not null && Regex.IsMatch(value, "^[a-fA-F0-9]{40}$");
}
