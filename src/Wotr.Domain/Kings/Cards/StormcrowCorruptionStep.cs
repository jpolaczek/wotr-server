using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record StormcrowCorruptionStep : EffectStep
{
    public static SovereignState? Sovereign(MatchState state, EffectExecution execution) =>
        state.Kings?.Sovereigns.Find(s => SovereignCatalog.All[s.Id].Nation == execution.NationId);

    public override void Apply(EffectContext context)
    {
        if (Sovereign(context.State, context.Execution) is not { Status: SovereignStatus.Initial } sovereign)
            return;
        var ignored = new List<string>();
        while (context.State.HuntTiles.Any(t => KingsTiles.IsNumeric(t.Id) && t.Location is "pool" or "discard"))
        {
            var tile = HuntPool.DrawPhysical(context.State, context.Random);
            if (KingsTiles.IsNumeric(tile.Id))
            {
                if (KingsTiles.IsKings(tile.Id))
                    KingsTiles.Reveal(context.State, [tile.Id]);
                CorruptionRules.Attach(context.State, sovereign, tile.Id);
                break;
            }
            ignored.Add(tile.Id);
            KingsTiles.Reveal(context.State, [tile.Id]);
        }
        KingsTiles.Return(context.State, ignored);
    }
}

public sealed record KingsStormcrowLossStep : EffectChoiceStep
{
    public override Side Chooser(MatchState state, EffectExecution execution) => Side.FreePeoples;
    public override string Presentation => "ArmyFigure";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        StormcrowCorruptionStep.Sovereign(state, execution)?.Status == SovereignStatus.Awakened ? [] : state.Units
            .Where(u => u.Nation == execution.NationId && BoardRules.Regions.ContainsKey(u.Location) && BoardRules.IsArmyUnit(u))
            .Select(u => u.Id).ToArray();
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, TargetId = state.Units.Single(u => u.Id == id).Location,
        Detail = "Eliminate this whole Army unit. An Elite is not downgraded."
    };
    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        var board = MovementPlan.CopyBoard(state);
        CasualtyRules.Eliminate(board, ids, CasualtyCause.Event);
        BattlePreviewRules.DescribeBoard(state, board, preview);
    }
    public override void Choose(EffectContext context, string id) => CasualtyRules.Eliminate(context.State, [id], CasualtyCause.Event);
}
