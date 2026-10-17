using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record DunlendingRegionStep : RecruitRegionStep
{
    private static readonly HashSet<string> Neighbors = BoardRules.Regions["north-dunland"].Neighbors
        .Concat(BoardRules.Regions["south-dunland"].Neighbors).ToHashSet(StringComparer.Ordinal);

    public DunlendingRegionStep() : base("isengard", AllowBesieged: false) { }
    protected override bool Eligible(MatchState state, RegionDefinition region) => Neighbors.Contains(region.Id) &&
        state.Units.Any(u => u.Nation == "isengard" && u.Location == "reserve" && u.Kind == UnitKind.Regular);
}

public sealed record DunlendingMoveStep : EffectChoiceStep
{
    public override string Presentation => "ArmyFigures";
    public override int Minimum(MatchState state, EffectExecution execution) => 0;
    public override int Maximum(MatchState state, EffectExecution execution) => Math.Min(4, Candidates(state, execution).Count);
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) => execution.RegionId.Length == 0 ? [] :
        state.Units.Where(u => u.Nation == "isengard" && BoardRules.IsArmyUnit(u) && !u.Garrison &&
            u.Location != execution.RegionId && u.Location is "north-dunland" or "south-dunland").Select(u => u.Id).ToArray();

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        SourceId = state.Units.Single(u => u.Id == id).Location,
        TargetId = execution.RegionId,
        SelectionGroup = "Movers",
        GroupMinimum = 0,
        GroupMaximum = Maximum(state, execution),
        Title = "Move to " + BoardRules.Regions[execution.RegionId].Name,
        Detail = "Move this Isengard unit directly to the recruitment region."
    };

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.Placements.AddRange(ids.Select(id => new UnitPlacementChoice { UnitId = id, RegionId = execution.RegionId }));

    public override void Choose(EffectContext context, string id) => ChooseSelection(context, [id]);
    public override void ChooseSelection(EffectContext context, IReadOnlyList<string> ids)
    {
        foreach (var unit in context.State.Units.Where(u => ids.Contains(u.Id)))
        {
            unit.Location = context.Execution.RegionId;
            unit.Garrison = false;
        }
        context.Record("EventArmyMoved", $"Rage of the Dunlendings moved {ids.Count} Isengard units to {context.Execution.RegionId}.");
    }
}

public sealed record WitchKingReturnStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var character = CharacterRules.WitchKingInPlay(context.State);
        if (character is null)
            return;
        character.Location = "angmar";
        character.Garrison = false;
        context.Record("CharacterMoved", "The Witch-king returned to Angmar.");
    }
}
