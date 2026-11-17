using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain.BotProtocol;

// BOT-225: characters' moves (SPEC §5): Companions group by group (MOVE_COMPANIONS, then JOIN_GROUP for each other
// Companion where the group starts, then the next group or the end), Companions leaving the Fellowship
// (SEPARATE_COMPANIONS: who, then where), and the Nazgûl and Minions figure by figure (MOVE_MINIONS: select, then move,
// then the next or the end). Every option leaves a legal command within reach; routes are the engine's to judge.
public static class ProtocolCharacterQuestions
{
    public static IEnumerable<ProtocolPath> Actions(ProtocolContext context)
    {
        foreach (var path in DieActions(context, CommandKind.MoveCompanions, "move_companions",
                     die => new CompanionPlan(context, context.Option(CommandKind.MoveCompanions)!, die, requireOne: true).First()))
            yield return path;
        foreach (var path in DieActions(context, CommandKind.SeparateCompanions, "separate_companions",
                     die => new SeparationPlan(context, context.Option(CommandKind.SeparateCompanions)!, die).First()))
            yield return path;
        foreach (var path in DieActions(context, CommandKind.MoveShadowCharacters, "move_minions",
                     die => new MinionPlan(context, context.Option(CommandKind.MoveShadowCharacters)!, die, requireOne: true).First()))
            yield return path;
    }

    // A forced or a card's separation and a card's Companion journey (CompanionSeparation).
    public static IEnumerable<ProtocolPath> Separation(ProtocolContext context)
    {
        // A card's journey (Gwaihir the Windlord, We Prove the Swifter, The Book of Mazarbul, Fear! Fire! Foes!): AlphaWOTR
        // settles a card's Companion moves inside its play and reads them back by token (the Companions' keys and the
        // destination), so each group and destination is one CARD_CHOICE.
        var source = context.View.Decision?.SourceId ?? "";
        if (CardCatalog.All.ContainsKey(source) && context.Option(CommandKind.MoveCompanions) is { } journey)
            return new JourneyPlan(context, context.Option(CommandKind.SeparateCompanions), journey, source).Paths();
        var separate = context.Option(CommandKind.SeparateCompanions) is { } s ? new SeparationPlan(context, s, "").First() : null;
        var move = context.Option(CommandKind.MoveCompanions) is { } m ? new CompanionPlan(context, m, "", requireOne: !m.AllowEmptySelection).First() : null;
        var nodes = new[] { separate, move }.Where(n => n is { Branches.Count: > 0 }).Select(n => n!).ToList();
        if (nodes.Count == 2)
        {
            // Both a separation and a journey: which one is the card's question first.
            var card = context.View.Decision?.SourceId ?? "";
            var question = new JsonObject { ["card"] = context.Card(card), ["question"] = "Separate Companions or move them?" };
            return new[]
            {
                new ProtocolPath(new ProtocolStep("CARD_CHOICE", ProtocolAtom.Of("card_choice", ("card", context.Card(card)), ("answer", "separate")),
                    "Separate Companions", question), () => nodes[0]),
                new ProtocolPath(new ProtocolStep("CARD_CHOICE", ProtocolAtom.Of("card_choice", ("card", context.Card(card)), ("answer", "move")),
                    "Move Companions", question), () => nodes[1])
            };
        }
        return nodes.SelectMany(Paths);
    }

    // The Nazgûl's and the Witch-king's flights a card allows (CharacterEvent).
    public static IEnumerable<ProtocolPath> MinionEvent(ProtocolContext context) =>
        context.Option(CommandKind.MoveShadowCharacters) is { } option
            ? Paths(new MinionPlan(context, option, "", requireOne: !option.AllowEmptySelection).First())
            : Enumerable.Empty<ProtocolPath>();

