using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

internal static class CorsairFixture
{
    public const string Card = "base.sh.strategy.10";
    public const string Stronghold = "dol-amroth";

    public static MatchState State(int arrivals = 2, string target = "pelargir", int defenders = 0)
    {
        var state = BattleFixture.State(Side.Shadow);
        for (var i = 0; i < arrivals; i++)
            BattleFixture.Put(state, "southrons", UnitKind.Regular, CorsairRules.Origin);
        for (var i = 0; i < defenders; i++)
            BattleFixture.Put(state, "gondor", UnitKind.Regular, target);
        EventTests.GiveCard(state, Side.Shadow, Card);
        return state;
    }

    public static CommandPayload EventCost(MatchState state, DieFace face = DieFace.Army)
    {
        var payload = Cost(state, face);
        payload.CardId = Card;
        payload.CardCost = face;
        return payload;
    }

    public static MatchState Play(MatchState state, DieFace face = DieFace.Army) =>
        Do(state, CommandKind.PlayEvent, EventCost(state, face));

    public static CommandPayload Move(MatchState state, string target = "pelargir") =>
        Shadow04Fixture.Army(state, CorsairRules.Origin, target);

    public static MatchState Land(MatchState state, string target = "pelargir") =>
        Do(Play(state), CommandKind.MoveArmies, Move(state, target));

    public static MatchState Round(MatchState state, params int[] values)
    {
        state = Do(state, CommandKind.CommitCombatCard);
        state = Do(state, CommandKind.CommitCombatCard, random: new SequenceRandom(values.Length == 0 ? new int[10] : values));
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = Do(state, CommandKind.RerollBattle);
        return state;
    }
}
