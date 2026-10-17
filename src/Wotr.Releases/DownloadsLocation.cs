namespace Wotr.Releases;

// The approved HTTPS origin of release files. Every manifest and package URL is derived from it.
public sealed class DownloadsLocation
{
    private DownloadsLocation(Uri root) => Root = root;

    public Uri Root { get; }

    public static DownloadsLocation Parse(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var root) || root.Scheme != Uri.UriSchemeHttps)
            throw new ReleaseRejected("The downloads URL must be an absolute HTTPS address.");
        if (root.UserInfo.Length > 0 || root.Query.Length > 0 || root.Fragment.Length > 0 || !root.AbsolutePath.EndsWith('/'))
            throw new ReleaseRejected("The downloads URL must end with '/' and carry no credentials, query or fragment.");
        return new DownloadsLocation(root);
    }

    public string FileUrl(string version, string platform, string fileName) =>
        new Uri(Root, $"releases/{version}/{platform}/{fileName}").AbsoluteUri;
}