    // A card's journey as CARD_CHOICE questions: a group (smallest first, so an exact group outranks a larger one in the bot's
    // token scoring) and its destination per question, or a separation of Companions from the Fellowship, then done.
    private sealed class JourneyPlan(ProtocolContext context, ActionOption? separate, ActionOption move, string card)
    {
        private readonly Dictionary<string, bool> judged = new(StringComparer.Ordinal);

        public IEnumerable<ProtocolPath> Paths()
        {
            var node = Node(new List<CompanionMoveChoice>());
            return node.Branches.Select(b => b.Terminal
                ? new ProtocolPath(new ProtocolStep(node.Question, b.Atom, b.Text, node.Context), b.Command!)
                : new ProtocolPath(new ProtocolStep(node.Question, b.Atom, b.Text, node.Context), () => b.Next));
        }

        private ProtocolNode Node(IReadOnlyList<CompanionMoveChoice> done)
        {
            var branches = new List<ProtocolBranch>();
            if ((done.Count > 0 || move.AllowEmptySelection) && Legal(Move(done)))
                branches.Add(new ProtocolBranch(Atom("done", null, null, null, skip: true), "Done", Move(done)));
            if (done.Count == 0 && separate is not null)
                foreach (var (group, target, command) in Separations())
                    branches.Add(new ProtocolBranch(Atom(Answer(group, target), group[0], context.View.Fellowship.RegionId, target),
                        "Separate " + string.Join(", ", group) + " to " + target, command));
            if (move.MaximumGroups == 0 || done.Count < move.MaximumGroups)
            {
                var moved = done.SelectMany(g => g.Ids).ToHashSet(StringComparer.Ordinal);
                foreach (var origin in move.Travel.Where(t => !moved.Contains(t.Id)).GroupBy(t => t.Origin))
                    foreach (var group in Subsets(origin.Select(t => t.Id).ToList(), 4))
                        foreach (var target in Targets(group))
                        {
                            var route = Route(done, group, target);
                            if (route is null)
                                continue;
                            var after = done.Append(new CompanionMoveChoice { Ids = group.ToList(), Route = route }).ToList();
                            var last = move.MaximumGroups != 0 && after.Count >= move.MaximumGroups;
                            var atom = Atom(Answer(group, target), group[0], origin.Key, target);
                            var text = string.Join(", ", group) + " to " + target;
                            branches.Add(last ? new ProtocolBranch(atom, text, Move(after)) : new ProtocolBranch(atom, text, () => Node(after)));
                        }
            }
            return new ProtocolNode("CARD_CHOICE", ProtocolEffectQuestions.Question(context, card,
                CardCatalog.Title(card) + ": which Companions go where?"), branches);
        }

        private JsonObject Atom(string answer, string? character, string? from, string? to, bool skip = false)
        {
            var atom = ProtocolAtom.Of("card_choice", ("card", context.Card(card)), ("answer", answer));
            if (skip)
                atom["skip"] = true;
            if (character is not null)
                atom["character"] = Key(context, character);
            if (from is not null)
                atom["from"] = context.Region(from);
            if (to is not null)
            {
                atom["to"] = context.Region(to);
                atom["region"] = context.Region(to);
            }
            return atom;
        }

        private string Answer(IReadOnlyList<string> group, string target) =>
            string.Join(":", group.Select(id => Key(context, id))) + ">" + context.Region(target);

        private IEnumerable<string> Targets(IReadOnlyList<string> group) =>
            move.Travel.Where(t => group.Contains(t.Id)).SelectMany(t => t.Targets).Distinct();

        private List<string>? Route(IReadOnlyList<CompanionMoveChoice> done, IReadOnlyList<string> group, string target)
        {
            var travels = move.Travel.Where(t => group.Contains(t.Id)).ToList();
            var best = travels.OrderByDescending(t => t.Range).First();
            foreach (var route in ProtocolRoutes.Between(best.Origin, target, travels.Max(t => Math.Max(t.Range, t.SmallGroupRange)),
                         best.StopRegions, best.BlockedRegions))
                if (Legal(Move(done.Append(new CompanionMoveChoice { Ids = group.ToList(), Route = route }).ToList())))
                    return route;
            return null;
        }

