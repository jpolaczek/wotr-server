using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record FellowshipProgressCondition : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) =>
        !state.Fellowship.MordorStep.HasValue && state.Fellowship.Progress >= 1;
}

public sealed record NazgulSearchStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        if (NazgulEventTravel.WithFellowship(context.State))
            FellowshipFlow.BeginReveal(context.State, context.Execution.SourceId);
    }
}

public sealed record CruelWeatherStep : EffectChoiceStep
{
    public override string Presentation => "Region";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        BoardRules.Regions[state.Fellowship.RegionId].Neighbors;

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        SourceId = "fellowship",
        TargetId = id,
        Title = "Fellowship",
        Detail = "Move the figure here. Progress and visibility stay unchanged."
    };

    public override void Choose(EffectContext context, string id)
    {
        context.State.Fellowship.RegionId = id;
        context.Record("FellowshipDisplaced", $"{context.Execution.SourceId}: the Fellowship figure moved to {id}; progress and visibility unchanged.");
    }

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.Fellowship = new()
        {
            RegionId = ids.Single(),
            Hidden = state.Fellowship.Hidden,
            Progress = state.Fellowship.Progress
        };
}

public sealed record NazgulStrikeStep : EffectChoiceStep
{
    public const string Hunt = "nazgul-strike.hunt";
    public const string Decline = "nazgul-strike.decline";
    public override string Presentation => "NazgulStrike";

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        !NazgulEventTravel.WithFellowship(state) ? [] :
        [.. state.Player(Side.FreePeoples).Table.Where(id => CardCatalog.All[id].Deck == "character"), Hunt, Decline];

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        SourceId = CardCatalog.All.ContainsKey(id) ? id : "",
        Title = id == Hunt ? "Roll for the Hunt" : id == Decline ? "Finish" : CardCatalog.All[id].Title,
        Detail = id == Hunt ? "Resolve a Hunt without advancing the Fellowship or spending another die."
            : id == Decline ? "Finish the event without discarding a card or rolling for the Hunt."
            : "Discard this Free Peoples Character card from the table."
    };

    public override void Choose(EffectContext context, string id)
    {
        if (id == Decline)
            return;
        if (id == Hunt)
        {
            HuntRuntime.BeginRoll(context.State, context.Execution.SourceId, context.Random);
            return;
        }
        ActiveEffects.RemoveWhere(context.State, effect => effect.Owner == Side.FreePeoples && effect.SourceId == id);
        var player = context.State.Player(Side.FreePeoples);
        if (player.Table.Remove(id))
            player.Discard.Add(id);
        context.Record("TableCardDiscarded", $"{context.Execution.SourceId}: discarded {id} from the Free Peoples table.");
    }

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        preview.HasUnknownResult = ids.Contains(Hunt);
        if (ids.Single() is not (Hunt or Decline))
            preview.RemovedIds.Add(ids.Single());
    }
}
