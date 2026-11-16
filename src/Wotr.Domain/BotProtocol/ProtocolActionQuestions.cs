using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain.BotProtocol;

// BOT-225: the ACTION question (SPEC §5): which die and which action with it, or pass, skip a die, or use an Elven Ring.
// Actions that need more answers (an Army's move, a recruitment, a Companion's or a Minion's move) open their own
// questions lazily, only for the option the bot takes.
public static class ProtocolActionQuestions
{
    public const string Question = "ACTION";

    public static IEnumerable<ProtocolPath> For(ProtocolContext context) =>
        Pass(context)
            .Concat(SkipDie(context))
            .Concat(ElvenRing(context))
            .Concat(Draw(context))
            .Concat(Diplomacy(context))
            .Concat(ProtocolRecruitQuestions.Actions(context))
            .Concat(ProtocolArmyQuestions.MoveActions(context))
            .Concat(ProtocolArmyQuestions.AttackActions(context))
            .Concat(PlayCard(context))
            .Concat(EnterCharacter(context))
            .Concat(Abilities(context))
            .Concat(Fellowship(context))
            .Concat(ProtocolCharacterQuestions.Actions(context))
            .Concat(ForceDiscard(context));

    // use_die with the die's index among its side's dice (SPEC §7), its current face and the action's fields.
    public static JsonObject UseDie(ProtocolContext context, string dieId, string action, params (string Key, JsonNode? Value)[] fields)
    {
        var atom = ProtocolAtom.Of("use_die", ("die", ProtocolView.DieIndex(context.View, dieId)), ("face", Face(context, dieId)),
            ("action", action));
        foreach (var (key, value) in fields)
            atom[key] = value;
        return atom;
    }

    public static string? Face(ProtocolContext context, string dieId) =>
        context.View.Dice.FirstOrDefault(d => d.Id == dieId)?.Face is { } face ? ProtocolVocabulary.Face(face) : null;

    public static string DieText(ProtocolContext context, string dieId) =>
        $"Die {ProtocolView.DieIndex(context.View, dieId)} ({Face(context, dieId) ?? "?"})";

    private static IEnumerable<ProtocolPath> Pass(ProtocolContext context) =>
        context.Option(CommandKind.Pass) is null ? Enumerable.Empty<ProtocolPath>()
            : context.One(Question, ProtocolAtom.Of("pass"), "Pass", context.Command(CommandKind.Pass));

    private static IEnumerable<ProtocolPath> SkipDie(ProtocolContext context) =>
        (context.Option(CommandKind.DiscardDie)?.EligibleDice ?? new List<string>()).SelectMany(die => context.One(Question,
            ProtocolAtom.Of("skip_die", ("die", ProtocolView.DieIndex(context.View, die)), ("face", Face(context, die))),
            DieText(context, die) + ": use it for nothing", context.Command(CommandKind.DiscardDie, new CommandPayload { DieId = die })));

    // An Elven Ring turns a die to another face; the same player then acts again.
    private static IEnumerable<ProtocolPath> ElvenRing(ProtocolContext context)
    {
        if (context.Option(CommandKind.UseRing) is not { } option || option.EligibleIds.Count == 0)
            yield break;
        var ring = option.EligibleIds[0];
        foreach (var die in option.EligibleDice)
            foreach (var face in option.Faces.Where(f => context.View.Dice.FirstOrDefault(d => d.Id == die)?.Face != f))
                foreach (var path in context.One(Question,
                             ProtocolAtom.Of("elven_ring", ("die", ProtocolView.DieIndex(context.View, die)), ("from", Face(context, die)),
                                 ("to", ProtocolVocabulary.Face(face))),
                             DieText(context, die) + ": an Elven Ring turns it to " + ProtocolVocabulary.Face(face),
                             context.Command(CommandKind.UseRing, new CommandPayload { RingId = ring, DieId = die, Face = face })))
                    yield return path;
    }

    private static IEnumerable<ProtocolPath> Draw(ProtocolContext context)
    {
        if (context.Option(CommandKind.DrawCard) is not { } option)
            yield break;
        foreach (var cost in option.DrawCosts)
            foreach (var deck in option.Targets)
                foreach (var path in context.One(Question,
                             UseDie(context, cost.DieId, "draw_card", ("deck", ProtocolVocabulary.Deck(deck))),
                             DieText(context, cost.DieId) + ": draw a " + deck + " card",
                             context.Command(CommandKind.DrawCard, new CommandPayload { DieId = cost.DieId, AbilityId = cost.AbilityId, Deck = deck })))
                    yield return path;
    }

