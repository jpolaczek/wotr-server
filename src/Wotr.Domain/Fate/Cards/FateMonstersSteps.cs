using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record FateMonstersBranchStep : EffectChoiceStep
{
    public override string Presentation => "EffectTargets";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        [FateCards.MonstersWest, FateCards.MonstersEast];

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        Title = id == FateCards.MonstersWest ? "West of the mountains" : "East of the mountains",
        Detail = id == FateCards.MonstersWest
            ? "One Sauron Regular each in Angmar, Ettenmoors and Weather Hills, and a Sauron Elite in Trollshaws."
            : "One Sauron Regular each in Western Mirkwood, Northern Mirkwood and Withered Heath, and a Sauron Elite in one of them."
    };

    public override void Choose(EffectContext context, string id) =>
        EffectRuntime.BeginProgram(context.State, context.Owner, context.Execution.SourceId, id);
}

public sealed record FateMonstersEliteStep : EffectChoiceStep
{
    public static IReadOnlyList<string> Regions { get; } = ["western-mirkwood", "northern-mirkwood", "withered-heath"];
    public override string Presentation => "Region";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        state.Units.Any(u => u.Nation == "sauron" && u.Kind == UnitKind.Elite && u.Location == "reserve")
            ? Regions.Where(region => RecruitmentRules.EventGarrison(state, Side.Shadow, region) is not null).ToArray() : [];

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, TargetId = id, Title = "Elite destination", Detail = "Recruit the Sauron Elite here, beside this region's Regular."
    };

    public override void Choose(EffectContext context, string id) => context.Execution.RegionId = id;
}

public sealed record FateMonstersEastRecruitStep() : RecruitmentBatchStep([])
{
    protected override IReadOnlyList<RecruitmentQuota> ResolveQuotas(MatchState state, EffectExecution execution)
    {
        var quotas = FateMonstersEliteStep.Regions.Select(r => new RecruitmentQuota("sauron", r, [UnitKind.Regular], 1)).ToList();
        if (FateMonstersEliteStep.Regions.Contains(execution.RegionId))
            quotas.Add(new("sauron", execution.RegionId, [UnitKind.Elite], 1));
        return quotas;
    }
}
