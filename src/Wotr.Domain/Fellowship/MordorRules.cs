using Wotr.Contracts;

namespace Wotr.Domain;

public static class MordorRules
{
    public const string Track = "mordor-track";
    public static bool CanEnter(MatchState state) => state.Phase == GamePhase.Fellowship &&
        state.Pending?.Kind == DecisionKind.FellowshipPhase && state.Resolution.Frames.Count == 0 &&
        !state.Fellowship.MordorStep.HasValue && state.Fellowship.RegionId is "morannon" or "minas-morgul";

    public static void Enter(MatchState state)
    {
        state.Fellowship.RegionId = Track;
        state.Fellowship.MordorStep = 0;
        state.Fellowship.Progress = 0;
        foreach (var tile in state.HuntTiles)
            if (tile.Location == "prepared" || tile.Location == "discard" && HuntPool.Definitions[tile.Id].Value == "eye")
                tile.Location = "pool";
        state.Events.Add(new() { Revision = state.Revision, Kind = "MordorEntered", Text = "The Fellowship enters the Mordor Track." });
        TurnFlow.OpenFellowshipPhase(state);
    }

    public static void Attempt(MatchState state)
    {
        if (state.Fellowship.MordorStep.HasValue)
            state.Fellowship.LastMordorAttemptTurn = state.Turn;
    }

    public static void CompleteMove(MatchState state, HuntProcedure hunt)
    {
        if (!hunt.Stop)
            state.Fellowship.MordorStep++;
        state.Events.Add(new()
        {
            Revision = state.Revision,
            Kind = "MordorMoveResolved",
            Text = hunt.Stop ? "The Fellowship is stopped on the Mordor Track."
                : $"The Fellowship advances to Mordor step {state.Fellowship.MordorStep}."
        });
    }

    public static bool EndActions(MatchState state)
    {
        if (state.Fellowship.MordorStep.HasValue && state.Fellowship.LastMordorAttemptTurn != state.Turn)
        {
            state.Fellowship.Corruption++;
            state.Events.Add(new() { Revision = state.Revision, Kind = "MordorInactivity", Text = "No attempt to move or hide in Mordor: gain one Corruption." });
        }
        return RingVictory.Check(state);
    }
}
