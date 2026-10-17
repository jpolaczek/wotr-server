using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record CorsairBattleStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var region = context.Execution.RegionId;
        if (region == "" || !BoardRules.HasEnemyArmy(context.State, region, Side.Shadow))
            return;
        var siege = BoardRules.HasEnemyArmy(context.State, region, Side.Shadow, true);
        var relief = context.State.Units.Any(u => u.Location == region && u.Garrison && BoardRules.Owner(u) == Side.Shadow && BoardRules.IsArmyUnit(u));
        var battle = new BattleState
        {
            Region = region,
            ArrivalBeforeBattle = true,
            Mode = siege ? BattleMode.Siege : relief ? BattleMode.Relief : BattleMode.Field,
            Attacker = Force(context.State, Side.Shadow, region, false),
            Defender = Force(context.State, Side.FreePeoples, region, siege)
        };
        if (BattleRules.Alive(context.State, battle.Attacker))
            BattleRuntime.Begin(context.State, battle, EffectReturn.Parent);
    }

    private static BattleForce Force(MatchState state, Side side, string region, bool garrison) => new()
    {
        Side = side,
        Region = region,
        Garrison = garrison,
        Participants = state.Units.Where(u => u.Location == region && u.Garrison == garrison && BoardRules.Owner(u) == side).Select(u => u.Id)
            .Concat(state.Characters.Where(c => c.Location == region && c.Garrison == garrison && c.Side == side).Select(c => c.Id)).ToList()
    };
}
