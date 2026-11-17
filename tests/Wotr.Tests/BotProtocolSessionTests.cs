using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.BotProtocol;
using Wotr.Server;
using Xunit;
using Xunit.Abstractions;

namespace Wotr.Tests;

// BOT-225: the seat's session with a protocol bot over TCP (SPEC §2-§4): the opening (hello, options, new_game, ready), the
// questions with their fingerprint and clock, one retry after an illegal answer, the events the bot follows, and whole base
// games where a random bot behind a socket plays the CPU seat through the server's ProtocolSeatHost.
public sealed class BotProtocolSessionTests(ITestOutputHelper output)
{
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);
    private static readonly DemoBot Demo = new(Engine, Views);

    private static ProtocolSeatHost Host(int port) => new(new ProtocolSeatOptions(true, "127.0.0.1", port, TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30), new Dictionary<string, JsonNode?> { ["sims"] = 50 }), Engine, NullLogger<ProtocolSeatHost>.Instance);

    [Fact]
    public async Task TheSessionOpensAsksWithItsFingerprintAndRetriesAnIllegalAnswerOnce()
    {
        using var bot = new ProtocolFakeBot(1) { IllegalFirst = true };
        using var host = Host(bot.Port);
        var state = NewGame("protocol-session", 3);
        var side = state.Pending!.Owner;
        var view = Views.For(state, side);
        var result = await host.Decide(view, state, Guid.NewGuid().ToString("N"), CancellationToken.None);
        Assert.Equal("Answered", result.Outcome);
        Assert.Null(Engine.Validate(state, side, result.Command!));
        List<JsonObject> received;
        lock (bot.Received)
            received = bot.Received.ToList();
        var types = received.Select(m => (string?)m["type"]).ToList();
        Assert.Equal(new[] { "hello", "options", "new_game" }, types.Take(3));
        Assert.Equal(50, (int)received[1]["values"]!["sims"]!);
        var game = received[2];
        Assert.Equal("base-2e", (string?)game["rules"]);
        Assert.Equal(ProtocolVocabulary.Side(side), (string?)game["side"]);
        Assert.Contains("error", types);
        var decide = received.First(m => (string?)m["type"] == "decide");
        var decision = (JsonObject)decide["decision"]!;
        var options = ((JsonArray)decision["options"]!).Select(o => (JsonObject)o!).ToList();
        Assert.Equal((string?)decision["fingerprint"], ProtocolJson.Fingerprint((string)decision["side"]!, (string)decision["kind"]!, options));
        Assert.True((int)decide["clock"]!["left_ms"]! > 0);
        Assert.Equal(ProtocolVocabulary.Side(side), (string?)decide["view"]!["viewer"]);
    }

    [Theory]
    [InlineData(21, Side.Shadow)]
    [InlineData(22, Side.FreePeoples)]
    public async Task ARandomBotBehindASocketPlaysAWholeGameThroughTheSeatHost(int seed, Side seat)
    {
        using var bot = new ProtocolFakeBot(seed);
        using var host = Host(bot.Port);
        var random = new SeededRandom(seed);
        var state = NewGame("protocol-host-" + seed, seed);
        var outcomes = new Dictionary<string, int>();
        for (var step = 0; step < 6000 && state.Winner is null && state.Turn <= 40; step++)
        {
            var side = state.Pending!.Owner;
            GameCommand command;
            if (side == seat)
            {
                var view = Views.For(state, side);
                var result = await host.Decide(view, state, Guid.NewGuid().ToString("N"), CancellationToken.None);
                outcomes[result.Outcome] = outcomes.GetValueOrDefault(result.Outcome) + 1;
                Assert.True(result.Command is not null, result.Outcome);
                command = result.Command!;
                var executed = Engine.Execute(state, side, command, random);
                Assert.True(executed.Receipt.Accepted, $"{command.Kind}: {executed.Receipt.Error}");
                await host.Applied(view, command, CancellationToken.None);
                state = executed.State;
                continue;
            }
            command = Demo.Next(state, side)!;
            state = Engine.Execute(state, side, command, random).State;
        }
        await host.Finished(Views.For(state, seat), CancellationToken.None);
        output.WriteLine($"seed {seed} seat {seat}: turn {state.Turn}, winner {state.Winner?.ToString() ?? "none"}; " +
            string.Join(", ", outcomes.Select(o => $"{o.Key} {o.Value}")));
        Assert.Equal(1, bot.Connections);
        Assert.True(outcomes.Keys.All(k => k is "Answered" or "Forced"));
        // The bot reads its socket on its own task: the end of the game reaches it a moment after the host sent it.
        for (var wait = 0; wait < 100 && state.Winner is not null; wait++)
        {
            lock (bot.Received)
                if (bot.Received.Any(m => (string?)m["type"] == "game_over"))
                    break;
            await Task.Delay(50);
        }
        lock (bot.Received)
        {
            Assert.Contains(bot.Received, m => (string?)m["type"] == "event" && (string?)m["event"]!["kind"] == "turn_started");
            if (state.Winner is not null)
                Assert.Contains(bot.Received, m => (string?)m["type"] == "game_over");
        }
    }

    // BOT-224: a failing seat is asked again after a growing interval and writes each failed decision for replay.
    [Fact]
    public async Task AFailingSeatBacksOffAndWritesTheDecisionForReplay()
    {
        var folder = Path.Combine(Path.GetTempPath(), "protocol-dump-" + Guid.NewGuid().ToString("N"));
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        using var host = new ProtocolSeatHost(new ProtocolSeatOptions(true, "127.0.0.1", port, TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(10), new Dictionary<string, JsonNode?>()) { DumpDirectory = folder }, Engine, NullLogger<ProtocolSeatHost>.Instance);
        try
        {
            // A position with a real question (the Shadow's Hunt allocation of the first turn).
            var state = NewGame("protocol-backoff", 5);
            for (var step = 0; step < 50 && state.Pending!.Kind != DecisionKind.AllocateHunt; step++)
                state = Engine.Execute(state, state.Pending.Owner, Demo.Next(state, state.Pending.Owner)!, new SeededRandom(5)).State;
            var view = Views.For(state, state.Pending!.Owner);
            var delays = new List<TimeSpan>();
            for (var attempt = 0; attempt < 5; attempt++)
            {
                var result = await host.Decide(view, state, Guid.NewGuid().ToString("N"), CancellationToken.None);
                Assert.Equal("SidecarUnavailable", result.Outcome);
                Assert.Null(result.Command);
                delays.Add(host.RetryDelay(view.MatchId, view.You));
            }
            Assert.Equal(new[] { 5.0, 10, 20, 40, 60 }, delays.Select(d => d.TotalSeconds));
            var dump = Assert.Single(Directory.GetFiles(folder));
            Assert.Contains("SidecarUnavailable", Path.GetFileName(dump));
            var record = JsonNode.Parse(File.ReadAllText(dump))!;
            Assert.Equal("AllocateHunt", (string?)record["decision"]);
            Assert.Equal(view.Revision, (long)record["view"]!["revision"]!);
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    // R11: the view a seat sends never holds a card of the opponent's hand, in any decision of a whole game.
    [Fact]
    public void TheViewNeverCarriesTheOpponentsHand()
    {
        var random = new SeededRandom(31);
        var state = NewGame("protocol-view", 31);
        var memories = new Dictionary<Side, ProtocolMemory> { [Side.FreePeoples] = new(), [Side.Shadow] = new() };
        for (var step = 0; step < 3000 && state.Winner is null && state.Turn <= 25; step++)
        {
            var side = state.Pending!.Owner;
            var other = side == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;
            var view = Views.For(state, side);
            var text = ProtocolJson.Write(ProtocolView.Write(view, memories[side]));
            foreach (var card in state.Player(other).Hand)
                Assert.DoesNotContain("\"" + ProtocolVocabulary.Card(card) + "\"", text);
            state = Engine.Execute(state, side, Demo.Next(state, side)!, random).State;
        }
    }

    private static MatchState NewGame(string id, int seed)
    {
        var state = MatchFactory.Create(id, new SeededRandom(seed), ConfigurationRules.Normalize(new()));
        TurnFlow.Start(state);
        return state;
    }
}
