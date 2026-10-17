using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record CompanionJourneyStep(int? FixedLevel = null, int Bonus = 0) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        if (context.State.Fellowship.Companions.Count == 0 && !context.State.Characters.Any(c =>
            c.Side == Side.FreePeoples && c.Level > 0 && CharacterRules.OnBoard(c)))
            return;
        FellowshipFlow.BeginJourney(context.State, context.Execution.SourceId, new()
        {
            FixedLevel = FixedLevel,
            Bonus = Bonus,
            AllowBesiegedDestination = true
        });
    }
}

public sealed record ThereAndBackPoliticsStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var state = context.State;
        if (!state.Characters.Any(c => FreePeoplesCharacterCards01.AxeCompanions.Any(id => CharacterRules.MatchesCardName(c, id)) &&
            c.Location is "dale" or "erebor" or "woodland-realm" && state.Control.GetValueOrDefault(c.Location) == Side.FreePeoples))
            return;
        PoliticalRules.Activate(state, "dwarves");
        PoliticalRules.Activate(state, "north");
        foreach (var nation in new[] { "dwarves", "elves", "north" })
            PoliticalRules.Advance(state, nation, PoliticalAdvanceCause.Event);
    }
}
