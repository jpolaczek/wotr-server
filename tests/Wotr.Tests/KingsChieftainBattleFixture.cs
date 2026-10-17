using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

internal static class KingsChieftainBattleFixture
{
    public const string Daring = FreePeoplesCharacterCards02.Alone;

    public static MatchState Ready(string chieftain, Side attacker = Side.FreePeoples)
    {
        var state = KingsBattleTests.Battle(SovereignCatalog.Dain, true, attacker);
        Add(state, chieftain);
        return state;
    }

    public static void Add(MatchState state, string chieftain)
    {
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        KingsDiceTests.Chieftain(state, chieftain, force.Region);
        force.Participants.Add(chieftain);
    }

    public static MatchState Cards(MatchState state, string shCard = KingsCards03.Valinor, string fpCard = "")
    {
        state.Player(Side.FreePeoples).Hand = fpCard == "" ? [] : [fpCard];
        state.Player(Side.Shadow).Hand = shCard == "" ? [] : [shCard];
        var first = state.Pending!.Owner == Side.Shadow ? shCard : fpCard;
        var second = state.Pending.Owner == Side.Shadow ? fpCard : shCard;
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = first });
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        return Do(state, CommandKind.CommitCombatCard, new() { CardId = second }, new int[16]);
    }

    public static MatchState Choose(MatchState state, string id) =>
        Do(state, CommandKind.ChooseEffect, new() { Ids = [id] }, new int[16]);

    public static MatchState PayWrath(MatchState state, BattleLossKind kind = BattleLossKind.Remove)
    {
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        var unit = BattleRules.Units(state, force).First(u => u.Kind == (kind == BattleLossKind.Downgrade ? UnitKind.Elite : UnitKind.Regular));
        return Do(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(unit.Id, kind)] }, new int[16]);
    }

    public static MatchState SkipRerolls(MatchState state)
    {
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = Do(state, CommandKind.RerollBattle);
        return state;
    }
}
