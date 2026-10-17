using System.Text;
using Wotr.Contracts;
using Wotr.Releases;
using Xunit;

namespace Wotr.Tests;

public class ReleaseSigningTests
{
    // RFC 8032 section 7.1, test 1: the library signs and verifies standard Ed25519.
    [Fact]
    public void Rfc8032VectorMatchesTheLibrary()
    {
        var key = ReleasePrivateKey.FromSeed(Convert.FromHexString("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60"));
        Assert.Equal("d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a", Convert.ToHexStringLower(key.PublicKey.Bytes));
        var signature = key.Sign([]);
        Assert.Equal("e5564300c360ac729086e2cc806e828a84877f1eb8e5d974d873e065224901555fb8821590a33bacc61e39701cf9b46bd25bf5f0595bbe24655141438e7a100b",
            Convert.ToHexStringLower(signature));
        Assert.True(key.PublicKey.Verifies([], signature));
    }

    [Fact]
    public void SignatureCoversEveryManifestByteAndOnlyThePinnedKey()
    {
        var key = ReleasePrivateKey.Generate();
        var manifest = Encoding.UTF8.GetBytes("{\"version\":\"0.3.0\"}\n");
        var signature = ManifestSignature.Create(manifest, key);
        ManifestSignature.Verify(manifest, signature, key.PublicKey, "test");

        var altered = (byte[])manifest.Clone();
        altered[3] ^= 1;
        Assert.Contains("does not match", Assert.Throws<ReleaseRejected>(() => ManifestSignature.Verify(altered, signature, key.PublicKey, "test")).Message);
        var foreign = ReleasePrivateKey.Generate();
        Assert.Contains("another key", Assert.Throws<ReleaseRejected>(() => ManifestSignature.Verify(manifest, signature, foreign.PublicKey, "test")).Message);
        // A foreign signature that claims the pinned key id still fails on the bytes.
        var forged = Encoding.UTF8.GetString(ManifestSignature.Create(manifest, foreign)).Replace(foreign.PublicKey.KeyId, key.PublicKey.KeyId);
        Assert.Contains("does not match", Assert.Throws<ReleaseRejected>(() =>
            ManifestSignature.Verify(manifest, Encoding.UTF8.GetBytes(forged), key.PublicKey, "test")).Message);
        Assert.Throws<ReleaseRejected>(() => ManifestSignature.Verify(manifest, Encoding.UTF8.GetBytes("{\"keyId\":1}"), key.PublicKey, "test"));
    }

