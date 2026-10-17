using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record CompanionMovePlan(IReadOnlyList<CharacterState> Characters, string RegionId, bool Garrison, bool Travelled,
    IReadOnlyList<string>? Route = null);

public static class CompanionRules
{
    public static IReadOnlyList<CharacterState> Inside(MatchState state) => state.Fellowship.Companions.Select(id => CharacterCatalog.Companion(state, id)).ToArray();

    public static int Level(MatchState state, CharacterState character) => character.DefinitionId == LordsCharacters.Smeagol
        ? SmeagolRules.Level(state) : character.Level;

    public static List<string> Guides(MatchState state)
    {
        if (SmeagolRules.Present(state))
            return [LordsCharacters.Smeagol];
        var inside = Inside(state);
        if (inside.Count == 0)
            return ["base.character.gollum"];
        var level = inside.Max(c => c.Level);
        return inside.Where(c => c.Level == level).Select(c => c.Id).ToList();
    }

    public static CompanionMovePlan? Separate(MatchState state, IReadOnlyList<string> ids, IReadOnlyList<string> route, bool forced = false, int rangeBonus = 0)
    {
        if (forced && FellowshipFlow.Current(state)?.Fellowship is { FixedCompanions.Count: > 0 } placement)
        {
            if (!DraftSelection.IsSubset(ids, placement.FixedCompanions, placement.FixedCompanions.Count, placement.FixedCompanions.Count) ||
                !TravelRules.CompanionPath(state, route, placement.SeparationOrigin, placement.SeparationRange))
                return null;
            return new(ids.Select(id => CharacterCatalog.Companion(state, id)).ToArray(), route[^1],
                BoardRules.Besieged(state, placement.SeparationOrigin, Side.FreePeoples), route.Count > 1, route);
        }
        if (!DraftSelection.IsSubset(ids, state.Fellowship.Companions, 1, state.Fellowship.Companions.Count))
            return null;
        var group = ids.Select(id => CharacterCatalog.Companion(state, id)).ToArray();
        if (state.Fellowship.MordorStep.HasValue)
            return forced && route.Count == 0 ? new(group, "casualties", false, false) : null;
        var range = Range(state, group, true) + rangeBonus;
        var origin = state.Fellowship.RegionId;
        if (!TravelRules.CompanionPath(state, route, origin, range, CanEnterSiege(state)))
            return null;
        return new(group, route[^1], BoardRules.Besieged(state, route[^1], Side.FreePeoples), route.Count > 1, route);
    }

    public static int SeparationBonus(MatchState state) =>
        FellowshipFlow.Current(state)?.Fellowship?.SeparationBonus ?? 0;

    public static CompanionJourney? Journey(MatchState state) => FellowshipFlow.Current(state)?.Fellowship?.Journey;
    public static bool CanEnterSiege(MatchState state) => Journey(state)?.AllowBesiegedDestination == true;

    public static int Range(MatchState state, IReadOnlyList<CharacterState> group, bool separation)
    {
        var journey = Journey(state);
        var level = journey?.FixedLevel ?? (separation ? group.Max(c => c.DefinitionId == LordsCharacters.Smeagol
            ? Level(state, c) : CharacterRules.MovementLevel(c)) : CharacterRules.CompanionRange(group));
        return level + (journey?.Bonus ?? 0) + (separation ? state.Fellowship.Progress + SeparationBonus(state) : 0);
    }

    public static List<CompanionMovePlan>? Move(MatchState state, IReadOnlyList<CompanionMoveChoice> moves, int rangeBonus = 0)
    {
        var journey = Journey(state);
        if (moves.Count == 0 && journey?.AllowNoMovement != true || journey is { MaximumGroups: > 0 } && moves.Count > journey.MaximumGroups)
            return null;
        var used = new HashSet<string>();
        var result = new List<CompanionMovePlan>();
        foreach (var move in moves)
        {
            if (move.Ids.Count == 0 || move.Ids.Any(id => !used.Add(id)))
                return null;
            var group = state.Characters.Where(c => move.Ids.Contains(c.Id) && c.Side == Side.FreePeoples &&
                BoardRules.Regions.ContainsKey(c.Location) && c.Level > 0).ToArray();
            if (group.Length != move.Ids.Count || group.Any(c => c.Location != group[0].Location || c.Garrison != group[0].Garrison) ||
                !TravelRules.CompanionPath(state, move.Route, group[0].Location, Range(state, group, false) + rangeBonus, CanEnterSiege(state)))
                return null;
            var garrison = move.Route.Count == 1 ? group[0].Garrison : BoardRules.Besieged(state, move.Route[^1], Side.FreePeoples);
            result.Add(new(group, move.Route[^1], garrison, move.Route.Count > 1, move.Route));
        }
        return result;
    }

    public static void Apply(MatchState state, CompanionMovePlan plan, bool separation)
    {
        var origin = plan.Route?.FirstOrDefault() ?? (separation ? state.Fellowship.RegionId : plan.Characters.FirstOrDefault()?.Location ?? "");
        var activity = MovementFacts.Event(state, ActionMovementKind.Travel, origin, plan.RegionId, [], plan.Characters, plan.Route);
        if (separation)
            state.Fellowship.Companions.RemoveAll(id => plan.Characters.Any(c => c.Id == id));
        foreach (var character in plan.Characters)
        {
            LordsCompanionSetup.Reveal(state, character);
            if (separation)
            {
                if (state.Characters.All(c => c.Id != character.Id))
                    state.Characters.Add(character);
            }
            if (character.DefinitionId == LordsCharacters.Smeagol)
            {
                SmeagolRules.Discard(state);
                continue;
            }
            character.Location = plan.RegionId;
            character.Garrison = plan.Garrison;
            if (BoardRules.Regions.ContainsKey(plan.RegionId))
                PoliticalRules.CompanionArrived(state, plan.RegionId, character.NationIcon);
        }
        SmeagolRules.Reconcile(state);
        state.Events.Add(new()
        {
            Revision = state.Revision,
            Kind = separation ? "CompanionsSeparated" : "CompanionsMoved",
            Activity = activity,
            Text = $"{string.Join(", ", plan.Characters.Select(c => c.Id))}: {plan.RegionId}."
        });
    }
}
