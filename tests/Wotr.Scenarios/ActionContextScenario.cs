using Wotr.Contracts;
using Wotr.Domain;

internal static class ActionContextScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["ui42-moves"] = Moves(random),
        ["ui42-battles"] = Battles(random),
        ["ui42-hunts"] = Hunts(random),
        ["ui42-hosts"] = Hosts(random),
        ["ui42-ring"] = Ring(random),
        ["ui42-politics"] = Politics(random),
        ["ui42-cards"] = Cards(random),
        ["ui42-characters"] = Characters(random),
        ["ui43-battle"] = BattleScenario.Field(random),
        ["ui43-stale"] = BattleScenario.Field(random),
        ["ui44-window"] = BattleWindowScenario.Create(random),
        ["ui44-stale"] = BattleWindowScenario.Create(random),
        ["ui44-layout"] = BattleWindowScenario.Create(random, relief: true),
        ["ui45-rerolls"] = BattleResolutionScenario.Rerolls(random),
        ["ui45-losses"] = BattleResolutionScenario.OrderedLosses(random),
        ["ui46-retreat"] = BattleScenario.Retreat(random),
        ["ui46-report"] = BattleEndingScenario.Report(random),
        ["ui46-shelter"] = BattleScenario.Stronghold(random),
        ["ui46-siege"] = BattleEndingScenario.Siege(random),
        ["ui46-no-regular"] = BattleEndingScenario.Siege(random, noRegular: true),
        ["ui46-weakness"] = BattleEndingScenario.Siege(random, weakness: true)
    };

    private static MatchState Moves(IRandomSource random)
    {
        var state = BattleScenario.Field(random);
        foreach (var leader in state.Units.Where(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Leader))
            BoardRules.ReturnToReserve(leader);
        foreach (var elite in state.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Elite && u.Location == "reserve").Take(2))
            elite.Location = "minas-tirith";
        foreach (var regular in state.Units.Where(u => u.Nation == "rohan" && u.Kind == UnitKind.Regular && u.Location == "reserve").Take(2))
            regular.Location = "edoras";
        state = Move(state, "minas-tirith", "osgiliath");
        return Move(state, "edoras", "westemnet");
    }

    private static MatchState Move(MatchState state, string from, string to) => ExpansionUiFixture.Execute(state,
        CommandKind.MoveArmies, new()
        {
            Stepwise = true, MovementCost = DieFace.Army,
            DieId = state.Resolution.ActionSequence?.DieId ?? ExpansionUiFixture.Die(state, state.Pending!.Owner).Id,
            Moves = [new() { From = from, To = to, UnitIds = state.Units.Where(u => u.Location == from).Select(u => u.Id).ToList() }]
        });

    private static MatchState Battles(IRandomSource random)
    {
        var state = BattleScenario.Retreat(random);
        state = ExpansionUiFixture.Execute(state, CommandKind.StopBattle);
        state = ExpansionUiFixture.Execute(state, CommandKind.DiscardDie,
            new() { DieId = ExpansionUiFixture.Die(state, Side.Shadow).Id });
        return ExpansionUiFixture.Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = ExpansionUiFixture.Die(state, Side.FreePeoples).Id, MovementCost = DieFace.Army,
            Moves = [new() { From = "minas-tirith", To = "lossarnach",
                UnitIds = state.Units.Where(u => u.Location == "minas-tirith").Select(u => u.Id).ToList() }]
        });
    }

    private static MatchState Hunts(IRandomSource random)
    {
        var state = HuntInteractionScenario.Rolled(random, false);
        state = ExpansionUiFixture.Execute(state, CommandKind.DiscardDie,
            new() { DieId = ExpansionUiFixture.Die(state, Side.Shadow).Id });
        return ExpansionUiFixture.Execute(state, CommandKind.MoveFellowship,
            new() { DieId = ExpansionUiFixture.Die(state, Side.FreePeoples).Id }, 0);
    }

    private static MatchState Hosts(IRandomSource random)
    {
        var state = ExpansionUiFixture.Actions(random, new());
        state = ExpansionUiFixture.Execute(state, CommandKind.Pass);
        return ExpansionUiFixture.Execute(state, CommandKind.DiscardDie,
            new() { DieId = ExpansionUiFixture.Die(state, Side.Shadow).Id });
    }

    private static MatchState Ring(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        return ExpansionUiFixture.Execute(state, CommandKind.UseRing, new()
        {
            DieId = ExpansionUiFixture.Die(state, Side.FreePeoples).Id,
            RingId = state.Rings.First(r => r.Owner == Side.FreePeoples).Id, Face = DieFace.Muster
        });
    }

    private static MatchState Politics(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        var die = ExpansionUiFixture.Die(state, Side.FreePeoples);
        die.Face = DieFace.Muster;
        var elves = state.Nations.Single(n => n.Id == "elves");
        elves.Active = true;
        elves.StepsToWar = 2;
        return ExpansionUiFixture.Execute(state, CommandKind.AdvancePolitics, new() { DieId = die.Id, NationId = elves.Id });
    }

    private static MatchState Cards(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        ExpansionUiFixture.Die(state, Side.FreePeoples).Face = DieFace.Event;
        return ExpansionUiFixture.Execute(state, CommandKind.DrawCard,
            new() { DieId = ExpansionUiFixture.Die(state, Side.FreePeoples).Id, Deck = "character" });
    }

    private static MatchState Characters(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        var gimli = ExpansionUiFixture.Put(state, "base.character.gimli", "hollin");
        return ExpansionUiFixture.Execute(state, CommandKind.MoveCompanions, new()
        {
            DieId = ExpansionUiFixture.Die(state, Side.FreePeoples).Id,
            CompanionMoves = [new() { Ids = [gimli.Id], Route = ["hollin", "moria"] }]
        });
    }
}
