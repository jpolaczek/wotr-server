using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain.BotProtocol;

// BOT-225/BOT-221: a card's recruitment (an EffectChoice with a recruitment plan), two CARD_CHOICE questions per figure:
// where (the region and its Nation, as typed fields) and then which unit (the bare word REGULAR, ELITE, LEADER or NAZGUL,
// with the region's slug and the Nation's key as words of the question). This is the shape AlphaWOTR reads a card's
// recruitment from: its engine places the card's figures inside the play, and its adapter answers from the board's
// difference (a region by the figures it gained, then a unit word matched against them).
public static class ProtocolCardRecruitQuestions
{
    private sealed record Figure(string Region, string Nation, UnitKind Kind);

    public static IEnumerable<ProtocolPath> For(ProtocolContext context, ActionOption option)
    {
        var plan = new Plan(context, option);
        var node = plan.First();
        return node.Branches.Select(b => b.Terminal
            ? new ProtocolPath(new ProtocolStep(node.Question, b.Atom, b.Text, node.Context), b.Command!)
            : new ProtocolPath(new ProtocolStep(node.Question, b.Atom, b.Text, node.Context), () => b.Next));
    }

    private sealed class Plan(ProtocolContext context, ActionOption option)
    {
        private readonly string card = context.View.Decision?.SourceId ?? "";
        private readonly int count = Math.Max(1, Math.Max(option.Minimum, option.Maximum));
        private readonly Dictionary<string, bool> judged = new(StringComparer.Ordinal);

        public ProtocolNode First() => Node(new List<Figure>());

        private ProtocolNode Node(IReadOnlyList<Figure> placed)
        {
            var branches = new List<ProtocolBranch>();
            if (placed.Count >= option.Minimum && placed.Count > 0 && placed.Count < count && Legal(placed))
                branches.Add(new ProtocolBranch(ProtocolAtom.Of("card_choice", ("card", context.Card(card)), ("answer", "done"), ("skip", true)),
                    "No more figures", Command(placed)));
            foreach (var place in Figures(placed).Where(f => Completable(placed.Append(f).ToList()))
                         .GroupBy(f => (f.Region, f.Nation)))
            {
                var region = context.Region(place.Key.Region);
                var nation = context.Nation(place.Key.Nation);
                var atom = ProtocolAtom.Of("card_choice", ("card", context.Card(card)), ("answer", region), ("region", region),
                    ("nation", nation));
                var kinds = place.ToList();
                branches.Add(new ProtocolBranch(atom, $"A figure of {place.Key.Nation} in {place.Key.Region}", () => Unit(placed, kinds)));
            }
            return new ProtocolNode("CARD_CHOICE", new JsonObject
            {
                ["card"] = context.Card(card), ["question"] = CardCatalog.Title(card) + ": where is a figure recruited?"
            }, branches);
        }

        // The unit placed in the chosen region, as a bare unit word.
        private ProtocolNode Unit(IReadOnlyList<Figure> placed, IReadOnlyList<Figure> kinds)
        {
            var branches = new List<ProtocolBranch>();
            foreach (var figure in kinds)
            {
                var next = placed.Append(figure).ToList();
                var unit = ProtocolVocabulary.Unit(figure.Kind);
                // The region and Nation are the question's words (every figure here shares them), not the option's fields:
                // AlphaWOTR scores an option's fields against its engine's choice, which names only the unit.
                var atom = ProtocolAtom.Of("card_choice", ("card", context.Card(card)), ("answer", unit.ToUpperInvariant()),
                    ("unit", unit));
                var text = $"A {figure.Kind} of {figure.Nation} in {figure.Region}";
                branches.Add(next.Count >= count ? new ProtocolBranch(atom, text, Command(next)) : new ProtocolBranch(atom, text, () => Node(next)));
            }
            var first = kinds[0];
            return new ProtocolNode("CARD_CHOICE", new JsonObject
            {
                ["card"] = context.Card(card),
                ["question"] = $"Which unit is recruited in {context.Region(first.Region)} for {context.Nation(first.Nation)} ?"
            }, branches);
        }

        // The figures a limit still allows, with a reserve figure left.
        private IEnumerable<Figure> Figures(IReadOnlyList<Figure> placed)
        {
            var limits = option.EventRecruitment?.Limits ?? new List<RecruitmentLimit>();
            foreach (var limit in limits)
                foreach (var kind in limit.Kinds)
                {
                    var figure = new Figure(limit.RegionId, limit.Nation, kind);
                    var here = placed.Count(p => p.Region == limit.RegionId && p.Nation == limit.Nation && limit.Kinds.Contains(p.Kind));
                    var reserve = Reserve(limit.Nation, kind).Count - placed.Count(p => p.Nation == limit.Nation && p.Kind == kind);
                    if ((limit.Maximum == 0 || here < limit.Maximum) && reserve > 0)
                        yield return figure;
                }
        }

        private IReadOnlyList<string> Reserve(string nation, UnitKind kind) =>
            option.Recruitment.FirstOrDefault(r => r.Nation == nation && r.Kind == kind)?.ReserveIds ?? new List<string>();

        // Filling the rest makes a legal plan: with the first figures allowed or the last ones, each either anywhere or in
        // the regions already chosen first (a card's "two in each of three settlements" is only completed so).
        private bool Completable(IReadOnlyList<Figure> placed)
        {
            foreach (var same in new[] { false, true })
                foreach (var last in new[] { false, true })
                {
                    var plan = placed.ToList();
                    while (plan.Count < count && Figures(plan).ToList() is { Count: > 0 } open)
                    {
                        var pool = same && open.Where(f => plan.Any(p => p.Region == f.Region)).ToList() is { Count: > 0 } kept ? kept : open;
                        plan.Add(last ? pool[^1] : pool[0]);
                    }
                    if (plan.Count >= option.Minimum && Legal(plan))
                        return true;
                }
            return false;
        }

        private bool Legal(IReadOnlyList<Figure> placed)
        {
            var key = string.Join(",", placed.Select(p => p.Region + ":" + p.Nation + ":" + p.Kind));
            return judged.TryGetValue(key, out var legal) ? legal : judged[key] = context.Legal(Command(placed));
        }

        private GameCommand Command(IReadOnlyList<Figure> placed)
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            var placements = placed.Select(p => new UnitPlacementChoice
            {
                RegionId = p.Region, UnitId = Reserve(p.Nation, p.Kind).FirstOrDefault(id => used.Add(id)) ?? ""
            }).ToList();
            return context.Command(CommandKind.ChooseEffect, new CommandPayload { Placements = placements });
        }
    }
}
