using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain.BotProtocol;

// BOT-225: the Fellowship's and the Hunt's questions: HUNT_ALLOCATION, FELLOWSHIP_PHASE, CHOOSE_GUIDE, HUNT_CASUALTY and
// REVEAL_FELLOWSHIP (SPEC §5).
public static class ProtocolHuntQuestions
{
    private static readonly IReadOnlyDictionary<string, RegionDefinition> Board =
        BoardCatalog.LoadBase().Regions.ToDictionary(r => r.Id, StringComparer.Ordinal);

    // How many Action dice go to the Hunt Box: the dice are alike before the roll, so a count names the answer.
    public static IEnumerable<ProtocolPath> Allocation(ProtocolContext context)
    {
        if (context.Option(CommandKind.AllocateHunt) is not { } option)
            yield break;
        for (var count = Math.Max(0, option.Minimum); count <= Math.Min(option.Maximum, option.EligibleIds.Count); count++)
        {
            var command = context.Command(CommandKind.AllocateHunt, new CommandPayload { Ids = option.EligibleIds.Take(count).ToList() });
            foreach (var path in context.One("HUNT_ALLOCATION", ProtocolAtom.Of("hunt_dice", ("count", count)),
                         count == 1 ? "1 die to the Hunt Box" : $"{count} dice to the Hunt Box", command))
                yield return path;
        }
    }

    public static IEnumerable<ProtocolPath> Guide(ProtocolContext context) =>
        Guides(context, "CHOOSE_GUIDE");

    // Hunt damage: the Ring-bearers take Corruption, or the Guide or a random Companion is eliminated.
    public static IEnumerable<ProtocolPath> Damage(ProtocolContext context)
    {
        if (context.Option(CommandKind.ResolveHuntDamage) is not { } option)
            return Enumerable.Empty<ProtocolPath>();
        var damage = new JsonObject { ["damage"] = context.View.Hunt?.Damage ?? 0 };
        return option.HuntResponses.SelectMany(response => context.One("HUNT_CASUALTY",
            ProtocolAtom.Of(response switch { HuntResponse.Ring => "corruption", HuntResponse.Guide => "guide", _ => "random" }),
            response switch
            {
                HuntResponse.Ring => "The Ring-bearers take the damage as Corruption",
                HuntResponse.Guide => "Eliminate the Guide",
                _ => "Eliminate a random Companion"
            },
            context.Command(CommandKind.ResolveHuntDamage, new CommandPayload { HuntResponse = response }), damage));
    }

    // The revealed Fellowship moves to a region within its Progress: the route the engine accepts first.
    public static IEnumerable<ProtocolPath> Reveal(ProtocolContext context)
    {
        if (context.Option(CommandKind.RevealFellowship) is not { } option)
            yield break;
        var start = context.View.Fellowship.RegionId;
        var paths = Routes(start, option.Targets.ToHashSet(StringComparer.Ordinal));
        foreach (var target in option.Targets)
        {
            var routes = paths.GetValueOrDefault(target) ?? new List<List<string>>();
            if (target == start)
                routes.Insert(0, new List<string> { start });
            var legal = routes.Select(route => context.Command(CommandKind.RevealFellowship, new CommandPayload { Route = route }))
                .FirstOrDefault(context.Legal);
            if (legal is not null)
                yield return new ProtocolPath(new ProtocolStep("REVEAL_FELLOWSHIP",
                    ProtocolAtom.Of("reveal", ("region", context.Region(target))), "Reveal the Fellowship in " + target), legal);
        }
    }

    // The Fellowship phase: declare, enter Mordor, change the Guide, or done.
    public static IEnumerable<ProtocolPath> FellowshipPhase(ProtocolContext context)
    {
        foreach (var path in context.One("FELLOWSHIP_PHASE", ProtocolAtom.Of("done"), "Done", context.Command(CommandKind.ContinueFellowship)))
            yield return path;
        if (context.Option(CommandKind.DeclareFellowship) is { } declare)
        {
            var start = context.View.Fellowship.RegionId;
            var range = declare.Travel.FirstOrDefault()?.Range ?? context.View.Fellowship.Progress;
            var targets = declare.Targets.Count > 0 ? declare.Targets : new List<string> { start };
            foreach (var region in targets)
            {
                var command = ProtocolRoutes.Between(start, region, range)
                    .Select(route => context.Command(CommandKind.DeclareFellowship, new CommandPayload { Route = route }))
                    .FirstOrDefault(context.Legal);
                if (command is not null)
                    yield return new ProtocolPath(new ProtocolStep("FELLOWSHIP_PHASE", ProtocolAtom.Of("declare", ("region", context.Region(region))),
                        "Declare the Fellowship in " + region), command);
            }
        }
        if (context.Option(CommandKind.EnterMordor) is not null)
            foreach (var path in context.One("FELLOWSHIP_PHASE", ProtocolAtom.Of("enter_mordor"), "Enter Mordor",
                         context.Command(CommandKind.EnterMordor)))
                yield return path;
        foreach (var path in Guides(context, "FELLOWSHIP_PHASE"))
            yield return path;
    }

    private static IEnumerable<ProtocolPath> Guides(ProtocolContext context, string question)
    {
        if (context.Option(CommandKind.SelectGuide) is not { } option)
            yield break;
        foreach (var guide in option.EligibleIds.Where(g => g != context.View.Fellowship.Guide || question == "CHOOSE_GUIDE"))
            foreach (var path in context.One(question, ProtocolAtom.Of("guide", ("character", context.Character(guide))),
                         CharacterCatalog.Title(guide) + " becomes the Guide",
                         context.Command(CommandKind.SelectGuide, new CommandPayload { GuideId = guide })))
                yield return path;
    }

    // Every shortest route from the start to each target (a few per target), on the base map.
    private static Dictionary<string, List<List<string>>> Routes(string start, HashSet<string> targets)
    {
        var routes = new Dictionary<string, List<List<string>>>(StringComparer.Ordinal);
        if (!Board.ContainsKey(start))
            return routes;
        var frontier = new List<List<string>> { new() { start } };
        var depth = new Dictionary<string, int>(StringComparer.Ordinal) { [start] = 0 };
        for (var step = 1; step <= 12 && frontier.Count > 0; step++)
        {
            var next = new List<List<string>>();
            foreach (var route in frontier)
                foreach (var neighbour in Board[route[^1]].Neighbors)
                {
                    if (depth.TryGetValue(neighbour, out var seen) && seen < step)
                        continue;
                    depth[neighbour] = step;
                    var longer = new List<string>(route) { neighbour };
                    if (targets.Contains(neighbour))
                    {
                        var list = routes.TryGetValue(neighbour, out var found) ? found : routes[neighbour] = new();
                        if (list.Count < 4)
                            list.Add(longer);
                    }
                    if (next.Count < 4000)
                        next.Add(longer);
                }
            frontier = next;
        }
        return routes;
    }
}
