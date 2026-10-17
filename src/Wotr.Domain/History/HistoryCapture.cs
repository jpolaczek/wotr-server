using Wotr.Contracts;

namespace Wotr.Domain;

// Per-command instrumentation. Never persisted, projected, or used to make a rules decision.
internal sealed class HistoryCapture(MatchState before, GameCommand command)
{
    public List<ActionCost> Costs { get; } = [];
    public ActionBudget? Budget { get; set; }

    public void HiddenCards(Side actor, string source, int count) =>
        Costs.Add(new() { Kind = ActionCostKind.Card, Owner = actor, Source = source, Count = count });

    public void React(Side actor, string source, bool cardCost)
    {
        Costs.Add(new()
        {
            Kind = cardCost ? ActionCostKind.Card : ActionCostKind.Ability,
            Id = source, Source = source, Owner = actor
        });
    }

    public void Pay(DieState die, DieFace use)
    {
        var original = before.Dice.Find(d => d.Id == die.Id) ?? die;
        Costs.Add(new()
        {
            Kind = ActionCostKind.Die, Id = die.Id, Owner = die.Owner, Source = die.Source,
            Face = original.Face, Use = use, AbilityId = command.Payload.AbilityId
        });
    }

    public void Complete(MatchState after)
    {
        foreach (var ring in before.Rings)
            if (after.Rings.Find(r => r.Id == ring.Id)?.Owner != ring.Owner && ring.Owner is { } owner && owner == before.Pending?.Owner)
                Costs.Add(new() { Kind = ActionCostKind.Ring, Id = ring.Id, Owner = owner });
        var payload = command.Payload;
        Token(payload.RulerTokenId, ActionCostKind.RulerToken, Side.FreePeoples);
        Token(payload.CouncilTokenId, ActionCostKind.CouncilToken, Side.Shadow);
        if (command.Kind == CommandKind.UseCouncilToken)
            Token(payload.AbilityId, ActionCostKind.CouncilToken, Side.Shadow);
        if (command.Kind == CommandKind.UseDismay)
            Token(payload.Ids.Single(), ActionCostKind.RulerToken, Side.Shadow);
        if (command.Kind == CommandKind.DiscardDie && before.Dice.Find(d => d.Id == payload.DieId) is { } die)
            Pay(die, die.Face);
        if (command.Kind == CommandKind.PlayEvent)
            Costs.Add(new() { Kind = ActionCostKind.Card, Id = payload.CardId, Owner = before.Pending!.Owner });
        if (command.Kind == CommandKind.ExtendSiege)
            Costs.Add(new() { Kind = ActionCostKind.Figure, Id = payload.Ids.Single(), Owner = before.Pending!.Owner, Source = "extend-siege" });
    }

    private void Token(string id, ActionCostKind kind, Side owner)
    {
        if (id.Length > 0)
            Costs.Add(new() { Kind = kind, Id = id, Owner = owner });
    }
}
