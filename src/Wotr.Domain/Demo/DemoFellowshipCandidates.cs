using Wotr.Contracts;

namespace Wotr.Domain;

// DEMO-02: the Free Peoples CPU and its Fellowship: it enters Mordor when it may, otherwise ends the phase; a new
// guide is the first one allowed; Hunt damage goes to the Ring; a reveal or a forced separation takes any legal route.
internal static class DemoFellowshipCandidates
{
    public static IEnumerable<(CommandKind, CommandPayload)> Phase(DemoContext context) =>
        DemoCandidates.Plain(CommandKind.EnterMordor, CommandKind.ContinueFellowship).Concat(Guide(context));

    public static IEnumerable<(CommandKind, CommandPayload)> Guide(DemoContext context) =>
        from option in context.Options(CommandKind.SelectGuide)
        from guide in option.EligibleIds
        select (CommandKind.SelectGuide, new CommandPayload { GuideId = guide });

    public static IEnumerable<(CommandKind, CommandPayload)> Reveal(DemoContext context) =>
        from option in context.Options(CommandKind.RevealFellowship)
        from travel in option.Travel
        from target in context.Choice.Shuffled(option.Targets)
        let route = DemoRoutes.Path(travel.Origin, target, travel.Range)
        where route is not null
        select (CommandKind.RevealFellowship, new CommandPayload { Route = route });

    // A required group leaves together; otherwise one companion at a time, or nobody when the journey allows it.
    public static IEnumerable<(CommandKind, CommandPayload)> Separation(DemoContext context)
    {
        foreach (var option in context.Options(CommandKind.SeparateCompanions))
            foreach (var travel in option.Travel)
                foreach (var target in context.Choice.Shuffled(travel.Targets))
                    if (DemoRoutes.Path(travel.Origin, target, Math.Max(travel.Range, 1)) is { } route)
                        yield return (CommandKind.SeparateCompanions, new CommandPayload
                        {
                            Ids = travel.RequiredGroup.Count > 0 ? [.. travel.RequiredGroup] : [travel.Id], Route = route
                        });
        foreach (var option in context.Options(CommandKind.MoveCompanions))
            if (option.AllowEmptySelection)
                yield return (CommandKind.MoveCompanions, new CommandPayload());
    }

    public static IEnumerable<(CommandKind, CommandPayload)> HuntDamage() =>
        new[] { HuntResponse.Ring, HuntResponse.Guide, HuntResponse.RandomCompanion }
            .Select(response => (CommandKind.ResolveHuntDamage, new CommandPayload { HuntResponse = response }));
}

// DEMO-02: the Shadow CPU and the Hunt: one or two dice in the Hunt Box within the limits, and every reroll it may.
internal static class DemoHuntCandidates
{
    public static IEnumerable<(CommandKind, CommandPayload)> Allocate(DemoContext context)
    {
        foreach (var option in context.Options(CommandKind.AllocateHunt))
        {
            var count = Math.Clamp(context.Choice.Next(1, 2), option.Minimum, option.Maximum);
            yield return (CommandKind.AllocateHunt, new CommandPayload { Ids = option.EligibleIds.Take(count).ToList() });
            yield return (CommandKind.AllocateHunt, new CommandPayload { Ids = option.EligibleIds.Take(option.Minimum).ToList() });
        }
    }

    public static IEnumerable<(CommandKind, CommandPayload)> Reroll(DemoContext context) =>
        context.Options(CommandKind.RerollHunt).Select(o => (CommandKind.RerollHunt, new CommandPayload { Ids = o.EligibleIds.Take(o.Maximum).ToList() }))
            .Concat(DemoCandidates.Plain(CommandKind.RerollHunt));
}
