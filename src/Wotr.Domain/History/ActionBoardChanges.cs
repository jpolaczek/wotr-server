using Wotr.Contracts;

namespace Wotr.Domain;

// Snapshots at the accepted command boundary supplement the intermediate producer facts.
// This reads authoritative domain objects, never text, a PlayerView or a client projection.
internal static class ActionBoardChanges
{
    public static void Capture(MatchState before, MatchState after, ActionFacts facts)
    {
        Figures(before, after, facts);
        Politics(before, after, facts);
        Dice(before, after, facts);
        Fellowship(before, after, facts);
        ActionResourceChanges.Capture(before, after, facts);
        if (facts.Control.Count > 0 || before.Winner != after.Winner)
            facts.Outcome = HistoryFacts.Outcome(after);
        if (after.HistoryCapture?.Budget is { } budget)
            facts.Budget = budget;
        else if (after.Resolution.ActionSequence is { } sequence)
            facts.Budget = new() { Completed = sequence.CompletedSteps, Remaining = sequence.Remaining };
        else if (before.Resolution.ActionSequence is { } completed)
            facts.Budget = new() { Completed = completed.CompletedSteps, Remaining = 0 };
    }

    private static void Figures(MatchState before, MatchState after, ActionFacts facts)
    {
        var units = before.Units.ToDictionary(u => u.Id);
        foreach (var next in after.Units)
        {
            units.TryGetValue(next.Id, out var previous);
            if (previous is not null && previous.Location == next.Location && previous.Garrison == next.Garrison &&
                previous.Kind == next.Kind && previous.Nation == next.Nation)
                continue;
            facts.Figures.Add(new()
            {
                Before = previous is null ? null : MovementFacts.Unit(previous), After = MovementFacts.Unit(next),
                From = previous?.Location ?? "", To = next.Location
            });
        }
        var characters = before.Characters.ToDictionary(c => c.Id);
        var remainingIds = after.Units.Select(u => u.Id).Concat(after.Characters.Select(c => c.Id)).ToHashSet();
        foreach (var removed in before.Units.Where(u => !remainingIds.Contains(u.Id)))
            facts.Figures.Add(new() { Before = MovementFacts.Unit(removed), From = removed.Location, To = "removed" });
        foreach (var next in after.Characters)
        {
            characters.TryGetValue(next.Id, out var previous);
            if (!PublicCharacter(after, next) || previous is not null && Same(previous, next))
                continue;
            facts.Figures.Add(new()
            {
                Before = previous is not null && PublicCharacter(before, previous) ? MovementFacts.Character(previous) :
                    units.TryGetValue(next.Id, out var unit) ? MovementFacts.Unit(unit) : null,
                After = MovementFacts.Character(next), From = previous?.Location ?? units.GetValueOrDefault(next.Id)?.Location ?? "fellowship", To = next.Location
            });
        }
    }

    private static bool Same(CharacterState a, CharacterState b) => a.Location == b.Location && a.Garrison == b.Garrison &&
        a.DefinitionId == b.DefinitionId && a.Level == b.Level && a.Leadership == b.Leadership && a.NationIcon == b.NationIcon;

    private static bool PublicCharacter(MatchState state, CharacterState character) =>
        character.Side == Side.Shadow || state.Lords is null ||
        state.Lords.RevealedCompanionVersions.GetValueOrDefault(character.Id) == character.DefinitionId ||
        BoardRules.Regions.ContainsKey(character.Location);

    private static void Politics(MatchState before, MatchState after, ActionFacts facts)
    {
        var nations = before.Nations.ToDictionary(n => n.Id);
        foreach (var next in after.Nations)
        {
            nations.TryGetValue(next.Id, out var previous);
            if (previous?.Active == next.Active && previous?.StepsToWar == next.StepsToWar)
                continue;
            facts.Nations.Add(new()
            {
                Id = next.Id, WasActive = previous?.Active, PreviousStepsToWar = previous?.StepsToWar,
                Active = next.Active, StepsToWar = next.StepsToWar
            });
        }
        foreach (var next in after.Control)
            if (before.Control.GetValueOrDefault(next.Key) != next.Value)
                facts.Control.Add(new() { RegionId = next.Key, Before = before.Control.GetValueOrDefault(next.Key), After = next.Value });
    }

    private static void Dice(MatchState before, MatchState after, ActionFacts facts)
    {
        var dice = before.Dice.ToDictionary(d => d.Id);
        foreach (var next in after.Dice)
        {
            dice.TryGetValue(next.Id, out var previous);
            if (previous?.Location == next.Location && HistoryFacts.Face(previous) == HistoryFacts.Face(next) && previous?.HuntOrigin == next.HuntOrigin)
                continue;
            facts.Dice.Add(new()
            {
                Id = next.Id, Owner = next.Owner, Source = next.Source, Before = HistoryFacts.Face(previous),
                After = HistoryFacts.Face(next), From = previous?.Location, To = next.Location, HuntOrigin = next.HuntOrigin
            });
        }
    }

    private static void Fellowship(MatchState before, MatchState after, ActionFacts facts)
    {
        var a = before.Fellowship;
        var b = after.Fellowship;
        if (a.RegionId == b.RegionId && a.Progress == b.Progress && a.Corruption == b.Corruption &&
            a.Hidden == b.Hidden && a.Guide == b.Guide && a.MordorStep == b.MordorStep)
            return;
        facts.FellowshipBefore = HistoryFacts.Fellowship(before);
        facts.FellowshipAfter = HistoryFacts.Fellowship(after);
    }
}
