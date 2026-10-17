using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record KingsCompanionJourneyStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        if (!context.State.Characters.Any(c => CharacterRules.OnBoard(c) && CharacterRules.IsCompanion(c)))
            return;
        FellowshipFlow.BeginJourney(context.State, context.Execution.SourceId, new()
        {
            AllowSeparation = false, AllowNoMovement = true, MaximumGroups = 0, AllowBesiegedDestination = true
        });
    }
}

public sealed record AwakenWithCompanionStep : EffectChoiceStep
{
    public override string Presentation => "Sovereign";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        var companions = state.Characters.Where(c => CharacterRules.OnBoard(c) && CharacterRules.IsCompanion(c))
            .Select(c => c.Location).ToHashSet();
        return state.Kings?.Sovereigns.Where(s => s.Status == SovereignStatus.Initial &&
            KingsCardRestrictions.CanAwaken(state, s.Id) && companions.Contains(SovereignLifecycle.Region(state, s)))
            .Select(s => s.Id).ToArray() ?? [];
    }
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, Title = SovereignCatalog.All[id].Title,
        TargetId = SovereignLifecycle.Region(state, SovereignLifecycle.Find(state, id)!),
        Detail = "Awaken here with the Companion; return this sovereign's Corruption tiles."
    };
    public override void Choose(EffectContext context, string id)
    {
        var sovereign = SovereignLifecycle.Find(context.State, id)!;
        SovereignLifecycle.Awaken(context.State, sovereign, SovereignLifecycle.Region(context.State, sovereign));
    }
    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        var id = ids.Single();
        preview.Placements.Add(new() { UnitId = id, RegionId = SovereignLifecycle.Region(state, SovereignLifecycle.Find(state, id)!) });
    }
}
