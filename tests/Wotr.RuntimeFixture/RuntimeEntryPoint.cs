using System.Text.Json;
using System.Text.Json.Nodes;

namespace Wotr.Bot.Runtime;

// Public ABI fixture only: no private policy, game engine or gameplay acceptance.
public sealed class RuntimeEntryPoint
{
    public string Describe() => JsonSerializer.Serialize(new
    {
        abiVersion = 2, runtimeVersion = typeof(RuntimeEntryPoint).Assembly.GetName().Version!.ToString(3), protocolVersion = 2,
        memoryFormat = "fixture.memory/1",
        rulesVersion = "base-2e-2011-faq1.2", contentVersion = "base-setup-1",
        gameplayReady = true, profiles = new[] { "free-peoples-strong", "shadow-strong" }, modules = new[] { "base" },
        policies = new[] { "free-peoples-strong", "shadow-strong" }.Select(profile => new
        { profileId = profile, policyVersion = "fixture-1", features = new[] { "base" } })
    });

    public string Decide(string requestJson, CancellationToken cancellation)
    {
        var request = JsonNode.Parse(requestJson)!;
        var mode = request["memory"]?.GetValue<string>();
        if (mode == "delay")
            Thread.Sleep(250);
        if (mode == "throw")
            throw new InvalidOperationException("fixture-private-secret");
        if (mode == "malformed")
            return "not-json";
        if (mode == "large")
            return new string('x', 4 * 1024 * 1024 + 1);
        var player = request["player"]!;
        // Only a small scripted transport fixture, never a game policy.
        var allowed = new[] { "ContinueFellowship", "AllocateHunt", "CommitCombatCard", "DeclineReaction", "StopBattle", "FinishActionSequence", "Pass", "ResolveHuntDamage" };
        var kind = player["options"]!.AsArray().FirstOrDefault(option =>
            option?["enabled"]?.GetValue<bool>() != false && allowed.Contains(option!["kind"]!.GetValue<string>()))?["kind"]?.GetValue<string>();
        if (kind is null)
            return JsonSerializer.Serialize(new { abiVersion = 2, invocationId = request["invocationId"]!.GetValue<string>(),
                status = "NoDecision", reason = "FixtureLimit", modules = new[] { "base" } });
        var payload = new { ids = mode == "illegal" ? new[] { "foreign-die" } : Array.Empty<string>() };
        if (mode == "preview-loop" || mode == "preview" && request["previews"]!.AsArray().Count == 0)
            return JsonSerializer.Serialize(new { abiVersion = 2, invocationId = request["invocationId"]!.GetValue<string>(),
                status = "NeedPreviews", reason = "", modules = new[] { "base" }, policyVersion = "fixture-1",
                previewRequests = new[] { new { matchId = request["matchId"]!.GetValue<string>(),
                    expectedRevision = player["revision"]!.GetValue<long>(), decisionId = player["decision"]!["id"]!.GetValue<string>(),
                    commandId = "preview", kind, payload } } });
        var pause = Environment.GetEnvironmentVariable("WOTR_FIXTURE_PAUSE_FILE");
        if (pause is not null)
        {
            File.AppendAllText(pause + ".entered", request["invocationId"]!.GetValue<string>() + "\n");
            while (File.Exists(pause))
                Thread.Sleep(10); // Deliberately ignores cancellation for timeout/restart tests.
        }
        return JsonSerializer.Serialize(new
        {
            abiVersion = 2,
            invocationId = mode == "correlation" ? "different" : request["invocationId"]!.GetValue<string>(),
            status = "Proposal", reason = "", modules = new[] { "base" }, policyVersion = "fixture-1",
            proposedMemory = JsonSerializer.Serialize(new { acceptedFrom = player["revision"]!.GetValue<long>(), marker = "fixture-private-memory",
                workLimit = request["maximumWork"]!.GetValue<int>() }),
            command = new
            {
                matchId = mode == "foreign" ? "foreign" : request["matchId"]!.GetValue<string>(),
                expectedRevision = player["revision"]!.GetValue<long>() + (mode == "future" ? 1 : 0),
                decisionId = player["decision"]!["id"]!.GetValue<string>(),
                commandId = request["invocationId"]!.GetValue<string>(), kind, payload
            }
        });
    }
}
