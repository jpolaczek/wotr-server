using System.Data;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Server;

// REPLAY-02: writes one recorded, finished match as a .wotrreplay v1 archive (docs/server/REPLAY-FORMAT.md). It reads
// the recording in order and writes it in chunks, so at most one chunk is held in memory; nothing is computed anew
// by the rules or drawn at random. The manifest goes last, with the size and SHA-256 of every other entry.
internal sealed class ReplayArchive
{
    private static readonly string[] CatalogFiles = ["base-cards.json", "base-card-text.json", "base-characters.json"];
    private readonly ZipArchive zip;
    private readonly ReplayHeader header;
    private readonly NpgsqlConnection connection;
    private readonly NpgsqlTransaction transaction;
    private readonly List<ReplayFile> files = [];
    private readonly List<ReplayStep> steps = [];
    private readonly Dictionary<string, long> battles = new(StringComparer.Ordinal);
    private DateTimeOffset started;
    private DateTimeOffset finished;

    private ReplayArchive(ZipArchive zip, ReplayHeader header, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        this.zip = zip;
        this.header = header;
        this.connection = connection;
        this.transaction = transaction;
    }

    public static async Task Write(Stream output, ReplayHeader header, NpgsqlConnection connection,
        NpgsqlTransaction transaction, CancellationToken cancellation)
    {
        using var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
        var archive = new ReplayArchive(zip, header, connection, transaction);
        var frameChunks = await archive.Frames(cancellation);
        var freePeoples = await archive.History(Side.FreePeoples, cancellation);
        var shadow = await archive.History(Side.Shadow, cancellation);
        await archive.Battles(cancellation);
        archive.Indexes();
        archive.Catalog();
        archive.Manifest(frameChunks, freePeoples, shadow);
    }

