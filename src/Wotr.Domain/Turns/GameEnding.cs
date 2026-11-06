using Wotr.Contracts;

namespace Wotr.Domain;

public static class GameEnding
{
    public static void Finish(MatchState state, Side winner, VictoryCondition condition)
    {
        if (state.Resolution.PaidDieIds.Count > 0)
        {
            var owner = state.Dice.Single(d => d.Id == state.Resolution.PaidDieIds[0]).Owner;
            var move = state.Resolution.Frames.Any(f => f.Hunt?.Return == HuntReturn.FinishMove);
            DiceRules.CompleteCost(state, owner, move);
        }
        CorruptionRuntime.Abandon(state);
        foreach (var tile in state.HuntTiles.Where(t => t.Location == "drawn"))
            tile.Location = "discard";
        HuntPool.CompleteAction(state);
        state.Winner = winner;
        state.Victory = condition;
        state.Pending = null;
        HuntReports.Capture(state);
        state.Resolution.Frames.Clear();
        HuntReports.CompleteIfIdle(state);
        state.Events.Add(new() { Revision = state.Revision, Kind = condition.ToString(), Text = Text(winner, condition) });
        TurnFlow.Phase(state, GamePhase.Finished);
    }

    private static string Text(Side winner, VictoryCondition condition) => condition switch
    {
        VictoryCondition.Conceded => $"{TurnFlow.Other(winner)} conceded. {winner} wins.",
        VictoryCondition.TimedOut => $"{TurnFlow.Other(winner)} ran out of time. {winner} wins.",
        _ => $"{winner} wins: {condition}."
    };
}
