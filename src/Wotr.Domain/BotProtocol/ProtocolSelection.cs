using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain.BotProtocol;

// BOT-225: which figures of a group go (SPEC §5 ARMY_SELECTION): one question per Nation and unit kind with the absolute
// count, then a 0-or-1 question per Character, as the bot answers them. A count is offered only when some choice of the
// questions still to come completes a command the engine accepts, so the bot never answers into a dead end.
public sealed class ProtocolSelection
{
    private readonly ProtocolContext context;
    private readonly List<Group> groups;
    private readonly Func<IReadOnlyList<string>, GameCommand> complete;
    private readonly JsonObject frame;
    private readonly bool rearguard;
    private readonly Func<IReadOnlyList<string>, bool>? viable;
    private readonly Dictionary<string, bool> judged = new(StringComparer.Ordinal);

    private sealed record Group(string? Nation, string? Unit, string? Character, IReadOnlyList<string> Ids, bool Army);

    // [ids] are the figures that may go; [complete] makes the command for a chosen set; [frame] holds the question's
    // context (from, to). A rearguard question (an attack) also names how many of each kind are there. [viable] judges a
    // set when more answers follow it (a card's next Army move); by default the set's own command must be legal.
    public ProtocolSelection(ProtocolContext context, IEnumerable<string> ids, Func<IReadOnlyList<string>, GameCommand> complete,
        JsonObject frame, bool rearguard = false, Func<IReadOnlyList<string>, bool>? viable = null)
    {
        this.viable = viable;
        this.context = context;
        this.complete = complete;
        this.frame = frame;
        this.rearguard = rearguard;
        groups = Groups(context, ids.Distinct().ToList());
    }

    public bool Legal(IReadOnlyList<string> ids)
    {
        var key = string.Join(",", ids.Order(StringComparer.Ordinal));
        return judged.TryGetValue(key, out var legal) ? legal : judged[key] = viable?.Invoke(ids) ?? context.Legal(complete(ids));
    }

    // Some set of figures completes a legal command.
    public bool Possible() => groups.Count > 0 && Witness(0, Array.Empty<int>());

    public ProtocolNode? First() => groups.Count == 0 ? null : Node(Array.Empty<int>());

    private ProtocolNode Node(IReadOnlyList<int> counts)
    {
        var index = counts.Count;
        var group = groups[index];
        var branches = new List<ProtocolBranch>();
        var limit = group.Character is null ? group.Ids.Count : 1;
        for (var count = 0; count <= limit; count++)
        {
            var chosen = counts.Append(count).ToList();
            if (!Witness(chosen.Count, chosen))
                continue;
            var atom = group.Character is null
                ? ProtocolAtom.Of("count", ("count", count), ("nation", group.Nation), ("unit", group.Unit))
                : ProtocolAtom.Of("count", ("count", count), ("character", group.Character));
            var text = group.Character is null ? $"{count} {group.Unit} of {group.Nation}" : (count == 1 ? "Takes " : "Leaves ") + group.Character;
            branches.Add(chosen.Count == groups.Count
                ? new ProtocolBranch(atom, text, complete(Ids(chosen)))
                : new ProtocolBranch(atom, text, () => Node(chosen)));
        }
        var question = (JsonObject)frame.DeepClone();
        if (group.Character is null)
        {
            question["nation"] = group.Nation;
            question["unit"] = group.Unit;
        }
        else
            question["character"] = group.Character;
        if (rearguard)
            question["available"] = limit;
        return new ProtocolNode("ARMY_SELECTION", question, branches);
    }

    // Completions of the questions after [counts]: all of the rest, none of it, all but the Characters, and all but one
    // figure of each army kind (the one a lone Leader may need behind).
    private bool Witness(int answered, IReadOnlyList<int> counts)
    {
        var rest = Enumerable.Range(answered, groups.Count - answered).ToList();
        IEnumerable<IReadOnlyList<int>> Completions()
        {
            yield return counts.Concat(rest.Select(i => Max(i))).ToList();
            yield return counts.Concat(rest.Select(_ => 0)).ToList();
            yield return counts.Concat(rest.Select(i => groups[i].Character is null ? Max(i) : 0)).ToList();
            foreach (var skip in rest.Where(i => groups[i].Army && Max(i) > 0))
                yield return counts.Concat(rest.Select(i => i == skip ? Max(i) - 1 : Max(i))).ToList();
            foreach (var only in rest.Where(i => groups[i].Army && Max(i) > 0))
                yield return counts.Concat(rest.Select(i => i == only ? 1 : 0)).ToList();
        }
        return Completions().Any(full => full.Sum() > 0 && Legal(Ids(full)));
    }

    private int Max(int index) => groups[index].Character is null ? groups[index].Ids.Count : 1;

    private IReadOnlyList<string> Ids(IReadOnlyList<int> counts) =>
        counts.SelectMany((count, index) => groups[index].Ids.Take(count)).ToList();

    // Army units by Nation and kind (Regulars and Elites first, then Leaders and Nazgûl), then each Character.
    private static List<Group> Groups(ProtocolContext context, IReadOnlyList<string> ids)
    {
        var units = ids.Select(id => context.Unit(id)).Where(u => u is not null).Select(u => u!).ToList();
        var groups = units.GroupBy(u => (u.Nation, u.Kind))
            .OrderBy(g => g.Key.Kind is UnitKind.Regular or UnitKind.Elite ? 0 : 1).ThenBy(g => g.Key.Nation, StringComparer.Ordinal)
            .ThenBy(g => g.Key.Kind)
            .Select(g => new Group(ProtocolVocabulary.Nation(g.Key.Nation), ProtocolVocabulary.Unit(g.Key.Kind), null,
                g.Select(u => u.Id).ToList(), g.Key.Kind is UnitKind.Regular or UnitKind.Elite))
            .ToList();
        foreach (var id in ids.Where(id => units.All(u => u.Id != id)))
        {
            var definition = context.View.Characters.FirstOrDefault(c => c.Id == id)?.DefinitionId ?? id;
            groups.Add(new Group(null, null, ProtocolVocabulary.Character(definition), new[] { id }, false));
        }
        return groups;
    }

    public static string Title(string id) => CharacterCatalog.Title(id);
}
