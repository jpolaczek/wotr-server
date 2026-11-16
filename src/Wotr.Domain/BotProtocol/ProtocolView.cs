using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain.BotProtocol;

// What the seat remembers of a game beyond one projection: the characters it has seen in play (so one gone from the board
// is eliminated, not yet to come) and the cards it has seen leave a hand face up. Built again from the history after a
// restart (BOT-224).
public sealed class ProtocolMemory
{
    public HashSet<string> CharactersSeen { get; } = new(StringComparer.Ordinal);
    public Dictionary<Side, List<string>> Discards { get; } = new() { [Side.FreePeoples] = new(), [Side.Shadow] = new() };
}

// BOT-225: the protocol's view (SPEC §7) written only from the deciding side's own projection (R11): the public board, its
// own hand, and counts for what is hidden. Nothing here reads the match state.
public static class ProtocolView
{
    private static readonly IReadOnlyList<RegionDefinition> Regions = BoardCatalog.LoadBase().Regions;
    private static readonly string[] Nations =
        ["dwarves", "elves", "gondor", "north", "rohan", "isengard", "sauron", "southrons"];
    private static readonly string[] Minions = ["base.character.witch-king", "base.character.saruman", "base.character.mouth-of-sauron"];
    private static readonly string[] Returning = ["base.character.gandalf-white", "base.character.aragorn"];

    public static JsonObject Write(PlayerView view, ProtocolMemory memory)
    {
        Remember(view, memory);
        return new JsonObject
        {
            ["viewer"] = ProtocolVocabulary.Side(view.You),
            ["turn"] = view.Turn,
            ["phase"] = Phase(view.Phase),
            ["to_act"] = view.DecisionOwner is { } owner ? ProtocolVocabulary.Side(owner) : null,
            ["active_side"] = view.DecisionOwner is { } active ? ProtocolVocabulary.Side(active) : null,
            ["regions"] = RegionsOf(view),
            ["politics"] = Politics(view),
            ["reinforcements"] = Figures(view, "reserve"),
            ["casualties"] = Figures(view, "casualties"),
            ["fellowship"] = Fellowship(view.Fellowship),
            ["characters"] = Characters(view, memory),
            ["dice"] = Dice(view),
            ["hunt"] = Hunt(view, memory),
            ["cards"] = Cards(view, memory),
            ["victory_points"] = PerSide(view, p => p.VictoryPoints),
            ["elven_rings"] = new JsonObject
            {
                ["FREE_PEOPLES"] = view.Rings.Count(r => r.Owner == Side.FreePeoples),
                ["SHADOW"] = view.Rings.Count(r => r.Owner == Side.Shadow)
            },
            ["elven_ring_used"] = PerSide(view, p => p.RingUsedThisTurn),
            ["battle"] = ProtocolBattle.Write(view),
            ["result"] = view.Winner is { } winner ? ProtocolVocabulary.Side(winner) : null
        };
    }

    // The dice of a side as the protocol numbers them: every die in play (not the ones a side has yet to gain), in the
    // projection's order. Options name a die by this index.
    public static IReadOnlyList<DieView> DiceOf(PlayerView view, Side side) =>
        view.Dice.Where(d => d.Owner == side && d.Location != DieLocation.Removed).ToList();

    public static int DieIndex(PlayerView view, string dieId)
    {
        var die = view.Dice.FirstOrDefault(d => d.Id == dieId);
        return die is null ? -1 : DiceOf(view, die.Owner).ToList().FindIndex(d => d.Id == dieId);
    }

    public static string Phase(GamePhase phase) => phase switch
    {
        GamePhase.Setup => "SETUP",
        GamePhase.RecoverDraw => "RECOVER_DRAW",
        GamePhase.Fellowship => "FELLOWSHIP",
        GamePhase.HuntAllocation => "HUNT_ALLOCATION",
        GamePhase.ActionRoll => "ACTION_ROLL",
        GamePhase.ActionResolution => "ACTION_RESOLUTION",
        GamePhase.VictoryCheck => "VICTORY_CHECK",
        _ => "FINISHED"
    };

    // The Fellowship's companions by their key: in the Fellowship Gandalf is the Grey and Aragorn is Strider.
    public static string Companion(string identity) => identity switch
    {
        "gandalf" => "gandalf-the-grey",
        "aragorn" => "strider",
        _ => ProtocolVocabulary.Character("base.character." + identity)
    };

    private static void Remember(PlayerView view, ProtocolMemory memory)
    {
        foreach (var character in view.Characters)
            memory.CharactersSeen.Add(character.DefinitionId);
    }

