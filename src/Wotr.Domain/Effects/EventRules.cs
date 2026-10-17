using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class EventRules
{
    public static bool FreeCharacterWindow(MatchState state, Side actor) =>
        state.Pending is { Kind: DecisionKind.Reaction } pending && pending.Owner == actor &&
        EffectRuntime.Current(state)?.Effect is { Wait: EffectWait.FreeEvent } effect && effect.Owner == actor;

    public static bool CanPlay(MatchState state, Side actor, EventDefinition definition) =>
        state.Phase == GamePhase.ActionResolution && (state.Pending?.Kind == DecisionKind.Action || FreeCharacterWindow(state, actor)) &&
        state.Pending?.Owner == actor && state.Player(actor).Hand.Contains(definition.CardId) &&
        CardCatalog.Enabled(state, definition.CardId) && CardCatalog.All.TryGetValue(definition.CardId, out var card) && card.Side == actor &&
        (!FreeCharacterWindow(state, actor) || card.EventType == DieFace.Character) &&
        definition.Condition.CanPlay(state, actor);

    public static bool ValidCost(MatchState state, Side actor, string cardId, string dieId, DieFace cost, string ability = "") =>
        CardCatalog.All.TryGetValue(cardId, out var card) && (FreeCharacterWindow(state, actor)
            ? card.EventType == DieFace.Character && cost == DieFace.Character && dieId == "" && ability == ""
            : (cost == DieFace.Event || cost == card.EventType) && CharacterCosts.CanPay(state, actor, dieId, cost, ability));

    public static List<EventCostOption> Costs(MatchState state, Side actor, string cardId)
    {
        if (FreeCharacterWindow(state, actor))
            return CardCatalog.All[cardId].EventType == DieFace.Character ? [new() { Face = DieFace.Character }] : [];
        var faces = new[] { DieFace.Event, CardCatalog.All[cardId].EventType };
        var costs = state.Dice.Where(d => d.Owner == actor && d.Location == DieLocation.Available)
            .SelectMany(d => faces.Where(f => DiceRules.CanPay(d, f))
                .Select(f => new EventCostOption { DieId = d.Id, Face = f })).ToList();
        if (CardCatalog.All[cardId].EventType == DieFace.Army)
            costs.AddRange(CharacterCosts.ArmyOptions(state, actor, 1).Where(c => c.AbilityId != "")
                .Select(c => new EventCostOption { DieId = c.DieId, Face = c.Face, AbilityId = c.AbilityId }));
        costs.AddRange(KingsPalantir.Costs(state, actor));
        return costs;
    }
}