        private IEnumerable<(IReadOnlyList<string> Group, string Target, GameCommand Command)> Separations()
        {
            foreach (var group in Subsets(separate!.EligibleIds, Math.Min(3, Math.Max(1, separate.Maximum))))
            {
                var travels = separate.Travel.Where(t => group.Contains(t.Id)).ToList();
                if (travels.Count == 0)
                    continue;
                var best = travels.OrderByDescending(t => t.Range).First();
                foreach (var target in best.Targets)
                {
                    var command = ProtocolRoutes.For(best, target)
                        .Select(route => context.Command(CommandKind.SeparateCompanions, new CommandPayload { Ids = group.ToList(), Route = route }))
                        .FirstOrDefault(Legal);
                    if (command is not null)
                        yield return (group, target, command);
                }
            }
        }

        // Every non-empty group of the ids up to [limit] Companions, the smallest first.
        private static IEnumerable<IReadOnlyList<string>> Subsets(IReadOnlyList<string> ids, int limit) =>
            Enumerable.Range(1, Math.Min(limit, ids.Count)).SelectMany(size => ProtocolActionQuestions.Choose(ids, size));

        private GameCommand Move(IReadOnlyList<CompanionMoveChoice> moves) => context.Command(CommandKind.MoveCompanions,
            new CommandPayload { CompanionMoves = moves.Select(m => new CompanionMoveChoice { Ids = m.Ids.ToList(), Route = m.Route.ToList() }).ToList() });

        private bool Legal(GameCommand command)
        {
            var key = command.Kind + "|" + string.Join(",", command.Payload.Ids) + "|" + string.Join(",", command.Payload.Route) + "|" +
                string.Join(";", command.Payload.CompanionMoves.Select(m => string.Join(",", m.Ids) + ">" + string.Join(",", m.Route)));
            return judged.TryGetValue(key, out var legal) ? legal : judged[key] = context.Legal(command);
        }
    }

    private static IEnumerable<ProtocolPath> Paths(ProtocolNode node) => node.Branches.Select(b => b.Terminal
        ? new ProtocolPath(new ProtocolStep(node.Question, b.Atom, b.Text, node.Context), b.Command!)
        : new ProtocolPath(new ProtocolStep(node.Question, b.Atom, b.Text, node.Context), () => b.Next));

    private static IEnumerable<ProtocolPath> DieActions(ProtocolContext context, CommandKind kind, string action, Func<string, ProtocolNode> plan)
    {
        if (context.Option(kind) is not { } option)
            yield break;
        bool? possible = null;
        foreach (var die in option.EligibleDice)
        {
            possible ??= plan(die).Branches.Count > 0;
            if (possible != true)
                yield break;
            var chosen = die;
            yield return new ProtocolPath(new ProtocolStep(ProtocolActionQuestions.Question, ProtocolActionQuestions.UseDie(context, die, action),
                ProtocolActionQuestions.DieText(context, die) + ": " + action.Replace('_', ' ')), () => plan(chosen));
        }
    }

    private static string Key(ProtocolContext context, string id) =>
        context.Character(context.View.Characters.FirstOrDefault(c => c.Id == id)?.DefinitionId ?? id);

    // Companions on the board moving group by group.
    private sealed class CompanionPlan(ProtocolContext context, ActionOption option, string die, bool requireOne)
    {
        private readonly Dictionary<string, bool> judged = new(StringComparer.Ordinal);

        public ProtocolNode First() => Groups(new List<CompanionMoveChoice>());

