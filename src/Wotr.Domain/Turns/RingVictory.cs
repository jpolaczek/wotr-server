using Wotr.Contracts;

namespace Wotr.Domain;

public static class RingVictory
{
    public static bool Check(MatchState state)
    {
        if (state.Winner is not null || CheckCorruption(state))
            return true;
        if (state.Fellowship.MordorStep is not >= 5)
            return false;
        GameEnding.Finish(state, Side.FreePeoples, VictoryCondition.RingDestroyed);
        return true;
    }

    public static bool CheckCorruption(MatchState state)
    {
        if (state.Fellowship.Corruption < 12)
            return false;
        GameEnding.Finish(state, Side.Shadow, VictoryCondition.RingCorrupted);
        return true;
    }
}
