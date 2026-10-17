using Wotr.Contracts;

namespace Wotr.Domain;

// DEMO-02: plain selections: the cards over the hand limit, the figures over the stacking limit, and a card effect's
// choice: as many as allowed, as few as allowed, then each candidate alone.
internal static class DemoChoiceCandidates
{
    public static IEnumerable<(CommandKind, CommandPayload)> Discard(DemoContext context) =>
        from option in context.Options(CommandKind.DiscardCards)
        let cards = context.Choice.Shuffled(option.EligibleIds)
        from count in new[] { option.Maximum, option.Minimum }
        select (CommandKind.DiscardCards, new CommandPayload { Ids = cards.Take(count).ToList() });

    public static IEnumerable<(CommandKind, CommandPayload)> Excess(DemoContext context) =>
        context.Options(CommandKind.RemoveExcess)
            .Select(o => (CommandKind.RemoveExcess, new CommandPayload { Ids = o.EligibleIds.Take(o.Minimum).ToList() }));

    public static IEnumerable<(CommandKind, CommandPayload)> Effect(DemoContext context)
    {
        foreach (var option in context.Options(CommandKind.ChooseEffect))
        {
            if (option.EventRecruitment is { } recruitment)
                foreach (var placements in DemoRecruitment.Plans(context, option, recruitment))
                    yield return (CommandKind.ChooseEffect, new CommandPayload { Placements = placements });
            foreach (var ids in Grouped(context, option))
                yield return (CommandKind.ChooseEffect, new CommandPayload { Ids = ids });
            var candidates = context.Choice.Shuffled(option.EligibleIds);
            foreach (var count in new[] { option.Maximum, option.Minimum }.Distinct())
                yield return (CommandKind.ChooseEffect, new CommandPayload { Ids = candidates.Take(count).ToList() });
            foreach (var id in candidates)
                yield return (CommandKind.ChooseEffect, new CommandPayload { Ids = [id] });
        }
    }

    // Choices in selection groups (for example one figure per region): groups in a random order, each up to its own
    // limit, until the choice is complete.
    private static IEnumerable<List<string>> Grouped(DemoContext context, ActionOption option)
    {
        const int attempts = 8;
        var groups = option.EffectChoices.Where(c => c.SelectionGroup.Length > 0).GroupBy(c => c.SelectionGroup).ToList();
        if (groups.Count == 0)
            yield break;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            var ids = new List<string>();
            foreach (var group in context.Choice.Shuffled(groups))
                ids.AddRange(context.Choice.Shuffled(group).Take(Math.Min(group.First().GroupMaximum, option.Maximum - ids.Count)).Select(c => c.Id));
            yield return ids;
        }
    }
}

// DEMO-02: a card's recruitment plan: exactly the required number of figures, each to a region whose limit admits
// its nation and kind, within the limit per region and the number of regions. Random orders of the limits are tried.
internal static class DemoRecruitment
{
    private const int Attempts = 24;

    public static IEnumerable<List<UnitPlacementChoice>> Plans(DemoContext context, ActionOption option, EventRecruitmentOption recruitment)
    {
        var units = option.EligibleIds.Select(id => context.State.Units.Single(u => u.Id == id)).ToList();
        for (var attempt = 0; attempt < Attempts; attempt++)
            if (Plan(context.Choice.Shuffled(recruitment.Limits), units, recruitment, option.Minimum) is { } plan)
                yield return plan;
    }

    private static List<UnitPlacementChoice>? Plan(IReadOnlyList<RecruitmentLimit> limits, IReadOnlyList<UnitState> units,
        EventRecruitmentOption recruitment, int count)
    {
        var plan = new List<UnitPlacementChoice>();
        var regions = new HashSet<string>();
        var nations = new HashSet<string>();
        foreach (var limit in limits)
        {
            if (recruitment.MaximumRegions > 0 && !regions.Contains(limit.RegionId) && regions.Count == recruitment.MaximumRegions)
                continue;
            var fitting = units.Where(u => u.Nation == limit.Nation && limit.Kinds.Contains(u.Kind) && plan.All(p => p.UnitId != u.Id))
                .Where(u => recruitment.DistinctNations == 0 || !nations.Contains(u.Nation))
                .Take(Math.Min(recruitment.DistinctNations > 0 ? 1 : limit.Maximum, count - plan.Count)).ToList();
            foreach (var unit in fitting)
            {
                plan.Add(new() { UnitId = unit.Id, RegionId = limit.RegionId });
                nations.Add(unit.Nation);
                regions.Add(limit.RegionId);
            }
            if (plan.Count == count)
                return plan;
        }
        return null;
    }
}

// DEMO-02: a shortest route over the map's neighbours, from the origin to the target, within a range of steps.
internal static class DemoRoutes
{
    public static List<string>? Path(string origin, string target, int range)
    {
        if (!BoardRules.Regions.ContainsKey(origin))
            return null;
        var previous = new Dictionary<string, string> { [origin] = origin };
        var frontier = new Queue<string>([origin]);
        while (frontier.Count > 0)
        {
            var region = frontier.Dequeue();
            if (region == target)
                return Route(previous, origin, target, range);
            foreach (var next in BoardRules.Regions[region].Neighbors.Where(n => !previous.ContainsKey(n)))
            {
                previous[next] = region;
                frontier.Enqueue(next);
            }
        }
        return null;
    }

    private static List<string>? Route(Dictionary<string, string> previous, string origin, string target, int range)
    {
        var route = new List<string> { target };
        while (route[^1] != origin)
            route.Add(previous[route[^1]]);
        route.Reverse();
        return route.Count - 1 <= range ? route : null;
    }
}
