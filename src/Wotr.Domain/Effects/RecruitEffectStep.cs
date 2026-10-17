using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record RecruitEffectStep(string Nation, string Region, bool Leader = false, int Count = 1, UnitKind? RequiredKind = null) : EffectChoiceStep
{
    public override string Presentation => "ReserveFigures";
    public override string TargetId => Region;
    private string Destination(EffectExecution execution) => Region.Length > 0 ? Region : execution.RegionId;
    public override int Minimum(MatchState state, EffectExecution execution) => Math.Min(Count, Candidates(state, execution).Count);
    public override int Maximum(MatchState state, EffectExecution execution) => Minimum(state, execution);
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        TargetId = Destination(execution),
        SelectionGroup = Count > 1 ? "Recruits" : "",
        GroupMinimum = Minimum(state, execution),
        GroupMaximum = Maximum(state, execution),
        Detail = "Recruit in " + BoardRules.Regions[Destination(execution)].Name
    };
    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.Placements.AddRange(ids.Select(id => new UnitPlacementChoice { UnitId = id, RegionId = Destination(execution) }));

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        if (RecruitmentRules.EventGarrison(state, execution.Owner, Destination(execution)) is null)
            return [];
        // Equivalent figures are interchangeable; Regular versus Elite remains a real choice.
        return state.Units.Where(u => u.Location == "reserve" && u.Nation == Nation &&
                (Leader ? u.Kind == UnitKind.Leader : BoardRules.IsArmyUnit(u)) && (!RequiredKind.HasValue || u.Kind == RequiredKind))
            .GroupBy(u => u.Kind).SelectMany(g => g.Take(Count).Select(u => u.Id)).ToArray();
    }

    public override void ChooseSelection(EffectContext context, IReadOnlyList<string> ids)
    {
        foreach (var id in ids)
            Choose(context, id);
    }

    public override void Choose(EffectContext context, string id)
    {
        var unit = context.State.Units.Single(u => u.Id == id);
        var region = Destination(context.Execution);
        unit.Garrison = RecruitmentRules.EventGarrison(context.State, context.Owner, region)
            ?? throw new InvalidOperationException("Recruitment target is no longer available.");
        unit.Location = region;
        context.Record("EventRecruited", $"{context.Execution.SourceId}: recruited {unit.Nation} {unit.Kind} in {region}.");
        CasualtyRules.RemoveUnaccompanied(context.State, region, unit.Garrison, context.Owner, CasualtyCause.Event);
    }
}
