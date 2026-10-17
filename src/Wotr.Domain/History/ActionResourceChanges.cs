using Wotr.Contracts;

namespace Wotr.Domain;

internal static class ActionResourceChanges
{
    public static void Capture(MatchState before, MatchState after, ActionFacts facts)
    {
        foreach (var next in after.Rings)
        {
            var previous = before.Rings.Find(r => r.Id == next.Id);
            if (previous?.Owner != next.Owner)
                facts.Resources.Add(new()
                {
                    Kind = ActionCostKind.Ring, Id = next.Id,
                    From = previous?.Owner?.ToString() ?? "removed", To = next.Owner?.ToString() ?? "removed"
                });
        }
        foreach (var next in after.Kings?.Tokens ?? [])
        {
            var previous = before.Kings?.Tokens.Find(t => t.SovereignId == next.SovereignId);
            if (previous?.Location != next.Location)
                facts.Resources.Add(new()
                {
                    Kind = ActionCostKind.RulerToken, Id = KingsRulerTokens.Id(next.SovereignId),
                    From = previous?.Location ?? "reserve", To = next.Location
                });
        }
        Council(before, after, facts);
        foreach (var player in after.Players)
        {
            var previous = before.Player(player.Side);
            facts.Cards.AddRange(player.Table.Except(previous.Table).Select(id => new ActionCardFact
                { Owner = player.Side, Use = ActionCardUse.TableEntered, Id = id }));
            facts.Cards.AddRange(previous.Table.Except(player.Table).Select(id => new ActionCardFact
                { Owner = player.Side, Use = ActionCardUse.TableRemoved, Id = id }));
        }
        var sovereigns = KingsProjection.PublicSovereigns(before).ToDictionary(s => s.Id);
        foreach (var next in KingsProjection.PublicSovereigns(after))
            if (!sovereigns.TryGetValue(next.Id, out var previous) || !Same(previous, next))
                facts.Sovereigns.Add(next);
    }

    private static bool Same(SovereignView a, SovereignView b) => a.Status == b.Status && a.RegionId == b.RegionId &&
        a.AttemptedThisTurn == b.AttemptedThisTurn && a.Corruption == b.Corruption &&
        a.Tiles.Select(t => t.TileId).SequenceEqual(b.Tiles.Select(t => t.TileId));

    private static void Council(MatchState before, MatchState after, ActionFacts facts)
    {
        if (after.Lords?.Council is not { } council)
            return;
        foreach (var id in council.GrantedTokens.Except(before.Lords?.Council?.GrantedTokens ?? []))
            facts.Resources.Add(new() { Kind = ActionCostKind.CouncilToken, Id = id, From = "reserve", To = "Shadow" });
        foreach (var id in council.SpentTokens.Except(before.Lords?.Council?.SpentTokens ?? []))
            facts.Resources.Add(new() { Kind = ActionCostKind.CouncilToken, Id = id, From = "Shadow", To = "spent" });
    }
}
