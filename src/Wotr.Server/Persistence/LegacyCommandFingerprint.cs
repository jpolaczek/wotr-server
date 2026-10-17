using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Server;

internal static class LegacyCommandFingerprint
{
    // Frozen property order from 77453b5, bdbe3e3 (+RulerTokenId), b142f31 (+CouncilTokenId).
    // Never replace these lists with reflection: the old hash depended on declaration order.
    private static readonly IReadOnlyDictionary<Type, string[]> Fields = new Dictionary<Type, string[]>
    {
        [typeof(GameCommand)] = ["matchId", "commandId", "expectedRevision", "decisionId", "kind", "payload"],
        [typeof(CommandPayload)] = ["dieId", "ringId", "rulerTokenId", "councilTokenId", "deck", "face", "ids",
            "nationId", "placements", "movementCost", "moves", "cardId", "cardCost", "reactionId", "guideId",
            "route", "companionMoves", "huntResponse", "losses", "regionId", "autoDecision", "characterId", "abilityId"],
        [typeof(ArmyMoveChoice)] = ["route", "from", "to", "unitIds", "characterIds"],
        [typeof(UnitPlacementChoice)] = ["unitId", "regionId"],
        [typeof(CompanionMoveChoice)] = ["ids", "route"],
        [typeof(BattleLossChoice)] = ["unitId", "kind"],
        [typeof(AutoDecisionChange)] = ["battleId", "expectedVersion", "handVersion", "enabled"]
    };

    public static bool Matches(string saved, GameCommand command)
    {
        var value = JsonSerializer.SerializeToElement(command, BoardCatalog.Json);
        for (var generation = 2; generation >= 0; generation--)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                if (!Write(writer, value, typeof(GameCommand), generation))
                    continue;
            }
            if (CommandFingerprint.Hash(stream.ToArray()) == saved)
                return true;
        }
        return false;
    }

    private static bool Write(Utf8JsonWriter writer, JsonElement value, Type type, int generation)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();
            foreach (var item in value.EnumerateArray())
                if (!Write(writer, item, type.GetGenericArguments()[0], generation))
                    return false;
            writer.WriteEndArray();
        }
        else if (value.ValueKind == JsonValueKind.Object)
        {
            if (!WriteObject(writer, value, type, generation))
                return false;
        }
        else
            value.WriteTo(writer);
        return true;
    }

    private static bool WriteObject(Utf8JsonWriter writer, JsonElement value, Type type, int generation)
    {
        if (!Fields.TryGetValue(type, out var fields))
            return false;
        var shape = CommandJsonShape.For(type);
        bool Included(string name) => fields.Contains(name) && (type != typeof(CommandPayload) ||
            (name != "rulerTokenId" || generation >= 1) && (name != "councilTokenId" || generation >= 2));
        if (value.EnumerateObject().Any(p => !Included(p.Name) && !shape.IsDefault(p)))
            return false;
        writer.WriteStartObject();
        foreach (var name in fields.Where(Included))
        {
            writer.WritePropertyName(name);
            if (!value.TryGetProperty(name, out var property) || !Write(writer, property, shape.Fields[name], generation))
                return false;
        }
        writer.WriteEndObject();
        return true;
    }
}