    private static IEnumerable<ProtocolPath> Diplomacy(ProtocolContext context)
    {
        if (context.Option(CommandKind.AdvancePolitics) is not { } option)
            yield break;
        foreach (var die in option.EligibleDice)
            foreach (var nation in option.Targets)
                foreach (var path in context.One(Question, UseDie(context, die, "diplomacy", ("nation", context.Nation(nation))),
                             DieText(context, die) + ": advance " + nation + " on the Political Track",
                             context.Command(CommandKind.AdvancePolitics, new CommandPayload { DieId = die, NationId = nation })))
                    yield return path;
    }

    // An event card with the die that pays it; the two base cards that name a Nation at play time ask it next.
    private static IEnumerable<ProtocolPath> PlayCard(ProtocolContext context)
    {
        if (context.Option(CommandKind.PlayEvent) is not { } option)
            yield break;
        foreach (var play in option.Events)
            foreach (var cost in play.Costs)
            {
                var atom = UseDie(context, cost.DieId, "play_card", ("card", context.Card(play.CardId)), ("as", ProtocolVocabulary.Face(cost.Face)));
                var text = DieText(context, cost.DieId) + ": play " + CardCatalog.Title(play.CardId);
                CommandPayload Payload(string nation) => new()
                {
                    CardId = play.CardId, DieId = cost.DieId, CardCost = cost.Face, AbilityId = cost.AbilityId, NationId = nation
                };
                if (play.NationTargets.Count == 0)
                {
                    foreach (var path in context.One(Question, atom, text, context.Command(CommandKind.PlayEvent, Payload(""))))
                        yield return path;
                    continue;
                }
                var nations = play.NationTargets.Select(n => (Nation: n, Command: context.Command(CommandKind.PlayEvent, Payload(n))))
                    .Where(n => context.Legal(n.Command)).ToList();
                if (nations.Count == 0)
                    continue;
                var question = new JsonObject
                {
                    ["card"] = context.Card(play.CardId), ["question"] = "Which Nation does " + CardCatalog.Title(play.CardId) + " target?"
                };
                yield return new ProtocolPath(new ProtocolStep(Question, atom, text), () => new ProtocolNode("CARD_CHOICE", question,
                    nations.Select(n => new ProtocolBranch(ProtocolAtom.Of("card_choice", ("card", context.Card(play.CardId)),
                        ("answer", context.Nation(n.Nation)), ("nation", context.Nation(n.Nation))), n.Nation, n.Command)).ToList()));
            }
    }

    // A Character entering play: Gandalf the White or Aragorn (Free Peoples), a Minion (Shadow).
    private static IEnumerable<ProtocolPath> EnterCharacter(ProtocolContext context)
    {
        if (context.Option(CommandKind.EnterCharacter) is not { } option)
            yield break;
        var action = context.View.You == Side.FreePeoples ? "play_companion" : "play_minion";
        foreach (var entry in option.CharacterActions)
            foreach (var die in entry.Dice)
                foreach (var region in entry.Targets.DefaultIfEmpty(""))
                {
                    var fields = new List<(string, JsonNode?)> { ("character", context.Character(entry.CharacterId)) };
                    if (region.Length > 0)
                        fields.Add(("region", context.Region(region)));
                    foreach (var path in context.One(Question, UseDie(context, die, action, fields.ToArray()),
                                 DieText(context, die) + ": " + CharacterCatalog.Title(entry.CharacterId) + " enters" +
                                 (region.Length > 0 ? " at " + region : ""),
                                 context.Command(CommandKind.EnterCharacter, new CommandPayload
                                 {
                                     CharacterId = entry.CharacterId, RegionId = region, DieId = die
                                 })))
                        yield return path;
                }
    }

