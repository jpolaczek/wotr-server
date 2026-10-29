using System.Text.Json;
using System.Text.Json.Nodes;

namespace Wotr.Bot.Runtime;

// Public, test-only ABI script in the seeder archive. Never a strategic policy or player dependency.
// It consumes only the copied player options. The scenario names select bounded transport examples.
public sealed class RuntimeEntryPoint
{
    public string Describe() => JsonSerializer.Serialize(new
    {
        abiVersion = 2, runtimeVersion = "0.0.13", protocolVersion = 2,
        rulesVersion = "base-2e-2011-faq1.2", contentVersion = "base-setup-1",
        memoryFormat = "client-fixture/13", gameplayReady = true,
        profiles = new[] { "free-peoples-strong", "shadow-strong" }, modules = new[] { "base" },
        policies = new[] { "free-peoples-strong", "shadow-strong" }.Select(profile => new
        { profileId = profile, policyVersion = "client-fixture-13", features = new[] { "base" } })
    });

    public string Decide(string requestJson, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        var request = JsonNode.Parse(requestJson)!;
        var player = request["player"]!;
        var mode = request["memory"]?.GetValue<string>() ?? "setup";
        var options = player["options"]!.AsArray().Where(o => o?["enabled"]?.GetValue<bool>() != false).ToArray();
        var kind = mode switch
        {
            "move" => "MoveArmies", "card" => "PlayEvent", "attack" => "AttackArmy",
            "combat" => "CommitCombatCard", "hunt" => "MoveFellowship",
            "finish" => "FinishActionSequence", "hunt-result" => "ResolveHuntDamage",
            "hunt-reroll" => "RerollHunt",
            _ => options.FirstOrDefault(o => new[] { "ContinueFellowship", "AllocateHunt" }
                .Contains(o!["kind"]!.GetValue<string>()))?["kind"]?.GetValue<string>()
        };
        var option = options.FirstOrDefault(o => o!["kind"]!.GetValue<string>() == kind);
        if (mode == "done" || option is null)
            return JsonSerializer.Serialize(new { abiVersion = 2, invocationId = request["invocationId"]!.GetValue<string>(),
                status = "NoDecision", reason = "ClientFixtureLimit", modules = new[] { "base" } });
        var payload = Payload(player, option!, kind!);
        return JsonSerializer.Serialize(new
        {
            abiVersion = 2, invocationId = request["invocationId"]!.GetValue<string>(),
            status = "Proposal", reason = "", modules = new[] { "base" }, policyVersion = "client-fixture-13",
            proposedMemory = mode switch { "setup" => "setup", "move" => "finish", "attack" => "combat", "hunt" => "hunt-result", _ => "done" },
            command = new
            {
                matchId = request["matchId"]!.GetValue<string>(), expectedRevision = player["revision"]!.GetValue<long>(),
                decisionId = player["decision"]!["id"]!.GetValue<string>(), commandId = request["invocationId"]!.GetValue<string>(),
                kind, payload
            }
        });
    }

    private static object Payload(JsonNode player, JsonNode option, string kind)
    {
        if (kind is "MoveArmies" or "AttackArmy")
        {
            var army = option["armies"]!.AsArray().First(a => a!["targets"]!.AsArray().Count > 0)!;
            var die = player["dice"]!.AsArray().First(d => d!["owner"]!.ToJsonString() == player["you"]!.ToJsonString() &&
                d["face"]!.GetValue<string>() == "Army" && d["location"]!.GetValue<string>() == "Available")!;
            return new { dieId = die["id"]!.GetValue<string>(), movementCost = "Army", moves = new[] { new
            {
                from = army["regionId"]!.GetValue<string>(), to = army["targets"]![0]!["regionId"]!.GetValue<string>(),
                unitIds = army["unitIds"]!.DeepClone(), characterIds = army["characterIds"]!.DeepClone()
            } } };
        }
        if (kind == "PlayEvent")
        {
            var card = option["events"]!.AsArray().Single(e => e!["cardId"]!.GetValue<string>() == "base.sh.strategy.20")!;
            var cost = card["costs"]![0]!;
            return new { cardId = card["cardId"]!.GetValue<string>(), dieId = cost["dieId"]!.GetValue<string>(),
                abilityId = cost["abilityId"]!.GetValue<string>(), cardCost = cost["face"]!.GetValue<string>() };
        }
        if (kind == "CommitCombatCard")
            return new { cardId = option["eligibleIds"]!.AsArray().FirstOrDefault()?.GetValue<string>() ?? "" };
        if (kind == "MoveFellowship")
            return new { dieId = option["eligibleDice"]![0]!.GetValue<string>() };
        if (kind == "ResolveHuntDamage")
            return new { huntResponse = "Ring" };
        return new { ids = Array.Empty<string>() };
    }
}
