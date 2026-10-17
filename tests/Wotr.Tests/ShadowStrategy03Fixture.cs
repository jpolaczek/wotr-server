using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.ShadowStrategyCards03;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

internal static class ShadowStrategy03Fixture
{
    public static readonly string[] Cards = [HalfOrcs, OlogHai, HillTrolls, NewPower, ManyKings, KingRevealed];
    public static MatchState State(string card)
    {
        var state = BattleFixture.State(Side.Shadow);
        EventTests.GiveCard(state, Side.Shadow, card);
        if (card == NewPower)
            Put(state, CharacterRules.Saruman, "orthanc");
        if (card == KingRevealed)
            Put(state, CharacterRules.Aragorn, "minas-tirith");
        return state;
    }

    public static CommandPayload EventCost(MatchState state, string card)
    {
        var face = card is HalfOrcs or OlogHai or HillTrolls ? DieFace.Army : DieFace.Muster;
        var payload = Cost(state, face);
        payload.CardId = card;
        payload.CardCost = face;
        return payload;
    }

    public static MatchState Play(MatchState state, string card) => Do(state, CommandKind.PlayEvent, EventCost(state, card));
    public static ActionOption Option(MatchState state) => new EffectOptions().GetOptions(state, Side.Shadow).Single(o => o.Kind == CommandKind.ChooseEffect);

    public static void Reserve(MatchState state, string nation, UnitKind kind, int count)
    {
        foreach (var unit in state.Units.Where(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve").Skip(count))
            unit.Location = "casualties";
    }

    public static CommandPayload Plan(MatchState state, params (string Nation, UnitKind Kind, string Region, int Count)[] allocations)
    {
        var option = Option(state);
        var available = option.Recruitment.ToDictionary(g => (g.Nation, g.Kind), g => new Queue<string>(g.ReserveIds));
        return new()
        {
            Placements = allocations.SelectMany(a => Enumerable.Range(0, a.Count).Select(_ => new UnitPlacementChoice
            {
                UnitId = available[(a.Nation, a.Kind)].Dequeue(), RegionId = a.Region
            })).ToList()
        };
    }
}
