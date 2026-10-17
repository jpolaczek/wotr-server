using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

// These are pre-game integration positions. The running server keeps its ordinary RNG.
internal static class BaseBoundaryScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["boundary-random-companion"] = RandomCompanion(random),
        ["boundary-field-defense"] = FieldDefense(random),
        ["boundary-military-fp"] = Military(random, 4, 0),
        ["boundary-military-sh"] = Military(random, 0, 10),
        ["boundary-military-priority"] = Military(random, 4, 10)
    };

    private static MatchState Actions(IRandomSource random, Side actor)
    {
        var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), random);
        TurnFlow.Start(state);
        state.Phase = GamePhase.ActionResolution;
        foreach (var die in state.Dice.Where(d => d.Location == DieLocation.Unrolled))
        {
            die.Location = DieLocation.Available;
            die.Face = DieFace.Army;
        }
        TurnFlow.SelectNext(state, actor);
        return state;
    }

    private static MatchState RandomCompanion(IRandomSource random)
    {
        var state = Actions(random, Side.FreePeoples);
        var companions = new[] { "base.character.gimli", "base.character.legolas", "base.character.boromir" };
        foreach (var id in state.Fellowship.Companions.Except(companions).ToArray())
        {
            var companion = CharacterCatalog.Companion(state, id);
            companion.Location = "casualties";
            state.Characters.Add(companion);
            state.Fellowship.Companions.Remove(id);
        }
        state.Fellowship.Guide = "base.character.gimli";
        state.Fellowship.RegionId = MordorRules.Track;
        state.Fellowship.MordorStep = 2;
        state.Fellowship.Corruption = 2;
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Id == "base.hunt.standard.3.0.1" ? "pool" : "removed";
        var die = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available);
        die.Face = DieFace.Character;
        var command = new GameCommand
        {
            MatchId = state.Id,
            CommandId = Guid.NewGuid().ToString("N"),
            ExpectedRevision = state.Revision,
            DecisionId = state.Pending!.Id,
            Kind = CommandKind.MoveFellowship,
            Payload = new() { DieId = die.Id }
        };
        var result = new GameEngine(new HuntCommands()).Execute(state, Side.FreePeoples, command, random);
        if (!result.Receipt.Accepted || result.State.Pending?.Kind != DecisionKind.HuntDamage)
            throw new InvalidOperationException("Random companion fixture must stop before the player's casualty choice.");
        return result.State;
    }

    private static MatchState FieldDefense(IRandomSource random)
    {
        var state = Actions(random, Side.Shadow);
        EmptyBoard(state);
        foreach (var nation in state.Nations)
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
        PlaceRegulars(state, "sauron", "osgiliath", 7);
        PlaceRegulars(state, "gondor", "minas-tirith", 7);
        state.Control["osgiliath"] = Side.Shadow;
        return state;
    }

    private static MatchState Military(IRandomSource random, int freePeoplesPoints, int shadowPoints)
    {
        var state = Actions(random, Side.FreePeoples);
        EmptyBoard(state);
        SetPoints(state, Side.FreePeoples, freePeoplesPoints);
        SetPoints(state, Side.Shadow, shadowPoints);
        foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
            foreach (var die in state.Dice.Where(d => d.Owner == side && d.Location == DieLocation.Available).Skip(1))
                die.Location = DieLocation.Spent;
        return state;
    }

    private static void EmptyBoard(MatchState state)
    {
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
    }

    private static void PlaceRegulars(MatchState state, string nation, string region, int count)
    {
        var units = state.Units.Where(u => u.Nation == nation && u.Kind == UnitKind.Regular && u.Location == "reserve")
            .Take(count).ToArray();
        if (units.Length != count)
            throw new InvalidOperationException("Boundary fixture requires available regular figures.");
        foreach (var unit in units)
            unit.Location = region;
    }

    private static void SetPoints(MatchState state, Side side, int points)
    {
        var remaining = points;
        foreach (var region in BoardRules.Regions.Values
            .Where(r => r.OriginalOwner == TurnFlow.Other(side) && r.VictoryPoints > 0)
            .OrderByDescending(r => r.VictoryPoints).ThenBy(r => r.Id, StringComparer.Ordinal))
        {
            if (region.VictoryPoints > remaining)
                continue;
            state.Control[region.Id] = side;
            remaining -= region.VictoryPoints;
        }
        if (remaining != 0 || MilitaryVictory.Points(state, side) != points)
            throw new InvalidOperationException("Boundary fixture has an incorrect military score.");
    }
}
