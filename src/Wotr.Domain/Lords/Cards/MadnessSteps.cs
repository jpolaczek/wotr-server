using Wotr.Contracts;

namespace Wotr.Domain;

// Capture this branch before event losses can end the siege or eliminate the target.
public sealed record MadnessTarget(string Region, bool Besieged);

public sealed record MadnessAttackStep : EffectChoiceStep
{
    public override string Presentation => "Region";

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        var regions = BoardRules.CurrentRegions(state).Where(IsSauronStronghold)
            .SelectMany(r => r.Neighbors.Append(r.Id)).ToHashSet(StringComparer.Ordinal);
        return state.Units.Where(u => BoardRules.Owner(u) == Side.FreePeoples && BoardRules.IsArmyUnit(u) && regions.Contains(u.Location))
            .Select(u => u.Location).Distinct().ToArray();
    }

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        TargetId = id,
        Title = "Madness and Horror",
        Detail = "Roll 3 dice against this army; each 5+ hits. Then recruit up to 2 Sauron Regulars."
    };

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.HasUnknownResult = true;

    public override void Choose(EffectContext context, string id)
    {
        var state = context.State;
        var target = ArmyTarget.At(state, Side.FreePeoples, id)!;
        context.Execution.MadnessTarget = new(id, target.Garrison && IsSauronStronghold(BoardRules.Region(state, id)) &&
            BoardRules.Besieged(state, id, Side.FreePeoples));
        var rolls = Enumerable.Range(0, 3).Select(_ => context.Random.Next(6) + 1).ToList();
        var hits = rolls.Count(r => r >= 5);
        context.Record("EventArmyRoll", $"{context.Execution.SourceId}: [{string.Join(", ", rolls)}], {hits} hit(s) in {id}.", rolls: rolls, hitTarget: 5, hits: hits, targets: [id]);
        EventLosses.Begin(context, target, hits, rolls, CasualtyCause.Event);
    }

    private static bool IsSauronStronghold(RegionDefinition region) => region.Nation == "sauron" && region.Settlement == "Stronghold";
}

public sealed record MadnessRecruitStep : EffectChoiceStep
{
    public override string Presentation => "Region";

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        if (execution.MadnessTarget is not { } target || Recruits(state).Count == 0)
            return [];
        IEnumerable<string> regions = target.Besieged ? [target.Region] : BoardRules.Regions[target.Region].Neighbors;
        return regions.Where(id => RecruitmentRules.EventGarrison(state, Side.Shadow, id) == false).ToArray();
    }

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        TargetId = id,
        Title = "Recruit",
        Detail = $"Place {Recruits(state).Count} Sauron Regulars together in this region."
    };

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.Placements.AddRange(Recruits(state).Select(u => new UnitPlacementChoice { UnitId = u.Id, RegionId = ids.Single() }));

    public override void Choose(EffectContext context, string id)
    {
        var recruits = Recruits(context.State);
        foreach (var unit in recruits)
        {
            unit.Location = id;
            unit.Garrison = false;
        }
        context.Record("EventRecruited", $"Madness and Horror recruited {recruits.Count} Sauron Regulars in {id}.");
    }

    private static IReadOnlyList<UnitState> Recruits(MatchState state) => state.Units
        .Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == "reserve").Take(2).ToArray();
}
