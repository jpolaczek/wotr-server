using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// REPLAY-02 (RP03, RP04): a finished, recorded match downloads as a .wotrreplay v1 archive — to its players and to
// an observer still watching — with both sides' frames, history, battle reports and the card texts for offline
// reading, but no sessions, account ids or hidden decks. During the game, outside the match, without a recording
// or after deletion nothing is given.
public sealed partial class ReplayExportTests
{
    [PostgresFact]
    public async Task AFinishedMatchGivesBothSidesToItsPlayersAndTheWatchingObserver()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var table = await ShadowWinsTable.Create(host);
        Assert.Equal("MatchNotFinished", await Refusal(table.FreePeoples, table.Match, "matches"));
        Assert.Equal("MatchNotFinished", await Refusal(table.Observer, table.Match, "observe"));

        await table.Finish();
        var fromPlayer = await Download(table.FreePeoples, $"/api/matches/{table.Match}/replay");
        var fromObserver = await Download(table.Observer, $"/api/observe/{table.Match}/replay");

        var manifest = fromPlayer.Manifest;
        Assert.Equal((ReplayFormat.Name, 1, Side.Shadow, VictoryCondition.ShadowMilitary, true),
            (manifest.Format, manifest.FormatVersion, manifest.Winner, manifest.Victory, manifest.Complete));
        Assert.Equal([table.FreePeoplesName, table.ShadowName], manifest.Players.Select(p => p.Name));
        var frames = await StoredFrames(table.Match);
        Assert.Equal(frames.Count, manifest.Steps);
        Assert.Equal(frames, fromPlayer.Frames().Select(Normalized));
        Assert.Equal(frames.Select((_, step) => step), fromPlayer.Read<List<ReplayStep>>(ReplayFormat.StepsEntry).Select(s => s.Step));
        Assert.Equal(0, fromPlayer.Read<List<ReplayTurn>>(ReplayFormat.TurnsEntry)[0].Step);
        Assert.NotEmpty(fromPlayer.HistorySteps(Side.FreePeoples));
        Assert.Equal(await PlayerSteps(table.FreePeoples, table.Match), fromPlayer.HistorySteps(Side.FreePeoples));
        Assert.Equal(await PlayerSteps(table.Shadow, table.Match), fromPlayer.HistorySteps(Side.Shadow));
        Assert.Contains(ReplayFormat.CatalogEntry("base-card-text.json"), fromPlayer.Entries);
        Assert.Equal(fromPlayer.Frames().Select(Normalized), fromObserver.Frames().Select(Normalized));
        fromPlayer.CheckIntegrity();
        fromPlayer.HasNone(table.Secrets);
        Assert.Equal(HttpStatusCode.NotFound, (await table.Outsider.GetAsync($"/api/matches/{table.Match}/replay")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await table.FreePeoples.GetAsync($"/api/observe/{table.Match}/replay")).StatusCode);
    }

    [PostgresFact]
    public async Task AReplayDownloadsAgainButNotWithoutRecordingAfterTheLeaseOrAfterDeletion()
    {
        using var host = new AccountHost(extra: new Dictionary<string, string?>(TestBudgets.Stress)
        {
            [ObserverOptions.LeaseKey] = "1"
        });
        var table = await ShadowWinsTable.Create(host);
        await table.Finish();

        var first = await Download(table.Shadow, $"/api/matches/{table.Match}/replay");
        var again = await Download(table.Shadow, $"/api/matches/{table.Match}/replay");
        Assert.Equal(first.Frames().Select(Normalized), again.Frames().Select(Normalized));
        await Task.Delay(1500);
        Assert.Equal(HttpStatusCode.NotFound, (await table.Observer.GetAsync($"/api/observe/{table.Match}/replay")).StatusCode);

        await Execute("UPDATE matches SET recorded=false WHERE id=$1", table.Match);
        Assert.Equal("ReplayUnavailable", await Refusal(table.Shadow, table.Match, "matches"));
        await Execute("UPDATE matches SET recorded=true WHERE id=$1", table.Match);
        Assert.True(await host.Services.GetRequiredService<MatchRemoval>().Delete(table.Match, table.FreePeoplesAccount));
        Assert.Equal(HttpStatusCode.NotFound, (await table.Shadow.GetAsync($"/api/matches/{table.Match}/replay")).StatusCode);
    }

    [PostgresFact]
    public async Task ALongRecordingIsWrittenInChunksAndATemporaryFileGoesWithItsStream()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var table = await ShadowWinsTable.Create(host);
        await table.Finish();
        await Execute("""
            INSERT INTO match_frames(match_id,step,revision,turn,phase,free_peoples,shadow)
            SELECT f.match_id,f.step + g,f.revision + g,f.turn,f.phase,f.free_peoples,f.shadow
            FROM match_frames f CROSS JOIN generate_series(1, 60) AS g
            WHERE f.match_id=$1 AND f.step=(SELECT max(step) FROM match_frames WHERE match_id=$1)
            """, table.Match);
        var exporter = host.Services.GetRequiredService<ReplayExporter>();

        var export = await exporter.ForPlayer(table.Match, table.FreePeoplesAccount);
        var path = ((FileStream)export.Archive!).Name;
        using (var archive = Archive.From(export.Archive))
        {
            Assert.Equal(3, archive.Manifest.FrameChunks);
            Assert.Equal([25, 25, archive.Manifest.Steps - 50], Enumerable.Range(0, 3)
                .Select(chunk => archive.Read<List<ReplayFrame>>(ReplayFormat.FramesEntry(chunk)).Count));
            archive.CheckIntegrity();
        }
        Assert.False(File.Exists(path), "The temporary archive goes when its stream is closed.");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            exporter.ForPlayer(table.Match, table.FreePeoplesAccount, new CancellationToken(true)));
    }

    private static async Task<string> Refusal(HttpClient client, string match, string area)
    {
        using var response = await client.GetAsync($"/api/{area}/{match}/replay");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiError>(BoardCatalog.Json))!.Error;
    }

    private static async Task<Archive> Download(HttpClient client, string route)
    {
        using var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/zip", response.Content.Headers.ContentType!.MediaType);
        Assert.EndsWith(ReplayFormat.Extension, response.Content.Headers.ContentDisposition!.FileName!.Trim('"'));
        var bytes = await response.Content.ReadAsByteArrayAsync();
        if (Environment.GetEnvironmentVariable("WOTR_REPLAY_SAMPLE") is { Length: > 0 } sample)
            await File.WriteAllBytesAsync(sample, bytes);
        return Archive.From(new MemoryStream(bytes));
    }

    // Every step id of one player's own history, read through the players' endpoint from the newest page back.
    private static async Task<List<string>> PlayerSteps(HttpClient client, string match)
    {
        var steps = new List<ActionLogStep>();
        var cursor = "";
        do
        {
            var page = (await client.GetFromJsonAsync<ActionLogPage>(
                $"/api/matches/{match}/actions?limit=50" + (cursor.Length > 0 ? "&before=" + cursor : ""), BoardCatalog.Json))!;
            steps.AddRange(page.Steps);
            cursor = page.NextCursor;
        } while (cursor.Length > 0);
        return steps.OrderBy(s => s.Revision).ThenBy(s => s.Order).Select(s => s.Id).ToList();
    }

    private static async Task<List<string>> StoredFrames(string match)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand(
            "SELECT step,revision,free_peoples::text,shadow::text FROM match_frames WHERE match_id=$1 ORDER BY step");
        command.Parameters.Add(new NpgsqlParameter { Value = match });
        await using var reader = await command.ExecuteReaderAsync();
        var frames = new List<string>();
        while (await reader.ReadAsync())
        {
            frames.Add(Normalized(new ReplayFrame
            {
                Step = reader.GetInt32(0), Revision = reader.GetInt64(1),
                FreePeoples = JsonSerializer.Deserialize<PlayerView>(reader.GetString(2), BoardCatalog.Json)!,
                Shadow = JsonSerializer.Deserialize<PlayerView>(reader.GetString(3), BoardCatalog.Json)!
            }));
        }
        return frames;
    }

    private static string Normalized(ReplayFrame frame) => JsonSerializer.Serialize(frame, BoardCatalog.Json);

    private static async Task Execute(string sql, string value)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand(sql);
        command.Parameters.Add(new NpgsqlParameter { Value = value });
        await command.ExecuteNonQueryAsync();
    }

    [GeneratedRegex(@"^(manifest\.json|battles\.json|index/(steps|turns)\.json|frames/\d{4}\.json|history/(free-peoples|shadow)/\d{4}\.json|catalog/[a-z-]+\.json)$")]
    private static partial Regex AllowedEntry();

    // An opened replay archive with the checks every test shares.
    private sealed class Archive : IDisposable
    {
        private readonly ZipArchive zip;
        public ReplayManifest Manifest { get; }
        public List<string> Entries => zip.Entries.Select(e => e.FullName).ToList();

        private Archive(Stream stream)
        {
            zip = new ZipArchive(stream, ZipArchiveMode.Read);
            Manifest = Read<ReplayManifest>(ReplayFormat.ManifestEntry);
        }

        public static Archive From(Stream stream) => new(stream);

        public T Read<T>(string entry) => JsonSerializer.Deserialize<T>(Bytes(entry), BoardCatalog.Json)!;

        public IEnumerable<ReplayFrame> Frames() =>
            Enumerable.Range(0, Manifest.FrameChunks).SelectMany(chunk => Read<List<ReplayFrame>>(ReplayFormat.FramesEntry(chunk)));

        public List<string> HistorySteps(Side side)
        {
            var chunks = side == Side.FreePeoples ? Manifest.FreePeoplesHistoryChunks : Manifest.ShadowHistoryChunks;
            return Enumerable.Range(0, chunks).SelectMany(chunk => Read<ActionLogPage>(ReplayFormat.HistoryEntry(side, chunk)).Steps)
                .OrderBy(s => s.Revision).ThenBy(s => s.Order).Select(s => s.Id).ToList();
        }

        // Fixed entry names only, and every entry but the manifest listed with its exact size and SHA-256.
        public void CheckIntegrity()
        {
            Assert.All(Entries, entry => Assert.Matches(AllowedEntry(), entry));
            Assert.Equal(Entries.Where(e => e != ReplayFormat.ManifestEntry).Order(), Manifest.Files.Select(f => f.Path).Order());
            foreach (var file in Manifest.Files)
            {
                var bytes = Bytes(file.Path);
                Assert.Equal(file.Size, bytes.Length);
                Assert.Equal(file.Sha256, Convert.ToHexStringLower(SHA256.HashData(bytes)));
            }
        }

        public void HasNone(IEnumerable<string> secrets)
        {
            var text = string.Concat(Entries.Select(e => Encoding.UTF8.GetString(Bytes(e))));
            foreach (var secret in secrets.Append("passwordHash").Append("\"decks\""))
                Assert.DoesNotContain(secret, text);
        }

        private byte[] Bytes(string entry)
        {
            using var stream = zip.GetEntry(entry)!.Open();
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            return copy.ToArray();
        }

        public void Dispose() => zip.Dispose();
    }

    // A recorded match from the "sh-military" test scenario, linked to a room an observer watches: one discard by the
    // Shadow ends it with a military victory.
    private sealed class ShadowWinsTable
    {
        public required string Match { get; init; }
        public required HttpClient FreePeoples { get; init; }
        public required HttpClient Shadow { get; init; }
        public required HttpClient Observer { get; init; }
        public required HttpClient Outsider { get; init; }
        public required string FreePeoplesAccount { get; init; }
        public required string FreePeoplesName { get; init; }
        public required string ShadowName { get; init; }
        public required IEnumerable<string> Secrets { get; init; }
        private MatchStore store = null!;
        private string shadowAccount = "";

        public static async Task<ShadowWinsTable> Create(AccountHost host)
        {
            var sessions = new List<SessionView>();
            for (var i = 0; i < 4; i++)
                sessions.Add(await MenuAccounts.Register(host));
            var (fp, sh, watcher, outsider) = (sessions[0], sessions[1], sessions[2], sessions[3]);
            var rooms = host.Services.GetRequiredService<RoomStore>();
            var room = await rooms.Create(fp.AccountId, Side.FreePeoples);
            await rooms.Join(room.JoinCode, sh.AccountId);
            await rooms.Join(room.JoinCode, watcher.AccountId, RoomRole.Observer);
            var state = TestScenarioCatalog.Find("sh-military")!.Create(Guid.NewGuid().ToString("N"));
            var store = host.Services.GetRequiredService<MatchStore>();
            await store.Create(state, fp.AccountId, sh.AccountId);
            await Link(room.Id, state.Id);
            return new ShadowWinsTable
            {
                Match = state.Id, FreePeoples = MenuAccounts.Client(host, fp), Shadow = MenuAccounts.Client(host, sh),
                Observer = MenuAccounts.Client(host, watcher), Outsider = MenuAccounts.Client(host, outsider),
                FreePeoplesAccount = fp.AccountId, FreePeoplesName = fp.Username, ShadowName = sh.Username,
                Secrets = sessions.SelectMany(s => new[] { s.Token, s.AccountId }),
                store = store, shadowAccount = sh.AccountId
            };
        }

        // The Shadow discards its last action die; the turn ends with ten points and a military victory.
        public async Task Finish()
        {
            var state = (await store.Read(Match, shadowAccount))!.State;
            var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
            var receipt = await store.Execute(new GameCommand
            {
                MatchId = Match, CommandId = Guid.NewGuid().ToString("N"), ExpectedRevision = state.Revision,
                DecisionId = state.Pending!.Id, Kind = CommandKind.DiscardDie, Payload = new() { DieId = die.Id }
            }, shadowAccount);
            Assert.True(receipt!.Accepted, receipt.Error);
            Assert.Equal(Side.Shadow, (await store.Read(Match, shadowAccount))!.State.Winner);
        }

        private static async Task Link(string room, string match)
        {
            await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
            await using var command = data.CreateCommand(
                "UPDATE rooms SET match_id=$1, state=jsonb_set(state,'{matchId}',to_jsonb($1::text)) WHERE id=$2");
            command.Parameters.Add(new NpgsqlParameter { Value = match });
            command.Parameters.Add(new NpgsqlParameter { Value = room });
            await command.ExecuteNonQueryAsync();
        }
    }
}
