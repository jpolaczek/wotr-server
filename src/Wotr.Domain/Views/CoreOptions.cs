using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public interface IActionOptionsProvider
{
    IEnumerable<ActionOption> GetOptions(MatchState state, Side actor);
}

public sealed class CoreOptions : IActionOptionsProvider
{
    public IEnumerable<ActionOption> GetOptions(MatchState state, Side actor)
    {
        if (state.Pending?.Owner != actor)
            yield break;
        if (state.Pending.Commands.Contains(CommandKind.DeclineReaction))
            yield return new()
            {
                Kind = CommandKind.DeclineReaction,
                Enabled = state.Resolution.Frames.Count > 0
            };
    }
}
