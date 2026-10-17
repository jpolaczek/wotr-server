using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record DeadMenCondition : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => KingArmyRules.King(state) is { } king &&
        BoardRules.Region(state, king.Location).Nation == "rohan";
}

public sealed record DeadMenJourneyStep : EffectChoiceStep
{
    public static IReadOnlyList<string> Destinations { get; } = Array.AsReadOnly<string>(["erech", "lamedon", "pelargir"]);
    public override string Presentation => "CompanionDestination";

    private static List<CharacterState> Companions(MatchState state)
    {
        var king = KingArmyRules.King(state)!;
        return state.Characters.Where(c => c.Location == king.Location && c.Id != king.Id &&
            CharacterCatalog.All[c.DefinitionId].Role == "Companion").ToList();
    }

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        Destinations.Concat(Companions(state).Select(c => c.Id)).ToList();
    public override int Maximum(MatchState state, EffectExecution execution) => Companions(state).Count + 1;
    public override bool ValidSelection(MatchState state, EffectExecution execution, IReadOnlyList<string> ids) =>
        base.ValidSelection(state, execution, ids) && ids.Count(Destinations.Contains) == 1;

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => Destinations.Contains(id) ? new()
    {
        Id = id,
        TargetId = id,
        SelectionGroup = "destination",
        Title = "Dead Men of Dunharrow",
        Detail = "Move Strider/Aragorn here with any selected companions."
    } : new()
    {
        Id = id,
        SourceId = state.Characters.Single(c => c.Id == id).DefinitionId,
        SelectionGroup = "companions",
        GroupMinimum = 0,
        GroupMaximum = Companions(state).Count,
        Title = CharacterCatalog.All[state.Characters.Single(c => c.Id == id).DefinitionId].Title
    };

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        var destination = ids.Single(Destinations.Contains);
        var companions = ids.Where(id => !Destinations.Contains(id)).Append(KingArmyRules.King(state)!.Id);
        preview.Placements.AddRange(companions.Select(id => new UnitPlacementChoice { UnitId = id, RegionId = destination }));
        preview.HasUnknownResult = ArmyTarget.At(state, Side.Shadow, destination) is not null;
    }

    public override void Choose(EffectContext context, string id) => ChooseSelection(context, [id]);
    public override void ChooseSelection(EffectContext context, IReadOnlyList<string> ids)
    {
        var king = KingArmyRules.King(context.State)!;
        var destination = ids.Single(Destinations.Contains);
        var group = Companions(context.State).Where(c => ids.Contains(c.Id)).Append(king).ToList();
        var movement = MovementFacts.Event(context.State, ActionMovementKind.Travel, king.Location, destination, [], group);
        foreach (var companion in group)
        {
            companion.Location = destination;
            companion.Garrison = false;
            PoliticalRules.CompanionArrived(context.State, destination, companion.NationIcon);
        }
        context.Execution.RegionId = destination;
        context.Record("CompanionsMoved", $"Dead Men of Dunharrow: {group.Count} companion(s) moved to {BoardRules.Regions[destination].Name}.",
            movements: movement.Movements);
    }
}
