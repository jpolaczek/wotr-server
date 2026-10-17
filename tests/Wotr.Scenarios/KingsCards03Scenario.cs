using Wotr.Contracts;
using Wotr.Domain;
using static CardPackScenario;
using static KingsCards01Scenario;
using static Wotr.Domain.KingsCards03;

internal static class KingsCards03Scenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["kings-valinor-regions"] = ValinorChoice(random, false),
        ["kings-valinor-losses"] = ValinorChoice(random, true),
        ["kings-stormcrow"] = StormcrowChoice(random),
        ["kings-folly"] = FollyTable(random),
        ["kings-king-revealed"] = KingChoice(random),
        ["kings-palantir-event"] = PalantirTable(random, Side.Shadow),
        ["kings-palantir-draw"] = PalantirTable(random, Side.Shadow),
        ["kings-palantir-removal"] = PalantirTable(random, Side.FreePeoples),
        ["kings-power"] = PowerChoice(random)
    };

    private static MatchState ShadowState(IRandomSource random)
    {
        var state = State(random);
        state.Player(Side.Shadow).Hand.Clear();
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    private static MatchState ValinorChoice(IRandomSource random, bool losses)
    {
        var state = ShadowState(random);
        state.Control["woodland-realm"] = Side.Shadow;
        state.Units.Single(u => u.Id == SovereignCatalog.Thranduil).Location = "dale";
        Unit(state, "elves", UnitKind.Regular, "lorien");
        Unit(state, "north", UnitKind.Elite, "lorien");
        Unit(state, "elves", UnitKind.Regular, "rivendell");
        state = Play(state, Valinor);
        return losses ? Execute(state, CommandKind.ChooseEffect, new() { Ids = ["lorien"] }, 5) : state;
    }

    private static MatchState StormcrowChoice(IRandomSource random)
    {
        var state = ShadowState(random);
        state.Fellowship.RegionId = "dale";
        Unit(state, "north", UnitKind.Regular, "dale");
        Unit(state, "north", UnitKind.Elite, "bree");
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Id == "base.hunt.standard.1.0.1" ? "pool" : "set-aside";
        Give(state, Stormcrow);
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        die.Face = DieFace.Muster;
        return Execute(state, CommandKind.PlayEvent, new() { CardId = Stormcrow, DieId = die.Id,
            CardCost = DieFace.Muster, NationId = "north" }, 0);
    }

    private static MatchState FollyTable(IRandomSource random)
    {
        var state = ShadowState(random);
        Character(state, "base.character.boromir", "minas-tirith");
        return Play(state, Denethor);
    }

    private static MatchState KingChoice(IRandomSource random)
    {
        var state = ShadowState(random);
        foreach (var id in new[] { SovereignCatalog.Denethor, SovereignCatalog.Dain })
            SovereignLifecycle.Awaken(state, SovereignLifecycle.Find(state, id)!, SovereignCatalog.All[id].Home);
        return Play(state, King);
    }

    private static MatchState PalantirTable(IRandomSource random, Side actor)
    {
        var state = Play(ShadowState(random), Palantir);
        state.Player(Side.Shadow).Table.Add(ShadowCharacterCards04.Palantir);
        Give(state, Threats);
        foreach (var die in state.Dice.Where(d => d.Location == DieLocation.Available))
            die.Face = die.Owner == Side.Shadow ? DieFace.Muster : DieFace.Army;
        TurnFlow.SelectNext(state, actor);
        return state;
    }

    internal static MatchState PowerChoice(IRandomSource random, string card = Palantir)
    {
        var state = State(random);
        foreach (var nation in state.Nations)
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
        var fp = new BattleForce { Side = Side.FreePeoples, Region = "osgiliath" };
        var sh = new BattleForce { Side = Side.Shadow, Region = "minas-tirith" };
        for (var i = 0; i < 2; i++)
        {
            fp.Participants.Add(Figure(state, "gondor", fp.Region));
            sh.Participants.Add(Figure(state, "sauron", sh.Region));
        }
        var denethor = SovereignLifecycle.Find(state, SovereignCatalog.Denethor)!;
        state.Units.RemoveAll(u => u.Id == denethor.Id);
        denethor.Status = SovereignStatus.Corrupted;
        denethor.CorruptedRegion = sh.Region;
        state.Units.Single(u => u.Id == SovereignCatalog.Theoden).Location = sh.Region;
        Give(state, card);
        BattleRuntime.Begin(state, new() { Attacker = fp, Defender = sh, Region = sh.Region, Stage = BattleStage.AttackerCard });
        new BattleRuntime().Continue(state, random);
        state = Execute(state, CommandKind.CommitCombatCard, new());
        return Execute(state, CommandKind.CommitCombatCard, new() { CardId = card });
    }

    private static string Figure(MatchState state, string nation, string region)
    {
        var unit = state.Units.First(u => u.Location == "reserve" && u.Nation == nation && u.Kind == UnitKind.Regular);
        unit.Location = region;
        return unit.Id;
    }
}
