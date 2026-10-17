using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.FreePeoplesCharacterCards02;
using static CardPackScenario;

internal static class CardPack02Scenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["staff"] = StaffWindow(random),
        ["another-way"] = FellowshipChoice(random, AnotherWay),
        ["alone"] = FellowshipChoice(random, Alone),
        ["anduril"] = CombatChoice(random, Athelas),
        ["heroic"] = CombatChoice(random, AnotherWay),
        ["fateful"] = CombatChoice(random, Bilbo)
    };

    private static MatchState StaffWindow(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        state.Fellowship.MordorStep = 0;
        state.Fellowship.RegionId = MordorRules.Track;
        Give(state, Staff);
        HandRules.Remove(state.Player(Side.FreePeoples), Staff);
        state.Player(Side.FreePeoples).Table.Add(Staff);
        return Execute(state, CommandKind.MoveFellowship, new() { DieId = state.Dice[0].Id });
    }

    private static MatchState FellowshipChoice(IRandomSource random, string card)
    {
        var state = FellowshipScenario.Actions(random);
        state.Fellowship.RegionId = "rivendell";
        state.Fellowship.Progress = 0;
        state.Fellowship.Corruption = 3;
        if (card == AnotherWay)
        {
            state.Fellowship.Companions.Clear();
            state.Fellowship.Guide = CharacterRules.Gollum;
            foreach (var die in state.Dice.Where(d => d.Location == DieLocation.Hunt))
                die.Location = DieLocation.Available;
        }
        Give(state, card);
        return Execute(state, CommandKind.PlayEvent, new() { DieId = state.Dice[0].Id, CardId = card, CardCost = DieFace.Character });
    }

    private static MatchState CombatChoice(IRandomSource random, string card)
    {
        var state = CardPackBattleScenario.BeforeCards(random, card);
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = card });
        state = Execute(state, CommandKind.CommitCombatCard, new(), card == AnotherWay ? [0, 1, 2, 3, 0, 5, 5, 5] : [0, 1, 2, 3, 0, 0, 0, 0]);
        if (card == Athelas)
            return state;
        var dice = BattleRuntime.Active(state)!.Attacker.Round.Dice.Take(2).Select(d => d.Id).ToList();
        state = Execute(state, CommandKind.RerollBattle, new() { Ids = card == Bilbo ? dice : [] });
        return state.Pending!.Kind == DecisionKind.BattleReroll
            ? Execute(state, CommandKind.RerollBattle, new(), 5, 5) : state;
    }
}
