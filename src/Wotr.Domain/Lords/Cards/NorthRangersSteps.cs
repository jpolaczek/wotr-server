using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record NorthRangersAttackStep : EffectChoiceStep
{
    public override string Presentation => "Region";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        var regions = BoardRules.CurrentRegions(state).Where(r => r.Nation == "north")
            .SelectMany(r => r.Neighbors.Append(r.Id)).ToHashSet(StringComparer.Ordinal);
        return state.Units.Where(u => BoardRules.IsArmyUnit(u) && BoardRules.Owner(u) == Side.Shadow && regions.Contains(u.Location))
            .Select(u => u.Location).Distinct().ToArray();
    }

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, TargetId = id, Title = "Rangers attack", Detail = "Roll three dice; each 5+ hits. Then recruit in an adjacent free North region."
    };
    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.HasUnknownResult = true;

    public override void Choose(EffectContext context, string id)
    {
        context.Execution.RegionId = id;
        var rolls = Enumerable.Range(0, 3).Select(_ => context.Random.Next(6) + 1).ToList();
        var hits = rolls.Count(r => r >= 5);
        context.Record("EventArmyRoll", $"Rangers of the North: [{string.Join(", ", rolls)}], {hits} hit(s) in {id}.", rolls: rolls, hitTarget: 5, hits: hits, targets: [id]);
        EventLosses.Begin(context, ArmyTarget.At(context.State, Side.Shadow, id)!, hits, rolls, CasualtyCause.Event);
    }
}

public sealed record NorthRangersRegionStep : EffectChoiceStep
{
    public override string Presentation => "Region";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        if (!BoardRules.Regions.TryGetValue(execution.RegionId, out var origin) ||
            !state.Units.Any(u => u.Nation == "north" && u.Location == "reserve"))
            return [];
        return origin.Neighbors.Where(id => BoardRules.Region(state, id).Nation == "north" &&
            BattleMovement.IsFree(state, Side.FreePeoples, id) &&
            RecruitmentRules.EventGarrison(state, Side.FreePeoples, id) is false).ToArray();
    }

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, TargetId = id, Title = "Recruit Rangers", Detail = "One North unit and one North Leader enter here."
    };

    public override void Apply(EffectContext context)
    {
        if (Candidates(context.State, context.Execution).Count == 0)
            context.Execution.RegionId = "";
        else
            base.Apply(context);
    }

    public override void Choose(EffectContext context, string id) => context.Execution.RegionId = id;
}
