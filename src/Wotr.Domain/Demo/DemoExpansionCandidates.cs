using Wotr.Contracts;

namespace Wotr.Domain;

// EXP-03: the demo CPU's answers to the expansions' own decisions, simple and legal like the rest of DEMO-02: the
// Companion versions and the starting Guide, the Council's homelands and token, Dismay and a corruption tile. Each
// answer comes from the CPU's seeded choice and is followed by a plainer one; the engine judges every candidate.
// The special die, the Ruler discard and Vilya need no more than the plain fallback over the options' dice.
internal static class DemoExpansionCandidates
{
    // A version of each Companion (Gandalf's is required) and a starting Guide, at random; then all base versions.
    public static IEnumerable<(CommandKind, CommandPayload)> Versions(DemoContext context)
    {
        foreach (var option in context.Options(CommandKind.ChooseCompanionVersions))
        {
            var guides = context.Choice.Shuffled(option.Targets);
            var picked = option.CompanionVersions
                .Select(v => context.Choice.Next(0, 1) == 0 ? v.BaseId : v.AlternateId)
                .ToList();
            var plain = option.CompanionVersions.Select(v => v.BaseId).ToList();
            foreach (var guide in guides)
            {
                yield return (CommandKind.ChooseCompanionVersions, new CommandPayload { Ids = picked, GuideId = guide });
                yield return (CommandKind.ChooseCompanionVersions, new CommandPayload { Ids = plain, GuideId = guide });
            }
        }
    }

    // Now and then one Companion starts in a homeland of its own (the Shadow then gains a Council token); otherwise
    // every Companion stays with the Fellowship.
    public static IEnumerable<(CommandKind, CommandPayload)> Homelands(DemoContext context)
    {
        foreach (var option in context.Options(CommandKind.ChooseCouncilHomelands))
        {
            var starters = option.CharacterActions.Where(c => c.Targets.Count > 0).ToList();
            if (starters.Count > 0 && context.Choice.Next(0, 2) == 0)
            {
                var starter = context.Choice.Shuffled(starters)[0];
                var region = context.Choice.Shuffled(starter.Targets)[0];
                yield return (CommandKind.ChooseCouncilHomelands, new CommandPayload
                {
                    Placements = [new() { UnitId = starter.CharacterId, RegionId = region }]
                });
            }
            yield return (CommandKind.ChooseCouncilHomelands, new CommandPayload());
        }
    }

    public static IEnumerable<(CommandKind, CommandPayload)> CouncilToken(DemoContext context) =>
        from option in context.Options(CommandKind.ChooseCouncilToken)
        from token in context.Choice.Shuffled(option.EligibleIds)
        select (CommandKind.ChooseCouncilToken, new CommandPayload { Ids = [token] });

    // Half the time a Dismay token re-rolls one of the offered Ruler dice; otherwise the tokens are kept.
    public static IEnumerable<(CommandKind, CommandPayload)> Dismay(DemoContext context)
    {
        if (context.Choice.Next(0, 1) == 0)
            foreach (var option in context.Options(CommandKind.UseDismay))
                foreach (var token in option.EligibleIds)
                    foreach (var die in context.Choice.Shuffled(option.EligibleDice))
                        yield return (CommandKind.UseDismay, new CommandPayload { Ids = [token], DieId = die });
        yield return (CommandKind.DeclineDismay, new CommandPayload());
    }

    // A corruption attempt draws again while it may, now and then, and otherwise keeps one of its drawn tiles.
    public static IEnumerable<(CommandKind, CommandPayload)> CorruptionTile(DemoContext context)
    {
        var draw = context.Options(CommandKind.DrawCorruptionTile).Any();
        if (draw && context.Choice.Next(0, 1) == 0)
            yield return (CommandKind.DrawCorruptionTile, new CommandPayload());
        foreach (var option in context.Options(CommandKind.ChooseCorruptionTile))
            foreach (var tile in context.Choice.Shuffled(option.EligibleIds))
                yield return (CommandKind.ChooseCorruptionTile, new CommandPayload { Ids = [tile] });
        if (draw)
            yield return (CommandKind.DrawCorruptionTile, new CommandPayload());
    }
}
