using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record KingsNationalCardStep(string Sovereign, string OtherwiseProgram) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        if (SovereignLifecycle.Find(context.State, Sovereign) is { Status: SovereignStatus.Corrupted } sovereign)
            SovereignLifecycle.Remove(context.State, sovereign);
        else
            EffectRuntime.BeginProgram(context.State, context.Owner, context.Execution.SourceId, OtherwiseProgram);
    }
}

// UI-278 (EXP-15 D15): each branch names its object — the recruit the sovereign's nation, whose Reserve figures choose it,
// the cleanse the sovereign himself.
public sealed record KingsRecruitOrCleanseStep(string Sovereign, string RecruitProgram) : EffectChoiceStep
{
    public override string Presentation => "SovereignCardBranch";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) => ["recruit", "corruption"];
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        TargetId = id == "recruit" ? SovereignCatalog.All[Sovereign].Nation : Sovereign,
        Title = id == "recruit" ? "Recruit" : "Remove corruption",
        Detail = id == "recruit" ? "Recruit the available figures specified by this card."
            : $"Return one randomly chosen {KingsCorruptionRemoval.TileDescription(state)} to the Hunt Pool, if any; the card's draw follows."
    };
    public override void Choose(EffectContext context, string id)
    {
        if (id == "corruption")
            KingsCorruptionRemoval.RandomHidden(context, Sovereign);
        else
        {
            var region = context.Execution.RegionId;
            EffectRuntime.BeginProgram(context.State, context.Owner, context.Execution.SourceId, RecruitProgram);
            EffectRuntime.Current(context.State)!.Effect!.RegionId = region;
        }
    }
    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.HasUnknownResult = ids.Single() == "corruption";
}

public sealed record KingsRohanRecruitStep() : RecruitmentBatchStep([], maximumRegions: 1)
{
    protected override IReadOnlyList<RecruitmentQuota> ResolveQuotas(MatchState state, EffectExecution execution) =>
        state.Characters.Where(c => CharacterRules.OnBoard(c) && CharacterRules.IsCompanion(c) &&
            BoardRules.Region(state, c.Location).Nation == "rohan").Select(c => c.Location).Append("edoras").Distinct()
            .SelectMany(region => new RecruitmentQuota[]
            {
                new("rohan", region, [UnitKind.Regular, UnitKind.Elite], 1),
                new("rohan", region, [UnitKind.Leader], 1)
            }).ToArray();
}
