using Wotr.Contracts;
using Wotr.Domain;

internal static class BaseTurnScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["turn-hunt-optional"] = Fellowship(random, mandatory: false),
        ["turn-hunt-mandatory"] = Fellowship(random, mandatory: true),
        ["turn-ring-politics"] = RingPolitics(random),
        ["turn-pass"] = Pass(random)
    };

    private static MatchState Fellowship(IRandomSource random, bool mandatory)
    {
        var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), random);
        TurnFlow.Start(state);
        if (mandatory)
        {
            var die = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Unrolled);
            die.Location = DieLocation.Hunt;
            die.HuntOrigin = DiceRules.FellowshipMove;
            TurnFlow.BeginTurn(state);
        }
        return state;
    }

    private static MatchState RingPolitics(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        var dice = state.Dice.Where(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).ToArray();
        dice[0].Face = DieFace.Army;
        dice[1].Face = DieFace.WillOfTheWest;
        var elves = state.Nations.Single(n => n.Id == "elves");
        elves.Active = true;
        elves.StepsToWar = 2;
        return state;
    }

    private static MatchState Pass(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Location = DieLocation.Spent;
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Take(2))
            die.Location = DieLocation.Spent;
        return state;
    }
}
