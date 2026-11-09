using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record EnemyArmyStep : EffectChoiceStep
{
    public override string Presentation => "Region";

    private static IEnumerable<ArmyTarget> Armies(MatchState state, EffectExecution execution) =>
        DreadfulSpellsRules.Armies(state, Side.FreePeoples).Where(a => a.Region == execution.RegionId);

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        Armies(state, execution).Select(DreadfulSpellsRules.Key).ToArray();

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) =>
        DreadfulSpellsRules.Describe(Armies(state, execution).Single(a => DreadfulSpellsRules.Key(a) == id),
            $"3 dice · {EnemySovereignStep.Threshold(state, execution.TargetIds.Single())}+", "The Free Peoples choose this Army's losses.");

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.HasUnknownResult = true;

    public override void Apply(EffectContext context)
    {
        if (context.Execution.TargetIds.Count == 0)
            return;
        if (!Armies(context.State, context.Execution).Any())
            Roll(context, null);
        else
            base.Apply(context);
    }

    public override void Choose(EffectContext context, string id) =>
        Roll(context, Armies(context.State, context.Execution).Single(a => DreadfulSpellsRules.Key(a) == id));

    private static void Roll(EffectContext context, ArmyTarget? army)
    {
        var threshold = EnemySovereignStep.Threshold(context.State, context.Execution.TargetIds.Single());
        var rolls = Enumerable.Range(0, 3).Select(_ => context.Random.Next(6) + 1).ToArray();
        context.Record("WorkOfTheEnemy", $"Work of the Enemy in {context.Execution.RegionId}: [{string.Join(", ", rolls)}] · {threshold}+.", rolls: rolls, hitTarget: threshold, hits: rolls.Count(r => r >= threshold));
        if (army is not null)
            EventLosses.Begin(context, army, rolls.Count(r => r >= threshold), rolls, CasualtyCause.Event);
    }
}
