namespace Wotr.Releases;

public static class CatalogFile
{
    public static ReleaseCatalog Read(DistributionLayout layout) =>
        File.Exists(layout.CatalogPath) ? Parse(File.ReadAllBytes(layout.CatalogPath)) : new ReleaseCatalog();

    public static ReleaseCatalog Parse(byte[] bytes)
    {
        var catalog = ReleaseJson.Parse<ReleaseCatalog>(bytes, "The release catalog");
        CatalogRules.Check(catalog);
        return catalog;
    }

    // Readers see the previous file or the new one, never a partial write: rename replaces it atomically.
    public static void Write(DistributionLayout layout, ReleaseCatalog catalog)
    {
        catalog.Schema = ReleaseCatalog.CurrentSchema;
        CatalogRules.Check(catalog);
        var temporary = layout.CatalogPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write))
            {
                stream.Write(ReleaseJson.Serialize(catalog));
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, layout.CatalogPath, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}

// Serializes operator commands on one distribution root. Readers never take it.
public sealed class CatalogLock : IDisposable
{
    private readonly FileStream stream;

    private CatalogLock(FileStream stream) => this.stream = stream;

    public static CatalogLock Acquire(DistributionLayout layout)
    {
        Directory.CreateDirectory(layout.Root);
        try
        {
            return new CatalogLock(new FileStream(layout.LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
        }
        catch (IOException)
        {
            throw new ReleaseRejected("Another release command is changing this catalog; try again when it finishes.");
        }
    }

    public void Dispose() => stream.Dispose();
}
