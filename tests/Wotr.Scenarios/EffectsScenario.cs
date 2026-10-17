using Wotr.Contracts;
using Wotr.Domain;

internal static class EffectsScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random)
    {
        var shadow = Shadow04Scenario.Create(random);
        return new Dictionary<string, MatchState>
        {
            ["effects-protection"] = Strategy01Scenario.Create(random)["strategy-protection"],
            ["effects-inspection"] = Strategy01Scenario.Create(random)["strategy-protection"],
            ["effects-movement"] = Movement(random),
            ["effects-palantir"] = shadow["shadow-palantir-remove"],
            ["effects-expiry"] = shadow["shadow-wormtongue"]
        };
    }

    private static MatchState Movement(IRandomSource random)
    {
        var state = Strategy01Scenario.Create(random)["strategy-protection"];
        var elves = state.Nations.Single(n => n.Id == "elves");
        elves.Active = true;
        elves.StepsToWar = 0;
        state.Units.First(u => u.Nation == elves.Id && u.Kind == UnitKind.Regular && u.Location == "reserve").Location = "dimrill-dale";
        state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Face = DieFace.Army;
        TurnFlow.SelectNext(state, Side.FreePeoples);
        return state;
    }
}