        private ProtocolNode Groups(IReadOnlyList<CompanionMoveChoice> done)
        {
            var branches = new List<ProtocolBranch>();
            if ((done.Count > 0 || !requireOne) && Legal(done))
                branches.Add(new ProtocolBranch(ProtocolAtom.Of("end"), "End the move", Command(done)));
            if (option.MaximumGroups == 0 || done.Count < option.MaximumGroups)
            {
                var moved = done.SelectMany(g => g.Ids).ToHashSet(StringComparer.Ordinal);
                foreach (var travel in option.Travel.Where(t => !moved.Contains(t.Id)))
                    foreach (var target in travel.Targets)
                    {
                        var group = new List<string> { travel.Id };
                        var joiners = Joiners(travel, moved);
                        if (!Reachable(done, group, joiners, target))
                            continue;
                        var atom = ProtocolAtom.Of("move_companion", ("character", Key(context, travel.Id)),
                            ("from", context.Region(travel.Origin)), ("to", context.Region(target)));
                        var text = CharacterCatalog.Title(travel.Id) + " moves to " + target;
                        branches.Add(new ProtocolBranch(atom, text, () => Join(done, group, joiners, 0, target)));
                    }
            }
            return new ProtocolNode("MOVE_COMPANIONS", null, branches);
        }

        // The other Companions where the group starts, asked one by one whether they join it.
        private ProtocolNode Join(IReadOnlyList<CompanionMoveChoice> done, List<string> group, IReadOnlyList<string> joiners, int index, string target)
        {
            if (index >= joiners.Count)
                return Groups(done.Append(Move(group, target)!).ToList());
            var who = joiners[index];
            var rest = joiners.Skip(index + 1).ToList();
            var branches = new List<ProtocolBranch>();
            foreach (var join in new[] { true, false })
            {
                var next = join ? group.Append(who).ToList() : group;
                if (!Reachable(done, next, rest, target))
                    continue;
                var atom = ProtocolAtom.Of(join ? "join" : "stay", ("character", Key(context, who)), ("to", context.Region(target)));
                branches.Add(new ProtocolBranch(atom, CharacterCatalog.Title(who) + (join ? " joins" : " stays"),
                    () => Join(done, next, joiners, index + 1, target)));
            }
            return new ProtocolNode("JOIN_GROUP", new JsonObject { ["character"] = Key(context, who), ["to"] = context.Region(target) }, branches);
        }

        private List<string> Joiners(TravelOption travel, HashSet<string> moved) =>
            option.Travel.Where(t => t.Id != travel.Id && !moved.Contains(t.Id) && t.Origin == travel.Origin).Select(t => t.Id).ToList();

        // The group (with none, or all, of the Companions still to be asked) reaches the target legally.
        private bool Reachable(IReadOnlyList<CompanionMoveChoice> done, List<string> group, IReadOnlyList<string> undecided, string target) =>
            new[] { group, group.Concat(undecided).ToList() }.Distinct().Any(g => Move(g, target) is { } move && Legal(done.Append(move).ToList()));

        // The group's move by its best route: the members' longest reach and every member's limits.
        private CompanionMoveChoice? Move(List<string> group, string target)
        {
            var travels = option.Travel.Where(t => group.Contains(t.Id)).ToList();
            if (travels.Count == 0)
                return null;
            var best = travels.OrderByDescending(t => group.Count <= 2 && t.SmallGroupRange > 0 ? t.SmallGroupRange : t.Range).First();
            var range = travels.Max(t => group.Count <= 2 && t.SmallGroupRange > 0 && group.All(g => g == t.Id || t.SmallGroupPartners.Contains(g))
                ? t.SmallGroupRange : t.Range);
            var route = ProtocolRoutes.Between(best.Origin, target, range, best.StopRegions, best.BlockedRegions)
                .FirstOrDefault(r => Legal(new[] { new CompanionMoveChoice { Ids = group.ToList(), Route = r } }));
            return route is null ? null : new CompanionMoveChoice { Ids = group.ToList(), Route = route };
        }

