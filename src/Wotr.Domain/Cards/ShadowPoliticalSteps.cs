using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record ShadowAtWarCondition : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) =>
        state.Nations.Where(n => BoardRules.Nations[n.Id].Side == Side.Shadow).All(n => n.StepsToWar == 0);
}

public sealed record DiscardWillDiceStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var dice = context.State.Dice.Where(d => d.Owner == Side.FreePeoples &&
            d.Location == DieLocation.Available && d.Face == DieFace.WillOfTheWest).ToArray();
        foreach (var die in dice)
            die.Location = DieLocation.Spent;
        context.Record("WillDiceDiscarded", $"The Day Without Dawn discarded {dice.Length} unused Will of the West dice.");
    }
}

public sealed record StormcrowCondition : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => NationTargets(state, actor).Count > 0;
    public override bool ValidTarget(MatchState state, Side actor, CommandPayload payload) => NationTargets(state, actor).Contains(payload.NationId);
    public override IReadOnlyList<string> NationTargets(MatchState state, Side actor)
    {
        var occupied = state.Characters.Where(c => CharacterRules.OnBoard(c) && CharacterRules.IsCompanion(c))
            .Select(c => c.Location).Append(state.Fellowship.RegionId).Where(BoardRules.Regions.ContainsKey)
            .Select(id => BoardRules.Region(state, id).Nation).ToHashSet();
        return state.Nations.Where(n => occupied.Contains(n.Id) && n.StepsToWar > 0 &&
            BoardRules.Nations[n.Id].Side == Side.FreePeoples).Select(n => n.Id).ToList();
    }
}

public sealed record RetreatPoliticalCounterStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var nation = context.State.Nations.Single(n => n.Id == context.Execution.NationId);
        nation.StepsToWar = Math.Min(3, nation.StepsToWar + 1);
        context.Record("PoliticsRetreated", $"{nation.Id}: {nation.StepsToWar} steps to war.");
    }
}

public sealed record StormcrowLossStep : EffectChoiceStep
{
    public override Side Chooser(MatchState state, EffectExecution execution) => Side.FreePeoples;
    public override string Presentation => "ArmyFigure";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) => state.Units
        .Where(u => u.Nation == execution.NationId && BoardRules.Regions.ContainsKey(u.Location) &&
            (BoardRules.IsArmyUnit(u) || u.Kind == UnitKind.Leader)).Select(u => u.Id).ToList();
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        TargetId = state.Units.Single(u => u.Id == id).Location,
        Detail = "Eliminate this whole figure. An elite is not downgraded."
    };
    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        var board = MovementPlan.CopyBoard(state);
        CasualtyRules.Eliminate(board, ids, CasualtyCause.Event);
        BattlePreviewRules.DescribeBoard(state, board, preview);
    }
    public override void Choose(EffectContext context, string id) => CasualtyRules.Eliminate(context.State, [id], CasualtyCause.Event);
}
