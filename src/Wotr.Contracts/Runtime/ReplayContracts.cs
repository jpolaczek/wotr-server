using System;
using System.Collections.Generic;

namespace Wotr.Contracts
{
    // REPLAY-02: the .wotrreplay file, format v1 — a ZIP of JSON entries with fixed names. The full description,
    // limits and versioning rules are in wotr-server docs/server/REPLAY-FORMAT.md.
    public static class ReplayFormat
    {
        public const string Name = "wotr-replay";
        public const int Version = 1;
        public const string Extension = ".wotrreplay";
        public const string ManifestEntry = "manifest.json";
        public const string StepsEntry = "index/steps.json";
        public const string TurnsEntry = "index/turns.json";
        public const string BattlesEntry = "battles.json";
        public const int FramesPerChunk = 25;
        public const int HistoryStepsPerChunk = 500;
        public const long MaxArchiveBytes = 512L * 1024 * 1024;
        public const long MaxEntryBytes = 32L * 1024 * 1024;
        public const int MaxEntries = 4096;
        public const int MaxSteps = 50000;

        public static string FramesEntry(int chunk) => "frames/" + chunk.ToString("D4") + ".json";

        public static string HistoryEntry(Side side, int chunk) =>
            "history/" + (side == Side.FreePeoples ? "free-peoples" : "shadow") + "/" + chunk.ToString("D4") + ".json";

        public static string CatalogEntry(string file) => "catalog/" + file;
    }

    public sealed class ReplayManifest
    {
        public string Format { get; set; } = "";
        public int FormatVersion { get; set; }
        public string ReplayId { get; set; } = "";
        public string MatchId { get; set; } = "";
        public string Source { get; set; } = "";
        public string Name { get; set; } = "";
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset FinishedAt { get; set; }
        public DateTimeOffset ExportedAt { get; set; }
        public List<ReplayPlayer> Players { get; set; } = new List<ReplayPlayer>();
        public MatchConfiguration Configuration { get; set; } = new MatchConfiguration();
        public Side? Winner { get; set; }
        public VictoryCondition? Victory { get; set; }
        public int Turns { get; set; }
        public int Steps { get; set; }
        public int FrameChunks { get; set; }
        public int FreePeoplesHistoryChunks { get; set; }
        public int ShadowHistoryChunks { get; set; }
        public bool Complete { get; set; }
        // Every other entry of the archive with its size and SHA-256 (lowercase hex).
        public List<ReplayFile> Files { get; set; } = new List<ReplayFile>();
    }

    public sealed class ReplayPlayer
    {
        public Side Side { get; set; }
        public string Name { get; set; } = "";
    }

    public sealed class ReplayFile
    {
        public string Path { get; set; } = "";
        public long Size { get; set; }
        public string Sha256 { get; set; } = "";
    }

    // index/steps.json: one entry per recorded step, in order; step 0 is the state before the first command.
    public sealed class ReplayStep
    {
        public int Step { get; set; }
        public long Revision { get; set; }
        public int Turn { get; set; }
        public GamePhase Phase { get; set; }
    }

    // index/turns.json: the first step of every turn of the game.
    public sealed class ReplayTurn
    {
        public int Turn { get; set; }
        public int Step { get; set; }
    }

    // frames/NNNN.json holds up to FramesPerChunk frames: both sides' read-only views, without their history.
    public sealed class ReplayFrame
    {
        public int Step { get; set; }
        public long Revision { get; set; }
        public PlayerView FreePeoples { get; set; } = new PlayerView();
        public PlayerView Shadow { get; set; } = new PlayerView();
    }

    // battles.json: each finished battle's report and the revision it was complete at; a frame before that
    // revision shows the battle only through its own view.
    public sealed class ReplayBattleReport
    {
        public long Revision { get; set; }
        public BattleReportView Report { get; set; } = new BattleReportView();
    }
}
