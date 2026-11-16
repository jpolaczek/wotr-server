using System.Text.Json.Nodes;
using Wotr.Contracts;

namespace Wotr.Domain.BotProtocol;

// BOT-225: the battle in progress (SPEC §7 `battle`) from the deciding side's projection: who attacks from where, the
// round, the attacking units, the White Rider, and the Combat cards as far as this side may see them (the opponent's
// hidden card only by its back, its deck).
public static class ProtocolBattle
{
    public static JsonObject? Write(PlayerView view)
    {
        if (view.Battle is not { } battle)
            return null;
        var attacker = battle.Attacker.Side;
        return new JsonObject
        {
            ["attacker"] = ProtocolVocabulary.Side(attacker),
            ["battle"] = battle.Mode switch { BattleMode.Siege => "siege", BattleMode.Sortie => "sortie", _ => "field" },
            ["from"] = ProtocolVocabulary.Region(battle.Attacker.RegionId.Length > 0 ? battle.Attacker.RegionId : battle.RegionId),
            ["region"] = ProtocolVocabulary.Region(battle.RegionId),
            ["round"] = Math.Max(1, battle.Round),
            ["attackers"] = Units(battle.Attacker.Units),
            ["defenders"] = Units(battle.Defender.Units),
            ["white_rider"] = battle.WhiteRiderActive,
            ["cards_revealed"] = battle.CardsRevealed,
            ["attacker_card"] = Card(view, attacker),
            ["attacker_card_back"] = Back(view, attacker),
            ["defender_card"] = Card(view, Other(attacker)),
            ["defender_card_back"] = Back(view, Other(attacker)),
            ["event_card"] = null,
            ["step"] = battle.Stage,
            ["hits_on_attacker"] = battle.LossOwner == attacker ? battle.PendingHits : 0,
            ["hits_on_defender"] = battle.LossOwner == Other(attacker) ? battle.PendingHits : 0
        };
    }

    private static JsonArray Units(IEnumerable<UnitView> units) => new(units.GroupBy(u => (u.Nation, u.Kind))
        .OrderBy(g => g.Key.Nation, StringComparer.Ordinal).ThenBy(g => g.Key.Kind)
        .Select(g => (JsonNode)new JsonObject
        {
            ["nation"] = ProtocolVocabulary.Nation(g.Key.Nation),
            ["unit"] = ProtocolVocabulary.Unit(g.Key.Kind),
            ["n"] = g.Count()
        }).ToArray());

    // A card this side may see: its own, or the opponent's once revealed.
    private static string? Card(PlayerView view, Side side) =>
        view.Players.FirstOrDefault(p => p.Side == side)?.VisibleCombatCard is { Length: > 0 } card ? ProtocolVocabulary.Card(card) : null;

    // The back of a committed card this side may not see.
    private static string? Back(PlayerView view, Side side)
    {
        var player = view.Players.FirstOrDefault(p => p.Side == side);
        if (player is null || !player.CombatCardCommitted || player.VisibleCombatCard is { Length: > 0 })
            return null;
        return player.CombatCardBack.Length > 0 ? ProtocolVocabulary.Deck(player.CombatCardBack) : null;
    }

    private static Side Other(Side side) => side == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;
}
