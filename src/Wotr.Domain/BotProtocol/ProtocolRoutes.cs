using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain.BotProtocol;

// BOT-225: the routes a figure may take on the base map, for commands whose options name only the destination: the
// shortest paths from the origin (included) to the target within a range, never entering a blocked region and never
// passing on from a stop region. The engine judges the route; a few alternatives are tried in order.
public static class ProtocolRoutes
{
    private static readonly IReadOnlyDictionary<string, RegionDefinition> Board =
        BoardCatalog.LoadBase().Regions.ToDictionary(r => r.Id, StringComparer.Ordinal);

    public static IReadOnlyList<List<string>> Between(string origin, string target, int range,
        IReadOnlyCollection<string>? stops = null, IReadOnlyCollection<string>? blocked = null, int limit = 4)
    {
        var found = new List<List<string>>();
        if (origin == target)
        {
            found.Add(new List<string> { origin });
            return found;
        }
        if (!Board.ContainsKey(origin) || range <= 0)
            return found;
        var frontier = new List<List<string>> { new() { origin } };
        var depth = new Dictionary<string, int>(StringComparer.Ordinal) { [origin] = 0 };
        for (var step = 1; step <= range && frontier.Count > 0 && found.Count == 0; step++)
        {
            var next = new List<List<string>>();
            foreach (var route in frontier)
            {
                var last = route[^1];
                if (route.Count > 1 && stops?.Contains(last) == true)
                    continue;
                foreach (var neighbour in Board[last].Neighbors)
                {
                    if (blocked?.Contains(neighbour) == true || depth.TryGetValue(neighbour, out var seen) && seen < step)
                        continue;
                    depth[neighbour] = step;
                    var longer = new List<string>(route) { neighbour };
                    if (neighbour == target)
                    {
                        if (found.Count < limit)
                            found.Add(longer);
                    }
                    else if (next.Count < 2000)
                        next.Add(longer);
                }
            }
            frontier = next;
        }
        return found;
    }

    public static IReadOnlyList<List<string>> For(TravelOption travel, string target, int? range = null) =>
        travel.Flight ? new List<List<string>> { new() { travel.Origin, target } }
            : Between(travel.Origin, target, range ?? travel.Range, travel.StopRegions, travel.BlockedRegions);
}
