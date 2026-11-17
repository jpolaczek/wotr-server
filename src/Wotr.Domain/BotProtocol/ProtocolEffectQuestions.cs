using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain.BotProtocol;

// BOT-225: a card's own questions (SPEC §5 CARD_CHOICE: the card asking, the answer in the referee's words and a typed
// field) and the reaction windows: Gandalf the White's charge (WHITE_RIDER), the Hobbit Guides' and Gollum's abilities
// (GUIDE_ABILITY), the bonus draws of Gandalf the Grey and the Witch-king (CARD_CHOICE draw), and the table cards' answers.
public static class ProtocolEffectQuestions
{
    private static readonly HashSet<string> Regions = BoardCatalog.LoadBase().Regions.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);

    // A card's choice of targets: one pick per question, the next picks only while the card allows more.
    public static IEnumerable<ProtocolPath> Choice(ProtocolContext context)
    {
        if (context.Option(CommandKind.ChooseEffect) is not { } option)
            return Enumerable.Empty<ProtocolPath>();
        if (option.EventRecruitment is not null)
            return ProtocolCardRecruitQuestions.For(context, option);
        var plan = new ChoicePlan(context, option);
        var node = plan.First();
        return node.Branches.Select(b => b.Terminal
            ? new ProtocolPath(new ProtocolStep(node.Question, b.Atom, b.Text, node.Context), b.Command!)
            : new ProtocolPath(new ProtocolStep(node.Question, b.Atom, b.Text, node.Context), () => b.Next));
    }

    public static JsonObject Question(ProtocolContext context, string card, string question) => new()
    {
        ["card"] = card.Length > 0 ? context.Card(card) : null, ["question"] = question
    };

    // The answer's words and typed field for one of our choice ids: a figure, a character, a region, a card or a word.
    public static JsonObject Atom(ProtocolContext context, string card, string id, EffectChoiceView? view)
    {
        var atom = ProtocolAtom.Of("card_choice", ("card", card.Length > 0 ? context.Card(card) : null));
        if (context.Unit(id) is { } unit && !Regions.Contains(unit.Location))
        {
            // A reserve figure a card recruits: the bare unit word (AlphaWOTR matches it against the figures its engine
            // placed), the destination as the region.
            atom["answer"] = ProtocolVocabulary.Unit(unit.Kind).ToUpperInvariant();
            if (view?.TargetId is { Length: > 0 } target && Regions.Contains(target))
                atom["region"] = context.Region(target);
            atom["unit"] = ProtocolVocabulary.Unit(unit.Kind);
            atom["nation"] = context.Nation(unit.Nation);
        }
        else if (context.Unit(id) is { } placed)
        {
            var region = context.Region(placed.Location);
            atom["answer"] = region + ":" + ProtocolVocabulary.Unit(placed.Kind).ToUpperInvariant();
            atom["region"] = region;
            atom["unit"] = ProtocolVocabulary.Unit(placed.Kind);
            atom["nation"] = context.Nation(placed.Nation);
        }
        else if (CharacterCatalog.All.ContainsKey(id) || context.View.Characters.Any(c => c.Id == id))
        {
            var key = context.Character(context.View.Characters.FirstOrDefault(c => c.Id == id)?.DefinitionId ?? id);
            atom["answer"] = key;
            atom["character"] = key;
        }
        else if (Regions.Contains(id))
        {
            atom["answer"] = context.Region(id);
            atom["region"] = context.Region(id);
        }
        else if (CardCatalog.All.ContainsKey(id))
        {
            atom["answer"] = context.Card(id);
            atom["target_card"] = context.Card(id);
        }
        else
        {
            var word = id[(id.LastIndexOf('.') + 1)..];
            atom["answer"] = word;
            if (view?.TargetId is { Length: > 0 } target && Regions.Contains(target))
                atom["region"] = context.Region(target);
            if (int.TryParse(word, out var count))
                atom["count"] = count;
            if (word is "decline" or "skip" or "none")
                atom["skip"] = true;
        }
        return atom;
    }

    // Picks of a card's choice in any order; a pick is offered only when some completion of it is legal.
    private sealed class ChoicePlan(ProtocolContext context, ActionOption option)
    {
        private readonly string card = context.View.Decision?.SourceId ?? "";
        private readonly Dictionary<string, bool> judged = new(StringComparer.Ordinal);

        public ProtocolNode First() => Node(new List<string>());

        private ProtocolNode Node(IReadOnlyList<string> picked)
        {
            var branches = new List<ProtocolBranch>();
            if (picked.Count >= option.Minimum && (picked.Count > 0 || option.Minimum == 0) && Legal(picked))
            {
                var decline = ProtocolAtom.Of("card_choice", ("card", card.Length > 0 ? context.Card(card) : null),
                    ("answer", picked.Count == 0 ? "skip" : "done"), ("skip", true));
                // Where the card's answers are counts, declining is the count 0 (AlphaWOTR needs a count on every option).
                if (picked.Count == 0 && option.EligibleIds.Any(id => int.TryParse(id[(id.LastIndexOf('.') + 1)..], out _)))
                    decline["count"] = 0;
                branches.Add(new ProtocolBranch(decline, picked.Count == 0 ? "None" : "Done", Command(picked)));
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (picked.Count < Math.Max(1, option.Maximum))
                foreach (var id in option.EligibleIds.Where(id => !picked.Contains(id)))
                {
                    var next = picked.Append(id).ToList();
                    if (!Completable(next))
                        continue;
                    var view = option.EffectChoices.FirstOrDefault(c => c.Id == id);
                    var atom = Atom(context, card, id, view);
                    // Figures alike (two Regulars of one Nation in the reserve) are one answer.
                    if (!seen.Add(ProtocolJson.Write(atom)))
                        continue;
                    var text = view?.Title is { Length: > 0 } title ? title + (view.Detail.Length > 0 ? ": " + view.Detail : "") : id;
                    var last = next.Count >= Math.Max(1, option.Maximum) || !Extendable(next);
                    branches.Add(last && Legal(next) ? new ProtocolBranch(atom, text, Command(next)) : new ProtocolBranch(atom, text, () => Node(next)));
                }
            // The question names what the card asks in words (a retreat, a target), as the bot reads them.
            var words = string.Join(" ", new[] { context.View.Decision?.Presentation ?? "" }.Concat(option.EffectChoices
                .Select(c => c.Title).Where(t => t.Length > 0).Distinct().Take(3))).ToLowerInvariant();
            // A recruitment's destination and Nation as words of the question (AlphaWOTR reads them so).
            var reserve = option.EligibleIds.Select(id => (Unit: context.Unit(id), View: option.EffectChoices.FirstOrDefault(c => c.Id == id)))
                .FirstOrDefault(u => u.Unit is { } x && !Regions.Contains(x.Location) && u.View?.TargetId is { Length: > 0 } t && Regions.Contains(t));
            if (reserve.Unit is { } recruit)
                words += " in " + context.Region(reserve.View!.TargetId) + " for " + context.Nation(recruit.Nation);
            var frame = Question(context, card, CardCatalog.Title(card) + ": choose (" + words + ")");
            if (context.View.Battle is not null)
                foreach (var (key, value) in ProtocolBattleQuestions.Context(context)!)
                    frame[key] = value?.DeepClone();
            return new ProtocolNode("CARD_CHOICE", frame, branches);
        }

        // The picks themselves, or with one or two more, make a legal choice.
        private bool Completable(IReadOnlyList<string> picked)
        {
            if (Legal(picked))
                return true;
            var rest = option.EligibleIds.Where(id => !picked.Contains(id)).ToList();
            if (picked.Count < option.Maximum && rest.Any(id => Legal(picked.Append(id).ToList())))
                return true;
            return picked.Count + 1 < option.Maximum && rest.Any(a => rest.Where(b => b != a).Any(b => Legal(picked.Append(a).Append(b).ToList())));
        }

        private bool Extendable(IReadOnlyList<string> picked) =>
            option.EligibleIds.Where(id => !picked.Contains(id)).Any(id => Completable(picked.Append(id).ToList()));

        private bool Legal(IReadOnlyList<string> picked)
        {
            var key = string.Join(",", picked);
            return judged.TryGetValue(key, out var legal) ? legal : judged[key] = context.Legal(Command(picked));
        }

        private GameCommand Command(IReadOnlyList<string> picked) =>
            context.Command(CommandKind.ChooseEffect, new CommandPayload { Ids = picked.ToList() });
    }

    // A reaction window: one question for the reactions this side may use now, or decline.
    public static IEnumerable<ProtocolPath> Reaction(ProtocolContext context)
    {
        var decline = context.Command(CommandKind.DeclineReaction);
        var respond = context.Option(CommandKind.RespondEffect);
        var ids = respond?.EligibleIds ?? new List<string>();
        var reactions = ids.Select(id => (Id: id, Command: context.Command(CommandKind.RespondEffect, new CommandPayload { ReactionId = id })))
            .Where(r => context.Legal(r.Command)).ToList();
        var declined = context.Legal(decline);
        if (reactions.Count == 1 && reactions[0].Id == "character.white-rider")
            return Two(context, "WHITE_RIDER", reactions[0].Command, declined ? decline : null, null);
        if (reactions.Count == 1 && reactions[0].Id is "character.merry-guide" or "character.pippin-guide" or "character.gollum-reveal")
            return Two(context, "GUIDE_ABILITY", reactions[0].Command, declined ? decline : null,
                new JsonObject { ["damage"] = context.View.Hunt?.Damage ?? 0, ["ability"] = reactions[0].Id });
        if (reactions.Count == 1 && reactions[0].Id is "character.grey-draw" or "character.sorcerer")
            return Draw(context, reactions[0].Id, reactions[0].Command, declined ? decline : null);
        var paths = new List<ProtocolPath>();
        var question = Question(context, context.View.Decision?.SourceId ?? "", "Use a card's or a character's answer now?");
        // A Hunt window's damage (Axe and Bow, Horn of Gondor, Mithril Coat and Sting read it).
        if (context.View.Hunt is { } hunt)
        {
            question["damage"] = hunt.Damage;
            question["tile"] = hunt.TileId.Length > 0 ? ProtocolVocabulary.HuntTile(hunt.TileId) : null;
        }
        foreach (var (id, command) in reactions)
        {
            var view = respond!.EffectChoices.FirstOrDefault(c => c.Id == id);
            var source = view?.SourceId ?? "";
            var atom = ProtocolAtom.Of("card_choice", ("card", source.Length > 0 ? context.Card(source) : null), ("answer", "yes"), ("yes", true));
            if (source.Length > 0 && CardCatalog.All.ContainsKey(source))
                atom["target_card"] = context.Card(source);
            atom["reaction"] = id;
            paths.Add(new ProtocolPath(new ProtocolStep("CARD_CHOICE", atom, view?.Title ?? id, question), command));
        }
        if (declined && paths.Count > 0)
            paths.Add(new ProtocolPath(new ProtocolStep("CARD_CHOICE", ProtocolAtom.Of("card_choice", ("card", null), ("answer", "skip"),
                ("skip", true)), "Decline", question), decline));
        // The Ents' follow-up: a Character event card played free, or none.
        if (context.Option(CommandKind.PlayEvent) is { } play)
        {
            var ents = Question(context, context.View.Decision?.SourceId ?? "", "Play a Character event card for free?");
            foreach (var card in play.Events.Select(e => e.CardId).Distinct())
                foreach (var path in context.One("CARD_CHOICE", ProtocolAtom.Of("card_choice", ("card", context.Card(context.View.Decision?.SourceId ?? "")),
                             ("answer", context.Card(card)), ("target_card", context.Card(card))), "Play " + CardCatalog.Title(card),
                             context.Command(CommandKind.PlayEvent, new CommandPayload { CardId = card, CardCost = DieFace.Character }), ents))
                    paths.Add(path);
            if (declined && paths.All(p => p.Command != decline))
                paths.Add(new ProtocolPath(new ProtocolStep("CARD_CHOICE", ProtocolAtom.Of("card_choice", ("card", null), ("answer", "skip"),
                    ("skip", true)), "Play none", ents), decline));
        }
        // A window whose only answer is to decline (no reaction, no card that may be played): forced, asked of nobody.
        if (paths.Count == 0 && declined)
            paths.Add(new ProtocolPath(new ProtocolStep("CARD_CHOICE", ProtocolAtom.Of("card_choice", ("card", null), ("answer", "skip"),
                ("skip", true)), "Decline", question), decline));
        return paths;
    }

    private static IEnumerable<ProtocolPath> Two(ProtocolContext context, string question, GameCommand use, GameCommand? skip, JsonObject? extra)
    {
        var frame = ProtocolBattleQuestions.Context(context);
        if (extra is not null)
        {
            frame ??= new JsonObject();
            foreach (var (key, value) in extra)
                frame[key] = value?.DeepClone();
        }
        yield return new ProtocolPath(new ProtocolStep(question, ProtocolAtom.Of("use"), "Use it", frame), use);
        if (skip is not null)
            yield return new ProtocolPath(new ProtocolStep(question, ProtocolAtom.Of("skip"), "Skip", frame), skip);
    }

    // A bonus draw: the deck it draws from, or none. Gandalf the Grey's is a yes or no in AlphaWOTR's engine (a draw from the
    // played card's deck), so it is a card_choice with yes; the Witch-king's stays a draw.
    private static IEnumerable<ProtocolPath> Draw(ProtocolContext context, string id, GameCommand use, GameCommand? skip)
    {
        var deck = id == "character.grey-draw" ? "character" : "strategy";
        var frame = new JsonObject { ["deck"] = deck, ["question"] = "Draw a bonus card?" };
        var draw = id == "character.grey-draw"
            ? ProtocolAtom.Of("card_choice", ("card", null), ("answer", "draw"), ("yes", true), ("deck", deck))
            : ProtocolAtom.Of("draw", ("deck", deck));
        yield return new ProtocolPath(new ProtocolStep("CARD_CHOICE", draw, "Draw a " + deck + " card", frame), use);
        if (skip is not null)
            yield return new ProtocolPath(new ProtocolStep("CARD_CHOICE", ProtocolAtom.Of("card_choice", ("card", null), ("answer", "skip"), ("skip", true)),
                "Do not draw", frame), skip);
    }
}
