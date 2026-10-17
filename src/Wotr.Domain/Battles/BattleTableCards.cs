using Wotr.Contracts;

namespace Wotr.Domain;

public static class BattleTableCards
{
    public static bool Available(MatchState state, BattleState battle, Side side, string card) => side == Side.Shadow &&
        card == ShadowCharacterCards03.Balrog && state.Player(side).Table.Contains(card) &&
        new NearMoriaBattle().CanPlay(state, battle, side);

    public static IEnumerable<string> Candidates(MatchState state, BattleState battle, Side side) =>
        Available(state, battle, side, ShadowCharacterCards03.Balrog) && !battle.Force(side).Round.CardFromTable
            ? [ShadowCharacterCards03.Balrog] : [];

    public static int PublicHandCount(MatchState state, PlayerState player)
    {
        var battle = BattleRuntime.Active(state);
        return player.Hand.Count + (battle is not null ? Candidates(state, battle, player.Side).Count() : 0);
    }

    public static void Reveal(MatchState state, BattleState battle)
    {
        foreach (var force in new[] { battle.Attacker, battle.Defender }.Where(f => f.Round.CardFromTable))
            TableCardRules.TakeForCombat(state, force.Side, force.Round.CardId);
    }
}