        private bool Legal(IReadOnlyList<CompanionMoveChoice> moves)
        {
            var key = string.Join("|", moves.Select(m => string.Join(",", m.Ids) + ">" + string.Join(",", m.Route)));
            return judged.TryGetValue(key, out var legal) ? legal : judged[key] = context.Legal(Command(moves));
        }

        private GameCommand Command(IReadOnlyList<CompanionMoveChoice> moves) => context.Command(CommandKind.MoveCompanions,
            new CommandPayload { DieId = die, CompanionMoves = moves.Select(m => new CompanionMoveChoice { Ids = m.Ids.ToList(), Route = m.Route.ToList() }).ToList() });
    }

    // Companions leaving the Fellowship: each one picked, then the group's destination.
    private sealed class SeparationPlan(ProtocolContext context, ActionOption option, string die)
    {
        private readonly Dictionary<string, bool> judged = new(StringComparer.Ordinal);

        public ProtocolNode First() => Node(new List<string>());

        private ProtocolNode Node(IReadOnlyList<string> picked)
        {
            var branches = new List<ProtocolBranch>();
            foreach (var id in option.EligibleIds.Where(id => !picked.Contains(id)))
            {
                var next = picked.Append(id).ToList();
                if (option.Maximum > 0 && next.Count > option.Maximum || !Possible(next, option.EligibleIds.Except(next).ToList()))
                    continue;
                branches.Add(new ProtocolBranch(ProtocolAtom.Of("separate", ("character", Key(context, id))),
                    CharacterCatalog.Title(id) + " leaves the Fellowship", () => Node(next)));
            }
            if (picked.Count > 0)
                foreach (var (target, command) in Destinations(picked))
                    branches.Add(new ProtocolBranch(ProtocolAtom.Of("separate_to", ("to", context.Region(target))), "They go to " + target, command));
            return new ProtocolNode("SEPARATE_COMPANIONS", null, branches);
        }

        // Some group containing [picked] (with none or some of the others) has a legal destination.
        private bool Possible(IReadOnlyList<string> picked, IReadOnlyList<string> others) =>
            Destinations(picked).Any() || others.Any(o => Destinations(picked.Append(o).ToList()).Any());

        private IEnumerable<(string Target, GameCommand Command)> Destinations(IReadOnlyList<string> picked)
        {
            var travels = option.Travel.Where(t => picked.Contains(t.Id)).ToList();
            if (travels.Count == 0)
            {
                // A separation without a route (on the Mordor Track the Companions are lost).
                var command = Command(picked, new List<string>());
                if (Legal(command))
                    yield return ("casualties", command);
                yield break;
            }
            var best = travels.OrderByDescending(t => t.Range).First();
            foreach (var target in best.Targets)
            {
                var route = ProtocolRoutes.For(best, target).Select(r => Command(picked, r)).FirstOrDefault(Legal);
                if (route is not null)
                    yield return (target, route);
            }
        }

        private bool Legal(GameCommand command)
        {
            var key = string.Join(",", command.Payload.Ids) + ">" + string.Join(",", command.Payload.Route);
            return judged.TryGetValue(key, out var legal) ? legal : judged[key] = context.Legal(command);
        }

        private GameCommand Command(IReadOnlyList<string> picked, List<string> route) => context.Command(CommandKind.SeparateCompanions,
            new CommandPayload { DieId = die, Ids = picked.ToList(), Route = route });
    }

    // The Nazgûl and the Minions, figure by figure: select one, then its destination.
    private sealed class MinionPlan(ProtocolContext context, ActionOption option, string die, bool requireOne)
    {
        private readonly Dictionary<string, bool> judged = new(StringComparer.Ordinal);

        public ProtocolNode First() => Select(new List<CompanionMoveChoice>());

