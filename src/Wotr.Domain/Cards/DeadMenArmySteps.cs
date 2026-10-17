using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record DeadMenAttackStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var target = ArmyTarget.At(context.State, Side.Shadow, context.Execution.RegionId);
        if (target is null)
            return;
        PoliticalRules.ArmyAttacked(context.State, target.Units(context.State), PoliticalAttack.DeadMenOfDunharrow);
        var roll = context.Random.Next(6) + 1;
        context.Record("DeadMenAttack", $"Dead Men of Dunharrow: rolled {roll}, {roll} hit(s) in {target.Region}.", rolls: [roll], hits: roll, targets: [target.Region]);
        EventLosses.Begin(context, target, roll, [roll]);
    }
}

public sealed record DeadMenRetreatStep : EffectChoiceStep
{
    public override string Presentation => "Region";
    public override Side Chooser(MatchState state, EffectExecution execution) => Side.Shadow;
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        ArmyTarget.At(state, Side.Shadow, execution.RegionId) is null ? [] : BoardRules.Region(state, execution.RegionId).Neighbors
            .Where(r => BattleMovement.IsFree(state, Side.Shadow, r)).ToList();

    public override void Apply(EffectContext context)
    {
        var target = ArmyTarget.At(context.State, Side.Shadow, context.Execution.RegionId);
        if (target is null)
            return;
        if (Candidates(context.State, context.Execution).Count > 0)
            base.Apply(context);
        else
            CasualtyRules.Eliminate(context.State, target.Units(context.State).Select(u => u.Id).ToArray(),
                CasualtyCause.DestructiveEvent, target.Participants.ToHashSet());
    }

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        TargetId = id,
        Title = "Retreat",
        Detail = "Retreat with the surviving Shadow Army."
    };

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.Placements.AddRange(ArmyTarget.At(state, Side.Shadow, execution.RegionId)!.Participants
            .Select(id => new UnitPlacementChoice { UnitId = id, RegionId = ids.Single() }));

    public override void Choose(EffectContext context, string id)
    {
        var target = ArmyTarget.At(context.State, Side.Shadow, context.Execution.RegionId)!;
        var movement = MovementFacts.Event(context.State, ActionMovementKind.Retreat, target.Region, id,
            target.Units(context.State), context.State.Characters.Where(c => target.Participants.Contains(c.Id) && CharacterRules.Mobile(c)));
        foreach (var unit in target.Units(context.State))
        {
            unit.Location = id;
            unit.Garrison = false;
        }
        foreach (var character in context.State.Characters.Where(c => target.Participants.Contains(c.Id) && CharacterRules.Mobile(c)))
        {
            character.Location = id;
            character.Garrison = false;
        }
        TerritoryRules.Arrive(context.State, Side.Shadow, target.Region, id, politicalEntry: false);
        context.Record("EventRetreat", $"Dead Men of Dunharrow: the Shadow Army retreated to {BoardRules.Regions[id].Name}.",
            movements: movement.Movements);
        StackLimits.OpenIfRequired(context.State, Side.Shadow);
    }
}

public sealed record DeadMenRecruitStep : EffectChoiceStep
{
    public override string Presentation => "ReserveFigures";
    public override int Minimum(MatchState state, EffectExecution execution) => 0;
    public override int Maximum(MatchState state, EffectExecution execution) => Math.Min(3, Candidates(state, execution).Count);
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) => state.Units
        .Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular && u.Location == "reserve").Take(3).Select(u => u.Id).ToList();

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        TargetId = execution.RegionId,
        SelectionGroup = "recruits",
        GroupMinimum = 0,
        GroupMaximum = 3
    };

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        preview.Placements.AddRange(ids.Select(id => new UnitPlacementChoice { UnitId = id, RegionId = execution.RegionId }));
        if (ids.Count > 0 && BoardRules.IsSettlement(BoardRules.Region(state, execution.RegionId)) && state.Control.GetValueOrDefault(execution.RegionId) != execution.Owner)
            preview.ControlChanges[execution.RegionId] = execution.Owner;
    }

    public override void Choose(EffectContext context, string id) => ChooseSelection(context, [id]);
    public override void ChooseSelection(EffectContext context, IReadOnlyList<string> ids)
    {
        foreach (var unit in context.State.Units.Where(u => ids.Contains(u.Id)))
        {
            unit.Location = context.Execution.RegionId;
            unit.Garrison = false;
        }
        if (ids.Count > 0)
            TerritoryRules.Capture(context.State, Side.FreePeoples, context.Execution.RegionId);
        context.Record("EventRecruited", $"Dead Men of Dunharrow: recruited {ids.Count} Gondor regular(s) in {context.Execution.RegionId}.");
    }
}
