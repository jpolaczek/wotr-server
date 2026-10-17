namespace Wotr.Releases;

// One distribution root on the host:
//   incoming/<version>/<platform>/   uploads, never served
//   public/releases/<version>/...    immutable published files; nginx serves public/ read-only
//   catalog.json                     the announced releases and minimum, replaced atomically
public sealed class DistributionLayout(string root)
{
    public string Root { get; } = Path.GetFullPath(root);
    public string CatalogPath => Path.Combine(Root, "catalog.json");
    public string LockPath => Path.Combine(Root, "catalog.lock");

    public string Incoming(string version) => Path.Combine(Root, "incoming", version);
    public string Published(string version) => Path.Combine(Root, "public", "releases", version);
}
