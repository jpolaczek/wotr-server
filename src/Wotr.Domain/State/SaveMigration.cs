using Wotr.Contracts;

namespace Wotr.Domain;

internal static class SaveMigration
{
    public static void FromVersionOne(MatchState state)
    {
        Refresh(state.Pending, state.Configuration);
        foreach (var frame in state.Resolution.Frames)
            Refresh(frame.ReturnDecision, state.Configuration);
        state.SchemaVersion = 2;
    }

    public static void FromVersionTwo(MatchState state)
    {
        // Old commands were atomic. Reading them cannot invent an unfinished action.
        if (state.Resolution.ActionSequence is not null)
            throw new InvalidDataException("CorruptSave");
        state.SchemaVersion = StateCodec.CurrentVersion;
    }

    private static void Refresh(PendingDecision? decision, MatchConfiguration configuration)
    {
        if (decision is null)
            return;
        if (decision.Kind == DecisionKind.Action)
            decision.Commands = TurnDecisionCommands.Actions(configuration, decision.Owner);
        else if (decision.Kind == DecisionKind.FellowshipPhase)
            decision.Commands = TurnDecisionCommands.Fellowship();
    }
}
