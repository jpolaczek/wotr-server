using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;
using Xunit.Abstractions;

namespace Wotr.Tests;

// REPLAY-07 (RP12) over the real server: two players play a whole game through the HTTP command route (the demo bot
// stands in for both people), an observer joins half-way and keeps watching to the end, and both the player and the
// observer download the replay. The two files show the same game, which is exactly the server's recording: every
// step from the first state to the final one, both sides' history and the turns. Deleting the game afterwards, and a
// finished demo replaced by a new one, leave the files whole. EXP-04: a whole Fate game gives the same.
public sealed class ReplayEndToEndTests(ITestOutputHelper output)
{
    private const int MoveLimit = 3000;
    private static readonly IReadOnlyDictionary<string, string?> Instant =
        new Dictionary<string, string?>(TestBudgets.Stress) { [DemoOptions.DelayKey] = "0" };

    [PostgresFact]
    public Task AWholeGameWithALateObserverGivesTheSameReplayToPlayerAndObserver() => WatchAndReplay(new MatchConfiguration());

    // EXP-04: the same for a whole Fate of Erebor game with New Cities; the replay keeps its rules and the Shadow's Erebor.
    [PostgresFact]
    public Task AWholeFateGameWithALateObserverGivesTheSameReplayToPlayerAndObserver() =>
        WatchAndReplay(new MatchConfiguration { Fate = true, NewCities = true });

    private async Task WatchAndReplay(MatchConfiguration configuration)
    {
        using var host = new AccountHost(extra: Instant);
        var game = await Game.Start(host, configuration);
        var half = await game.Play(MoveLimit, stopAtTurn: 3);
        using (var joined = await game.Observer.PostAsync($"/api/rooms/{game.Room}/join?role=Observer", null))
            Assert.Equal(HttpStatusCode.OK, joined.StatusCode);
        var clock = Stopwatch.StartNew();
        var rest = await game.Play(MoveLimit - half, stopAtTurn: int.MaxValue);
        var final = (await game.FreePeoples.GetFromJsonAsync<PlayerView>($"/api/matches/{game.Match}", BoardCatalog.Json))!;
        Assert.True(final.Winner.HasValue, $"No end after {half + rest} moves.");

        var fromPlayer = await Download(game.FreePeoples, $"/api/matches/{game.Match}/replay");
        var fromObserver = await Download(game.Observer, $"/api/observe/{game.Match}/replay");
        output.WriteLine($"{half + rest} moves, turn {final.Turn}, {final.Winner} ({final.Victory}); replay {fromPlayer.Length / 1024} KB; " +
            $"second half {clock.Elapsed.TotalSeconds:F1} s.");
        if (Environment.GetEnvironmentVariable("WOTR_REPLAY_FULL") is { Length: > 0 } target)
            await File.WriteAllBytesAsync(target, fromPlayer);

        var player = Read(fromPlayer);
        var observer = Read(fromObserver);
        Assert.Equal(player.Manifest.Files.Where(f => f.Path != ReplayFormat.ManifestEntry).Select(f => f.Sha256),
            observer.Manifest.Files.Select(f => f.Sha256));
        var recorded = await Recorded(game.Match);
        Assert.Equal(recorded.Count, player.Manifest.Steps);
        Assert.Equal(recorded, player.Frames.Select(Normalized));
        Assert.Equal(final.Winner, player.Frames[^1].FreePeoples.Winner);
        Assert.Equal(final.Winner, player.Manifest.Winner);
        Assert.Equal(player.Frames.GroupBy(f => f.FreePeoples.Turn).Select(g => g.Min(f => f.Step)), player.Turns.Select(t => t.Step));
        Assert.Equal(await LogSteps(game.FreePeoples, game.Match), player.History[Side.FreePeoples]);
        Assert.Equal(await LogSteps(game.Shadow, game.Match), player.History[Side.Shadow]);
        Assert.Equal(ExpansionModules.Of(configuration), ExpansionModules.Of(player.Manifest.Configuration));
        Assert.All(player.Frames, frame => Assert.Equal(configuration.Fate, frame.Shadow.Configuration.Fate));

        Assert.True(await host.Services.GetRequiredService<MatchRemoval>().Delete(game.Match, game.FreePeoplesAccount));
        Assert.Equal(HttpStatusCode.NotFound, (await game.FreePeoples.GetAsync($"/api/matches/{game.Match}/replay")).StatusCode);
        Assert.Equal(player.Manifest.Steps, Read(fromPlayer).Frames.Count);
    }

