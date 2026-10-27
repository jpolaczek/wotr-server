using System.Text.Json;
using System.Text.Json.Nodes;

namespace Wotr.Bot.Runtime;

// Public ABI fixture only: no private policy, game engine or gameplay acceptance.
public sealed class RuntimeEntryPoint
{
    public string Describe() => JsonSerializer.Serialize(new
    {
        abiVersion = 1, runtimeVersion = "0.1.0", protocolVersion = 2,
        rulesVersion = "base-2e-2011-faq1.2", contentVersion = "base-setup-1",
        gameplayReady = true, profiles = new[] { "free-peoples-strong", "shadow-strong" }, modules = new[] { "base" }
    });

    public string Decide(string requestJson, CancellationToken cancellation)
    {
        var request = JsonNode.Parse(requestJson)!;
        var mode = request["memory"]?.GetValue<string>();
        if (mode == "throw")
            throw new InvalidOperationException("fixture-private-secret");
        if (mode == "malformed")
            return "not-json";
        if (mode == "large")
            return new string('x', 4 * 1024 * 1024 + 1);
        var player = request["player"]!;
        return JsonSerializer.Serialize(new
        {
            abiVersion = 1,
            invocationId = mode == "correlation" ? "different" : request["invocationId"]!.GetValue<string>(),
            status = "Proposal", reason = "", modules = new[] { "base" }, policyVersion = "fixture-1",
            proposedMemory = "{}",
            command = new
            {
                matchId = mode == "foreign" ? "foreign" : request["matchId"]!.GetValue<string>(),
                expectedRevision = player["revision"]!.GetValue<long>() + (mode == "future" ? 1 : 0),
                decisionId = player["decision"]!["id"]!.GetValue<string>(),
                commandId = "fixture-command", kind = "ContinueFellowship", payload = new { }
            }
        });
    }
}
