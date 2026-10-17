using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record BreakingFellowshipStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        if (CharacterRules.GuideDefinition(context.State) == CharacterRules.Gollum)
            DirectCorruption.Add(context, 1);
        else
            HuntRuntime.BeginTile(context.State, context.Execution.SourceId,
                tileRule: HuntTileRule.IgnoreEyesAndFellowship, purpose: HuntPurpose.CompanionSeparation);
    }
}

public sealed record BreakingSeparationStep : EffectChoiceStep
{
    public override string Presentation => "FellowshipCompanions";
    public override Side Chooser(MatchState state, EffectExecution execution) => Side.FreePeoples;
    public override int Minimum(MatchState state, EffectExecution execution) => Maximum(state, execution);
    public override int Maximum(MatchState state, EffectExecution execution) =>
        Math.Min(HuntRuntime.Active(state)!.Damage, state.Fellowship.Companions.Count);
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        Maximum(state, execution) == 0 ? [] : [.. state.Fellowship.Companions];

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        SourceId = id,
        SelectionGroup = "Companions",
        GroupMinimum = Minimum(state, execution),
        GroupMaximum = Maximum(state, execution),
        TargetId = state.Fellowship.MordorStep.HasValue ? "" : state.Fellowship.RegionId,
        Title = CharacterCatalog.All[CharacterCatalog.Companion(state, id).DefinitionId].Title,
        Detail = state.Fellowship.MordorStep.HasValue ? "Remove from play on the Mordor Track." : "Separate into the Fellowship's region."
    };

    public override void Choose(EffectContext context, string id) => ChooseSelection(context, [id]);
    public override void ChooseSelection(EffectContext context, IReadOnlyList<string> ids)
    {
        var state = context.State;
        var destination = state.Fellowship.MordorStep.HasValue ? "casualties" : state.Fellowship.RegionId;
        var characters = ids.Select(id => CharacterCatalog.Companion(state, id)).ToArray();
        CompanionRules.Apply(state, new(characters, destination,
            destination != "casualties" && BoardRules.Besieged(state, destination, Side.FreePeoples), false), true);
        CharacterLifecycle.ReconcileDice(state);
        FellowshipFlow.CompositionChanged(state, FellowshipReturn.Parent);
    }

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        if (state.Fellowship.MordorStep.HasValue)
            preview.RemovedIds.AddRange(ids);
        else
            foreach (var id in ids)
                if (id == LordsCharacters.Smeagol)
                    preview.RemovedIds.Add(id);
                else
                    preview.Placements.Add(new() { UnitId = id, RegionId = state.Fellowship.RegionId });
        if (state.Fellowship.Companions.Except(ids).SequenceEqual([LordsCharacters.Smeagol]))
            preview.RemovedIds.Add(LordsCharacters.Smeagol);
    }
}
