using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

internal static class CharacterScenario
{
    private static readonly GameEngine Engine = new(new CharacterCommands(), new HuntCommands(), new EffectCommands(),
        new FellowshipCommands(), new BattleCommands(), new DeclineReactionHandler());

    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["entry"] = Entry(random),
        ["grey"] = Grey(random),
        ["hobbits"] = Hobbits(random),
        ["white-rider"] = WhiteRider(random),
        ["travel-drag"] = Travel(random),
        ["separate-drag"] = FellowshipScenario.Actions(random)
    };

    private static MatchState Travel(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        Put(state, "base.character.gimli", "hollin");
        Put(state, "base.character.legolas", "dale");
        return state;
    }

    private static MatchState Entry(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        foreach (var nation in state.Nations)
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
        Put(state, CharacterRules.Grey, "casualties");
        Put(state, CharacterRules.Strider, "pelargir");
        Put(state, CharacterRules.Saruman, "orthanc");
        state.MinionEverEntered = true;
        foreach (var die in state.Dice.Where(d => d.Owner == Side.FreePeoples).Take(2))
            die.Face = DieFace.WillOfTheWest;
        state.Dice.First(d => d.Owner == Side.Shadow).Face = DieFace.Muster;
        foreach (var unit in state.Units.Where(u => u.Kind == UnitKind.Nazgul))
            unit.Location = "minas-morgul";
        state.Fellowship.Guide = "base.character.gimli";
        return state;
    }

    private static MatchState Grey(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        const string card = "base.fp.character.12";
        GiveCard(state, Side.FreePeoples, card);
        state.Dice[0].Face = DieFace.Event;
        return Execute(state, CommandKind.PlayEvent, new() { CardId = card, DieId = state.Dice[0].Id });
    }

    private static MatchState Hobbits(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        state.Fellowship.RegionId = "hollin";
        state.Fellowship.Progress = 0;
        state.Fellowship.Companions = ["base.character.merry", "base.character.pippin"];
        state.Fellowship.Guide = "base.character.merry";
        state.Dice.First(d => d.Owner == Side.Shadow).Location = DieLocation.Hunt;
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Id == "base.hunt.standard.3.0.1" ? "pool" : "removed";
        return Execute(state, CommandKind.MoveFellowship, new() { DieId = state.Dice[0].Id });
    }

    private static MatchState WhiteRider(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        foreach (var nation in state.Nations)
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
        var attacker = state.Units.First(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular);
        attacker.Location = "minas-tirith";
        state.Units.First(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular).Location = "lossarnach";
        var white = Put(state, CharacterRules.White, "minas-tirith");
        Put(state, CharacterRules.WitchKing, "lossarnach");
        state.Fellowship.Guide = CharacterRules.Strider;
        return Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = state.Dice[0].Id,
            MovementCost = DieFace.Character,
            Moves = [new() { From = "minas-tirith", To = "lossarnach", UnitIds = [attacker.Id], CharacterIds = [white.Id] }]
        });
    }

    private static CharacterState Put(MatchState state, string definition, string region)
    {
        var figure = definition == CharacterRules.White ? CharacterRules.Grey : definition;
        var character = CharacterCatalog.Companion(state, figure);
        CharacterLifecycle.ApplyVersion(character, CharacterCatalog.Base[definition]);
        state.Fellowship.Companions.Remove(figure);
        character.Location = region;
        state.Characters.Add(character);
        CharacterLifecycle.ReconcileDice(state);
        return character;
    }

    private static void GiveCard(MatchState state, Side side, string card)
    {
        foreach (var deck in state.Player(side).Decks.Values)
            deck.Remove(card);
        HandRules.Add(state.Player(side), card);
    }

    private static MatchState Execute(MatchState state, CommandKind kind, CommandPayload payload)
    {
        var result = Engine.Execute(state, state.Pending!.Owner, new()
        {
            MatchId = state.Id,
            ExpectedRevision = state.Revision,
            DecisionId = state.Pending.Id,
            CommandId = Guid.NewGuid().ToString("N"),
            Kind = kind,
            Payload = payload
        }, new SuccessfulHunt());
        if (!result.Receipt.Accepted)
            throw new InvalidOperationException("Invalid character scenario: " + result.Receipt.Error);
        return result.State;
    }

    private sealed class SuccessfulHunt : IRandomSource { public int Next(int upperBound) => upperBound - 1; }
}
