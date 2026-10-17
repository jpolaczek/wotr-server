using Wotr.Contracts;

namespace Wotr.Domain;

// DEMO-02: the simple CPU of the demo mode (R32). It knows only what its side's projection shows: from the legal
// options of its decision it builds candidate commands in a plain order of preference and plays the first one the
// engine accepts, so it never repeats a rule of its own. Its choices come from a seed of the match and revision;
// every roll stays with the server's random source.
public sealed class DemoBot(GameEngine engine, Projection projection)
{
    public GameCommand? Next(MatchState state, Side side)
    {
        if (state.Winner is not null || state.Pending?.Owner != side)
            return null;
        var view = projection.For(state, side);
        var context = new DemoContext(state, view, DemoChoice.For(state));
        foreach (var (kind, payload) in DemoCandidates.For(context))
        {
            var command = Command(state, view, kind, payload);
            if (engine.Validate(state, side, command) is null)
                return command;
        }
        return null;
    }

    private static GameCommand Command(MatchState state, PlayerView view, CommandKind kind, CommandPayload payload) => new()
    {
        MatchId = state.Id,
        CommandId = $"demo-cpu-{state.Revision}",
        ExpectedRevision = state.Revision,
        DecisionId = view.Decision!.Id,
        Kind = kind,
        Payload = payload
    };
}

// What one choice of the CPU looks at: the state for its own pieces, its projection for the legal options.
public sealed record DemoContext(MatchState State, PlayerView View, DemoChoice Choice)
{
    public Side Side => View.You;

    public IEnumerable<ActionOption> Options(CommandKind kind) => View.Options.Where(o => o.Kind == kind && o.Enabled);
}