    // A Companion's political ability (Boromir, Legolas, Gimli) and the Voice of Saruman. The Voice is a recruitment with a
    // Muster die (`use_die` "recruit") marked by its variant, as AlphaWOTR's engine plays it: "settlements" (an Isengard
    // Regular in each settlement) or "swap2" (Regulars in Orthanc become Elites); ProtocolHints reads which one it chose.
    private static IEnumerable<ProtocolPath> Abilities(ProtocolContext context)
    {
        if (context.Option(CommandKind.UseCharacterAbility) is not { } option)
            yield break;
        foreach (var ability in option.CharacterActions)
        {
            var definition = context.View.Characters.FirstOrDefault(c => c.Id == ability.CharacterId)?.DefinitionId ?? ability.CharacterId;
            var key = context.Character(definition);
            var voice = definition == "base.character.saruman";
            foreach (var die in ability.Dice)
                foreach (var targets in Choose(ability.Targets, Math.Max(1, ability.TargetCount)))
                {
                    var upgrade = ability.AbilityId == "voice-upgrade";
                    var fields = voice
                        ? new (string, JsonNode?)[] { ("nation", "ISENGARD"), ("saruman", upgrade ? "swap2" : "settlements") }
                        : new (string, JsonNode?)[] { ("character", key) };
                    var atom = UseDie(context, die, voice ? "recruit" : "companion_politics", fields);
                    if (voice)
                        atom[upgrade ? "region" : "placements"] = upgrade ? context.Region("orthanc")
                            : new JsonArray(targets.Select(t => (JsonNode)context.Region(t)!).ToArray());
                    foreach (var path in context.One(Question, atom, DieText(context, die) + ": " + CharacterCatalog.Title(definition) +
                                 " uses " + ability.AbilityId, context.Command(CommandKind.UseCharacterAbility, new CommandPayload
                                 {
                                     CharacterId = ability.CharacterId, AbilityId = ability.AbilityId, DieId = die, Ids = targets.ToList()
                                 })))
                        yield return path;
                }
        }
    }

    private static IEnumerable<ProtocolPath> Fellowship(ProtocolContext context)
    {
        foreach (var die in context.Option(CommandKind.MoveFellowship)?.EligibleDice ?? new List<string>())
            foreach (var path in context.One(Question, UseDie(context, die, "move_fellowship"), DieText(context, die) + ": move the Fellowship",
                         context.Command(CommandKind.MoveFellowship, new CommandPayload { DieId = die })))
                yield return path;
        var strider = context.View.Fellowship.Guide == "base.character.strider";
        foreach (var die in context.Option(CommandKind.HideFellowship)?.EligibleDice ?? new List<string>())
        {
            var face = context.View.Dice.FirstOrDefault(d => d.Id == die)?.Face;
            var action = strider && face != DieFace.Character ? "hide_fellowship_strider" : "hide_fellowship";
            foreach (var path in context.One(Question, UseDie(context, die, action), DieText(context, die) + ": hide the Fellowship",
                         context.Command(CommandKind.HideFellowship, new CommandPayload { DieId = die })))
                yield return path;
        }
    }

    // The Shadow forces a protecting card off the table: a die and two cards from the hand, one Army and one Character
    // event, discarded face down.
    private static IEnumerable<ProtocolPath> ForceDiscard(ProtocolContext context)
    {
        if (context.Option(CommandKind.DiscardProtection) is not { } option)
            yield break;
        var hand = option.EligibleIds;
        foreach (var die in option.EligibleDice.Take(1))
            foreach (var card in option.Targets)
                foreach (var pair in Choose(hand, 2))
                    foreach (var path in context.One(Question, UseDie(context, die, "force_discard", ("card", context.Card(card)),
                                     ("any_die", true), ("elven_ring", false),
                                     ("pay", new JsonArray(pair.Select(c => (JsonNode)context.Card(c)!).ToArray()))),
                                 DieText(context, die) + ": force " + CardCatalog.Title(card) + " off the table",
                                 context.Command(CommandKind.DiscardProtection, new CommandPayload { DieId = die, CardId = card, Ids = pair.ToList() })))
                        yield return path;
    }

    // Every unordered choice of [count] distinct items.
    public static IEnumerable<IReadOnlyList<string>> Choose(IReadOnlyList<string> items, int count, int start = 0)
    {
        if (count == 0)
        {
            yield return Array.Empty<string>();
            yield break;
        }
        for (var i = start; i <= items.Count - count; i++)
            foreach (var rest in Choose(items, count - 1, i + 1))
                yield return new[] { items[i] }.Concat(rest).ToList();
    }
}