    [PostgresFact]
    public async Task AFinishedDemosReplayOutlivesTheDemoThatReplacedIt()
    {
        using var host = new AccountHost(extra: Instant);
        var session = await MenuAccounts.Register(host);
        using var client = MenuAccounts.Client(host, session);
        var room = await host.Services.GetRequiredService<RoomStore>().Create(session.AccountId, Side.Shadow);
        var demo = await StartDemo(client, room.Id);
        var bot = host.Services.GetRequiredService<DemoBot>();
        var store = host.Services.GetRequiredService<MatchStore>();
        var moves = 0;
        for (var view = await Owned(client, demo, Side.Shadow); !view.Winner.HasValue && moves < MoveLimit; moves++)
        {
            var state = (await store.Read(demo, session.AccountId))!.State;
            using var sent = await client.PostAsJsonAsync($"/api/matches/{demo}/commands", bot.Next(state, Side.Shadow)!, BoardCatalog.Json);
            Assert.True((await sent.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!.Accepted);
            view = await Owned(client, demo, Side.Shadow);
        }

        var saved = await Download(client, $"/api/matches/{demo}/replay");
        var replay = Read(saved);
        Assert.Contains(replay.Manifest.Players, p => p.Name == DemoSessionStore.CpuName);
        // A new demo starts from a new room, as the menu does (MENU-31); the waiting room of the first may be gone.
        var again = await host.Services.GetRequiredService<RoomStore>().Create(session.AccountId, Side.Shadow);
        var next = await StartDemo(client, again.Id);
        Assert.NotEqual(demo, next);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/matches/{demo}/replay")).StatusCode);
        Assert.Equal(replay.Manifest.Steps, Read(saved).Frames.Count);
        output.WriteLine($"Demo: {moves} own moves, {replay.Manifest.Steps} steps, replay {saved.Length / 1024} KB.");
    }

    private static async Task<string> StartDemo(HttpClient client, string room)
    {
        using var started = await client.PostAsync($"/api/rooms/{room}/demo", null);
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        return (await started.Content.ReadFromJsonAsync<DemoMatchView>(BoardCatalog.Json))!.MatchId;
    }

    // The owner's view once the decision is theirs or the game is over; each read also wakes the CPU.
    private static async Task<PlayerView> Owned(HttpClient client, string match, Side side)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (true)
        {
            var view = (await client.GetFromJsonAsync<PlayerView>($"/api/matches/{match}", BoardCatalog.Json))!;
            if (view.Winner.HasValue || view.DecisionOwner == side)
                return view;
            Assert.True(DateTime.UtcNow < deadline, "The CPU did not hand over.");
            await Task.Delay(20);
        }
    }

    private static async Task<byte[]> Download(HttpClient client, string route)
    {
        using var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsByteArrayAsync();
    }

    // Every step id of one player's history, from the newest page back.
    private static async Task<List<string>> LogSteps(HttpClient client, string match)
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

