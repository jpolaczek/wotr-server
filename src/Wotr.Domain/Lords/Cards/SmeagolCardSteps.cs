using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record AddSmeagolTilesStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var extra = SmeagolTiles.Definitions.Skip(2).Select(t => t.Id).ToHashSet();
        foreach (var tile in context.State.HuntTiles.Where(t => extra.Contains(t.Id) && t.Location == "set-aside"))
            tile.Location = "pool";
        context.Record("SmeagolTilesAdded", "The additional Sméagol tiles entered the Hunt pool.");
    }
}

public sealed record SafePathsDiscardStep : EffectChoiceStep
{
    public override string Presentation => "TableCards";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        state.Player(Side.Shadow).Table.Where(id => id is SmeagolRules.WeShallGetIt or LordsFreePeoplesCards.WeWontGoBack).ToArray();

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, SourceId = id, Title = CardCatalog.All[id].Title, Detail = "Discard without effect."
    };

    public override void Choose(EffectContext context, string id)
    {
        TableCardRules.Discard(context.State, Side.Shadow, id);
        context.Record("TableCardDiscarded", $"{context.Execution.SourceId}: discarded {id} without effect.");
    }
}
