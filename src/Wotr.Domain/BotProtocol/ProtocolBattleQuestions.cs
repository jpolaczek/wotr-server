using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain.BotProtocol;

// BOT-225: a battle's questions: COMBAT_CARD, CEASE_ATTACK, EXTEND_SIEGE, RETREAT and FIELD_OR_SIEGE (SPEC §5). The
// context names the battle (its region and round) as the bot keeps one plan per battle.
public static class ProtocolBattleQuestions
{
    public static JsonObject? Context(ProtocolContext context) => context.View.Battle is not { } battle ? null : new JsonObject
    {
        ["region"] = context.Region(battle.RegionId),
        ["round"] = Math.Max(1, battle.Round),
        ["attacker"] = ProtocolVocabulary.Side(battle.Attacker.Side)
    };

    // A Combat card for this round, or none.
    public static IEnumerable<ProtocolPath> CombatCard(ProtocolContext context)
    {
        if (context.Option(CommandKind.CommitCombatCard) is not { } option)
            yield break;
        var battle = Context(context);
        foreach (var path in context.One("COMBAT_CARD", ProtocolAtom.Of("none"), "No Combat card",
                     context.Command(CommandKind.CommitCombatCard), battle))
            yield return path;
        foreach (var card in option.EligibleIds)
        {
            var command = new[]
            {
                context.Command(CommandKind.CommitCombatCard, new CommandPayload { Ids = new List<string> { card } }),
                context.Command(CommandKind.CommitCombatCard, new CommandPayload { CardId = card })
            }.FirstOrDefault(context.Legal);
            if (command is not null)
                yield return new ProtocolPath(new ProtocolStep("COMBAT_CARD", ProtocolAtom.Of("combat_card", ("card", context.Card(card))),
                    "Play " + CardCatalog.Title(card), battle), command);
        }
    }

    // The attacker continues the battle or ceases it.
    public static IEnumerable<ProtocolPath> Continue(ProtocolContext context) =>
        context.One("CEASE_ATTACK", ProtocolAtom.Of("continue"), "Continue the attack", context.Command(CommandKind.ContinueBattle), Context(context))
            .Concat(context.One("CEASE_ATTACK", ProtocolAtom.Of("cease"), "Cease the attack", context.Command(CommandKind.StopBattle), Context(context)));

    // The besieger reduces an Elite to fight another round, or stops.
    public static IEnumerable<ProtocolPath> Siege(ProtocolContext context)
    {
        var battle = Context(context);
        foreach (var path in context.One("EXTEND_SIEGE", ProtocolAtom.Of("stop"), "Stop the siege", context.Command(CommandKind.StopBattle), battle))
            yield return path;
        if (context.Option(CommandKind.ExtendSiege) is not { } option)
            yield break;
        foreach (var group in option.EligibleIds.GroupBy(id => context.Unit(id)?.Nation ?? ""))
        {
            var command = context.Command(CommandKind.ExtendSiege, new CommandPayload { Ids = new List<string> { group.First() } });
            foreach (var path in context.One("EXTEND_SIEGE", ProtocolAtom.Of("extend", ("nation", context.Nation(group.Key))),
                         "Reduce an Elite to continue the siege", command, battle))
                yield return path;
        }
    }

    // The defender retreats to a region, or stands.
    public static IEnumerable<ProtocolPath> Retreat(ProtocolContext context)
    {
        var battle = Context(context);
        foreach (var path in context.One("RETREAT", ProtocolAtom.Of("stand"), "Stand and fight on",
                     context.Command(CommandKind.HoldBattle), battle))
            yield return path;
        foreach (var region in context.Option(CommandKind.RetreatBattle)?.Targets ?? new List<string>())
            foreach (var path in context.One("RETREAT", ProtocolAtom.Of("retreat", ("region", context.Region(region))), "Retreat to " + region,
                         context.Command(CommandKind.RetreatBattle, new CommandPayload { RegionId = region }), battle))
                yield return path;
    }

    // The defender of a Stronghold fights in the field or withdraws into it.
    public static IEnumerable<ProtocolPath> FieldOrSiege(ProtocolContext context)
    {
        var battle = Context(context);
        foreach (var path in context.One("FIELD_OR_SIEGE", ProtocolAtom.Of("field"), "Fight a field battle",
                     context.Command(CommandKind.FightFieldBattle), battle))
            yield return path;
        var siege = context.Option(CommandKind.RetreatIntoSiege);
        if (siege is null)
            yield break;
        var command = (siege.Targets.Count > 0
                ? siege.Targets.Select(r => context.Command(CommandKind.RetreatIntoSiege, new CommandPayload { RegionId = r }))
                : Enumerable.Empty<GameCommand>())
            .Append(context.Command(CommandKind.RetreatIntoSiege)).FirstOrDefault(context.Legal);
        if (command is not null)
            yield return new ProtocolPath(new ProtocolStep("FIELD_OR_SIEGE", ProtocolAtom.Of("siege"), "Withdraw into the Stronghold", battle), command);
    }
}
