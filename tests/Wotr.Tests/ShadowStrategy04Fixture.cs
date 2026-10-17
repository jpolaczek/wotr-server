using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.ShadowStrategyCards04;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

internal static class ShadowStrategy04Fixture
{
    public static readonly string[] Cards = [MistyMountains, OrcsMultiplying, Horde, Monsters, Musterings, Pits];

    public static MatchState State(string card)
    {
        var state = BattleFixture.State(Side.Shadow);
        EventTests.GiveCard(state, Side.Shadow, card);
        return state;
    }

    public static CommandPayload EventCost(MatchState state, string card, DieFace face = DieFace.Muster)
    {
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
        var available = Option(state).Recruitment.ToDictionary(g => (g.Nation, g.Kind), g => new Queue<string>(g.ReserveIds));
        return new()
        {
            Placements = allocations.SelectMany(a => Enumerable.Range(0, a.Count).Select(_ => new UnitPlacementChoice
            {
                UnitId = available[(a.Nation, a.Kind)].Dequeue(), RegionId = a.Region
            })).ToList()
        };
    }
}
