using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain.BotProtocol;

// BOT-225: Armies' moves and attacks (SPEC §4-§5): the die and action (ACTION), then which Army from where to where
// (ARMY_MOVE, CHOOSE_ATTACK), which units go if not all (ARMY_SELECTION), then the next Army or the end. The same questions
// ask a card's Army moves and attacks (ArmyEvent) and the victor's advance (ADVANCE).
public static class ProtocolArmyQuestions
{
    public static IEnumerable<ProtocolPath> MoveActions(ProtocolContext context) => Actions(context, CommandKind.MoveArmies);

    public static IEnumerable<ProtocolPath> AttackActions(ProtocolContext context) => Actions(context, CommandKind.AttackArmy);

    private static IEnumerable<ProtocolPath> Actions(ProtocolContext context, CommandKind kind)
    {
        if (context.Option(kind) is not { } option)
            yield break;
        // Dice of one face move the same Armies: whether any move exists is judged once per face and ability.
        var possible = new Dictionary<(DieFace, string), bool>();
        foreach (var cost in option.MovementCosts)
        {
            var leader = cost.Face == DieFace.Character;
            var action = kind == CommandKind.MoveArmies ? (leader ? "move_army_with_leader" : "move_armies")
                : leader ? "attack_with_leader" : "attack";
            var plan = new ArmyPlan(context, option, kind, new CommandPayload
            {
                DieId = cost.DieId, MovementCost = cost.Face, AbilityId = cost.AbilityId
            }, Math.Max(1, cost.MaximumMoves), finish: null);
            if (!possible.TryGetValue((cost.Face, cost.AbilityId), out var any))
                any = possible[(cost.Face, cost.AbilityId)] = plan.Possible();
            if (!any)
                continue;
            yield return new ProtocolPath(new ProtocolStep(ProtocolActionQuestions.Question,
                    ProtocolActionQuestions.UseDie(context, cost.DieId, action),
                    ProtocolActionQuestions.DieText(context, cost.DieId) + (kind == CommandKind.MoveArmies ? ": move Armies" : ": attack")),
                plan.First);
        }
    }

    // A card's Army move or attack (ArmyEvent): no die; its routes come with its targets; an optional order may end at once.
    // A card that allows both a move and an attack first asks which (CARD_CHOICE "move" or "attack"), as the bot expects.
    public static IEnumerable<ProtocolPath> Event(ProtocolContext context)
    {
        var finish = context.Option(CommandKind.FinishArmyEvent) is null ? null : context.Command(CommandKind.FinishArmyEvent);
        var finished = finish is not null && context.Legal(finish);
        var nodes = new List<(CommandKind Kind, ProtocolNode Node)>();
        foreach (var kind in new[] { CommandKind.MoveArmies, CommandKind.AttackArmy })
            if (context.Option(kind) is { } option &&
                new ArmyPlan(context, option, kind, new CommandPayload(), Math.Max(1, option.Maximum), finish).First() is { Branches.Count: > 0 } node)
                nodes.Add((kind, node));
        // A card's single move or attack (Corsairs of Umbar, Grond, The Fighting Uruk-hai, a relief attack, the Nazgûl's or
        // the Witch-king's manoeuvre): its target first, as CARD_CHOICE with from, to and region (AlphaWOTR's engine settles
        // the card's target inside its play and its adapter reads it by these fields), then the Army's own questions for
        // that target only.
        if (nodes.Count > 0 && nodes.All(n => (context.Option(n.Kind)?.Maximum ?? 1) <= 1))
        {
            foreach (var path in Targets(context, nodes, finished ? finish : null))
                yield return path;
            yield break;
        }
        if (nodes.Count == 2)
        {
            var card = context.View.Decision?.SourceId ?? "";
            var question = ProtocolEffectQuestions.Question(context, card, CardCatalog.Title(card) + ": move or attack?");
            foreach (var (kind, node) in nodes)
            {
                var answer = kind == CommandKind.AttackArmy ? "attack" : "move";
                yield return new ProtocolPath(new ProtocolStep("CARD_CHOICE", ProtocolAtom.Of("card_choice", ("card", context.Card(card)),
                    ("answer", answer)), answer == "move" ? "Move an Army" : "Attack", question), () => Without(node, "end"));
            }
            if (finished)
                yield return new ProtocolPath(new ProtocolStep("CARD_CHOICE", ProtocolAtom.Of("card_choice", ("card", context.Card(card)),
                    ("answer", "skip"), ("skip", true)), "Neither", question), finish!);
            yield break;
        }
        foreach (var (_, node) in nodes)
            foreach (var branch in node.Branches)
                yield return branch.Terminal
                    ? new ProtocolPath(new ProtocolStep(node.Question, branch.Atom, branch.Text), branch.Command!)
                    : new ProtocolPath(new ProtocolStep(node.Question, branch.Atom, branch.Text), () => branch.Next);
        if (nodes.Count == 0 && finished)
            yield return new ProtocolPath(new ProtocolStep("ARMY_MOVE", ProtocolAtom.Of("end"), "End the move"), finish!);
    }

