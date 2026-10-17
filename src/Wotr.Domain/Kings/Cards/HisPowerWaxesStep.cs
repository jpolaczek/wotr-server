using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record HisPowerWaxesCondition : ICombatCondition
{
    public static IEnumerable<SovereignState> Sovereigns(MatchState state, BattleState battle) =>
        state.Kings?.Sovereigns.Where(s => s.Status != SovereignStatus.Removed && SovereignLifecycle.Region(state, s) == battle.Region) ?? [];
    public bool CanPlay(MatchState state, BattleState battle, Side actor) => Sovereigns(state, battle).Any();
}

public sealed record HisPowerWaxesStep : EffectChoiceStep
{
    public override string Presentation => "Sovereign";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        HisPowerWaxesCondition.Sovereigns(state, BattleRuntime.Active(state)!)
            .GroupBy(s => s.Status == SovereignStatus.Corrupted).Select(g => g.First().Id).ToArray();
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, SourceId = id, TargetId = SovereignLifecycle.Region(state, SovereignLifecycle.Find(state, id)!),
        Title = SovereignCatalog.All[id].Title,
        Detail = SovereignLifecycle.Find(state, id)!.Status == SovereignStatus.Corrupted
            ? "+1 to Shadow Combat and Leader re-roll." : "+1 to both Armies' Combat and Leader re-roll."
    };
    public override void Choose(EffectContext context, string id)
    {
        if (SovereignLifecycle.Find(context.State, id)!.Status == SovereignStatus.Corrupted)
            new CombatModifierStep(1, 1).Apply(context);
        else
            new BothArmiesModifierStep(1).Apply(context);
        context.Record("HisPowerWaxes", $"His Power Waxes: {SovereignCatalog.All[id].Title}.");
    }
}