        private ProtocolNode Select(IReadOnlyList<CompanionMoveChoice> done)
        {
            var branches = new List<ProtocolBranch>();
            if ((done.Count > 0 || !requireOne) && Legal(done))
                branches.Add(new ProtocolBranch(ProtocolAtom.Of("end"), "End the move", Command(done)));
            var moved = done.SelectMany(m => m.Ids).ToHashSet(StringComparer.Ordinal);
            foreach (var group in Figures().Where(f => !moved.Contains(f.Id)).GroupBy(f => (f.Nazgul, f.Nazgul ? f.Origin : f.Id)))
            {
                var figure = group.First();
                if (!Destinations(done, figure).Any())
                    continue;
                var atom = figure.Nazgul
                    ? ProtocolAtom.Of("select_nazgul", ("from", context.Region(figure.Origin)))
                    : ProtocolAtom.Of("select_minion", ("character", Key(context, figure.Id)), ("from", context.Region(figure.Origin)));
                branches.Add(new ProtocolBranch(atom, (figure.Nazgul ? "A Nazgûl" : CharacterCatalog.Title(figure.Id)) + " in " + figure.Origin,
                    () => Destination(done, figure)));
            }
            return new ProtocolNode("MOVE_MINIONS", null, branches);
        }

        private ProtocolNode Destination(IReadOnlyList<CompanionMoveChoice> done, Figure figure)
        {
            var branches = Destinations(done, figure).Select(move =>
            {
                var target = move.Route[^1];
                var atom = figure.Nazgul
                    ? ProtocolAtom.Of("move_nazgul", ("from", context.Region(figure.Origin)), ("to", context.Region(target)))
                    : ProtocolAtom.Of("move_minion", ("character", Key(context, figure.Id)), ("from", context.Region(figure.Origin)),
                        ("to", context.Region(target)));
                var after = done.Append(move).ToList();
                return new ProtocolBranch(atom, "To " + target, () => Select(after));
            }).ToList();
            return new ProtocolNode("MOVE_MINIONS", null, branches);
        }

        private IEnumerable<CompanionMoveChoice> Destinations(IReadOnlyList<CompanionMoveChoice> done, Figure figure)
        {
            var travel = option.Travel.FirstOrDefault(t => t.Id == figure.Id);
            var targets = travel?.Targets.Count > 0 ? travel.Targets : option.Targets;
            foreach (var target in targets.Where(t => t != figure.Origin))
            {
                var routes = travel is null || travel.Flight ? new List<List<string>> { new() { figure.Origin, target } }
                    : ProtocolRoutes.For(travel, target);
                var move = routes.Select(r => new CompanionMoveChoice { Ids = new List<string> { figure.Id }, Route = r })
                    .FirstOrDefault(m => Legal(done.Append(m).ToList()));
                if (move is not null)
                    yield return move;
            }
        }

        private sealed record Figure(string Id, string Origin, bool Nazgul);

        private IEnumerable<Figure> Figures()
        {
            foreach (var id in option.EligibleIds)
            {
                if (context.Unit(id) is { } unit)
                    yield return new Figure(id, unit.Location, true);
                else if (context.View.Characters.FirstOrDefault(c => c.Id == id || c.DefinitionId == id) is { } character)
                    yield return new Figure(id, character.Location, false);
                else if (option.Travel.FirstOrDefault(t => t.Id == id) is { } travel)
                    yield return new Figure(id, travel.Origin, false);
            }
        }

        private bool Legal(IReadOnlyList<CompanionMoveChoice> moves)
        {
            var key = string.Join("|", moves.Select(m => string.Join(",", m.Ids) + ">" + string.Join(",", m.Route)));
            return judged.TryGetValue(key, out var legal) ? legal : judged[key] = context.Legal(Command(moves));
        }

        private GameCommand Command(IReadOnlyList<CompanionMoveChoice> moves) => context.Command(CommandKind.MoveShadowCharacters,
            new CommandPayload { DieId = die, CompanionMoves = moves.Select(m => new CompanionMoveChoice { Ids = m.Ids.ToList(), Route = m.Route.ToList() }).ToList() });
    }
}
