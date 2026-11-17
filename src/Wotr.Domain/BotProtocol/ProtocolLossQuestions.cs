using System.Text.Json.Nodes;
using Wotr.Contracts;

namespace Wotr.Domain.BotProtocol;

// BOT-225: casualties one hit at a time (SPEC §5 CASUALTY: remove a Regular, reduce an Elite, or remove an Elite for two
// hits) with the hits still to take in the context, and the figures leaving an overfull region one by one
// (REMOVE_EXCESS_UNIT). A loss is offered only when the rest of the hits can still be taken legally.
public static class ProtocolLossQuestions
{
    private sealed record Loss(string UnitId, BattleLossKind Kind, string Nation, string Unit, int Hits);

    public static IEnumerable<ProtocolPath> Casualties(ProtocolContext context)
    {
        var kind = context.Option(CommandKind.TakeBattleLosses) is not null ? CommandKind.TakeBattleLosses : CommandKind.TakeEventLosses;
        if (context.Option(kind) is not { } option)
            return Enumerable.Empty<ProtocolPath>();
        var plan = new CasualtyPlan(context, option, kind);
        var node = plan.First();
        return node.Branches.Select(b => b.Terminal
            ? new ProtocolPath(new ProtocolStep(node.Question, b.Atom, b.Text, node.Context), b.Command!)
            : new ProtocolPath(new ProtocolStep(node.Question, b.Atom, b.Text, node.Context), () => b.Next));
    }

    private sealed class CasualtyPlan(ProtocolContext context, ActionOption option, CommandKind kind)
    {
        private readonly int hits = Math.Max(option.Maximum, context.View.Decision?.RequiredCount ?? 0);
        private readonly Dictionary<string, bool> judged = new(StringComparer.Ordinal);

        public ProtocolNode First() => Node(new List<Loss>());

        private ProtocolNode Node(IReadOnlyList<Loss> taken)
        {
            var left = hits - taken.Sum(l => l.Hits);
            var branches = new List<ProtocolBranch>();
            if (option.Minimum == 0 && taken.Count == 0 && Legal(taken))
                branches.Add(new ProtocolBranch(ProtocolAtom.Of("none"), "Take no losses", Command(taken)));
            foreach (var loss in Choices(taken, left).GroupBy(l => (l.Nation, l.Unit, l.Kind, l.Hits)).Select(g => g.First()))
            {
                var next = taken.Append(loss).ToList();
                if (!Completable(next))
                    continue;
                var atom = ProtocolAtom.Of("casualty", ("nation", loss.Nation), ("unit", loss.Unit),
                    ("effect", loss.Kind == BattleLossKind.Downgrade ? "reduce" : "remove"));
                var text = (loss.Kind == BattleLossKind.Downgrade ? "Reduce " : "Remove ") + "a " + loss.Unit + " of " + loss.Nation;
                branches.Add(Done(next) ? new ProtocolBranch(atom, text, Command(next)) : new ProtocolBranch(atom, text, () => Node(next)));
            }
            var question = ProtocolBattleQuestions.Context(context) ?? new JsonObject();
            question["hits"] = left;
            // AlphaWOTR's engine allocates one loss per decision, but its adapter answers every further hit of a round from a
            // plan keyed on (region, round, attacker); a round told apart per hit (1.01, 1.02, ...; its whole part is the
            // battle's round) lets each hit reach its engine, whose choice ProtocolHints then reads.
            if (question["round"] is JsonValue round && round.TryGetValue<int>(out var number))
                question["round"] = number + (taken.Count + 1) / 100.0;
            return new ProtocolNode("CASUALTY", question, branches);
        }

