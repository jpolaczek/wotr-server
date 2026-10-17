using Wotr.Contracts;

namespace Wotr.Domain;

public record RecruitmentBatchStep : EffectChoiceStep
{
    public IReadOnlyList<RecruitmentQuota> Quotas { get; }
    public int MaximumRegions { get; }
    public int DistinctNations { get; }
    public override string Presentation => "RecruitmentPlan";

    public RecruitmentBatchStep(IReadOnlyList<RecruitmentQuota> quotas, int maximumRegions = 0, int distinctNations = 0)
    {
        RecruitmentCapacity.Validate(quotas, maximumRegions, distinctNations);
        Quotas = quotas;
        MaximumRegions = maximumRegions;
        DistinctNations = distinctNations;
    }

    protected virtual IReadOnlyList<RecruitmentQuota> ResolveQuotas(MatchState state, EffectExecution execution) => Quotas;
    private RecruitmentPlan Plan(MatchState state, EffectExecution execution) =>
        new(state, execution.Owner, ResolveQuotas(state, execution), MaximumRegions, DistinctNations);
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) => Plan(state, execution).Units.Select(u => u.Id).ToArray();
    public override int Minimum(MatchState state, EffectExecution execution) => Plan(state, execution).Maximum;
    public override int Maximum(MatchState state, EffectExecution execution) => Minimum(state, execution);
    public override bool ValidPayload(MatchState state, EffectExecution execution, CommandPayload payload) =>
        payload.Ids.Count == 0 && Plan(state, execution).Valid(payload.Placements);
    public override bool ValidSelection(MatchState state, EffectExecution execution, IReadOnlyList<string> ids) => false;
    public override void Choose(EffectContext context, string id) => throw new InvalidOperationException("A recruitment plan requires placements.");

    public override void Apply(EffectContext context)
    {
        var plan = Plan(context.State, context.Execution);
        if (plan.Maximum == 0)
            return;
        if (plan.Automatic() is { } placements)
            ChoosePayload(context, new() { Placements = placements });
        else
            OpenChoice(context, plan.Units.Select(u => u.Id).ToArray(), plan.Maximum);
    }

    public override ActionOption Option(MatchState state, EffectExecution execution)
    {
        var plan = Plan(state, execution);
        return new()
        {
            Kind = CommandKind.ChooseEffect,
            Minimum = plan.Maximum,
            Maximum = plan.Maximum,
            EligibleIds = plan.Units.Select(u => u.Id).ToList(),
            Targets = plan.Quotas.Select(q => q.Region).Distinct().ToList(),
            Recruitment = plan.Options(),
            EventRecruitment = new()
            {
                MaximumRegions = MaximumRegions,
                DistinctNations = DistinctNations,
                Limits = plan.Quotas.Select(q => new RecruitmentLimit
                {
                    Nation = q.Nation, RegionId = q.Region, Kinds = [.. q.Kinds], Maximum = q.Maximum
                }).ToList()
            }
        };
    }

    public override IEnumerable<EffectSelection> Selections(MatchState state, EffectExecution execution, CommandPayload payload) =>
        payload.Placements.Select(p => new EffectSelection(execution.Owner, execution.SourceId, p.UnitId, p.RegionId));

    public override void PreviewPayload(MatchState state, EffectExecution execution, CommandPayload payload, ActionPreview preview) =>
        preview.Placements.AddRange(payload.Placements.Select(p => new UnitPlacementChoice { UnitId = p.UnitId, RegionId = p.RegionId }));

    public override void ChoosePayload(EffectContext context, CommandPayload payload)
    {
        var units = context.State.Units.ToDictionary(u => u.Id);
        var garrisons = payload.Placements.Select(p => p.RegionId).Distinct().ToDictionary(region => region,
            region => RecruitmentRules.EventGarrison(context.State, context.Owner, region)
                ?? throw new InvalidOperationException("Recruitment target is no longer available."));
        foreach (var placement in payload.Placements)
        {
            var unit = units[placement.UnitId];
            unit.Location = placement.RegionId;
            unit.Garrison = garrisons[placement.RegionId];
            context.Record("EventRecruited", $"{context.Execution.SourceId}: recruited {unit.Nation} {unit.Kind} in {placement.RegionId}.");
        }
        foreach (var (region, garrison) in garrisons)
            CasualtyRules.RemoveUnaccompanied(context.State, region, garrison, context.Owner, CasualtyCause.Event);
    }
}
