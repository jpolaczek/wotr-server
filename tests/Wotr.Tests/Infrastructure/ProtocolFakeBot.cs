using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;

namespace Wotr.Tests;

// A protocol bot behind a TCP listener: it declares a "sims" option, answers every question at random (or illegally
// once when asked to), echoes the fingerprint and records what it was sent.
internal sealed class ProtocolFakeBot : IDisposable
{
    private readonly TcpListener listener = new(IPAddress.Loopback, 0);
    private readonly Random random;
    private readonly CancellationTokenSource stop = new();
    public List<JsonObject> Received { get; } = new();
    public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
    public bool IllegalFirst { get; set; }
    public int Connections { get; private set; }

    public ProtocolFakeBot(int seed)
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
