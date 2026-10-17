using Wotr.Contracts;
using Wotr.Domain;

internal static class MordorInteractionScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random)
    {
        var result = new Dictionary<string, MatchState>();
        foreach (var gate in new[] { "morannon", "minas-morgul" })
        {
            foreach (var hidden in new[] { true, false })
                result.Add($"ui34-mordor-{gate}-{hidden}", Entry(random, gate, hidden));
            result.Add($"ui34-mordor-decline-{gate}", Entry(random, gate, true));
        }
        result.Add("ui34-mordor-stop", Track(random, "base.hunt.special-sh.1.1.1"));
        result.Add("ui34-mordor-eye", Track(random, "base.hunt.standard.eye.1.1"));
        var hide = Track(random, "base.hunt.standard.1.0.1");
        hide.Fellowship.Hidden = false;
        result.Add("ui34-mordor-hide", hide);
        var inactive = Track(random, "base.hunt.standard.1.0.1");
        var lastDie = inactive.Dice.First(d => d.Owner == Side.FreePeoples);
        foreach (var die in inactive.Dice)
            die.Location = die == lastDie ? DieLocation.Available : DieLocation.Spent;
        lastDie.Face = DieFace.Event;
        TurnFlow.SelectNext(inactive, Side.FreePeoples);
        result.Add("ui34-mordor-inactive", inactive);
        result.Add("ui34-mordor-final-fp", FellowshipScenario.Mordor(random, 10));
        result.Add("ui34-mordor-final-sh", FellowshipScenario.Mordor(random, 11));
        result.Add("ui52-mordor-replace-stop", ReplaceStop(random));
        result.Add("ui52-mordor-eye-value", EyeValue(random));
        result.Add("ui52-mordor-event-eye", EventEye(random));
        result.Add("ui52-mordor-breaking", Breaking(random));
        return result;
    }

    private static MatchState ReplaceStop(IRandomSource random)
    {
        var state = Track(random, "base.hunt.special-sh.1.1.1");
        TableMithril(state);
        state.HuntTiles.Single(t => t.Id == "base.hunt.standard.1.0.1").Location = "discard";
        return BeginMove(state);
    }

    private static MatchState EyeValue(IRandomSource random)
    {
        var state = Track(random, "base.hunt.standard.eye.1.1");
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow).Take(2))
            die.Location = DieLocation.Hunt;
        var previous = state.Dice.Where(d => d.Owner == Side.FreePeoples).Skip(1).First();
        previous.Location = DieLocation.Hunt;
        previous.HuntOrigin = DiceRules.FellowshipMove;
        return BeginMove(state);
    }

    private static MatchState EventEye(IRandomSource random)
    {
        var state = Track(random, "base.hunt.standard.eye.1.1");
        TableMithril(state);
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow).Take(2))
            die.Location = DieLocation.Hunt;
        state.Dice.Last(d => d.Owner == Side.Shadow).Location = DieLocation.Available;
        TurnFlow.SelectNext(state, Side.Shadow);
        return CardPackScenario.PlayEvent(state, ShadowCharacterCards01.OrcPatrol, 0);
    }

    private static MatchState Breaking(IRandomSource random)
    {
        var state = Track(random, "base.hunt.standard.2.0.1");
        state.Fellowship.Hidden = false;
        state.Fellowship.Companions = [HuntAftermathScenario.Gimli, HuntAftermathScenario.Merry, HuntAftermathScenario.Pippin];
        state.Fellowship.Guide = HuntAftermathScenario.Gimli;
        state.Dice.Last(d => d.Owner == Side.Shadow).Location = DieLocation.Available;
        TurnFlow.SelectNext(state, Side.Shadow);
        return CardPackScenario.PlayEvent(state, ShadowCharacterCards03.Breaking, 0);
    }

    private static MatchState BeginMove(MatchState state) => ExpansionUiFixture.Execute(state,
        CommandKind.MoveFellowship, new() { DieId = state.Dice.First(d => d.Owner == Side.FreePeoples).Id }, 0);

    private static void TableMithril(MatchState state)
    {
        const string card = FreePeoplesCharacterCards01.Mithril;
        CardPackScenario.Give(state, card);
        state.Player(Side.FreePeoples).Hand.Remove(card);
        state.Player(Side.FreePeoples).Table.Add(card);
    }

    private static MatchState Entry(IRandomSource random, string gate, bool hidden)
    {
        var state = FellowshipScenario.Entry(random);
        state.Fellowship.RegionId = gate;
        state.Fellowship.Hidden = hidden;
        return state;
    }

    private static MatchState Track(IRandomSource random, string tileId)
    {
        var state = FellowshipScenario.Mordor(random, 4);
        state.Fellowship.MordorStep = 2;
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Id == tileId ? "pool" : "removed";
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow))
            die.Location = DieLocation.Spent;
        state.Dice.First(d => d.Owner == Side.FreePeoples).Face = DieFace.WillOfTheWest;
        return state;
    }
}
