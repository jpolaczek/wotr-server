using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

// REPLAY-02: the committed v1 sample (a "sh-military" game the Shadow won) stays readable with the Contracts alone —
// no rules engine and no database — so format changes that would break saved replays show up here.
public sealed class ReplayFormatSampleTests
{
    private static readonly string Sample = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Replays", "replay-v1-sample.wotrreplay");

    [Fact]
    public void TheSampleReadsWithTheContractsAloneAndEveryEntryMatchesItsSum()
    {
        using var zip = ZipFile.OpenRead(Sample);
        var manifest = Read<ReplayManifest>(zip, ReplayFormat.ManifestEntry);
        Assert.Equal((ReplayFormat.Name, ReplayFormat.Version, true, Side.Shadow), (manifest.Format, manifest.FormatVersion, manifest.Complete, manifest.Winner));
        foreach (var file in manifest.Files)
        {
            var bytes = Bytes(zip, file.Path);
            Assert.Equal(file.Size, bytes.Length);
            Assert.Equal(file.Sha256, Convert.ToHexStringLower(SHA256.HashData(bytes)));
        }

        var frames = Enumerable.Range(0, manifest.FrameChunks)
            .SelectMany(chunk => Read<List<ReplayFrame>>(zip, ReplayFormat.FramesEntry(chunk))).ToList();
        Assert.Equal(manifest.Steps, frames.Count);
        Assert.Equal(manifest.Steps, Read<List<ReplayStep>>(zip, ReplayFormat.StepsEntry).Count);
        Assert.All(frames, frame => Assert.Empty(frame.FreePeoples.Options));
        Assert.Equal(Side.Shadow, frames[^1].Shadow.Winner);
        Assert.Equal(Side.FreePeoples, frames[0].FreePeoples.You);
        Assert.Equal(Side.Shadow, frames[0].Shadow.You);
    }

    private static T Read<T>(ZipArchive zip, string entry) => JsonSerializer.Deserialize<T>(Bytes(zip, entry), BoardCatalog.Json)!;

    private static byte[] Bytes(ZipArchive zip, string entry)
    {
        using var stream = zip.GetEntry(entry)!.Open();
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }
}
