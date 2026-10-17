using Wotr.Contracts;

namespace Wotr.Domain;

public static class TableCardRules
{
    public static void Discard(MatchState state, Side owner, string id)
    {
        ActiveEffects.RemoveWhere(state, effect => effect.Owner == owner && effect.SourceId == id);
        var player = state.Player(owner);
        if (player.Table.Remove(id))
            player.Discard.Add(id);
    }

    // Moving to a private commitment must not also discard the physical card.
    public static void TakeForCombat(MatchState state, Side owner, string id)
    {
        state.Player(owner).Table.Remove(id);
        ActiveEffects.RemoveWhere(state, effect => effect.Owner == owner && effect.SourceId == id);
    }
}
