using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Wotr.Domain.BotProtocol;

// BOT-225: what a bot says about its own answer (SPEC §4 `info`), used where the protocol's options cannot carry the bot's
// intent. AlphaWOTR plays the Voice of Saruman as a Muster recruitment with a variant ("settlements" or "swap2") and
// answers any of the die's recruitments with the first `use_die` "recruit" of that die; its `info.engine` names the
// engine action it chose, variant included. The seat then takes the option of that die whose variant matches (or the
// plain recruitment when the engine's action has none). Every option is still a legal command the engine judged.
public static partial class ProtocolHints
{
    public static int Adjust(ProtocolNode question, int chosen, JsonNode? info)
    {
        if (question.Question != "ACTION" || chosen < 0 || chosen >= question.Branches.Count)
            return chosen;
        var atom = question.Branches[chosen].Atom;
        if ((string?)atom["kind"] != "use_die" || (string?)atom["action"] != "recruit" || Engine(info) is not { } engine)
            return chosen;
        var type = (string?)engine["type"];
        if (type is not ("muster_recruit" or "muster_elite"))
            return chosen;
        var variant = (string?)engine["params"]?["saruman"];
        var die = atom["die"]?.ToJsonString();
        for (var o = 0; o < question.Branches.Count; o++)
        {
            var other = question.Branches[o].Atom;
            if ((string?)other["kind"] == "use_die" && (string?)other["action"] == "recruit" && other["die"]?.ToJsonString() == die &&
                (string?)other["saruman"] == variant)
                return o;
        }
        return chosen;
    }

    // The engine's action: an object, or the JSON text AlphaWOTR sends (cut at 240 characters: the variant is read from the
    // text when the cut leaves it unparsable).
    public static JsonObject? Engine(JsonNode? info)
    {
        var engine = info?["engine"];
        if (engine is JsonObject direct)
            return direct;
        if (engine is not JsonValue value || !value.TryGetValue<string>(out var text))
            return null;
        try
        {
            return JsonNode.Parse(text) as JsonObject;
        }
        catch (JsonException)
        {
            var type = TypeField().Match(text);
            if (!type.Success)
                return null;
            var parameters = new JsonObject();
            var variant = SarumanField().Match(text);
            if (variant.Success)
                parameters["saruman"] = variant.Groups[1].Value;
            return new JsonObject { ["type"] = type.Groups[1].Value, ["params"] = parameters };
        }
    }

    [GeneratedRegex("\"type\"\\s*:\\s*\"([a-z_]+)\"")]
    private static partial Regex TypeField();

    [GeneratedRegex("\"saruman\"\\s*:\\s*\"([a-z0-9_]+)\"")]
    private static partial Regex SarumanField();
}
