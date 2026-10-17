using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class EaglesRules
{
    public static IReadOnlyList<string> Targets(MatchState state)
    {
        var companions = state.Characters.Where(c => CharacterRules.OnBoard(c) &&
            CharacterCatalog.All.GetValueOrDefault(c.DefinitionId)?.Role == "Companion" && HasArmy(state, c.Location, c.Garrison, Side.FreePeoples)).ToList();
        return state.Units.Where(u => u.Kind == UnitKind.Nazgul && BoardRules.Regions.ContainsKey(u.Location) &&
            HasArmy(state, u.Location, u.Garrison, Side.Shadow) && companions.Any(c => c.Location == u.Location ||
                BoardRules.Regions[c.Location].Neighbors.Contains(u.Location))).Select(u => u.Location).Distinct().ToList();
    }

    private static bool HasArmy(MatchState state, string region, bool garrison, Side side) => state.Units.Any(u =>
        u.Location == region && u.Garrison == garrison && BoardRules.Owner(u) == side && BoardRules.IsArmyUnit(u));

    public static List<UnitState> Nazgul(MatchState state, string region) => state.Units.Where(u =>
        u.Location == region && u.Kind == UnitKind.Nazgul && HasArmy(state, region, u.Garrison, Side.Shadow)).ToList();

    public static List<UnitState> Survivors(MatchState state, EffectExecution execution) => state.Units.Where(u =>
        execution.TargetIds.Contains(u.Id) && u.Location == execution.RegionId).ToList();
}

public sealed record EaglesCondition : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => EaglesRules.Targets(state).Count > 0;
}

public sealed record EaglesAttackStep : EffectChoiceStep
{
    public override string Presentation => "Region";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) => EaglesRules.Targets(state);
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        TargetId = id,
        Title = "Eagles",
        Detail = "Roll for the Nazgûl in this Shadow Army. The Witch-king is unaffected."
    };

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.HasUnknownResult = true;

    public override void Choose(EffectContext context, string id)
    {
        var nazgul = EaglesRules.Nazgul(context.State, id);
        context.Execution.RegionId = id;
        context.Execution.TargetIds = nazgul.Select(u => u.Id).ToList();
        var dice = Enumerable.Range(0, Math.Min(5, nazgul.Count)).Select(_ => context.Random.Next(6) + 1).ToArray();
        foreach (var casualty in nazgul.Take(dice.Count(d => d >= 5)))
            BoardRules.ReturnToReserve(casualty);
        context.Record("EaglesAttack", $"The Eagles are Coming! {BoardRules.Regions[id].Name}: [{string.Join(", ", dice)}], {dice.Count(d => d >= 5)} Nazgûl eliminated.", rolls: dice, hitTarget: 5, hits: dice.Count(d => d >= 5), targets: [id]);
    }
}

public sealed record EaglesRetreatStep : EffectChoiceStep
{
    public override string Presentation => "Region";
    public override Side Chooser(MatchState state, EffectExecution execution) => Side.Shadow;
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) => EaglesRules.Survivors(state, execution).Count == 0 ? [] :
        BoardRules.CurrentRegions(state).Where(r => r.Nation == "sauron" && r.Settlement == "Stronghold" &&
            r.Id != execution.RegionId && r.OriginalOwner == Side.Shadow && state.Control.GetValueOrDefault(r.Id) == Side.Shadow).Select(r => r.Id).ToList();

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        TargetId = id,
        Title = "Retreat",
        Detail = "Move all surviving Nazgûl together to this Sauron Stronghold."
    };

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.Placements.AddRange(EaglesRules.Survivors(state, execution).Select(u => new UnitPlacementChoice { UnitId = u.Id, RegionId = ids.Single() }));

    public override void Choose(EffectContext context, string id)
    {
        var movement = MovementFacts.Event(context.State, ActionMovementKind.Travel, context.Execution.RegionId, id,
            EaglesRules.Survivors(context.State, context.Execution), []);
        foreach (var unit in EaglesRules.Survivors(context.State, context.Execution))
        {
            unit.Location = id;
            unit.Garrison = BoardRules.Besieged(context.State, id, Side.Shadow);
        }
        context.Record("NazgulRetreated", $"The surviving Nazgûl moved to {BoardRules.Regions[id].Name}.", movements: movement.Movements);
    }
}
