using Wotr.Contracts;

namespace Wotr.Domain;

// Resolve printed national settlements when the action runs, after board modifiers.
public sealed record NationalRecruitmentStep(string Nation, bool StrongholdsOnly, int PerRegion, int Regions)
    : RecruitmentBatchStep([], Regions)
{
    protected override IReadOnlyList<RecruitmentQuota> ResolveQuotas(MatchState state, EffectExecution execution) =>
        BoardRules.CurrentRegions(state).Where(r => r.Nation == Nation &&
            (StrongholdsOnly ? r.Settlement == "Stronghold" : BoardRules.IsSettlement(r)))
        .Select(r => new RecruitmentQuota(Nation, r.Id, [UnitKind.Regular], PerRegion)).ToArray();
}
