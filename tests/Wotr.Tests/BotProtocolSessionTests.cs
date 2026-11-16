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

    // A protocol bot behind a TCP listener: it declares a "sims" option, answers every question at random (or illegally
    // once when asked to), echoes the fingerprint and records what it was sent.
    private sealed class FakeBot : IDisposable
    {
        private readonly TcpListener listener = new(IPAddress.Loopback, 0);
        private readonly Random random;
        private readonly CancellationTokenSource stop = new();
        public List<JsonObject> Received { get; } = new();
        public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
        public bool IllegalFirst { get; set; }
        public int Connections { get; private set; }

        public FakeBot(int seed)
        {
            random = new Random(seed);
            listener.Start();
            _ = Accept();
        }

        private async Task Accept()
        {
            while (!stop.IsCancellationRequested)
            {
                TcpClient client;
                try { client = await listener.AcceptTcpClientAsync(stop.Token); }
                catch (OperationCanceledException) { return; }
                Connections++;
                _ = Serve(client);
            }
        }

        private async Task Serve(TcpClient client)
        {
            using var _ = client;
            var stream = client.GetStream();
            using var reader = new StreamReader(stream, new UTF8Encoding(false));
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\n", AutoFlush = true };
            while (await reader.ReadLineAsync() is { } line)
            {
                var message = (JsonObject)JsonNode.Parse(line)!;
                lock (Received)
                    Received.Add(message);
                var reply = Reply(message);
                if (reply is not null)
                    await writer.WriteLineAsync(reply.ToJsonString());
                if ((string?)message["type"] == "quit")
                    return;
            }
        }

        private JsonObject? Reply(JsonObject message) => (string?)message["type"] switch
        {
            "hello" => new JsonObject
            {
                ["type"] = "hello", ["id"] = message["id"]?.DeepClone(), ["protocol"] = "wotr-bot/0.1", ["name"] = "FakeBot",
                ["version"] = "1", ["events"] = true,
                ["options"] = new JsonArray(new JsonObject { ["name"] = "sims", ["type"] = "int", ["default"] = 300 })
            },
            "new_game" => new JsonObject { ["type"] = "ready", ["id"] = message["id"]?.DeepClone() },
            "decide" => Choose(message),
            _ => null
        };

        private JsonObject Choose(JsonObject message)
        {
            var decision = (JsonObject)message["decision"]!;
            var count = ((JsonArray)decision["options"]!).Count;
            var o = IllegalFirst ? count + 5 : random.Next(count);
            IllegalFirst = false;
            return new JsonObject
            {
                ["type"] = "choice", ["id"] = message["id"]?.DeepClone(), ["o"] = o, ["fingerprint"] = decision["fingerprint"]?.DeepClone()
            };
        }

        public void Dispose()
        {
            stop.Cancel();
            listener.Stop();
        }
    }

    private static ProtocolSeatHost Host(int port) => new(new ProtocolSeatOptions(true, "127.0.0.1", port, TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30), new Dictionary<string, JsonNode?> { ["sims"] = 50 }), Engine, NullLogger<ProtocolSeatHost>.Instance);

    [Fact]
    public async Task TheSessionOpensAsksWithItsFingerprintAndRetriesAnIllegalAnswerOnce()
    {
        using var bot = new FakeBot(1) { IllegalFirst = true };
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
        using var bot = new FakeBot(seed);
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
        lock (bot.Received)
        {
            Assert.Contains(bot.Received, m => (string?)m["type"] == "event" && (string?)m["event"]!["kind"] == "turn_started");
            if (state.Winner is not null)
                Assert.Contains(bot.Received, m => (string?)m["type"] == "game_over");
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