    private static JsonObject RegionsOf(PlayerView view)
    {
        var regions = new JsonObject();
        foreach (var region in Regions)
        {
            var units = view.Units.Where(u => u.Location == region.Id).ToList();
            var characters = view.Characters.Where(c => c.Location == region.Id).ToList();
            regions[ProtocolVocabulary.Region(region.Id)] = new JsonObject
            {
                ["control"] = view.Control.TryGetValue(region.Id, out var side) && side is { } s ? ProtocolVocabulary.Side(s) : null,
                ["units"] = new JsonArray(units.GroupBy(u => (u.Nation, u.Kind)).OrderBy(g => g.Key.Nation, StringComparer.Ordinal)
                    .ThenBy(g => g.Key.Kind).Select(g => (JsonNode)new JsonObject
                    {
                        ["nation"] = ProtocolVocabulary.Nation(g.Key.Nation),
                        ["unit"] = ProtocolVocabulary.Unit(g.Key.Kind),
                        ["n"] = g.Count()
                    }).ToArray()),
                ["characters"] = new JsonArray(characters.Select(c => (JsonNode)ProtocolVocabulary.Character(c.DefinitionId)!).ToArray()),
                ["siege"] = units.Any(u => u.Garrison) || characters.Any(c => c.Garrison)
            };
        }
        return regions;
    }

    private static JsonObject Politics(PlayerView view)
    {
        var politics = new JsonObject();
        foreach (var nation in view.Nations.Where(n => Nations.Contains(n.Id)))
            politics[ProtocolVocabulary.Nation(nation.Id)] = new JsonObject { ["step"] = nation.StepsToWar, ["active"] = nation.Active };
        return politics;
    }

    private static JsonObject Figures(PlayerView view, string location)
    {
        var figures = new JsonObject();
        foreach (var nation in Nations)
        {
            var units = view.Units.Where(u => u.Nation == nation && u.Location == location).ToList();
            figures[ProtocolVocabulary.Nation(nation)] = new JsonObject
            {
                ["regular"] = units.Count(u => u.Kind == UnitKind.Regular),
                ["elite"] = units.Count(u => u.Kind == UnitKind.Elite),
                ["leader"] = units.Count(u => u.Kind == UnitKind.Leader),
                ["nazgul"] = units.Count(u => u.Kind == UnitKind.Nazgul)
            };
        }
        return figures;
    }

    private static JsonObject Fellowship(FellowshipView fellowship) => new()
    {
        ["region"] = ProtocolVocabulary.Region(fellowship.RegionId),
        ["progress"] = fellowship.Progress,
        ["corruption"] = fellowship.Corruption,
        ["hidden"] = fellowship.Hidden,
        ["mordor"] = fellowship.MordorStep,
        ["guide"] = fellowship.Guide.Length == 0 ? null : ProtocolVocabulary.Character(fellowship.Guide),
        ["companions"] = new JsonArray(fellowship.CompanionIdentities.Select(i => (JsonNode)Companion(i)!).ToArray())
    };

    // Where each character is: in the Fellowship, on the board, eliminated (seen in play and gone) or not yet in play.
    private static JsonObject Characters(PlayerView view, ProtocolMemory memory)
    {
        var characters = new JsonObject();
        var inFellowship = view.Fellowship.CompanionIdentities.Select(Companion).ToHashSet(StringComparer.Ordinal);
        foreach (var definition in CharacterCatalog.Base.Values.Where(c => c.Id != "base.character.ring-bearers"))
        {
            var key = ProtocolVocabulary.Character(definition.Id);
            var onBoard = view.Characters.FirstOrDefault(c => c.DefinitionId == definition.Id);
            string where;
            if (inFellowship.Contains(key))
                where = "fellowship";
            else if (onBoard is not null)
                where = "region";
            else if (memory.CharactersSeen.Contains(definition.Id) || Eliminated(view, definition))
                where = "eliminated";
            else
                where = "not_in_play";
            var entry = new JsonObject { ["where"] = where };
            if (onBoard is not null)
                entry["region"] = ProtocolVocabulary.Region(onBoard.Location);
            characters[key] = entry;
        }
        return characters;
    }

    // A companion that started in the Fellowship and is in neither it nor on the board has left the game, unless its
    // later version has taken its place (Gandalf the White, Aragorn).
    private static bool Eliminated(PlayerView view, CharacterDefinition definition)
    {
        if (definition.Side != Side.FreePeoples || !definition.Initial || definition.Id == "base.character.gollum")
            return false;
        return !view.Characters.Any(c => CharacterCatalog.All.TryGetValue(c.DefinitionId, out var other) &&
            other.Identity == definition.Identity);
    }

