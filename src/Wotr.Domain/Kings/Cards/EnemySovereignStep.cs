using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record EnemySovereignStep : EffectChoiceStep
{
    public override string Presentation => "Sovereign";

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        state.Kings!.Sovereigns.Where(s => s.Status != SovereignStatus.Removed)
            .GroupBy(s => (Region: SovereignLifecycle.Region(state, s), Corrupted: s.Status == SovereignStatus.Corrupted))
            .Where(g => BoardRules.Regions.ContainsKey(g.Key.Region))
            .Select(g => g.First().Id).ToArray();

    public static int Threshold(MatchState state, string id) =>
        SovereignLifecycle.Find(state, id)!.Status == SovereignStatus.Corrupted ? 4 : 5;

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        SourceId = id,
        TargetId = SovereignLifecycle.Region(state, SovereignLifecycle.Find(state, id)!),
        Title = $"{SovereignCatalog.All[id].Title} · {Threshold(state, id)}+",
        Detail = $"3 dice · {Threshold(state, id)}+ · One Free Peoples Army in this region."
    };

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.HasUnknownResult = true;

    public override void Choose(EffectContext context, string id)
    {
        context.Execution.RegionId = SovereignLifecycle.Region(context.State, SovereignLifecycle.Find(context.State, id)!);
        context.Execution.TargetIds = [id];
    }
}
