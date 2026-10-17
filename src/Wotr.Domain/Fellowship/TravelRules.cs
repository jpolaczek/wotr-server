using Wotr.Contracts;

namespace Wotr.Domain;

public static class TravelRules
{
    public static bool ValidPath(IReadOnlyList<string> path, string origin, int range) =>
        path.Count > 0 && path[0] == origin && path.Count - 1 <= range && path.All(BoardRules.Regions.ContainsKey) &&
        path.Zip(path.Skip(1)).All(edge => BoardRules.Regions[edge.First].Neighbors.Contains(edge.Second));

    public static bool Sanctuary(MatchState state, string regionId)
    {
        var region = BoardRules.Region(state, regionId);
        return region.OriginalOwner == Side.FreePeoples && region.Settlement is "City" or "Stronghold" &&
            state.Control.GetValueOrDefault(regionId) == Side.FreePeoples;
    }

    public static bool ShadowStronghold(MatchState state, string regionId) =>
        BoardRules.Region(state, regionId).Settlement == "Stronghold" && state.Control.GetValueOrDefault(regionId) == Side.Shadow;

    public static bool CompanionPath(MatchState state, IReadOnlyList<string> path, string origin, int range, bool allowBesiegedDestination = false)
    {
        if (!ValidPath(path, origin, range))
            return false;
        if (path.Count == 1)
            return true;
        var siegeRestricted = allowBesiegedDestination ? path.SkipLast(1) : path;
        if (siegeRestricted.Any(r => BoardRules.Besieged(state, r, Side.FreePeoples)))
            return false;
        return !path.Skip(1).SkipLast(1).Any(r => ShadowStronghold(state, r));
    }

    public static List<string> AdditionalHunts(MatchState state, IEnumerable<string> path) => path.Distinct()
        .Where(r => BoardRules.Region(state, r).OriginalOwner == Side.Shadow && ShadowStronghold(state, r)).ToList();

    // Small breadth-first traversal returns candidates, never every possible route or group.
    public static List<string> Reachable(MatchState state, string origin, int range, bool companions, bool allowBesiegedDestination = false)
    {
        var visited = new HashSet<string> { origin };
        var queue = new Queue<(string Region, int Distance)>();
        queue.Enqueue((origin, 0));
        while (queue.TryDequeue(out var current))
        {
            if (current.Distance >= range || companions && (BoardRules.Besieged(state, current.Region, Side.FreePeoples) ||
                current.Distance > 0 && ShadowStronghold(state, current.Region)))
                continue;
            foreach (var next in BoardRules.Regions[current.Region].Neighbors)
            {
                if (companions && !allowBesiegedDestination && BoardRules.Besieged(state, next, Side.FreePeoples) || !visited.Add(next))
                    continue;
                queue.Enqueue((next, current.Distance + 1));
            }
        }
        return visited.ToList();
    }
}
