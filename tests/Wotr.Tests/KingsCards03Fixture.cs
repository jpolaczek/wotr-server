using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

internal static class KingsCards03Fixture
{
    public static MatchState Ready(string card)
    {
        var state = State(Side.Shadow);
        if (card == KingsCards03.Valinor)
            Corrupt(state, SovereignCatalog.Theoden);
        if (card == KingsCards03.King)
            SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Denethor), "minas-tirith");
        if (card == KingsCards03.Stormcrow)
        {
            state.Fellowship.RegionId = "dale";
            Pool(state, One);
        }
        return state;
    }

    public static CommandPayload EventCost(MatchState state, string card, DieFace face = DieFace.Event)
    {
        var payload = Cost(state, card, face);
        if (card == KingsCards03.Stormcrow)
            payload.NationId = "north";
        return payload;
    }

    public static void Corrupt(MatchState state, string id)
    {
        var sovereign = Sovereign(state, id);
        sovereign.Status = SovereignStatus.Corrupted;
        sovereign.CorruptedRegion = SovereignCatalog.All[id].Home;
        state.Units.RemoveAll(u => u.Id == id);
    }

    public static MatchState Table(string card)
    {
        var state = Ready(card);
        state = Do(state, CommandKind.PlayEvent, EventCost(state, card));
        TurnFlow.SelectNext(state, Side.FreePeoples);
        return state;
    }
}
