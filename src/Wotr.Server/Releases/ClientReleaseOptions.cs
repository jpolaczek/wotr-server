using Wotr.Releases;

namespace Wotr.Server;

// DIST-05 configuration. Without Wotr:Releases:Root the server announces nothing and sets no minimum.
public sealed class ClientReleaseOptions
{
    public const string RootKey = "Wotr:Releases:Root";
    public const string DownloadsUrlKey = "Wotr:Releases:DownloadsUrl";
    public const string PublicKeyKey = "Wotr:Releases:PublicKey";
    public const string RefreshKey = "Wotr:Releases:RefreshMilliseconds";
    // The transition for ZIP clients that predate the version headers; production rejects them by default.
    public const string AcceptUnversionedKey = "Wotr:Clients:AcceptUnversioned";

    public DistributionLayout? Layout { get; private init; }
    public ReleaseTrust? Trust { get; private init; }
    public TimeSpan Refresh { get; private init; }
    public bool AcceptUnversioned { get; private init; }

    public static ClientReleaseOptions Read(IConfiguration configuration, IHostEnvironment environment)
    {
        var accept = configuration.GetValue<bool?>(AcceptUnversionedKey) ?? !environment.IsProduction();
        var refresh = TimeSpan.FromMilliseconds(configuration.GetValue<int?>(RefreshKey) ?? 1000);
        var root = configuration[RootKey];
        if (string.IsNullOrWhiteSpace(root))
            return new ClientReleaseOptions { AcceptUnversioned = accept, Refresh = refresh };
        try
        {
            var trust = new ReleaseTrust(ReleasePublicKey.Parse(configuration[PublicKeyKey] ?? ""),
                DownloadsLocation.Parse(configuration[DownloadsUrlKey] ?? ""), ProtocolSupport.Current);
            return new ClientReleaseOptions { Layout = new DistributionLayout(root), Trust = trust, AcceptUnversioned = accept, Refresh = refresh };
        }
        catch (ReleaseRejected error)
        {
            throw new InvalidOperationException($"{RootKey} needs {DownloadsUrlKey} and {PublicKeyKey}: {error.Message}");
        }
    }
}
