using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record WindFromWestStep : EffectChoiceStep
{
    public const string Inspect = "wind.inspect-hand";
    public const string Decline = "wind.finish";
    public override string Presentation => "TableCards";

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        var shadow = state.Player(Side.Shadow);
        if (shadow.Table.Count > 0)
            return shadow.Table.ToArray();
        return shadow.Hand.Count > 0 ? [Inspect, Decline] : [];
    }

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        SourceId = CardCatalog.All.ContainsKey(id) ? id : "",
        Title = id == Inspect ? "Inspect Shadow hand" : id == Decline ? "Finish" : CardCatalog.All[id].Title,
        Detail = id == Inspect ? "Look at the hand, then choose one card to discard." :
            id == Decline ? "Do not inspect the hand." : "Discard from the table without effect."
    };

    public override void Choose(EffectContext context, string id)
    {
        if (id == Inspect)
            context.Execution.InspectedHand = Side.Shadow;
        else if (id != Decline)
        {
            TableCardRules.Discard(context.State, Side.Shadow, id);
            context.Record("TableCardDiscarded", $"{context.Execution.SourceId}: discarded {id} without effect.");
        }
    }
}

public sealed record InspectShadowHandStep : EffectChoiceStep
{
    public override string Presentation => "InspectedHand";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        execution.InspectedHand == Side.Shadow ? state.Player(Side.Shadow).Hand.ToArray() : [];

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, SourceId = id, Title = CardCatalog.All[id].Title, Detail = "Discard this card from Shadow's hand."
    };

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.Summary = "Discard " + CardCatalog.All[ids.Single()].Title + " from Shadow's hand.";

    public override void Apply(EffectContext context)
    {
        var choices = Candidates(context.State, context.Execution);
        // Even a single inspected card must be visible before its mandatory discard.
        if (choices.Count > 0)
            OpenChoice(context, choices, 1);
        else
            context.Execution.InspectedHand = null;
    }

    public override void Choose(EffectContext context, string id)
    {
        var shadow = context.State.Player(Side.Shadow);
        HandRules.Remove(shadow, id);
        shadow.Discard.Add(id);
        context.Execution.InspectedHand = null;
        context.Record("HandCardDiscarded", "Wind from the West discarded one Shadow card.",
            cards: [new() { Owner = Side.Shadow, Use = ActionCardUse.Discard }]);
        context.Record("InspectedCardDiscarded", $"Wind from the West discarded {id}.", context.Owner,
            cards: [new() { Owner = Side.Shadow, Use = ActionCardUse.Discard, Id = id }]);
    }
}