    private static IEnumerable<ProtocolPath> Targets(ProtocolContext context, IReadOnlyList<(CommandKind Kind, ProtocolNode Node)> nodes,
        GameCommand? finish)
    {
        var card = context.View.Decision?.SourceId ?? "";
        var question = ProtocolEffectQuestions.Question(context, card, CardCatalog.Title(card) + ": which Army goes where?");
        foreach (var (kind, node) in nodes)
            foreach (var pair in node.Branches.Where(b => (string?)b.Atom["kind"] is "army_move" or "attack")
                         .Select(b => ((string?)b.Atom["from"], (string?)b.Atom["to"])).Distinct())
            {
                var answer = kind == CommandKind.AttackArmy ? "attack" : "move";
                var atom = ProtocolAtom.Of("card_choice", ("card", context.Card(card)), ("answer", answer), ("from", pair.Item1),
                    ("to", pair.Item2), ("region", pair.Item2));
                var only = node;
                yield return new ProtocolPath(new ProtocolStep("CARD_CHOICE", atom,
                    (answer == "move" ? "Move from " : "Attack from ") + pair.Item1 + " to " + pair.Item2, question),
                    () => new ProtocolNode(only.Question, only.Context, only.Branches.Where(b =>
                        (string?)b.Atom["from"] == pair.Item1 && (string?)b.Atom["to"] == pair.Item2).ToList()));
            }
        if (finish is not null)
            yield return new ProtocolPath(new ProtocolStep("CARD_CHOICE", ProtocolAtom.Of("card_choice", ("card", context.Card(card)),
                ("answer", "skip"), ("skip", true)), "Neither", question), finish);
    }

    private static ProtocolNode Without(ProtocolNode node, string kind) =>
        new(node.Question, node.Context, node.Branches.Where(b => b.Atom["kind"]?.ToString() != kind).ToList());

    // The victor advances into the emptied region with all, some or none of its figures.
    public static IEnumerable<ProtocolPath> Advance(ProtocolContext context)
    {
        if (context.Option(CommandKind.AdvanceBattle) is not { } option)
            yield break;
        GameCommand Command(IReadOnlyList<string> ids) => context.Command(CommandKind.AdvanceBattle, new CommandPayload { Ids = ids.ToList() });
        var battle = ProtocolBattleQuestions.Context(context);
        foreach (var path in context.One("ADVANCE", ProtocolAtom.Of("stay"), "Stay", Command(Array.Empty<string>()), battle))
            yield return path;
        foreach (var path in context.One("ADVANCE", ProtocolAtom.Of("advance", ("all", true)), "Advance with everything",
                     Command(option.EligibleIds), battle))
            yield return path;
        var region = context.View.Battle?.Attacker.RegionId ?? "";
        var selection = new ProtocolSelection(context, option.EligibleIds, Command, new JsonObject
        {
            ["from"] = context.Region(region), ["to"] = context.Region(option.Targets.FirstOrDefault() ?? context.View.Battle?.RegionId ?? "")
        });
        if (option.EligibleIds.Count > 1 && selection.Possible())
            yield return new ProtocolPath(new ProtocolStep("ADVANCE", ProtocolAtom.Of("advance", ("all", false)), "Advance with some", battle),
                () => selection.First()!);
    }

