using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record WestCorruptionStep : EffectChoiceStep
{
    public override string Presentation => "Sovereign";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        if (!CorruptionRules.Eyes(state).Any())
            return [];
        var regions = state.Units.Where(u => u.Kind == UnitKind.Nazgul).Select(u => u.Location)
            .Concat(state.Characters.Where(CharacterRules.IsWitchKing).Select(c => c.Location))
            .Where(BoardRules.Regions.ContainsKey).ToHashSet();
        return NonCorruptedSovereigns.OnBoard(state).Where(s => s.Status == SovereignStatus.Initial)
            .Where(s => InRange(SovereignLifecycle.Region(state, s), regions)).Select(s => s.Id).ToArray();
    }
    private static bool InRange(string region, HashSet<string> nazgulRegions) =>
        nazgulRegions.Contains(region) || BoardRules.Regions[region].Neighbors.Any(nazgulRegions.Contains);
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, SourceId = id, Title = SovereignCatalog.All[id].Title,
        TargetId = SovereignLifecycle.Region(state, SovereignLifecycle.Find(state, id)!),
        Detail = "Attempt corruption without spending an Eye."
    };
    public override void Choose(EffectContext context, string id) => CorruptionRuntime.BeginEvent(context.State, id, context.Random);
    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.HasUnknownResult = true;
}
