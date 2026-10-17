using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record KingsRevealedCondition : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => new AragornPresentCondition().CanPlay(state, actor) ||
        SovereignLifecycle.Find(state, SovereignCatalog.Denethor) is { Status: SovereignStatus.Awakened } sovereign &&
        BoardRules.Regions.ContainsKey(SovereignLifecycle.Region(state, sovereign));
}

public sealed record KingBonusRecruitStep : EffectChoiceStep
{
    public const string Region = "minas-morgul";
    public override string Presentation => "ReserveFigures";
    private static int Count(MatchState state) => state.Kings?.Sovereigns.Count(s => s.Status == SovereignStatus.Awakened &&
        BoardRules.Regions.ContainsKey(SovereignLifecycle.Region(state, s))) ?? 0;
    public override int Minimum(MatchState state, EffectExecution execution) => Math.Min(Count(state), Candidates(state, execution).Count);
    public override int Maximum(MatchState state, EffectExecution execution) => Minimum(state, execution);
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        RecruitmentRules.EventGarrison(state, execution.Owner, Region) is null ? [] : state.Units
            .Where(u => u.Location == "reserve" && u.Nation == "sauron" && u.Kind is UnitKind.Regular or UnitKind.Nazgul)
            .GroupBy(u => u.Kind).SelectMany(g => g.Take(Count(state)).Select(u => u.Id)).ToArray();
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, TargetId = Region, SelectionGroup = "Recruits",
        GroupMinimum = Minimum(state, execution), GroupMaximum = Maximum(state, execution),
        Detail = "Recruit one Sauron Regular or Nazgûl per Awakened Sovereign."
    };
    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.Placements.AddRange(ids.Select(id => new UnitPlacementChoice { UnitId = id, RegionId = Region }));
    public override void ChooseSelection(EffectContext context, IReadOnlyList<string> ids)
    {
        foreach (var id in ids)
            Choose(context, id);
    }
    public override void Choose(EffectContext context, string id)
    {
        var unit = context.State.Units.Single(u => u.Id == id);
        unit.Garrison = RecruitmentRules.EventGarrison(context.State, context.Owner, Region)!.Value;
        unit.Location = Region;
        context.Record("EventRecruited", $"The King Is Revealed: recruited {unit.Kind} in Minas Morgul.");
        CasualtyRules.RemoveUnaccompanied(context.State, Region, unit.Garrison, context.Owner, CasualtyCause.Event);
    }
}
