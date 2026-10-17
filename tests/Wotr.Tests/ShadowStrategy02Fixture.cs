using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.ShadowStrategyCards02;

namespace Wotr.Tests;

internal static class ShadowStrategy02Fixture
{
    public static readonly string[] Cards = [Gather, Lengthens, Moving, Corsairs, Rage, Return];
    public static MatchState State(string card)
    {
        var state = BattleFixture.State(Side.Shadow);
        EventTests.GiveCard(state, Side.Shadow, card);
        CharacterFixture.Put(state, CharacterRules.WitchKing, "osgiliath");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "moria");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "south-dunland");
        return state;
    }

    public static CommandPayload Cost(MatchState state, string card)
    {
        var face = card is Rage or Return ? DieFace.Muster : DieFace.Army;
        var payload = CharacterFixture.Cost(state, face);
        payload.CardId = card;
        payload.CardCost = face;
        return payload;
    }

    public static MatchState Play(MatchState state, string card) => CharacterFixture.Do(state, CommandKind.PlayEvent, Cost(state, card));
    public static ArmyMoveChoice Move(MatchState state, string from, params string[] route) => new()
    {
        From = from,
        To = route[^1],
        Route = [.. route],
        UnitIds = state.Units.Where(u => u.Location == from && !u.Garrison && BoardRules.Owner(u) == Side.Shadow).Select(u => u.Id).ToList(),
        CharacterIds = state.Characters.Where(c => c.Location == from && c.Side == Side.Shadow && !c.Garrison && CharacterRules.Mobile(c)).Select(c => c.Id).ToList()
    };
}