    private static JsonObject Dice(PlayerView view)
    {
        var dice = new JsonObject();
        foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
        {
            var list = new JsonArray();
            var index = 0;
            foreach (var die in DiceOf(view, side))
                list.Add(new JsonObject
                {
                    ["die"] = index++,
                    // An Eye allocated to the Hunt before the roll has no face of its own (SPEC §7, as AlphaWOTR reads it).
                    ["face"] = die.Face is { } face && die.HuntOrigin != "allocated" ? ProtocolVocabulary.Face(face) : null,
                    ["status"] = Status(die.Location),
                    ["used"] = die.Location is not (DieLocation.Available or DieLocation.Unrolled or DieLocation.AwaitingChoice)
                });
            dice[ProtocolVocabulary.Side(side)] = list;
        }
        return dice;
    }

    private static string Status(DieLocation location) => location switch
    {
        DieLocation.Unrolled => "unrolled",
        DieLocation.Available or DieLocation.AwaitingChoice => "available",
        DieLocation.Hunt => "hunt_box",
        _ => "used"
    };

    private static JsonObject Hunt(PlayerView view, ProtocolMemory memory)
    {
        var pool = new JsonArray();
        foreach (var face in view.HuntPool.Tiles.Where(t => t.Source == "base" || t.Source.Length == 0))
            foreach (var key in TileKeys(face, view, memory))
                pool.Add(key);
        var ready = new JsonArray();
        foreach (var face in view.HuntPool.Prepared)
            foreach (var key in TileKeys(face, view, memory))
                ready.Add(key);
        return new JsonObject
        {
            ["box"] = new JsonObject
            {
                ["FREE_PEOPLES"] = view.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Hunt),
                ["SHADOW"] = view.Dice.Count(d => d.Owner == Side.Shadow && d.Location == DieLocation.Hunt)
            },
            ["pool"] = pool,
            ["reserve"] = new JsonArray(),
            ["ready"] = ready,
            ["drawn"] = new JsonArray(),
            ["removed"] = new JsonArray(),
            ["shadow_must_hunt"] = false
        };
    }

    // The keys of the tiles of one face: standard tiles numbered in their printed order, special tiles by their card, the
    // ones whose card was seen played first.
    private static IEnumerable<JsonNode> TileKeys(HuntTileFaceView face, PlayerView view, ProtocolMemory memory)
    {
        if (face.Pool == "standard")
            return ProtocolVocabulary.StandardTiles(face.Value, face.Reveal).Take(face.Count).Select(k => (JsonNode)k!);
        var special = ProtocolVocabulary.SpecialTiles(face.Pool, face.Value, face.Reveal);
        var played = memory.Discards.Values.SelectMany(d => d).Concat(view.Players.SelectMany(p => p.TableCards)).ToHashSet();
        return special.OrderBy(s => played.Contains(s.Card) ? 0 : 1).Take(face.Count).Select(s => (JsonNode)s.Key!);
    }

    private static JsonObject Cards(PlayerView view, ProtocolMemory memory)
    {
        var opponent = view.Players.FirstOrDefault(p => p.Side != view.You) ?? new PlayerSummary();
        return new JsonObject
        {
            ["hand"] = Keys(view.Hand),
            ["discarded_face_down"] = new JsonArray(),
            ["opponent_hand"] = opponent.HandCount,
            ["opponent_hand_by_deck"] = new JsonObject
            {
                ["character"] = opponent.HandDeckCounts.GetValueOrDefault("character"),
                ["strategy"] = opponent.HandDeckCounts.GetValueOrDefault("strategy")
            },
            ["opponent_discarded_face_down"] = 0,
            ["decks"] = PerSide(view, p => new JsonObject
            {
                ["character"] = p.DeckCounts.GetValueOrDefault("character"),
                ["strategy"] = p.DeckCounts.GetValueOrDefault("strategy")
            }),
            ["discards"] = new JsonObject
            {
                ["FREE_PEOPLES"] = Keys(memory.Discards[Side.FreePeoples]),
                ["SHADOW"] = Keys(memory.Discards[Side.Shadow])
            },
            ["on_table"] = PerSide(view, p => Keys(p.TableCards))
        };
    }

    private static JsonArray Keys(IEnumerable<string> cards) =>
        new(cards.Select(c => (JsonNode)ProtocolVocabulary.Card(c)!).ToArray());

    private static JsonObject PerSide(PlayerView view, Func<PlayerSummary, JsonNode?> value)
    {
        var result = new JsonObject();
        foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
        {
            var player = view.Players.FirstOrDefault(p => p.Side == side) ?? new PlayerSummary { Side = side };
            result[ProtocolVocabulary.Side(side)] = value(player);
        }
        return result;
    }
}
