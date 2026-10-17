using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class RegionProtection
{
    public static bool Blocks(MatchState state, Side actor, string region) => actor == Side.Shadow &&
        state.ActiveEffects.Any(e => e.Modifier == EffectModifier.PreventShadowArmyEntry && e.TargetId == region);

    private static List<string> Sources(MatchState state) => state.ActiveEffects
        .Where(e => e.Modifier == EffectModifier.PreventShadowArmyEntry).Select(e => e.SourceId).Distinct().ToList();

    private static bool CostCard(string id, DieFace face) => CardCatalog.All.TryGetValue(id, out var card) &&
        card.Side == Side.Shadow && card.EventType == face;

    public static bool Valid(MatchState state, Side actor, CommandPayload payload) => actor == Side.Shadow &&
        state.Pending is { Kind: DecisionKind.Action, Owner: Side.Shadow } &&
        DiceRules.AvailableDie(state, actor, payload.DieId) is not null && Sources(state).Contains(payload.CardId) &&
        DraftSelection.IsSubset(payload.Ids, state.Player(actor).Hand, 2, 2) &&
        payload.Ids.Count(id => CostCard(id, DieFace.Army)) == 1 && payload.Ids.Count(id => CostCard(id, DieFace.Character)) == 1;

    public static ActionOption? Option(MatchState state, Side actor)
    {
        if (actor != Side.Shadow || state.Pending?.Kind != DecisionKind.Action || Sources(state) is not { Count: > 0 } sources)
            return null;
        var cards = state.Player(actor).Hand.Where(id => CostCard(id, DieFace.Army) || CostCard(id, DieFace.Character)).ToList();
        if (!cards.Any(id => CostCard(id, DieFace.Army)) || !cards.Any(id => CostCard(id, DieFace.Character)))
            return null;
        return new()
        {
            Kind = CommandKind.DiscardProtection,
            EligibleDice = state.Dice.Where(d => d.Owner == actor && d.Location == DieLocation.Available).Select(d => d.Id).ToList(),
            Targets = sources,
            EligibleIds = cards,
            Minimum = 2,
            Maximum = 2,
            EffectChoices = cards.Select(id => new EffectChoiceView
            {
                Id = id,
                SelectionGroup = CostCard(id, DieFace.Army) ? "Army" : "Character"
            }).ToList()
        };
    }

    public static void Remove(MatchState state, CommandPayload payload)
    {
        CharacterRules.PayAny(state, Side.Shadow, payload.DieId);
        var player = state.Player(Side.Shadow);
        foreach (var id in payload.Ids)
        {
            HandRules.Remove(player, id);
            player.Discard.Add(id);
        }
        ActiveEffects.RemoveWhere(state, e => e.SourceId == payload.CardId);
        state.HistoryCapture?.HiddenCards(Side.Shadow, payload.CardId, payload.Ids.Count);
        var own = HistoryFacts.At(state, ActionContextKind.Cards, payload.CardId);
        own.Facts.Cards = payload.Ids.Select(id => new ActionCardFact
            { Id = id, Owner = Side.Shadow, Use = ActionCardUse.Discard }).ToList();
        state.Events.Add(new()
        {
            Revision = state.Revision, Kind = "OwnProtectionCost", OnlyFor = Side.Shadow,
            Text = "Discarded the selected cards to remove protection.", Activity = own
        });
        state.Events.Add(new()
        {
            Revision = state.Revision,
            Kind = "ProtectionRemoved",
            Activity = HistoryFacts.Card(state, Side.Shadow, ActionCardUse.Discard, count: payload.Ids.Count),
            Text = $"Shadow spent one die and discarded an Army and a Character Event card to remove {Content.CardCatalog.Title(payload.CardId)}."
        });
        TurnFlow.FinishAction(state, Side.Shadow);
    }
}

public sealed record ProtectRegionsStep(string Nation, IReadOnlyList<string> Regions) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        PoliticalRules.Advance(context.State, Nation, PoliticalAdvanceCause.Event);
        foreach (var region in Regions)
            context.State.ActiveEffects.Add(new()
            {
                Id = $"{context.State.Id}:{context.Execution.SourceId}:{region}",
                SourceId = context.Execution.SourceId,
                Owner = context.Owner,
                TargetId = region,
                Modifier = EffectModifier.PreventShadowArmyEntry,
                Duration = EffectDuration.UntilRemoved,
                Turn = context.State.Turn
            });
    }
}
