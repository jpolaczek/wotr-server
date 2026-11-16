using System.Text.Json.Nodes;
using Wotr.Contracts;

namespace Wotr.Domain.BotProtocol;

// What a family of questions reads: the deciding side's projection (its options and board) and a judge of complete
// commands (the engine's validation of the real decision). Every path offered is a command the judge accepts.
public sealed class ProtocolContext(PlayerView view, Func<GameCommand, bool> legal)
{
    public PlayerView View { get; } = view;
    public string Side => ProtocolVocabulary.Side(View.You);

    public bool Legal(GameCommand command) => legal(command);

    public IEnumerable<ActionOption> Options(CommandKind kind) => View.Options.Where(o => o.Enabled && o.Kind == kind);

    public ActionOption? Option(CommandKind kind) => Options(kind).FirstOrDefault();

    public GameCommand Command(CommandKind kind, CommandPayload? payload = null) => new()
    {
        MatchId = View.MatchId,
        CommandId = "protocol-" + View.Revision,
        ExpectedRevision = View.Revision,
        DecisionId = View.Decision?.Id ?? "",
        Kind = kind,
        Payload = payload ?? new CommandPayload()
    };

    // A single-step path when its command is legal.
    public IEnumerable<ProtocolPath> One(string question, JsonObject atom, string text, GameCommand command, JsonObject? context = null)
    {
        if (Legal(command))
            yield return new ProtocolPath(new ProtocolStep(question, atom, text, context), command);
    }

    public string Region(string id) => ProtocolVocabulary.Region(id);
    public string Card(string id) => ProtocolVocabulary.Card(id);
    public string Character(string id) => ProtocolVocabulary.Character(id);
    public string Nation(string id) => ProtocolVocabulary.Nation(id);
    public UnitView? Unit(string id) => View.Units.FirstOrDefault(u => u.Id == id);
}

public static class ProtocolAtom
{
    public static JsonObject Of(string kind, params (string Key, JsonNode? Value)[] fields)
    {
        var atom = new JsonObject { ["kind"] = kind };
        foreach (var (key, value) in fields)
            atom[key] = value;
        return atom;
    }
}

// BOT-225: the protocol's questions for our current decision (SPEC §4-§5). Null when our decision has no question in
// protocol 0.1's base game: the caller reports it (BOT-221/BOT-222 close every such gap).
public static class ProtocolQuestions
{
    public static ProtocolNode? For(PlayerView view, Func<GameCommand, bool> legal)
    {
        if (view.Decision is null || view.DecisionOwner != view.You)
            return null;
        var context = new ProtocolContext(view, legal);
        if (Forced(context) is { } forced)
            return forced;
        var paths = Paths(context).ToList();
        return ProtocolTree.From(paths);
    }

    private static IEnumerable<ProtocolPath> Paths(ProtocolContext context) => context.View.Decision!.Kind switch
    {
        DecisionKind.AllocateHunt => ProtocolHuntQuestions.Allocation(context),
        DecisionKind.Guide => ProtocolHuntQuestions.Guide(context),
        DecisionKind.HuntDamage => ProtocolHuntQuestions.Damage(context),
        DecisionKind.FellowshipRoute => ProtocolHuntQuestions.Reveal(context),
        DecisionKind.FellowshipPhase => ProtocolHuntQuestions.FellowshipPhase(context),
        DecisionKind.CombatCard => ProtocolBattleQuestions.CombatCard(context),
        DecisionKind.BattleContinue => ProtocolBattleQuestions.Continue(context),
        DecisionKind.SiegeContinuation => ProtocolBattleQuestions.Siege(context),
        DecisionKind.BattleRetreat => ProtocolBattleQuestions.Retreat(context),
        DecisionKind.StrongholdDefense => ProtocolBattleQuestions.FieldOrSiege(context),
        DecisionKind.DiscardCards => ProtocolCardQuestions.Discard(context),
        DecisionKind.Action => ProtocolActionQuestions.For(context),
        DecisionKind.Losses => ProtocolLossQuestions.Casualties(context),
        DecisionKind.RemoveExcess => ProtocolLossQuestions.Excess(context),
        DecisionKind.BattleAdvance => ProtocolArmyQuestions.Advance(context),
        DecisionKind.ArmyEvent => ProtocolArmyQuestions.Event(context),
        DecisionKind.CharacterEvent => ProtocolCharacterQuestions.MinionEvent(context),
        DecisionKind.CompanionSeparation => ProtocolCharacterQuestions.Separation(context),
        DecisionKind.EffectChoice => ProtocolEffectQuestions.Choice(context),
        DecisionKind.Reaction => ProtocolEffectQuestions.Reaction(context),
        _ => Enumerable.Empty<ProtocolPath>()
    };

    // Decisions whose answer the rules fix: a reroll can only add hits, so every miss the leaders, Nazgûl or abilities
    // allow is rerolled (protocol 0.1 has no reroll question: its referee rerolls by rule); an action sequence's finish.
    private static ProtocolNode? Forced(ProtocolContext context)
    {
        var kind = context.View.Decision!.Kind;
        GameCommand? command = kind switch
        {
            DecisionKind.BattleReroll => Reroll(context, CommandKind.RerollBattle),
            DecisionKind.HuntReroll => Reroll(context, CommandKind.RerollHunt),
            DecisionKind.ActionSequence => context.Command(CommandKind.FinishActionSequence),
            _ => null
        };
        if (command is null || !context.Legal(command))
            return null;
        return new ProtocolNode("REROLL", null, new[] { new ProtocolBranch(ProtocolAtom.Of("reroll"), "Reroll", command) });
    }

    private static GameCommand? Reroll(ProtocolContext context, CommandKind kind) => context.Option(kind) is { } option
        ? context.Command(kind, new CommandPayload { Ids = option.EligibleIds.Take(Math.Max(0, option.Maximum)).ToList() })
        : null;
}