        // Every loss the remaining figures and hits allow.
        private IEnumerable<Loss> Choices(IReadOnlyList<Loss> taken, int left)
        {
            var used = taken.Select(l => l.UnitId).ToHashSet(StringComparer.Ordinal);
            foreach (var id in option.EligibleIds.Where(id => !used.Contains(id)))
            {
                if (context.Unit(id) is not { } unit)
                    continue;
                var nation = context.Nation(unit.Nation);
                if (unit.Kind == UnitKind.Regular && left >= 1)
                    yield return new Loss(id, BattleLossKind.Remove, nation, "regular", 1);
                if (unit.Kind == UnitKind.Elite && left >= 1)
                    yield return new Loss(id, BattleLossKind.Downgrade, nation, "elite", 1);
                if (unit.Kind == UnitKind.Elite && left >= 2)
                    yield return new Loss(id, BattleLossKind.Remove, nation, "elite", 2);
            }
        }

        // No more hits to take, or none of them can be taken (the force is gone).
        private bool Done(IReadOnlyList<Loss> taken)
        {
            var left = hits - taken.Sum(l => l.Hits);
            return left <= 0 || !Choices(taken, left).Any();
        }

        // Some way to take the rest of the hits ends in a legal command: heaviest losses first, or lightest first.
        private bool Completable(IReadOnlyList<Loss> taken)
        {
            foreach (var order in new Func<Loss, int>[] { l => -l.Hits, l => l.Hits, l => l.Kind == BattleLossKind.Downgrade ? 1 : 0 })
            {
                var plan = taken.ToList();
                while (!Done(plan))
                    plan.Add(Choices(plan, hits - plan.Sum(l => l.Hits)).OrderBy(order).First());
                if (Legal(plan))
                    return true;
            }
            return false;
        }

        private bool Legal(IReadOnlyList<Loss> taken)
        {
            // In the answers' order: the engine judges losses in the order they are listed.
            var key = string.Join(",", taken.Select(l => l.UnitId + ":" + l.Kind));
            return judged.TryGetValue(key, out var legal) ? legal : judged[key] = context.Legal(Command(taken));
        }

        private GameCommand Command(IReadOnlyList<Loss> taken) => context.Command(kind, new CommandPayload
        {
            Losses = taken.Select(l => new BattleLossChoice { UnitId = l.UnitId, Kind = l.Kind }).ToList()
        });
    }

    // The figures leaving an overfull region, one per question, any order.
    public static IEnumerable<ProtocolPath> Excess(ProtocolContext context)
    {
        if (context.Option(CommandKind.RemoveExcess) is not { } option)
            return Enumerable.Empty<ProtocolPath>();
        var count = Math.Max(option.Minimum, context.View.Decision?.RequiredCount ?? 0);
        var region = option.Targets.FirstOrDefault() ?? "";
        var units = option.EligibleIds.Select(id => context.Unit(id)).Where(u => u is not null).Select(u => u!).ToList();
        var judged = new Dictionary<string, bool>(StringComparer.Ordinal);
        bool Legal(GameCommand command)
        {
            var key = string.Join(",", command.Payload.Ids);
            return judged.TryGetValue(key, out var legal) ? legal : judged[key] = context.Legal(command);
        }
        IEnumerable<ProtocolPath> Sequences(List<UnitView> chosen)
        {
            if (chosen.Count == count)
            {
                var command = context.Command(CommandKind.RemoveExcess, new CommandPayload { Ids = chosen.Select(u => u.Id).ToList() });
                if (Legal(command))
                    yield return new ProtocolPath(chosen.Select(u => new ProtocolStep("REMOVE_EXCESS_UNIT",
                        ProtocolAtom.Of("remove_unit", ("nation", context.Nation(u.Nation)), ("unit", ProtocolVocabulary.Unit(u.Kind)),
                            ("region", context.Region(region))), $"A {u.Kind} of {u.Nation} leaves {region}")).ToList(), command);
                yield break;
            }
            foreach (var kind in units.Where(u => !chosen.Contains(u)).GroupBy(u => (u.Nation, u.Kind)))
                foreach (var path in Sequences(chosen.Append(kind.First()).ToList()))
                    yield return path;
        }
        return Sequences(new List<UnitView>());
    }
}
