using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// REPLAY-01 (RP01, RP02): a new match records its first state and one frame per accepted public change, as both
// sides see it, in the transaction that stores the state. Retries, rollbacks, rejections and private preferences
// add no frame; a restart continues the numbering; the frames go with the match (also a replaced demo).
public sealed class MatchRecordingTests
{
    [PostgresFact]
    public async Task EveryPublicChangeIsOneFrameEqualToBothSidesViews()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var game = await Game.Start(host);

        var changes = await game.Play(30);

        var frames = await Frames(game.Match);
        Assert.Equal(changes + 1, frames.Count);
        Assert.Equal(Enumerable.Range(0, frames.Count), frames.Select(f => f.Step));
        Assert.True(frames.Zip(frames.Skip(1)).All(pair => pair.First.Revision < pair.Second.Revision));
        Assert.All(frames, frame => Assert.Empty(frame.FreePeoples.History));
        Assert.True(frames.Max(f => f.Size) < 2 * frames.Min(f => f.Size), "Frames do not grow with the game's history.");
    }

    [PostgresFact]
    public async Task RetriesRollbacksRejectionsAndARestartAddNoGapOrDuplicate()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var game = await Game.Start(host);
        var (command, account) = await game.Next();

        var first = await game.Store.Execute(command, account);
        var retry = await game.Store.Execute(command, account);
        Assert.Equal(first!.Revision, retry!.Revision);
        Assert.Equal(2, (await Frames(game.Match)).Count);

        var (next, nextAccount) = await game.Next();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            game.Store.Execute(next, nextAccount, beforeCommit: () => throw new InvalidOperationException("Fault before commit.")));
        Assert.Equal(2, (await Frames(game.Match)).Count);
        var stale = await game.Store.Execute(Copy(next, revision: next.ExpectedRevision - 1), nextAccount);
        Assert.False(stale!.Accepted);
        Assert.Equal(2, (await Frames(game.Match)).Count);

        var restarted = new MatchStore(host.Services.GetRequiredService<Database>(),
            host.Services.GetRequiredService<GameEngine>(), new ServerRandom());
        Assert.True((await restarted.Execute(next, nextAccount))!.Accepted);
        var frames = await Frames(game.Match);
        Assert.Equal([0, 1, 2], frames.Select(f => f.Step));
        await game.Check(frames[^1]);
    }

    [PostgresFact]
    public async Task APrivatePreferenceAddsNoFrame()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var (fp, sh) = (await MenuAccounts.Register(host), await MenuAccounts.Register(host));
        var store = host.Services.GetRequiredService<MatchStore>();
        var state = AutoDecisionTests.Battle(Side.FreePeoples);
        state.Id = Guid.NewGuid().ToString("N");
        await store.Create(state, fp.AccountId, sh.AccountId);
        var defender = BattleRuntime.Current(state)!.Defender.Side;

        var receipt = await store.Execute(AutoDecisionTests.Change(state, defender, true),
            defender == Side.FreePeoples ? fp.AccountId : sh.AccountId);

        Assert.True(receipt!.Accepted, receipt.Error);
        Assert.False(receipt.PublicStateChanged);
        Assert.Single(await Frames(state.Id));
    }

    [PostgresFact]
    public async Task ADemoRecordsTheCpuMovesAndItsFramesGoWhenItIsReplaced()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var owner = await MenuAccounts.Register(host);
        var rooms = host.Services.GetRequiredService<RoomStore>();
        var demos = host.Services.GetRequiredService<DemoSessionStore>();
        var room = await rooms.Create(owner.AccountId, Side.FreePeoples);
        var demo = (await demos.Start(owner.AccountId, room.Id)).Demo!;
        var game = new Game(host, demo.MatchId, owner.AccountId, DemoSessionStore.CpuAccount);

        var changes = await game.Play(12);

        var frames = await Frames(demo.MatchId);
        Assert.Equal(changes + 1, frames.Count);
        Assert.True(game.CpuMoves > 0, "The CPU moved in the recorded part.");
        await demos.Start(owner.AccountId, room.Id);
        Assert.Empty(await Frames(demo.MatchId));
    }

    [PostgresFact]
    public async Task AMatchStartedBeforeRecordingStaysUnrecordedAndADeletedOneTakesItsFrames()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var legacy = await Game.Start(host);
        await Execute("UPDATE matches SET recorded=false WHERE id=$1", legacy.Match);
        await Execute("DELETE FROM match_frames WHERE match_id=$1", legacy.Match);
        await legacy.Play(3);
        Assert.Empty(await Frames(legacy.Match));

        var deleted = await Game.Start(host);
        await deleted.Play(2);
        Assert.True(await host.Services.GetRequiredService<MatchRemoval>().Delete(deleted.Match, deleted.FreePeoples));
        Assert.Empty(await Frames(deleted.Match));
    }

    private static GameCommand Copy(GameCommand command, long revision) => new()
    {
        MatchId = command.MatchId, CommandId = Guid.NewGuid().ToString("N"), ExpectedRevision = revision,
        DecisionId = command.DecisionId, Kind = command.Kind, Payload = command.Payload
    };

    private sealed record Frame(int Step, long Revision, PlayerView FreePeoples, PlayerView Shadow, int Size);

    private static async Task<List<Frame>> Frames(string match)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand(
            "SELECT step,revision,free_peoples::text,shadow::text FROM match_frames WHERE match_id=$1 ORDER BY step");
        command.Parameters.Add(new NpgsqlParameter { Value = match });
        await using var reader = await command.ExecuteReaderAsync();
        var frames = new List<Frame>();
        while (await reader.ReadAsync())
        {
            var (free, shadow) = (reader.GetString(2), reader.GetString(3));
            frames.Add(new Frame(reader.GetInt32(0), reader.GetInt64(1), View(free), View(shadow), free.Length + shadow.Length));
        }
        return frames;
    }

    private static PlayerView View(string json) => JsonSerializer.Deserialize<PlayerView>(json, BoardCatalog.Json)!;

    private static async Task Execute(string sql, string value)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand(sql);
        command.Parameters.Add(new NpgsqlParameter { Value = value });
        await command.ExecuteNonQueryAsync();
    }

    // A match driven by the demo bot for both sides through the real MatchStore, checking each new frame.
    private sealed class Game(AccountHost host, string match, string freePeoples, string shadow)
    {
        public string Match => match;
        public string FreePeoples => freePeoples;
        public MatchStore Store { get; } = host.Services.GetRequiredService<MatchStore>();
        public int CpuMoves { get; private set; }
        private readonly DemoBot bot = host.Services.GetRequiredService<DemoBot>();

        public static async Task<Game> Start(AccountHost host)
        {
            var (fp, sh) = (await MenuAccounts.Register(host), await MenuAccounts.Register(host));
            var rooms = host.Services.GetRequiredService<RoomStore>();
            var room = await rooms.Create(fp.AccountId, Side.FreePeoples);
            await rooms.Join(room.JoinCode, sh.AccountId);
            await rooms.Confirm(room.Id, fp.AccountId, 1);
            await rooms.Confirm(room.Id, sh.AccountId, 1);
            var started = await rooms.Start(room.Id, fp.AccountId, 1);
            return new Game(host, started.Room!.MatchId, fp.AccountId, sh.AccountId);
        }

        public async Task<(GameCommand Command, string Account)> Next()
        {
            var state = await State();
            var owner = state.Pending!.Owner;
            var command = bot.Next(state, owner)!;
            command.CommandId = Guid.NewGuid().ToString("N");
            return (command, owner == Side.FreePeoples ? freePeoples : shadow);
        }

        // Plays up to the given number of public changes and returns how many there were.
        public async Task<int> Play(int moves)
        {
            var changes = 0;
            for (var i = 0; i < moves && (await State()).Winner is null; i++)
            {
                var (command, account) = await Next();
                var receipt = await Store.Execute(command, account);
                Assert.True(receipt!.Accepted, receipt.Error);
                if (account == DemoSessionStore.CpuAccount)
                    CpuMoves++;
                if (!receipt.PublicStateChanged)
                    continue;
                changes++;
                var frames = await Frames(match);
                if (frames.Count > 0)
                    await Check(frames[^1]);
            }
            return changes;
        }

        // The frame shows exactly what each side's read-only view shows of the stored state.
        public async Task Check(Frame frame)
        {
            var state = await State();
            Assert.Equal(state.Revision, frame.Revision);
            Assert.Equal(MatchFrames.View(state, Side.FreePeoples), Json(frame.FreePeoples));
            Assert.Equal(MatchFrames.View(state, Side.Shadow), Json(frame.Shadow));
            Assert.Equal(state.Player(Side.Shadow).Hand, frame.Shadow.Hand);
        }

        private async Task<MatchState> State() => (await Store.Read(match, freePeoples))!.State;

        private static string Json(PlayerView view) => JsonSerializer.Serialize(view, BoardCatalog.Json);
    }
}
