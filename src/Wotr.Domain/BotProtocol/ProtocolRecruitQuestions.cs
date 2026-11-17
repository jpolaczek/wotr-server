using Wotr.Contracts;

namespace Wotr.Domain.BotProtocol;

// BOT-225: a Muster die's recruitment (SPEC §5 RECRUIT): one figure per question (region, Nation, unit), then the next
// figure or the end. Figures of one Nation and kind are alike, so a kind names the figure.
public static class ProtocolRecruitQuestions
{
    public static IEnumerable<ProtocolPath> Actions(ProtocolContext context)
    {
        if (context.Option(CommandKind.Recruit) is not { } option)
            yield break;
        foreach (var die in option.EligibleDice)
        {
            var plan = new RecruitPlan(context, option, die);
            if (plan.Placements().Count == 0)
                continue;
            yield return new ProtocolPath(new ProtocolStep(ProtocolActionQuestions.Question,
                    ProtocolActionQuestions.UseDie(context, die, "recruit"), ProtocolActionQuestions.DieText(context, die) + ": recruit"),
                plan.First);
        }
    }

    private sealed record Placement(string Region, string Nation, UnitKind Kind, IReadOnlyList<string> Reserve);

    private sealed class RecruitPlan(ProtocolContext context, ActionOption option, string die)
    {
        private List<Placement>? placements;
        private readonly Dictionary<string, bool> judged = new(StringComparer.Ordinal);

        // Every (settlement, Nation, kind) the option offers.
        public List<Placement> Placements() => placements ??= option.Recruitment
            .Where(r => r.ReserveIds.Count > 0)
            .SelectMany(r => r.Targets.Select(t => new Placement(t, r.Nation, r.Kind, r.ReserveIds)))
            .Where(p => Legal(new[] { p }) || Pairs(p).Any())
            .ToList();

        public ProtocolNode First()
        {
            var branches = Placements().Select(first =>
            {
                var alone = Legal(new[] { first });
                var pairs = Pairs(first).ToList();
                return pairs.Count == 0 || option.Maximum < 2
                    ? new ProtocolBranch(Atom(first), Text(first), Command(new[] { first }))
                    : new ProtocolBranch(Atom(first), Text(first), () => Second(first, alone, pairs));
            }).ToList();
            return new ProtocolNode("RECRUIT", null, branches);
        }

        private ProtocolNode Second(Placement first, bool alone, IReadOnlyList<Placement> pairs)
        {
            var branches = new List<ProtocolBranch>();
            if (alone)
                branches.Add(new ProtocolBranch(ProtocolAtom.Of("end"), "End the recruitment", Command(new[] { first })));
            branches.AddRange(pairs.Select(second => new ProtocolBranch(Atom(second), Text(second), Command(new[] { first, second }))));
            return new ProtocolNode("RECRUIT", null, branches);
        }

        // The second figures that complete a legal recruitment with [first].
        private IEnumerable<Placement> Pairs(Placement first)
        {
            if (option.Maximum < 2)
                yield break;
            foreach (var second in option.Recruitment.Where(r => r.ReserveIds.Count > 0)
                         .SelectMany(r => r.Targets.Select(t => new Placement(t, r.Nation, r.Kind, r.ReserveIds))))
                if (second.Region != first.Region && Legal(new[] { first, second }))
                    yield return second;
        }

        private System.Text.Json.Nodes.JsonObject Atom(Placement p) => ProtocolAtom.Of("recruit", ("region", context.Region(p.Region)),
            ("nation", context.Nation(p.Nation)), ("unit", ProtocolVocabulary.Unit(p.Kind)));

        private static string Text(Placement p) => $"A {p.Kind} of {p.Nation} in {p.Region}";

        private bool Legal(IReadOnlyList<Placement> chosen)
        {
            var key = string.Join("|", chosen.Select(p => p.Region + ":" + p.Nation + ":" + p.Kind));
            return judged.TryGetValue(key, out var legal) ? legal : judged[key] = context.Legal(Command(chosen));
        }

        private GameCommand Command(IReadOnlyList<Placement> chosen)
        {
            var taken = new HashSet<string>(StringComparer.Ordinal);
            var units = new List<UnitPlacementChoice>();
            foreach (var p in chosen)
            {
                var unit = p.Reserve.FirstOrDefault(id => taken.Add(id)) ?? "";
                units.Add(new UnitPlacementChoice { UnitId = unit, RegionId = p.Region });
            }
            return context.Command(CommandKind.Recruit, new CommandPayload { DieId = die, Placements = units });
        }
    }
}