    private async Task<int> Frames(CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "SELECT step,revision,turn,phase,free_peoples::text,shadow::text,recorded_at FROM match_frames WHERE match_id=$1 ORDER BY step",
            header.Match);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellation);
        var chunk = new FrameChunk();
        var chunks = 0;
        while (await reader.ReadAsync(cancellation))
        {
            var step = new ReplayStep
            {
                Step = reader.GetInt32(0), Revision = reader.GetInt64(1), Turn = reader.GetInt32(2),
                Phase = Enum.Parse<GamePhase>(reader.GetString(3))
            };
            chunk.Add(step, reader.GetString(4), reader.GetString(5));
            Recorded(new DateTimeOffset(reader.GetDateTime(6)));
            steps.Add(step);
            if (chunk.Count < ReplayFormat.FramesPerChunk)
                continue;
            Add(ReplayFormat.FramesEntry(chunks++), chunk.Close());
            chunk = new FrameChunk();
        }
        if (chunk.Count > 0)
            Add(ReplayFormat.FramesEntry(chunks++), chunk.Close());
        return chunks;
    }

    private void Recorded(DateTimeOffset at)
    {
        if (steps.Count == 0)
            started = at;
        finished = at;
    }

    // Each side's history in chunks of steps, every action summarized once, in the order the steps happened.
    private async Task<int> History(Side side, CancellationToken cancellation)
    {
        var summarized = new HashSet<string>(StringComparer.Ordinal);
        var chunks = 0;
        var position = 0L;
        while (true)
        {
            var records = await ActionLogQuery.Page(connection, transaction, header.Match, side, header.EventsIndexed,
                ActionLogFilter.All, position, true, ReplayFormat.HistoryStepsPerChunk, cancellation);
            if (records.Count == 0)
                return chunks;
            var page = await ActionLogStore.Assemble(connection, transaction, header.Match, side, header.EventsIndexed,
                ActionLogFilter.All, records, cancellation);
            page.Actions.RemoveAll(action => !summarized.Add(action.Id));
            NoteBattles(page.Steps);
            Add(ReplayFormat.HistoryEntry(side, chunks++), Json(page));
            position = records.Max(r => r.Position);
            if (records.Count < ReplayFormat.HistoryStepsPerChunk)
                return chunks;
        }
    }

    private void NoteBattles(IEnumerable<ActionLogStep> logged)
    {
        foreach (var step in logged.Where(s => s.Context.BattleId.Length > 0))
            battles[step.Context.BattleId] = Math.Max(step.Revision, battles.GetValueOrDefault(step.Context.BattleId));
    }

    private async Task Battles(CancellationToken cancellation)
    {
        var reports = new List<ReplayBattleReport>();
        foreach (var (battle, revision) in battles.OrderBy(b => b.Value))
        {
            var report = await BattleReportStore.Report(connection, transaction, header.Match, battle, Side.FreePeoples,
                header.EventsIndexed, cancellation);
            if (report is not null)
                reports.Add(new ReplayBattleReport { Revision = revision, Report = report });
        }
        Add(ReplayFormat.BattlesEntry, Json(reports));
    }

    private void Indexes()
    {
        Add(ReplayFormat.StepsEntry, Json(steps));
        var turns = steps.GroupBy(s => s.Turn).Select(g => new ReplayTurn { Turn = g.Key, Step = g.Min(s => s.Step) }).ToList();
        Add(ReplayFormat.TurnsEntry, Json(turns));
    }

    // The card and character descriptions of the game's time, for reading the replay offline.
    private void Catalog()
    {
        foreach (var file in CatalogFiles)
        {
            using var resource = typeof(BoardCatalog).Assembly.GetManifestResourceStream("Wotr." + file)!;
            using var content = new MemoryStream();
            resource.CopyTo(content);
            Add(ReplayFormat.CatalogEntry(file), content.ToArray());
        }
    }

    private void Manifest(int frameChunks, int freePeoplesChunks, int shadowChunks)
    {
        var state = header.State;
        var manifest = new ReplayManifest
        {
            Format = ReplayFormat.Name, FormatVersion = ReplayFormat.Version, ReplayId = header.Match,
            MatchId = header.Match, Source = "wotr-server", Name = header.Name,
            StartedAt = started, FinishedAt = finished, ExportedAt = DateTimeOffset.UtcNow,
            Players =
            [
                new ReplayPlayer { Side = Side.FreePeoples, Name = header.FreePeoples },
                new ReplayPlayer { Side = Side.Shadow, Name = header.Shadow }
            ],
            Configuration = ConfigurationRules.Normalize(state.Configuration),
            Winner = state.Winner, Victory = state.Victory,
            Turns = steps.Select(s => s.Turn).Distinct().Count(), Steps = steps.Count,
            FrameChunks = frameChunks, FreePeoplesHistoryChunks = freePeoplesChunks, ShadowHistoryChunks = shadowChunks,
            Complete = header.Recorded, Files = files
        };
        Write(ReplayFormat.ManifestEntry, Json(manifest));
    }

    private void Add(string path, byte[] content)
    {
        files.Add(new ReplayFile { Path = path, Size = content.Length, Sha256 = Convert.ToHexStringLower(SHA256.HashData(content)) });
        Write(path, content);
    }

    private void Write(string path, byte[] content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(content);
    }

    private static byte[] Json<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, BoardCatalog.Json);

    // Frames as recorded: the stored views are copied into the chunk as they are, not parsed and written again.
    private sealed class FrameChunk
    {
        private readonly MemoryStream buffer = new();
        private readonly Utf8JsonWriter writer;
        public int Count { get; private set; }

        public FrameChunk()
        {
            writer = new Utf8JsonWriter(buffer);
            writer.WriteStartArray();
        }

        public void Add(ReplayStep step, string freePeoples, string shadow)
        {
            writer.WriteStartObject();
            writer.WriteNumber("step", step.Step);
            writer.WriteNumber("revision", step.Revision);
            writer.WritePropertyName("freePeoples");
            writer.WriteRawValue(freePeoples, skipInputValidation: true);
            writer.WritePropertyName("shadow");
            writer.WriteRawValue(shadow, skipInputValidation: true);
            writer.WriteEndObject();
            Count++;
        }

        public byte[] Close()
        {
            writer.WriteEndArray();
            writer.Flush();
            writer.Dispose();
            return buffer.ToArray();
        }
    }
}
