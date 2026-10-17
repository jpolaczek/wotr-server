using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record ShadowTravelPlan(IReadOnlyList<string> Ids, string Region, bool Garrison, IReadOnlyList<string>? Route = null);

public static class ShadowTravel
{
    public static bool CanEnter(MatchState state, string region) => BoardRules.Regions.ContainsKey(region) &&
        (BoardRules.Region(state, region).Settlement != "Stronghold" || state.Control.GetValueOrDefault(region) != Side.FreePeoples ||
            BoardRules.Besieged(state, region, Side.FreePeoples));

    public static bool MouthPath(MatchState state, IReadOnlyList<string> route, string origin, int range) =>
        TravelRules.ValidPath(route, origin, range) && (route.Count == 1 ||
            route.All(r => !BoardRules.Besieged(state, r, Side.Shadow)) && route.Skip(1).All(r => CanEnter(state, r)));

    public static IEnumerable<TravelOption> Options(MatchState state, bool nazgulOnly = false)
    {
        var destinations = BoardRules.Regions.Keys.Where(r => CanEnter(state, r)).ToList();
        foreach (var unit in state.Units.Where(u => u.Kind == UnitKind.Nazgul && BoardRules.Regions.ContainsKey(u.Location)))
            yield return Flight(unit.Id, unit.Location, destinations);
        foreach (var character in state.Characters.Where(c => c.Side == Side.Shadow && CharacterRules.OnBoard(c) && CharacterRules.Mobile(c) &&
            (!nazgulOnly || CharacterRules.IsWitchKing(c))))
        {
            if (CharacterRules.IsWitchKing(character))
                yield return Flight(character.Id, character.Location, destinations);
            else
                yield return new()
                {
                    Id = character.Id,
                    Origin = character.Location,
                    Range = character.Level,
                    Targets = MouthTargets(state, character.Location, character.Level),
                    BlockedRegions = BoardRules.Regions.Keys.Where(r => BoardRules.Besieged(state, r, Side.Shadow) || !CanEnter(state, r)).ToList()
                };
        }
    }

    private static TravelOption Flight(string id, string origin, List<string> targets) =>
        new() { Id = id, Origin = origin, Flight = true, Targets = targets };

    private static List<string> MouthTargets(MatchState state, string origin, int range)
    {
        var visited = new HashSet<string> { origin };
        var queue = new Queue<(string Region, int Distance)>();
        queue.Enqueue((origin, 0));
        while (queue.TryDequeue(out var current))
        {
            if (current.Distance == range || BoardRules.Besieged(state, current.Region, Side.Shadow))
                continue;
            foreach (var next in BoardRules.Regions[current.Region].Neighbors.Where(r => CanEnter(state, r) && !BoardRules.Besieged(state, r, Side.Shadow)))
                if (visited.Add(next))
                    queue.Enqueue((next, current.Distance + 1));
        }
        return visited.ToList();
    }

    public static List<ShadowTravelPlan>? Plan(MatchState state, IReadOnlyList<CompanionMoveChoice> moves, bool nazgulOnly = false)
    {
        if (moves.Count == 0)
            return nazgulOnly ? [] : null;
        var options = Options(state, nazgulOnly).ToDictionary(o => o.Id);
        var used = new HashSet<string>();
        var plans = new List<ShadowTravelPlan>();
        foreach (var move in moves)
        {
            if (move.Ids.Count == 0 || move.Ids.Any(id => string.IsNullOrEmpty(id) || !options.ContainsKey(id) || !used.Add(id)))
                return null;
            var group = move.Ids.Select(id => options[id]).ToArray();
            if (group.Any(o => o.Origin != group[0].Origin || o.Flight != group[0].Flight))
                return null;
            var flight = group[0].Flight;
            if (flight ? move.Route.Count != 2 || move.Route[0] != group[0].Origin || !CanEnter(state, move.Route[1]) :
                group.Length != 1 || !MouthPath(state, move.Route, group[0].Origin, group[0].Range))
                return null;
            plans.Add(new(move.Ids, move.Route[^1], BoardRules.Besieged(state, move.Route[^1], Side.Shadow), move.Route));
        }
        return plans;
    }

    public static void Apply(MatchState state, IEnumerable<ShadowTravelPlan> plans)
    {
        foreach (var plan in plans)
        {
            var origin = plan.Route?.FirstOrDefault() ?? state.Units.FirstOrDefault(u => plan.Ids.Contains(u.Id))?.Location ??
                state.Characters.First(c => plan.Ids.Contains(c.Id)).Location;
            var activity = MovementFacts.Event(state, ActionMovementKind.Travel, origin, plan.Region,
                state.Units.Where(u => plan.Ids.Contains(u.Id)), state.Characters.Where(c => plan.Ids.Contains(c.Id)), plan.Route);
            foreach (var unit in state.Units.Where(u => plan.Ids.Contains(u.Id)))
            {
                unit.Location = plan.Region;
                unit.Garrison = plan.Garrison;
            }
            foreach (var character in state.Characters.Where(c => plan.Ids.Contains(c.Id)))
            {
                character.Location = plan.Region;
                character.Garrison = plan.Garrison;
            }
            state.Events.Add(new() { Revision = state.Revision, Kind = "ShadowCharactersMoved", Activity = activity,
                Text = $"{string.Join(", ", plan.Ids)} moved to {plan.Region}." });
        }
    }
}
