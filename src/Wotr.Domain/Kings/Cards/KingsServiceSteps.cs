using Wotr.Contracts;

namespace Wotr.Domain;

public static class NonCorruptedSovereigns
{
    public static IEnumerable<SovereignState> OnBoard(MatchState state) =>
        state.Kings?.Sovereigns.Where(s => s.Status is SovereignStatus.Initial or SovereignStatus.Awakened &&
            BoardRules.Regions.ContainsKey(SovereignLifecycle.Region(state, s))) ?? [];
}

public sealed record ServiceSovereignStep : EffectChoiceStep
{
    public override string Presentation => "Sovereign";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        NonCorruptedSovereigns.OnBoard(state).Select(s => s.Id).ToArray();
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, SourceId = id, Title = SovereignCatalog.All[id].Title,
        TargetId = SovereignLifecycle.Region(state, SovereignLifecycle.Find(state, id)!),
        Detail = $"Recruit here, then return one random {KingsCorruptionRemoval.TileDescription(state)} if possible."
    };
    public override void Choose(EffectContext context, string id)
    {
        context.Execution.TargetIds = [id];
        context.Execution.RegionId = SovereignLifecycle.Region(context.State, SovereignLifecycle.Find(context.State, id)!);
        context.Execution.NationId = SovereignCatalog.All[id].Nation;
    }
}

public sealed record ServiceRecruitStep() : RecruitmentBatchStep([])
{
    protected override IReadOnlyList<RecruitmentQuota> ResolveQuotas(MatchState state, EffectExecution execution) =>
        execution.TargetIds.Count == 0 ? [] : [new(execution.NationId, execution.RegionId, [UnitKind.Regular, UnitKind.Elite], 1)];
}

public sealed record ServiceCorruptionStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        if (context.Execution.TargetIds.SingleOrDefault() is { } id)
            KingsCorruptionRemoval.RandomHidden(context, id);
    }
}

public sealed record VillagesRecruitStep() : RecruitmentBatchStep([], distinctNations: 4)
{
    protected override IReadOnlyList<RecruitmentQuota> ResolveQuotas(MatchState state, EffectExecution execution) =>
        BoardRules.CurrentRegions(state).Where(r =>
            r.Nation is not null && BoardRules.Nations[r.Nation].Side == execution.Owner && BoardRules.IsSettlement(r) &&
            state.Control.GetValueOrDefault(r.Id) == execution.Owner && !BoardRules.HasEnemyArmy(state, r.Id, execution.Owner))
            .Select(r => new RecruitmentQuota(r.Nation!, r.Id, [UnitKind.Regular, UnitKind.Leader], 1)).ToArray();
}