    [Fact]
    public void PublicKeysMustBeEd25519AndPrivateKeysStayOwnerOnly()
    {
        Assert.Throws<ReleaseRejected>(() => ReleasePublicKey.Parse("not base64!"));
        Assert.Throws<ReleaseRejected>(() => ReleasePublicKey.Parse(Convert.ToBase64String(new byte[31])));
        var folder = Directory.CreateTempSubdirectory("wotr-key-");
        try
        {
            var key = ReleasePrivateKey.Generate();
            var path = Path.Combine(folder.FullName, "release.key");
            key.Save(path);
            Assert.Equal(key.PublicKey.Encoded, ReleasePrivateKey.Load(path).PublicKey.Encoded);
            Assert.Throws<IOException>(() => key.Save(path));
            if (!OperatingSystem.IsWindows())
            {
                Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.OtherRead);
                Assert.Contains("chmod 600", Assert.Throws<ReleaseRejected>(() => ReleasePrivateKey.Load(path)).Message);
            }
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [Fact]
    public void ClientVersionsFollowSemVerPrecedence()
    {
        string[] ascending = ["0.2.0-rc1", "0.2.0", "0.9.0", "0.10.0", "1.0.0-alpha", "1.0.0-alpha.1", "1.0.0-alpha.beta",
            "1.0.0-beta", "1.0.0-beta.2", "1.0.0-beta.11", "1.0.0-rc.1", "1.0.0", "1.0.1", "2.0.0"];
        for (var index = 1; index < ascending.Length; index++)
            Assert.True(ClientVersion.Parse(ascending[index - 1]).CompareTo(ClientVersion.Parse(ascending[index])) < 0,
                ascending[index - 1] + " < " + ascending[index]);
        Assert.Equal(ClientVersion.Parse("0.3.0"), ClientVersion.Parse("0.3.0"));
        foreach (var invalid in new[] { null, "", "1.0", "01.0.0", "1.0.0+build", "1.0.0-Alpha", "1.0.0-01", "1.0.0-", "v1.0.0",
                     "1.0.0-a..b", "99999999999.0.0", "1.0.0-" + new string('a', 64) })
            Assert.False(ClientVersion.TryParse(invalid, out _), invalid ?? "null");
    }

    // DS10: the signed manifest repeats the identity of build-info.json; nothing is typed in by hand.
    [Fact]
    public void SignedManifestCarriesTheBuildIdentityAndThePackageDigest()
    {
        using var releases = new ReleaseFixture();
        releases.Upload("0.3.0-rc.1", platforms: ClientPlatforms.MacOS);
        var folder = releases.Incoming("0.3.0-rc.1", ClientPlatforms.MacOS);
        var bytes = File.ReadAllBytes(Path.Combine(folder, ReleaseManifest.FileName));
        ManifestSignature.Verify(bytes, File.ReadAllBytes(Path.Combine(folder, ReleaseManifest.SignatureFileName)), releases.Key.PublicKey, "test");
        var manifest = ReleaseJson.Parse<ReleaseManifest>(bytes, "test");
        var build = ReleaseFixture.Build("0.3.0-rc.1", ClientPlatforms.MacOS);
        Assert.Equal((build.Version, build.Platform, build.ProtocolVersion, build.SourceCommit),
            (manifest.Version, manifest.Platform, manifest.ProtocolVersion, manifest.SourceCommit));
        var package = Path.Combine(folder, manifest.Package.FileName);
        Assert.Equal(new FileInfo(package).Length, manifest.Package.Size);
        Assert.Equal(ReleaseJson.Sha256(package), manifest.Package.Sha256);
        Assert.Equal($"{ReleaseFixture.DownloadsUrl}releases/0.3.0-rc.1/macos-universal/{manifest.Package.FileName}", manifest.Package.Url);
        Assert.Equal(ReleaseManifest.CurrentSchema, manifest.Schema);
        var installer = Path.Combine(folder, manifest.Installer!.FileName);
        Assert.Equal((new FileInfo(installer).Length, ReleaseJson.Sha256(installer)), (manifest.Installer.Size, manifest.Installer.Sha256));
        Assert.Equal($"{ReleaseFixture.DownloadsUrl}releases/0.3.0-rc.1/macos-universal/{manifest.Installer.FileName}", manifest.Installer.Url);
    }

    // DIST-09: the client reads signed manifests through the Contracts type; it must name every server field.
    [Fact]
    public void TheClientManifestContractReadsEverySignedField()
    {
        Assert.Equal(PropertyNames(typeof(ReleaseManifest)), PropertyNames(typeof(ClientReleaseManifest)));
        Assert.Equal(PropertyNames(typeof(ReleasePackage)), PropertyNames(typeof(ClientReleaseFile)));
        Assert.Equal(ReleaseManifest.CurrentSchema, ClientReleaseManifest.CurrentSchema);

        using var releases = new ReleaseFixture();
        releases.Upload("0.3.0", platforms: ClientPlatforms.Windows);
        var bytes = File.ReadAllBytes(releases.Incoming("0.3.0", ClientPlatforms.Windows, ReleaseManifest.FileName));
        var server = ReleaseJson.Parse<ReleaseManifest>(bytes, "server");
        var client = ReleaseJson.Parse<ClientReleaseManifest>(bytes, "client");
        Assert.Equal((server.Version, server.Platform, server.SourceCommit, server.Package.Sha256, server.Installer!.Url),
            (client.Version, client.Platform, client.SourceCommit, client.Package.Sha256, client.Installer!.Url));
    }

    private static string[] PropertyNames(Type type) =>
        type.GetProperties().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();

    [Fact]
    public void SignerRefusesDevelopmentBuildsUnsafeNamesAndExistingFolders()
    {
        using var releases = new ReleaseFixture();
        var downloads = DownloadsLocation.Parse(ReleaseFixture.DownloadsUrl);
        var output = Path.Combine(releases.Layout.Root, "incoming");
        var package = Path.Combine(releases.Layout.Root, "Wotr-0.3.0.zip");
        File.WriteAllText(package, "package");
        var installer = Path.Combine(releases.Layout.Root, "Wotr-0.3.0-Setup.exe");
        File.WriteAllText(installer, "installer");
        var development = ReleaseFixture.Build("0.3.0", ClientPlatforms.Windows);
        development.Development = true;
        var windows = ReleaseFixture.Build("0.3.0", ClientPlatforms.Windows);
        Assert.Throws<ReleaseRejected>(() => ReleaseSigner.Sign(development, package, installer, "Notes", downloads, releases.Key, output, DateTimeOffset.UtcNow));
        Assert.Throws<ReleaseRejected>(() => ReleaseSigner.Sign(windows, package, installer, "  ", downloads, releases.Key, output, DateTimeOffset.UtcNow));
        Assert.Contains("Installer", Assert.Throws<ReleaseRejected>(() => ReleaseSigner.Sign(windows, package,
            Path.Combine(releases.Layout.Root, "missing-Setup.exe"), "Notes", downloads, releases.Key, output, DateTimeOffset.UtcNow)).Message);
        Assert.Contains("package and installer", Assert.Throws<ReleaseRejected>(() =>
            ReleaseSigner.Sign(windows, package, package, "Notes", downloads, releases.Key, output, DateTimeOffset.UtcNow)).Message);
        Assert.False(Directory.Exists(output));
        ReleaseSigner.Sign(windows, package, installer, "Notes", downloads, releases.Key, output, DateTimeOffset.UtcNow);
        Assert.Contains("immutable", Assert.Throws<ReleaseRejected>(() => ReleaseSigner.Sign(windows,
            package, installer, "Notes", downloads, releases.Key, output, DateTimeOffset.UtcNow)).Message);
        Assert.Throws<ReleaseRejected>(() => DownloadsLocation.Parse("http://downloads.example.test/"));
        Assert.Throws<ReleaseRejected>(() => DownloadsLocation.Parse("https://user:secret@downloads.example.test/"));
        Assert.Throws<ReleaseRejected>(() => DownloadsLocation.Parse("https://downloads.example.test/files"));
    }
}
