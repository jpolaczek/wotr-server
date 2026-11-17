using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Wotr.Domain.BotProtocol;

// BOT-225: one line of JSON each way (SPEC §2). A transport is a TCP stream to the operator's sidecar, a child process's
// standard streams, or an in-memory pair in tests.
public interface IProtocolTransport : IDisposable
{
    Task Send(string line, CancellationToken cancel);
    Task<string?> Receive(CancellationToken cancel);
}

public sealed class StreamTransport(Stream input, Stream output, IDisposable? owner = null) : IProtocolTransport
{
    private readonly StreamReader reader = new(input, new UTF8Encoding(false));
    private readonly StreamWriter writer = new(output, new UTF8Encoding(false)) { NewLine = "\n", AutoFlush = true };

    public static async Task<StreamTransport> Connect(string host, int port, CancellationToken cancel)
    {
        var client = new TcpClient { NoDelay = true };
        try
        {
            await client.ConnectAsync(host, port, cancel);
            var stream = client.GetStream();
            return new StreamTransport(stream, stream, client);
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    // A bot as a child process (SPEC §2, local): its stderr is a log, never protocol.
    public static StreamTransport Start(ProcessStartInfo start, Action<string>? log = null)
    {
        start.RedirectStandardInput = true;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.UseShellExecute = false;
        var process = Process.Start(start) ?? throw new InvalidOperationException("The protocol bot did not start.");
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) log?.Invoke(e.Data); };
        process.BeginErrorReadLine();
        return new StreamTransport(process.StandardOutput.BaseStream, process.StandardInput.BaseStream, new ProcessOwner(process));
    }

    public Task Send(string line, CancellationToken cancel) => writer.WriteLineAsync(line.AsMemory(), cancel);

    public async Task<string?> Receive(CancellationToken cancel) => await reader.ReadLineAsync(cancel);

    public void Dispose()
    {
        writer.Dispose();
        reader.Dispose();
        owner?.Dispose();
    }

    private sealed class ProcessOwner(Process process) : IDisposable
    {
        public void Dispose()
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
            process.Dispose();
        }
    }
}

// What a game tells the bot when it starts (SPEC §3).
public sealed record ProtocolGame(string GameId, string Side, int SeedBot, int ClockMilliseconds,
    IReadOnlyDictionary<string, JsonNode?> OptionValues);

// The bot's answer to one question, or why there is none.
public sealed record ProtocolAnswer(int? Option, string Outcome, double Seconds, JsonNode? Info = null);

// What the bot said about itself in its hello.
public sealed record ProtocolBotIdentity(string Name, string Version, string Author, bool Events,
    IReadOnlyList<JsonObject> Options);

// One game with one bot over a transport: the session's opening, the questions with their clock, the events and the end.
// Outcomes are words the seat reports: "answered", "timeout", "illegal", "closed", "malformed".
public sealed class ProtocolSession : IDisposable
{
    public const string Referee = "wotr-server";
    private readonly IProtocolTransport transport;
    private readonly string gameId;
    private int next = 1;
    private int sequence;

    private ProtocolSession(IProtocolTransport transport, string gameId, ProtocolBotIdentity bot)
    {
        this.transport = transport;
        this.gameId = gameId;
        Bot = bot;
    }

    public ProtocolBotIdentity Bot { get; }
    public int Ignored { get; private set; }