    private static async Task<List<string>> Recorded(string match)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand(
            "SELECT step,revision,free_peoples::text,shadow::text FROM match_frames WHERE match_id=$1 ORDER BY step");
        command.Parameters.Add(new NpgsqlParameter { Value = match });
        await using var reader = await command.ExecuteReaderAsync();
        var frames = new List<string>();
        while (await reader.ReadAsync())
            frames.Add(Normalized(new ReplayFrame
            {
                Step = reader.GetInt32(0), Revision = reader.GetInt64(1),
                FreePeoples = JsonSerializer.Deserialize<PlayerView>(reader.GetString(2), BoardCatalog.Json)!,
                Shadow = JsonSerializer.Deserialize<PlayerView>(reader.GetString(3), BoardCatalog.Json)!
            }));
        return frames;
    }

    private static string Normalized(ReplayFrame frame) => JsonSerializer.Serialize(frame, BoardCatalog.Json);

    private sealed record Replay(ReplayManifest Manifest, List<ReplayFrame> Frames, List<ReplayTurn> Turns, Dictionary<Side, List<string>> History);

    // The whole file read with the Contracts alone, each part checked against its sum.
    private static Replay Read(byte[] bytes)
    {
        using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        byte[] Entry(string name)
        {
            using var stream = zip.GetEntry(name)!.Open();
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            return copy.ToArray();
        }
        T Json<T>(string name) => JsonSerializer.Deserialize<T>(Entry(name), BoardCatalog.Json)!;
        var manifest = Json<ReplayManifest>(ReplayFormat.ManifestEntry);
        foreach (var file in manifest.Files)
            Assert.Equal(file.Sha256, Convert.ToHexStringLower(SHA256.HashData(Entry(file.Path))));
        var frames = Enumerable.Range(0, manifest.FrameChunks).SelectMany(c => Json<List<ReplayFrame>>(ReplayFormat.FramesEntry(c))).ToList();
        List<string> History(Side side, int chunks) => Enumerable.Range(0, chunks)
            .SelectMany(c => Json<ActionLogPage>(ReplayFormat.HistoryEntry(side, c)).Steps)
            .OrderBy(s => s.Revision).ThenBy(s => s.Order).Select(s => s.Id).ToList();
        return new Replay(manifest, frames, Json<List<ReplayTurn>>(ReplayFormat.TurnsEntry), new Dictionary<Side, List<string>>
        {
            [Side.FreePeoples] = History(Side.FreePeoples, manifest.FreePeoplesHistoryChunks),
            [Side.Shadow] = History(Side.Shadow, manifest.ShadowHistoryChunks)
        });
    }

    // A public room whose two players move with the demo bot through HTTP; the observer renews its place as it goes.
    private sealed class Game
    {
        public required string Room { get; init; }
        public required string Match { get; init; }
        public required HttpClient FreePeoples { get; init; }
        public required HttpClient Shadow { get; init; }
        public required HttpClient Observer { get; init; }
        public required string FreePeoplesAccount { get; init; }
        public required AccountHost Host { get; init; }

        public static async Task<Game> Start(AccountHost host, MatchConfiguration configuration)
        {
            var (fp, sh, watcher) = (await MenuAccounts.Register(host), await MenuAccounts.Register(host), await MenuAccounts.Register(host));
            var rooms = host.Services.GetRequiredService<RoomStore>();
            var room = await rooms.Create(fp.AccountId, new CreateRoomRequest { Name = "Whole game", Visibility = RoomVisibility.Public });
            await rooms.JoinPublic(room.Id, sh.AccountId);
            var version = (await rooms.Read(room.Id, fp.AccountId))!.Version;
            if (!ConfigurationRules.IsBase(configuration))
                version = (await rooms.Configure(room.Id, fp.AccountId,
                    new ConfigureRoomRequest { ExpectedVersion = version, Configuration = configuration })).Room!.Version;
            await rooms.Confirm(room.Id, fp.AccountId, version);
            await rooms.Confirm(room.Id, sh.AccountId, version);
            var started = await rooms.Start(room.Id, fp.AccountId, version);
            return new Game
            {
                Room = room.Id, Match = started.Room!.MatchId, FreePeoples = MenuAccounts.Client(host, fp),
                Shadow = MenuAccounts.Client(host, sh), Observer = MenuAccounts.Client(host, watcher),
                FreePeoplesAccount = fp.AccountId, Host = host
            };
        }

        // Moves until the game ends, the given turn begins or the limit; returns how many moves were made.
        public async Task<int> Play(int limit, int stopAtTurn)
        {
            var store = Host.Services.GetRequiredService<MatchStore>();
            var bot = Host.Services.GetRequiredService<DemoBot>();
            for (var moves = 0; moves < limit; moves++)
            {
                var state = (await store.Read(Match, FreePeoplesAccount))!.State;
                if (state.Winner is not null || state.Turn >= stopAtTurn)
                    return moves;
                var side = state.Pending!.Owner;
                var command = bot.Next(state, side)!;
                using var sent = await (side == Side.FreePeoples ? FreePeoples : Shadow)
                    .PostAsJsonAsync($"/api/matches/{Match}/commands", command, BoardCatalog.Json);
                var receipt = (await sent.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
                Assert.True(receipt.Accepted, $"{command.Kind}: {receipt.Error}");
                if (moves % 25 == 0)
                    using (await Observer.PostAsync($"/api/rooms/{Room}/presence", null)) { }
            }
            return limit;
        }
    }
}
