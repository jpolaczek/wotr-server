using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record OutsideCompanionJourneyStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        if (!context.State.Characters.Any(c => c.Side == Side.FreePeoples && c.Level > 0 && CharacterRules.OnBoard(c)))
            return;
        FellowshipFlow.BeginJourney(context.State, context.Execution.SourceId, new()
        {
            AllowSeparation = false,
            MaximumGroups = 0,
            AllowNoMovement = true
        });
    }
}

public sealed record CompanionNationWarStep(string Nation, IReadOnlyList<string> Regions) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var state = context.State;
        if (!state.Characters.Any(c => Regions.Contains(c.Location) &&
            CharacterCatalog.All[c.DefinitionId].Role == "Companion" && state.Control.GetValueOrDefault(c.Location) == Side.FreePeoples))
            return;
        PoliticalRules.Activate(state, Nation);
        var nation = state.Nations.Single(n => n.Id == Nation);
        while (PoliticalRules.CanAdvance(nation))
            PoliticalRules.Advance(state, Nation, PoliticalAdvanceCause.Event);
    }
}
