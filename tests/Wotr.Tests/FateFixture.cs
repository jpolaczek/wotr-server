using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Tests;

internal static class FateFixture
{
    public static MatchState Setup(bool cities = false, bool kings = false, bool lords = false)
    {
        var configuration = new MatchConfiguration { Fate = true, NewCities = cities, Kings = kings, Lords = lords };
        var samples = CardCatalog.ConfiguredDeck(configuration).Count() - 4;
        return MatchFactory.Create("fate-test", new SequenceRandom(new int[samples]), configuration);
    }

    public static MatchState Actions(Side actor = Side.FreePeoples, bool cities = false)
    {
        var state = Setup(cities);
        state.Turn = 2;
        state.Phase = GamePhase.ActionResolution;
        foreach (var die in state.Dice.Where(d => d.Location == DieLocation.Unrolled))
        {
            die.Location = DieLocation.Available;
            die.Face = DieFace.Muster;
        }
        TurnFlow.SelectNext(state, actor);
        return state;
    }

    public static void Clear(MatchState state, string region)
    {
        foreach (var unit in state.Units.Where(u => u.Location == region))
            BoardRules.ReturnToReserve(unit);
    }
}
