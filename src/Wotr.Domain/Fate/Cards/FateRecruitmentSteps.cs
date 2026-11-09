using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record FateDaleRecruitStep(string Nation) : RecruitmentBatchStep([], maximumRegions: 1)
{
    protected override IReadOnlyList<RecruitmentQuota> ResolveQuotas(MatchState state, EffectExecution execution)
    {
        IEnumerable<string> regions = BoardRules.Regions["dale"].Neighbors;
        if (state.Units.Any(u => u.Location == "dale" && BoardRules.Owner(u) == Side.FreePeoples && BoardRules.IsArmyUnit(u)))
            regions = regions.Append("dale");
        return regions.SelectMany(region => new RecruitmentQuota[]
        {
            new(Nation, region, [UnitKind.Regular, UnitKind.Elite], 1),
            new(Nation, region, [UnitKind.Leader], 1)
        }).ToArray();
    }
}

public sealed record FateOrcsRegionStep : EffectChoiceStep
{
    public override string Presentation => "Region";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        state.Units.Any(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == "reserve")
            ? new[] { "mount-gundabad", "erebor" }.Where(region =>
                RecruitmentRules.EventGarrison(state, Side.Shadow, region) is not null).ToArray() : [];

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, TargetId = id, Title = "Second recruitment region",
        Detail = "Recruit three Sauron Regulars here and three in Dol Guldur. If the Reserve runs short, you choose how to split them."
    };

    public override void Choose(EffectContext context, string id) => context.Execution.RegionId = id;
}

public sealed record FateOrcsRecruitStep() : RecruitmentBatchStep([])
{
    protected override IReadOnlyList<RecruitmentQuota> ResolveQuotas(MatchState state, EffectExecution execution)
    {
        var quotas = new List<RecruitmentQuota> { new("sauron", "dol-guldur", [UnitKind.Regular], 3) };
        if (execution.RegionId is "mount-gundabad" or "erebor")
            quotas.Add(new("sauron", execution.RegionId, [UnitKind.Regular], 3));
        return quotas;
    }
}
