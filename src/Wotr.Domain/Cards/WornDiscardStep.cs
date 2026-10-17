using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record HuntCasualtyCondition : PublicEventCondition
{
    public override bool CanPlay(MatchState state, Side actor) =>
        actor == Side.Shadow && HuntRuntime.Active(state) is { CasualtyTaken: true, Purpose: HuntPurpose.Hunt } &&
        (state.Player(Side.FreePeoples).Hand.Count > 0 || state.Player(Side.FreePeoples).Table.Any(id => CardCatalog.All[id].Deck == "character"));
}

public sealed record WornDiscardStep : EffectChoiceStep
{
    public const string RandomHand = "worn.random-character";
    public const string Decline = "worn.decline";
    public override string Presentation => "CharacterCardDiscard";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        var player = state.Player(Side.FreePeoples);
        var table = player.Table.Where(id => CardCatalog.All[id].Deck == "character").ToList();
        // The opponent may know the total hand size, never its hidden deck composition.
        if (player.Hand.Count > 0)
            table.Add(RandomHand);
        return table.Count == 0 ? [] : [.. table, Decline];
    }

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        SourceId = CardCatalog.All.ContainsKey(id) ? id : "",
        Title = Title(id),
        Detail = id == RandomHand ? "Discard a random Character card from the Free Peoples hand, if any. Its identity stays hidden." :
            id == Decline ? "Keep Worn with Sorrow and Toil for another casualty." : "Discard this card from the table."
    };

    private static string Title(string id) => id switch
    {
        RandomHand => "Random hand",
        Decline => "Keep cards",
        FreePeoplesCharacterCards01.Mithril => "Mithril",
        FreePeoplesCharacterCards01.Axe => "Axe and Bow",
        FreePeoplesCharacterCards02.Horn => "Horn",
        FreePeoplesCharacterCards02.Staff => "Staff",
        _ => CardCatalog.All[id].Title
    };

    public override void Choose(EffectContext context, string id)
    {
        if (id == Decline)
            return;
        var state = context.State;
        var player = state.Player(Side.FreePeoples);
        if (id != RandomHand)
        {
            TableCardRules.Discard(state, Side.FreePeoples, id);
            return;
        }
        var candidates = player.Hand.Where(card => CardCatalog.All[card].Deck == "character").ToArray();
        if (candidates.Length == 0)
            return;
        var chosen = candidates[context.Random.Next(candidates.Length)];
        HandRules.Remove(player, chosen);
        player.Discard.Add(chosen);
        context.Record("CardDiscarded", $"Worn with Sorrow and Toil: discarded {chosen}.", Side.FreePeoples,
            cards: [new() { Owner = Side.FreePeoples, Use = ActionCardUse.Discard, Id = chosen, Deck = "character" }]);
        context.Record("CardsDiscarded", "Free Peoples discarded one Character card from hand.",
            cards: [new() { Owner = Side.FreePeoples, Use = ActionCardUse.Discard, Deck = "character" }]);
    }
}
