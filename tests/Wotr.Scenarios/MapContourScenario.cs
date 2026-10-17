using Wotr.Contracts;
using Wotr.Domain;

internal static class MapContourScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random)
    {
        var result = new Dictionary<string, MatchState>();
        foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
        {
            foreach (var gesture in new[] { "click", "drop", "attack" })
                result.Add($"map15-{side}-{gesture}", Position(random, side));
        }
        foreach (var name in new[] { "invalid", "reconnect", "revision", "navigation", "notice" })
            result.Add("map15-" + name, Position(random, Side.FreePeoples));
        result.Add("ui105-nazgul-army", NazgulArmy(random));
        result.Add("ui105-nazgul-alone", NazgulArmy(random));
        return result;
    }

    // UI-105: a Shadow army with a Nazgûl at Lossarnach and a Character die, which moves either the army with its
    // Nazgûl or the Nazgûl alone.
    private static MatchState NazgulArmy(IRandomSource random)
    {
        var state = BattleScenario.Field(random);
        var nazgul = state.Units.First(u => u.Kind == UnitKind.Nazgul && u.Location == "reserve");
        nazgul.Location = "lossarnach";
        nazgul.Garrison = false;
        state.Dice.First(d => d.Owner == Side.Shadow).Face = DieFace.Character;
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    private static MatchState Position(IRandomSource random, Side side)
    {
        var state = BattleScenario.Field(random);
        TurnFlow.SelectNext(state, side);
        return state;
    }
}
