using System.Net.Http.Headers;
using Wotr.Contracts;
using Wotr.Releases;
using Wotr.Server;

namespace Wotr.Tests;

// A distribution root and signing key in temporary folders; packages are small unique files.
internal sealed class ReleaseFixture : IDisposable
{
    public const string DownloadsUrl = "https://downloads.example.test/";
    public static readonly string Commit = new('a', 40);

    private readonly DirectoryInfo root = Directory.CreateTempSubdirectory("wotr-releases-");
    private readonly DirectoryInfo sources = Directory.CreateTempSubdirectory("wotr-packages-");

    public ReleasePrivateKey Key { get; } = ReleasePrivateKey.Generate();
    // Operator commands and the test server share this clock, so schedules can be stepped through.
    public TestClock Clock { get; } = new();
    public DistributionLayout Layout => new(root.FullName);
    public ReleaseTrust Trust => new(Key.PublicKey, DownloadsLocation.Parse(DownloadsUrl), ProtocolSupport.Current);
    public ReleasePublisher Publisher => new(Layout, Trust, Clock);

    public Dictionary<string, string?> ServerSettings(bool acceptUnversioned = false) => new()
    {
        [ClientReleaseOptions.RootKey] = root.FullName,
        [ClientReleaseOptions.DownloadsUrlKey] = DownloadsUrl,
        [ClientReleaseOptions.PublicKeyKey] = Key.PublicKey.Encoded,
        [ClientReleaseOptions.RefreshKey] = "0",
        [ClientReleaseOptions.AcceptUnversionedKey] = acceptUnversioned ? "true" : "false"
    };

    // Signs each platform of a release into incoming/<version>, as the release machine uploads it.
    public void Upload(string version, int protocol = Protocol.Version, ReleasePrivateKey? key = null,
        string downloadsUrl = DownloadsUrl, params string[] platforms)
    {
        foreach (var platform in platforms.Length == 0 ? ClientPlatforms.All : platforms)
        {
            var package = Path.Combine(sources.FullName, PackageName(version, platform));
            File.WriteAllText(package, $"package {version} {platform} {Guid.NewGuid():N}");
            var installer = Path.Combine(sources.FullName, InstallerName(version, platform));
            File.WriteAllText(installer, $"installer {version} {platform} {Guid.NewGuid():N}");
            ReleaseSigner.Sign(Build(version, platform, protocol), package, installer, $"Changes in {version}.",
                DownloadsLocation.Parse(downloadsUrl), key ?? Key, Path.Combine(root.FullName, "incoming"), DateTimeOffset.UtcNow);
        }
    }

    public void Publish(string version, bool minimum = false)
    {
        Upload(version);
        Publisher.Publish(version, minimum);
    }

    public string Incoming(string version, string platform, string file = "") => Path.Combine(Layout.Incoming(version), platform, file);
    public string Published(string version, string platform, string file = "") => Path.Combine(Layout.Published(version), platform, file);
    public static string PackageName(string version, string platform) => $"Wotr-{version}-{platform}.zip";
    public static string InstallerName(string version, string platform) => $"Wotr-{version}-{platform}-Setup.bin";

    public static ClientBuildInfo Build(string version, string platform, int protocol = Protocol.Version) => new()
    {
        Version = version,
        Platform = platform,
        SourceCommit = Commit,
        ProtocolVersion = protocol,
        RulesVersion = Protocol.RulesVersion,
        ContentVersion = Protocol.ContentVersion,
        PublicContentVersion = "1.0.0",
        PublicContentSha256 = new string('b', 64),
        UnityVersion = "6000.6.0f1"
    };

    public static HttpClient Versioned(HttpClient client, string? version, string? protocol = "2")
    {
        if (version is not null)
            client.DefaultRequestHeaders.Add(ClientCompatibility.ClientVersionHeader, version);
        if (protocol is not null)
            client.DefaultRequestHeaders.Add(ClientCompatibility.ProtocolVersionHeader, protocol);
        return client;
    }

    public static void Authorize(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public void Dispose()
    {
        root.Delete(recursive: true);
        sources.Delete(recursive: true);
    }
}
