using System.Data;
using System.Text.RegularExpressions;

namespace Wotr.Server;

public sealed record ReplayExport(Stream? Archive = null, string FileName = "", string Error = "");

// REPLAY-02: a replay is given only for a finished, recorded match, to its players or to an observer still watching
// it. The archive is written to a temporary file that goes when the response stream is closed; a cancelled request
// stops the export and removes the file.
public sealed partial class ReplayExporter(Database database)
{
    public Task<ReplayExport> ForPlayer(string match, string account, CancellationToken cancellation = default) =>
        Export(match, account, ReplaySource.Player, cancellation);

    public Task<ReplayExport> ForObserver(string match, string account, CancellationToken cancellation = default) =>
        Export(match, account, MatchObservers.Watching, cancellation);

    private async Task<ReplayExport> Export(string match, string account, string access, CancellationToken cancellation)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellation);
        var header = await ReplaySource.Header(connection, transaction, match, account, access, cancellation);
        if (header is null)
            return new(Error: "MatchNotFound");
        if (!header.Finished)
            return new(Error: "MatchNotFinished");
        if (!header.Recorded)
            return new(Error: "ReplayUnavailable");
        var file = TemporaryFile();
        try
        {
            await ReplayArchive.Write(file, header, connection, transaction, cancellation);
            file.Position = 0;
            return new(file, FileName(header));
        }
        catch
        {
            await file.DisposeAsync();
            throw;
        }
    }

    private static FileStream TemporaryFile() => new(
        Path.Combine(Path.GetTempPath(), "wotr-replay-" + Guid.NewGuid().ToString("N") + ".tmp"),
        FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.DeleteOnClose);

    private static string FileName(ReplayHeader header)
    {
        var name = Unsafe().Replace(header.Name, "-").Trim('-');
        name = name.Length == 0 ? "game" : name[..Math.Min(name.Length, 40)];
        return name + "-" + header.Match[..Math.Min(header.Match.Length, 8)] + Wotr.Contracts.ReplayFormat.Extension;
    }

    [GeneratedRegex("[^A-Za-z0-9]+")]
    private static partial Regex Unsafe();
}
