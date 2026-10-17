using Wotr.Contracts;

namespace Wotr.Domain;

public abstract record RecruitRegionStep(string Nation, bool IncludeLeaders = false, bool AllowBesieged = true) : EffectChoiceStep
{
    public override string Presentation => "Region";
    protected abstract bool Eligible(MatchState state, RegionDefinition region);

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        if (!state.Units.Any(u => u.Nation == Nation && u.Location == "reserve" &&
                (BoardRules.IsArmyUnit(u) || IncludeLeaders && u.Kind == UnitKind.Leader)))
            return [];
        return BoardRules.CurrentRegions(state).Where(r => Eligible(state, r) &&
            RecruitmentRules.EventGarrison(state, execution.Owner, r.Id) is { } garrison && (AllowBesieged || !garrison)).Select(r => r.Id).ToArray();
    }

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        TargetId = id,
        Title = "Recruit",
        Detail = "All recruits from this card enter this region."
    };

    public override void Choose(EffectContext context, string id) => context.Execution.RegionId = id;
}

public sealed record CoastalArmyRecruitStep() : RecruitRegionStep("elves")
{
    protected override bool Eligible(MatchState state, RegionDefinition region) => region.Coastal &&
        state.Units.Any(u => u.Location == region.Id && BoardRules.Owner(u) == Side.FreePeoples && BoardRules.IsArmyUnit(u));
}

public sealed record RohanCompanionRecruitStep() : RecruitRegionStep("rohan", true)
{
    protected override bool Eligible(MatchState state, RegionDefinition region) => region.Id == "edoras" ||
        region.Nation == "rohan" && state.Characters.Any(c => c.Location == region.Id && CharacterRules.IsCompanion(c));
}

public sealed record FreeRohanSettlementRecruitStep() : RecruitRegionStep("rohan", true, false)
{
    protected override bool Eligible(MatchState state, RegionDefinition region) => region.Nation == "rohan" && BoardRules.IsSettlement(region);
}
