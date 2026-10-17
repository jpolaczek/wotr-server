using Wotr.Contracts;
using Wotr.Domain;
using static CardPackScenario;
using static KingsCards01Scenario;

internal static class KingsCards04Scenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["kings-enemy-sovereign"] = Event(random, 0),
        ["kings-enemy-army"] = Event(random, 1),
        ["kings-enemy-losses"] = Event(random, 2),
        ["kings-enemy-power"] = KingsCards03Scenario.PowerChoice(random, KingsCards04.Enemy)
    };

    private static MatchState Event(IRandomSource random, int step)
    {
        var state = State(random);
        state.Player(Side.Shadow).Hand.Clear();
        TurnFlow.SelectNext(state, Side.Shadow);
        Unit(state, "gondor", UnitKind.Regular, "minas-tirith");
        Unit(state, "gondor", UnitKind.Elite, "minas-tirith");
        var garrison = state.Units.First(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular && u.Location == "reserve");
        garrison.Location = "minas-tirith";
        garrison.Garrison = true;
        var denethor = SovereignLifecycle.Find(state, SovereignCatalog.Denethor)!;
        state.Units.RemoveAll(u => u.Id == denethor.Id);
        denethor.Status = SovereignStatus.Corrupted;
        denethor.CorruptedRegion = "minas-tirith";
        state.Units.Single(u => u.Id == SovereignCatalog.Theoden).Location = "minas-tirith";
        CorruptionRules.Attach(state, SovereignLifecycle.Find(state, SovereignCatalog.Dain)!, "base.hunt.standard.1.0.1");
        state = Play(state, KingsCards04.Enemy);
        if (step == 0)
            return state;
        state = Execute(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Denethor] });
        return step == 1 ? state : Execute(state, CommandKind.ChooseEffect, new() { Ids = ["minas-tirith:field"] }, 3, 0, 0);
    }
}
