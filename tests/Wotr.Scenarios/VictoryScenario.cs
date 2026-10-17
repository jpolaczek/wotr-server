using Wotr.Contracts;
using Wotr.Domain;

// Prepared positions use the normal engine, projection and persisted commands.
// They do not stand in for a played campaign or supply client-side outcomes.
internal static class VictoryScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random)
    {
        var military = BaseBoundaryScenario.Create(random);
        return new()
        {
            ["victory-military-fp"] = military["boundary-military-fp"],
            ["victory-military-sh"] = military["boundary-military-sh"],
            ["victory-military-both"] = military["boundary-military-priority"],
            ["victory-lost-threshold"] = Recapture(random),
            ["victory-fate-kings"] = FateKings(random),
            ["victory-siege"] = Siege(random),
            ["victory-ring-fp"] = Ring(random, 10),
            ["victory-ring-sh"] = Ring(random, 11)
        };
    }

    private static MatchState Recapture(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        state.Control["moria"] = Side.FreePeoples;
        state.Control["dol-guldur"] = Side.FreePeoples;
        var unitToMove = state.Units.First(unit => unit.Nation == "sauron" && unit.Kind == UnitKind.Regular);
        unitToMove.Location = "dimrill-dale";
        var sauron = state.Nations.Single(nation => nation.Id == "sauron");
        sauron.Active = true;
        sauron.StepsToWar = 0;
        LastDice(state);
        state.Dice.Single(die => die.Owner == Side.Shadow && die.Location == DieLocation.Available).Face = DieFace.Army;
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    private static MatchState FateKings(IRandomSource random)
    {
        var state = ExpansionUiFixture.Actions(random, new MatchConfiguration { Kings = true, Fate = true, NewCities = true });
        var captured = new[] { "erebor", "south-rhun", "iron-hills", "ered-luin", "helms-deep" };
        foreach (var unit in state.Units.Where(unit => captured.Contains(unit.Location)))
            BoardRules.ReturnToReserve(unit);
        state.Control["erebor"] = Side.FreePeoples;
        state.Control["south-rhun"] = Side.FreePeoples;
        state.Control["iron-hills"] = Side.Shadow;
        state.Control["ered-luin"] = Side.Shadow;
        state.Control["helms-deep"] = Side.Shadow;
        var theoden = state.Kings!.Sovereigns.Single(sovereign => sovereign.Id == SovereignCatalog.Theoden);
        theoden.Status = SovereignStatus.Corrupted;
        theoden.CorruptedRegion = "edoras";
        state.Units.RemoveAll(unit => unit.Id == theoden.Id);
        return state;
    }

    private static MatchState Siege(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        foreach (var unit in state.Units.Where(unit => unit.Location == "minas-tirith"))
            unit.Garrison = true;
        var besieger = state.Units.First(unit => unit.Nation == "sauron" && unit.Kind == UnitKind.Regular);
        besieger.Location = "minas-tirith";
        return state;
    }

    private static MatchState Ring(IRandomSource random, int corruption)
    {
        var state = FellowshipScenario.Mordor(random, corruption);
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        // Both military thresholds make the timing/priority visible in the same ending.
        state.Control["moria"] = Side.FreePeoples;
        state.Control["dol-guldur"] = Side.FreePeoples;
        foreach (var region in new[] { "minas-tirith", "dol-amroth", "helms-deep", "lorien", "rivendell" })
            state.Control[region] = Side.Shadow;
        return state;
    }

    private static void LastDice(MatchState state)
    {
        foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
            foreach (var die in state.Dice.Where(die => die.Owner == side && die.Location == DieLocation.Available).Skip(1))
                die.Location = DieLocation.Spent;
    }
}