    // One die's (or one card's) Army moves or attack, asked Army by Army.
    private sealed class ArmyPlan(ProtocolContext context, ActionOption option, CommandKind kind, CommandPayload cost, int moves,
        GameCommand? finish)
    {
        private readonly string question = kind == CommandKind.AttackArmy ? "CHOOSE_ATTACK" : "ARMY_MOVE";
        private readonly Dictionary<string, bool> judged = new(StringComparer.Ordinal);

        private ProtocolNode? first;

        public bool Possible() => First().Branches.Count > 0;

        public ProtocolNode First() => first ??= Node(new List<ArmyMoveChoice>());

        private ProtocolNode Node(IReadOnlyList<ArmyMoveChoice> done)
        {
            var branches = new List<ProtocolBranch>();
            if (done.Count > 0 && Legal(done))
                branches.Add(new ProtocolBranch(ProtocolAtom.Of("end"), "End the move", Command(done)));
            else if (done.Count == 0 && finish is not null && context.Legal(finish) && kind == CommandKind.MoveArmies)
                branches.Add(new ProtocolBranch(ProtocolAtom.Of("end"), "Move no Army", finish));
            foreach (var (army, target, ids) in Choices(done))
            {
                var all = Move(army, target, ids);
                var last = done.Count + 1 >= moves;
                var atomKind = kind == CommandKind.AttackArmy ? "attack" : "army_move";
                if (last ? Legal(done.Append(all).ToList()) : Viable(done.Append(all).ToList()))
                {
                    var after = done.Append(all).ToList();
                    var atom = ProtocolAtom.Of(atomKind, ("from", context.Region(army.RegionId)), ("to", context.Region(target.RegionId)), ("all", true));
                    var text = (kind == CommandKind.AttackArmy ? "Attack " : "Move from ") + army.RegionId + " to " + target.RegionId;
                    branches.Add(last ? new ProtocolBranch(atom, text, Command(after)) : new ProtocolBranch(atom, text, () => Node(after)));
                }
                if (ids.Count < 2)
                    continue;
                var selection = new ProtocolSelection(context, ids, chosen => Command(done.Append(Move(army, target, chosen)).ToList()),
                    new JsonObject { ["from"] = context.Region(army.RegionId), ["to"] = context.Region(target.RegionId) },
                    rearguard: kind == CommandKind.AttackArmy,
                    viable: last ? null : chosen => Viable(done.Append(Move(army, target, chosen)).ToList()));
                if (!selection.Possible())
                    continue;
                var partial = ProtocolAtom.Of(atomKind, ("from", context.Region(army.RegionId)), ("to", context.Region(target.RegionId)), ("all", false));
                var words = (kind == CommandKind.AttackArmy ? "Attack " : "Move part of the Army from ") + army.RegionId + " to " + target.RegionId;
                if (last)
                    branches.Add(new ProtocolBranch(partial, words, () => selection.First()!));
                else
                    branches.Add(new ProtocolBranch(partial, words, () => Continue(selection, army, target, done)));
            }
            return new ProtocolNode(question, null, branches);
        }

        // A partial move that is not the last: its selection's answers lead to the next Army's question.
        private ProtocolNode Continue(ProtocolSelection selection, ArmyOption army, ArmyTargetOption target, IReadOnlyList<ArmyMoveChoice> done) =>
            Relink(selection.First()!, command => Node(command.Payload.Moves));

        private static ProtocolNode Relink(ProtocolNode node, Func<GameCommand, ProtocolNode> next) =>
            new(node.Question, node.Context, node.Branches.Select(b => b.Terminal
                ? new ProtocolBranch(b.Atom, b.Text, () => next(b.Command!))
                : new ProtocolBranch(b.Atom, b.Text, () => Relink(b.Next, next))).ToList());

        // The Armies and targets still open: each Army's figures not yet moved that may enter the target.
        private IEnumerable<(ArmyOption Army, ArmyTargetOption Target, List<string> Ids)> Choices(IReadOnlyList<ArmyMoveChoice> done)
        {
            var used = done.SelectMany(m => m.UnitIds.Concat(m.CharacterIds)).ToHashSet(StringComparer.Ordinal);
            if (done.Count >= moves)
                yield break;
            foreach (var army in option.Armies)
                foreach (var target in army.Targets)
                {
                    var eligible = target.EligibleIds.Count > 0 ? target.EligibleIds.ToHashSet(StringComparer.Ordinal) : null;
                    var ids = army.UnitIds.Concat(army.CharacterIds)
                        .Where(id => !used.Contains(id) && (eligible is null || eligible.Contains(id))).Distinct().ToList();
                    if (ids.Count > 0)
                        yield return (army, target, ids);
                }
        }

        private ArmyMoveChoice Move(ArmyOption army, ArmyTargetOption target, IReadOnlyList<string> ids)
        {
            var characters = ids.Where(id => army.CharacterIds.Contains(id)).ToList();
            return new ArmyMoveChoice
            {
                From = army.RegionId, To = target.RegionId,
                UnitIds = ids.Where(id => !characters.Contains(id)).ToList(), CharacterIds = characters,
                Route = option.ArmyEvent is { RouteMovement: true, SeparateRoutes: true } ? target.Route.ToList() : new List<string>()
            };
        }

        private readonly Dictionary<string, bool> viable = new(StringComparer.Ordinal);

        // The moves so far are a legal command, or more whole-Army moves make one (a card that must move twice).
        private bool Viable(IReadOnlyList<ArmyMoveChoice> done)
        {
            var key = Key(done);
            if (viable.TryGetValue(key, out var known))
                return known;
            viable[key] = false;
            return viable[key] = Legal(done) ||
                done.Count < moves && Choices(done).Any(c => Viable(done.Append(Move(c.Army, c.Target, c.Ids)).ToList()));
        }

        private static string Key(IReadOnlyList<ArmyMoveChoice> done) =>
            string.Join("|", done.Select(m => m.From + ">" + m.To + ":" + string.Join(",", m.UnitIds.Concat(m.CharacterIds).Order())));

        private bool Legal(IReadOnlyList<ArmyMoveChoice> done)
        {
            var key = Key(done);
            return judged.TryGetValue(key, out var legal) ? legal : judged[key] = context.Legal(Command(done));
        }

        private GameCommand Command(IReadOnlyList<ArmyMoveChoice> done)
        {
            var payload = new CommandPayload
            {
                DieId = cost.DieId, MovementCost = cost.MovementCost, AbilityId = cost.AbilityId,
                Moves = done.Select(m => new ArmyMoveChoice
                {
                    From = m.From, To = m.To, UnitIds = m.UnitIds.ToList(), CharacterIds = m.CharacterIds.ToList(), Route = m.Route.ToList()
                }).ToList()
            };
            // A card's single route (Rohan's passage, a Companion's march) is the command's own.
            if (option.ArmyEvent is { RouteMovement: true, SeparateRoutes: false } && done.Count == 1)
                payload.Route = option.Armies.SelectMany(a => a.Targets).FirstOrDefault(t => t.RegionId == done[0].To)?.Route.ToList() ?? new();
            return context.Command(kind, payload);
        }
    }
}
