using System.Text.Json.Nodes;
using Wotr.Contracts;

namespace Wotr.Domain.BotProtocol;

// BOT-225: one of our decisions asked as the protocol's atomic questions (SPEC §4). A node is one question: its kind, its
// context and its options (atoms). An option leads to the next question or, at the end, to one complete command the
// engine accepts. Every option is therefore a prefix of a legal command: a bot can never answer into a dead end.
public sealed class ProtocolNode(string question, JsonObject? context, IReadOnlyList<ProtocolBranch> branches)
{
    public string Question { get; } = question;
    public JsonObject? Context { get; } = context;
    public IReadOnlyList<ProtocolBranch> Branches { get; } = branches;
}

// An option of a question: its atom (kind and fields, SPEC §5), the referee's words for it, and either the next question
// (built only when this option is chosen) or the command it completes.
public sealed class ProtocolBranch
{
    private readonly Func<ProtocolNode>? next;
    private ProtocolNode? built;

    public ProtocolBranch(JsonObject atom, string text, Func<ProtocolNode> next)
    {
        Atom = atom;
        Text = text;
        this.next = next;
    }

    public ProtocolBranch(JsonObject atom, string text, GameCommand command)
    {
        Atom = atom;
        Text = text;
        Command = command;
    }

    public JsonObject Atom { get; }
    public string Text { get; }
    public GameCommand? Command { get; }
    public bool Terminal => Command is not null;
    public ProtocolNode Next => built ??= next!();
}

// One step of a path: the question it answers, the atom chosen and the question's context.
public sealed record ProtocolStep(string Question, JsonObject Atom, string Text, JsonObject? Context = null);

// A complete legal command written as its steps, or steps that open a larger family asked lazily (Rest builds its next
// question when the bot takes this path; its builder guarantees a legal completion).
public sealed record ProtocolPath(IReadOnlyList<ProtocolStep> Steps, GameCommand? Command, Func<ProtocolNode>? Rest = null)
{
    public ProtocolPath(ProtocolStep step, GameCommand command) : this(new[] { step }, command)
    {
    }

    public ProtocolPath(ProtocolStep step, Func<ProtocolNode> rest) : this(new[] { step }, null, rest)
    {
    }
}

public static class ProtocolTree
{
    // The question tree of enumerated paths: the paths that share a prefix share its questions; each question offers the
    // distinct next atoms. Paths must agree on the question kind at each shared prefix and none may end where another goes
    // on (chains end with their own explicit "end" atom).
    public static ProtocolNode? From(IReadOnlyList<ProtocolPath> paths) => paths.Count == 0 ? null : Node(paths, 0);

    private static ProtocolNode Node(IReadOnlyList<ProtocolPath> paths, int depth)
    {
        var first = paths[0].Steps[depth];
        var branches = new List<ProtocolBranch>();
        foreach (var group in paths.GroupBy(p => ProtocolJson.Write(p.Steps[depth].Atom)))
        {
            var members = group.ToList();
            var step = members[0].Steps[depth];
            if (members.Any(p => p.Steps[depth].Question != first.Question))
                throw new InvalidOperationException($"Paths disagree on the question after {depth} steps: {first.Question}.");
            var ending = members.Where(p => p.Steps.Count == depth + 1).ToList();
            if (ending.Count > 0 && ending.Count != members.Count)
                throw new InvalidOperationException($"A path ends where another goes on at {first.Question} {group.Key}.");
            // Paths with the same atoms are the same answer in the protocol's terms (two Regulars of one Nation): the first
            // stands for them.
            branches.Add(ending.Count == 0 ? new ProtocolBranch(step.Atom, step.Text, () => Node(members, depth + 1))
                : ending[0].Rest is { } rest ? new ProtocolBranch(step.Atom, step.Text, rest)
                : new ProtocolBranch(step.Atom, step.Text, ending[0].Command!));
        }
        return new ProtocolNode(first.Question, first.Context, branches);
    }

    // A tree whose every question has one option, to its command; null where some question has a choice.
    public static GameCommand? Forced(ProtocolNode node)
    {
        for (var steps = 0; steps < 64; steps++)
        {
            if (node.Branches.Count != 1)
                return null;
            var branch = node.Branches[0];
            if (branch.Terminal)
                return branch.Command;
            node = branch.Next;
        }
        return null;
    }
}

// One decision in progress: the question to ask now, the answers so far, and the command once complete.
public sealed class ProtocolDecision
{
    private ProtocolNode? current;

    private ProtocolDecision(ProtocolNode? current, GameCommand? command)
    {
        this.current = current;
        Command = command;
    }

    // A tree with a single complete answer asks nothing: its command is ready at once.
    public static ProtocolDecision Start(ProtocolNode root) =>
        ProtocolTree.Forced(root) is { } forced ? new(null, forced) : new(root, null);

    public ProtocolNode? Question => current;
    public GameCommand? Command { get; private set; }
    public List<int> Answers { get; } = new();

    public bool Answer(int option)
    {
        if (current is null || option < 0 || option >= current.Branches.Count)
            return false;
        Answers.Add(option);
        var branch = current.Branches[option];
        if (branch.Terminal)
        {
            Command = branch.Command;
            current = null;
        }
        else
            current = branch.Next;
        return true;
    }
}
