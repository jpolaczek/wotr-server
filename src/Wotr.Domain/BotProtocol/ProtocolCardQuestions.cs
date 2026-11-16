using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain.BotProtocol;

// BOT-225: the hand's questions: DISCARD_EXCESS_CARD, one card per question in any order (SPEC §5).
public static class ProtocolCardQuestions
{
    public static IEnumerable<ProtocolPath> Discard(ProtocolContext context)
    {
        if (context.Option(CommandKind.DiscardCards) is not { } option)
            return Enumerable.Empty<ProtocolPath>();
        var count = Math.Max(option.Minimum, context.View.Decision!.RequiredCount);
        // The same cards in another order are the same command: judged once.
        var judged = new Dictionary<string, bool>(StringComparer.Ordinal);
        bool Legal(GameCommand command)
        {
            var key = string.Join(",", command.Payload.Ids.Order(StringComparer.Ordinal));
            return judged.TryGetValue(key, out var legal) ? legal : judged[key] = context.Legal(command);
        }
        return Orders(option.EligibleIds.Distinct().ToList(), count).Select(order =>
        {
            var command = context.Command(CommandKind.DiscardCards, new CommandPayload { Ids = order.ToList() });
            var steps = order.Select(card => new ProtocolStep("DISCARD_EXCESS_CARD",
                ProtocolAtom.Of("discard", ("card", context.Card(card))), "Discard " + CardCatalog.Title(card))).ToList();
            return new ProtocolPath(steps, command);
        }).Where(path => Legal(path.Command!));
    }

    // Every ordered choice of [count] distinct items.
    public static IEnumerable<IReadOnlyList<string>> Orders(IReadOnlyList<string> items, int count)
    {
        if (count <= 0)
        {
            yield return Array.Empty<string>();
            yield break;
        }
        foreach (var item in items)
            foreach (var rest in Orders(items.Where(i => i != item).ToList(), count - 1))
                yield return new[] { item }.Concat(rest).ToList();
    }
}
