using Wotr.Contracts;
using Wotr.Domain;
using static CardPackScenario;
using static KingsCards01Scenario;
using static Wotr.Domain.KingsCards02;

internal static class KingsCards02Scenario
{
    private const string One = "base.hunt.standard.1.0.1";
    private const string Two = "base.hunt.standard.2.0.1";

    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["kings-villages"] = Play(State(random), Villages),
        ["kings-service"] = ServiceChoice(random),
        ["kings-welcome"] = WelcomeTable(random),
        ["kings-west"] = WestAttempt(random, false),
        ["kings-west-draw"] = WestAttempt(random, true),
        ["kings-hope"] = Combat(random, false),
        ["kings-vain"] = Combat(random, true)
    };

    private static MatchState ServiceChoice(IRandomSource random)
    {
        var state = State(random);
        CorruptionRules.Attach(state, SovereignLifecycle.Find(state, SovereignCatalog.Dain)!, One);
        return Play(state, Service);
    }

    private static MatchState WelcomeTable(IRandomSource random)
    {
        var state = State(random);
        TurnFlow.SelectNext(state, Side.Shadow);
        state = Play(state, Welcome);
        state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Face = DieFace.WillOfTheWest;
        return state;
    }

    private static MatchState WestAttempt(IRandomSource random, bool draw)
    {
        var state = State(random);
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Take(2))
        {
            die.Face = DieFace.Eye;
            die.Location = DieLocation.Hunt;
            die.HuntOrigin = "allocated";
        }
        Unit(state, "sauron", UnitKind.Nazgul, "dale");
        Unit(state, "sauron", UnitKind.Nazgul, "edoras");
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Id is One or Two ? "pool" : "set-aside";
        TurnFlow.SelectNext(state, Side.Shadow);
        state = Play(state, West);
        return draw ? Execute(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Dain] }, 0) : state;
    }

    private static UnitState Figure(MatchState state, string nation, UnitKind kind, string region)
    {
        var unit = state.Units.First(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve");
        unit.Location = region;
        return unit;
    }

    private static MatchState Combat(IRandomSource random, bool vain)
    {
        var state = State(random);
        foreach (var nation in state.Nations)
        {
            nation.StepsToWar = 0;
            nation.Active = true;
        }
        var fp = new BattleForce { Side = Side.FreePeoples, Region = "lossarnach" };
        var sh = new BattleForce { Side = Side.Shadow, Region = "pelargir" };
        for (var i = 0; i < 2; i++)
        {
            fp.Participants.Add(Figure(state, "gondor", UnitKind.Regular, fp.Region).Id);
            sh.Participants.Add(Figure(state, "sauron", UnitKind.Regular, sh.Region).Id);
        }
        foreach (var id in new[] { SovereignCatalog.Theoden, SovereignCatalog.Denethor })
        {
            state.Units.Single(u => u.Id == id).Location = fp.Region;
            fp.Participants.Add(id);
        }
        if (vain)
            for (var i = 0; i < 6; i++)
                sh.Participants.Add(Figure(state, "sauron", UnitKind.Nazgul, sh.Region).Id);
        Give(state, vain ? West : Service);
        BattleRuntime.Begin(state, new() { Attacker = fp, Defender = sh, Region = sh.Region, Stage = BattleStage.AttackerCard });
        new BattleRuntime().Continue(state, random);
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = vain ? "" : Service });
        return Execute(state, CommandKind.CommitCombatCard, new() { CardId = vain ? West : "" }, new int[4]);
    }
}
