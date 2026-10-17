using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record SmeagolGuideCondition : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => SmeagolRules.Present(state) && state.Fellowship.Guide == LordsCharacters.Smeagol;
}

public sealed record WeWontGoBackStep : EffectChoiceStep
{
    public const string Program = "lords.we-wont-go-back";
    public const string Decline = "smeagol.no-corruption";
    public override string Presentation => "SmeagolCorruption";

    public static void Begin(MatchState state, string tile)
    {
        if (HuntPool.Definitions[tile].Pool == SmeagolTiles.Pool &&
            HuntRuntime.Active(state)?.Purpose == HuntPurpose.Hunt &&
            CharacterRules.GuideDefinition(state) == LordsCharacters.Smeagol &&
            state.Player(Side.Shadow).Table.Contains(LordsShadowCards.WeWontGoBack))
            EffectRuntime.BeginProgram(state, Side.Shadow, LordsShadowCards.WeWontGoBack, Program);
    }

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        [LordsShadowCards.WeWontGoBack, Decline, .. state.Player(Side.Shadow).Hand.Where(id => CardCatalog.All[id].Deck == "character")];

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, SourceId = id == Decline ? "" : id,
        Title = id == Decline ? "Decline" : CardCatalog.All[id].Title,
        Detail = id == Decline ? "Keep your cards." : id == LordsShadowCards.WeWontGoBack
            ? "Discard from the table · add 2 Corruption." : "Discard from your hand · add 2 Corruption."
    };

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        preview.Summary = ids.Single() == Decline ? "Keep your cards; add no Corruption." : "Discard the selected card; add 2 Corruption immediately.";
        if (ids.Single() != Decline)
            preview.Costs.Add(ids.Single());
    }

    public override void Choose(EffectContext context, string id)
    {
        if (id == Decline)
            return;
        if (id == LordsShadowCards.WeWontGoBack)
        {
            context.State.HistoryCapture?.React(Side.Shadow, id, cardCost: true);
            TableCardRules.Discard(context.State, Side.Shadow, id);
        }
        else
        {
            var player = context.State.Player(Side.Shadow);
            HandRules.Remove(player, id);
            player.Discard.Add(id);
            context.State.HistoryCapture?.HiddenCards(Side.Shadow, context.Execution.SourceId, 1);
            context.Record("CardsDiscarded", "Shadow discarded one Character Event card.",
                cards: [new() { Owner = Side.Shadow, Use = ActionCardUse.Discard, Deck = "character" }]);
            context.Record("CardDiscarded", $"Discarded {id}.", Side.Shadow,
                cards: [new() { Owner = Side.Shadow, Use = ActionCardUse.Discard, Id = id, Deck = "character" }]);
        }
        DirectCorruption.Add(context, 2);
    }
}
