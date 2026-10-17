using Wotr.Contracts;

namespace Wotr.Domain;

public static class BattleLanding
{
    public static bool MustContinue(BattleState battle) => battle.ArrivalBeforeBattle && battle.Mode != BattleMode.Siege;

    public static void Finish(MatchState state, BattleState battle)
    {
        battle.Stage = BattleStage.Complete;
        StrongholdRules.ReconcileRegion(state, battle.Region);
        if (BattleRules.Alive(state, battle.Attacker))
            TerritoryRules.Capture(state, battle.Attacker.Side, battle.Region);
        if (!StackLimits.OpenIfRequired(state, battle.Defender.Side))
            StackLimits.OpenIfRequired(state, battle.Attacker.Side);
    }
}
