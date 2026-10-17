using Wotr.Contracts;
using Wotr.Domain;

internal static class LordsFinalCardsScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["lords-madness-target"] = Madness(random, false),
        ["lords-madness-losses"] = Madness(random, true),
        ["lords-special-replace"] = Special(random),
        ["lords-special-decline"] = Special(random)
    };

    private static MatchState Madness(IRandomSource random, bool attack)
    {
        var state = LordsShadowScenario.State(random);
        var sauron = state.Nations.Single(n => n.Id == "sauron");
        sauron.Active = true;
        sauron.StepsToWar = 0;
        state.Control["carrock"] = Side.Shadow;
        foreach (var unit in state.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular).Take(3))
            unit.Location = "eagles-eyrie";
        state.Units.First(u => u.Nation == "elves" && u.Kind == UnitKind.Regular).Location = "moria";
        state = LordsShadowScenario.Play(state, LordsShadowCards.Madness, DieFace.Army);
        return attack ? CardPackScenario.Execute(state, CommandKind.ChooseEffect, new() { Ids = ["eagles-eyrie"] }, 4, 0, 0) : state;
    }

    private static MatchState Special(IRandomSource random)
    {
        var state = LordsShadowScenario.State(random);
        state.Fellowship.RegionId = "hollin";
        SmeagolRules.Enter(state);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        state = CardPackScenario.Execute(state, CommandKind.SeparateCompanions, new()
        {
            DieId = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Id,
            Ids = [.. state.Fellowship.Companions], Route = ["hollin"]
        });
        const string mithril = FreePeoplesCharacterCards01.Mithril;
        foreach (var deck in state.Player(Side.FreePeoples).Decks.Values)
            deck.Remove(mithril);
        state.Player(Side.FreePeoples).Table.Add(mithril);
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Id == "base.hunt.standard.3.0.1" ? "pool" : "removed";
        var shadow = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        shadow.Face = DieFace.Eye;
        shadow.Location = DieLocation.Hunt;
        shadow.HuntOrigin = "allocated";
        TurnFlow.SelectNext(state, Side.FreePeoples);
        state = CardPackScenario.Execute(state, CommandKind.MoveFellowship, new()
        {
            DieId = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Id
        }, 5, 0);
        state.HuntTiles.Single(t => t.Id == "base.hunt.standard.eye.1.1").Location = "pool";
        return state;
    }
}
