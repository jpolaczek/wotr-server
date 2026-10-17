using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

internal static class Strategy01Fixture
{
    public static MatchState Play(MatchState state, string card, params int[] rolls)
    {
        EventTests.GiveCard(state, Side.FreePeoples, card);
        var payload = Cost(state, CardCatalog.Base[card].EventType);
        payload.CardId = card;
        payload.CardCost = CardCatalog.Base[card].EventType;
        return Do(state, CommandKind.PlayEvent, payload, new SequenceRandom(rolls));
    }

    public static MatchState KingArmy(string region = "fangorn")
    {
        var state = BattleFixture.State();
        var king = Put(state, CharacterRules.Strider, region);
        CharacterLifecycle.ApplyVersion(king, CharacterCatalog.Base[CharacterRules.Aragorn]);
        BattleFixture.Put(state, "gondor", UnitKind.Regular, region);
        return state;
    }

    public static MatchState MixedArmy(string region = "orthanc")
    {
        var state = BattleFixture.State();
        BattleFixture.Put(state, "isengard", UnitKind.Regular, region);
        BattleFixture.Put(state, "sauron", UnitKind.Elite, region);
        BattleFixture.Put(state, "sauron", UnitKind.Nazgul, region);
        Put(state, CharacterRules.WitchKing, region);
        state.Nations.Single(n => n.Id == "isengard").StepsToWar = 2;
        return state;
    }
}