    public static async Task<ProtocolSession> Open(IProtocolTransport transport, ProtocolGame game, TimeSpan timeout,
        CancellationToken cancel)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        limit.CancelAfter(timeout);
        await Send(transport, new JsonObject
        {
            ["type"] = "hello", ["protocol"] = ProtocolVocabulary.Protocol, ["referee"] = Referee, ["id"] = 1
        }, limit.Token);
        var hello = await Expect(transport, "hello", 1, limit.Token) ?? throw new IOException("The bot sent no hello.");
        var declared = (hello["options"] as JsonArray)?.OfType<JsonObject>().ToList() ?? new List<JsonObject>();
        var bot = new ProtocolBotIdentity((string?)hello["name"] ?? "", (string?)hello["version"] ?? "",
            (string?)hello["author"] ?? "", hello["events"] is JsonValue e && e.TryGetValue<bool>(out var on) && on, declared);
        var values = new JsonObject();
        foreach (var option in declared)
            if ((string?)option["name"] is { Length: > 0 } name)
                values[name] = (game.OptionValues.TryGetValue(name, out var set) ? set : option["default"])?.DeepClone();
        await Send(transport, new JsonObject { ["type"] = "options", ["values"] = values }, limit.Token);
        await Send(transport, new JsonObject
        {
            ["type"] = "new_game", ["id"] = 2, ["game"] = game.GameId, ["side"] = game.Side, ["rules"] = ProtocolVocabulary.Rules,
            ["variant"] = new JsonArray(), ["seed_bot"] = game.SeedBot,
            ["clock"] = new JsonObject { ["kind"] = "per_decision", ["ms"] = game.ClockMilliseconds }
        }, limit.Token);
        _ = await Expect(transport, "ready", 2, limit.Token) ?? throw new IOException("The bot was not ready.");
        return new ProtocolSession(transport, game.GameId, bot) { next = 3 };
    }

    public static JsonObject Decision(string side, ProtocolNode node, string prompt)
    {
        var options = new List<JsonObject>();
        for (var o = 0; o < node.Branches.Count; o++)
        {
            var option = new JsonObject { ["o"] = o };
            foreach (var (key, value) in node.Branches[o].Atom)
                option[key] = value?.DeepClone();
            option["text"] = node.Branches[o].Text;
            options.Add(option);
        }
        var decision = new JsonObject
        {
            ["kind"] = node.Question, ["side"] = side, ["prompt"] = prompt,
            ["options"] = new JsonArray(options.Select(o => (JsonNode)o).ToArray()),
            ["fingerprint"] = ProtocolJson.Fingerprint(side, node.Question, options)
        };
        if (node.Context is not null)
            decision["context"] = node.Context.DeepClone();
        return decision;
    }

    // Asks one question and waits for its answer until the clock runs out. An illegal answer is answered with an error and
    // the question is asked once more (SPEC §4); a second illegal answer, a timeout or a closed stream ends the question
    // without an option, and the caller takes the decision elsewhere.
    public async Task<ProtocolAnswer> Ask(JsonObject decision, Func<JsonObject> view, int clockMilliseconds,
        CancellationToken cancel)
    {
        var watch = Stopwatch.StartNew();
        var count = ((JsonArray)decision["options"]!).Count;
        var fingerprint = (string?)decision["fingerprint"];
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var id = next++;
            var left = Math.Max(1, clockMilliseconds - (int)watch.ElapsedMilliseconds);
            await Send(transport, new JsonObject
            {
                ["type"] = "decide", ["id"] = id, ["game"] = gameId, ["decision"] = decision.DeepClone(), ["view"] = view(),
                ["clock"] = new JsonObject { ["left_ms"] = left }
            }, cancel);
            using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            limit.CancelAfter(left);
            while (true)
            {
                JsonObject? message;
                try
                {
                    message = await Read(limit.Token);
                }
                catch (OperationCanceledException) when (!cancel.IsCancellationRequested)
                {
                    await Send(transport, new JsonObject { ["type"] = "timeout", ["id"] = id, ["game"] = gameId, ["played"] = null }, cancel);
                    return new ProtocolAnswer(null, "timeout", watch.Elapsed.TotalSeconds);
                }
                if (message is null)
                    return new ProtocolAnswer(null, "closed", watch.Elapsed.TotalSeconds);
                var type = (string?)message["type"];
                if (type == "get_view" && message["id"] is { } viewId)
                {
                    await Send(transport, new JsonObject { ["type"] = "view", ["id"] = viewId.DeepClone(), ["view"] = view() }, cancel);
                    continue;
                }
                if (Id(message) != id)
                {
                    Ignored++;
                    continue;
                }
                string? code = null;
                if (type != "choice")
                    code = "not_a_choice";
                else if (message["fingerprint"] is JsonValue f && f.TryGetValue<string>(out var echoed) && echoed != fingerprint)
                    code = "stale_fingerprint";
                else if (message["o"] is not JsonValue v || !v.TryGetValue<int>(out var chosen) || chosen < 0 || chosen >= count)
                    code = "illegal_option";
                else
                    return new ProtocolAnswer(chosen, "answered", watch.Elapsed.TotalSeconds, message["info"]?.DeepClone());
                await Send(transport, new JsonObject { ["type"] = "error", ["id"] = id, ["code"] = code }, cancel);
                break;
            }
        }
        return new ProtocolAnswer(null, "illegal", watch.Elapsed.TotalSeconds);
    }

    public Task Event(JsonObject e, CancellationToken cancel) => Send(transport, new JsonObject
    {
        ["type"] = "event", ["game"] = gameId, ["seq"] = ++sequence, ["event"] = e
    }, cancel);

    public async Task End(string? winner, string how, int turn, CancellationToken cancel)
    {
        await Send(transport, new JsonObject
        {
            ["type"] = "game_over", ["game"] = gameId, ["winner"] = winner, ["how"] = how, ["turn"] = turn
        }, cancel);
        await Send(transport, new JsonObject { ["type"] = "quit" }, cancel);
    }

    public void Dispose() => transport.Dispose();

    private async Task<JsonObject?> Read(CancellationToken cancel)
    {
        while (true)
        {
            var line = await transport.Receive(cancel);
            if (line is null)
                return null;
            if (Parse(line) is { } message)
                return message;
            Ignored++;
        }
    }

    private static int? Id(JsonObject message) =>
        message["id"] is JsonValue value && value.TryGetValue<int>(out var id) ? id : null;

    private static Task Send(IProtocolTransport transport, JsonObject message, CancellationToken cancel) =>
        transport.Send(ProtocolJson.Write(message), cancel);

    private static async Task<JsonObject?> Expect(IProtocolTransport transport, string type, int id, CancellationToken cancel)
    {
        while (true)
        {
            var line = await transport.Receive(cancel);
            if (line is null)
                return null;
            if (Parse(line) is { } message && (string?)message["type"] == type && Id(message) == id)
                return message;
        }
    }

    private static JsonObject? Parse(string line)
    {
        if (line.Length > 1_000_000)
            return null;
        try
        {
            return JsonNode.Parse(line) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
