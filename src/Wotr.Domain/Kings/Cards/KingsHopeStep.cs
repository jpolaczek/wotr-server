using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record HopeCondition : ICombatCondition
{
    public bool CanPlay(MatchState state, BattleState battle, Side actor) => HopeAwakeningStep.Sovereigns(state, battle, actor).Any();
}

public sealed record HopeAwakeningStep : EffectChoiceStep
{
    public override string Presentation => "Sovereign";
    public static IEnumerable<SovereignState> Sovereigns(MatchState state, BattleState battle, Side owner) =>
        NonCorruptedSovereigns.OnBoard(state).Where(s => battle.Force(owner).Participants.Contains(s.Id));
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        Sovereigns(state, BattleRuntime.Active(state)!, execution.Owner).Select(s => s.Id).ToArray();
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, SourceId = id, Title = SovereignCatalog.All[id].Title,
        Detail = "Awaken here if permitted. Add +1 to your Leader re-roll."
    };
    public override void Choose(EffectContext context, string id)
    {
        var sovereign = SovereignLifecycle.Find(context.State, id)!;
        if (sovereign.Status == SovereignStatus.Initial && KingsCardRestrictions.CanAwaken(context.State, id))
            SovereignLifecycle.Awaken(context.State, sovereign, SovereignLifecycle.Region(context.State, sovereign));
    }
}
