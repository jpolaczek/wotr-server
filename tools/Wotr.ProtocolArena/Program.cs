using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;

// BOT-225/BOT-221: whole base games on the server's own engine with a protocol bot (the operator's sidecar) on one side or
// both, through the server's ProtocolSeatHost, without a database or HTTP. A decision the seat cannot answer ends the game
// with its error (no other engine plays for the bot). Each game is one JSON line: who won, the seat's outcomes, the
// decisions by kind and the time per decision. The bot's own log (its engine's answers, fallbacks, unmatched actions) is
// the sidecar's.
//
//   dotnet tools/Wotr.ProtocolArena/bin/Release/net10.0/Wotr.ProtocolArena.dll --games 2 --seed 1 \
//     --protocol shadow|free-peoples|both --port 7311 --sims 30 --seconds 120 --turns 40 --output arena.jsonl
var options = new Dictionary<string, string>(StringComparer.Ordinal);
for (var i = 0; i + 1 < args.Length; i += 2)
    options[args[i].TrimStart('-')] = args[i + 1];
string Option(string name, string fallback) => options.GetValueOrDefault(name, fallback);
var games = int.Parse(Option("games", "1"));
var seed = int.Parse(Option("seed", "1"));
var protocol = Option("protocol", "both");
var turns = int.Parse(Option("turns", "40"));
var output = Option("output", "protocol-arena.jsonl");
var seats = protocol switch
{
    "shadow" => new[] { Side.Shadow },
    "free-peoples" => new[] { Side.FreePeoples },
    _ => new[] { Side.FreePeoples, Side.Shadow }
};
var botOptions = new Dictionary<string, JsonNode?>();
if (options.TryGetValue("sims", out var sims))
    botOptions["sims"] = int.Parse(sims);
using var logging = LoggerFactory.Create(builder => builder.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Information));
var engine = GameRuntime.Engine(EffectCatalog.All);
var views = GameRuntime.Projection(EffectCatalog.All);
var demo = new DemoBot(engine, views);
using var host = new ProtocolSeatHost(new ProtocolSeatOptions(true, Option("host", "127.0.0.1"), int.Parse(Option("port", "7311")),
    TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(int.Parse(Option("seconds", "120"))), botOptions), engine,
    logging.CreateLogger<ProtocolSeatHost>());
if (options.TryGetValue("trace", out var tracePath))
{
    var writer = new StreamWriter(tracePath, append: true) { AutoFlush = true };
    host.Trace = line => { lock (writer) writer.WriteLine(line.ToJsonString()); };
}
for (var game = 0; game < games; game++)
{
    var gameSeed = seed + game;
    var random = new SeededRandom(gameSeed);
    var state = MatchFactory.Create($"arena-{gameSeed}-{Guid.NewGuid():N}"[..40], new SeededRandom(gameSeed), ConfigurationRules.Normalize(new()));
    TurnFlow.Start(state);
    var outcomes = new Dictionary<string, int>();
    var kinds = new Dictionary<string, int>();
    var times = new List<double>();
    string? error = null;
    var watch = Stopwatch.StartNew();
    for (var step = 0; step < 20_000 && state.Winner is null && state.Turn <= turns; step++)
    {
        var side = state.Pending!.Owner;
        GameCommand? command;
        if (seats.Contains(side))
        {
            var view = views.For(state, side);
            var result = await host.Decide(view, state, Guid.NewGuid().ToString("N"), CancellationToken.None);
            outcomes[result.Outcome] = outcomes.GetValueOrDefault(result.Outcome) + 1;
            var kind = view.Decision!.Kind.ToString();
            kinds[kind] = kinds.GetValueOrDefault(kind) + 1;
            if (result.Outcome == "Answered")
                times.Add(result.Seconds);
            if (result.Command is null)
            {
                error = $"turn {state.Turn} {side} {kind}: {result.Outcome}";
                break;
            }
            command = result.Command;
            var executed = engine.Execute(state, side, command, random);
            if (!executed.Receipt.Accepted)
            {
                error = $"turn {state.Turn} {side} {kind}: refused {command.Kind} {executed.Receipt.Error}";
                break;
            }
            await host.Applied(view, command, CancellationToken.None);
            state = executed.State;
            continue;
        }
        command = demo.Next(state, side);
        if (command is null)
        {
            error = $"turn {state.Turn} {side}: the demo bot has no move";
            break;
        }
        state = engine.Execute(state, side, command, random).State;
    }
    foreach (var side in seats)
        await host.Finished(views.For(state, side), CancellationToken.None);
    times.Sort();
    var line = new JsonObject
    {
        ["seed"] = gameSeed, ["protocol"] = protocol, ["turn"] = state.Turn, ["winner"] = state.Winner?.ToString(),
        ["victory"] = state.Victory?.ToString(), ["error"] = error, ["minutes"] = Math.Round(watch.Elapsed.TotalMinutes, 1),
        ["outcomes"] = JsonSerializer.SerializeToNode(outcomes), ["kinds"] = JsonSerializer.SerializeToNode(kinds),
        ["p50"] = times.Count > 0 ? times[times.Count / 2] : 0, ["p95"] = times.Count > 0 ? times[(int)(times.Count * 0.95)] : 0,
        ["max"] = times.Count > 0 ? times[^1] : 0
    };
    await File.AppendAllTextAsync(output, line.ToJsonString() + "\n");
    Console.WriteLine(line.ToJsonString());
}
