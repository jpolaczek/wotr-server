using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

// Deliberately small pools and recorded rolls belong only to these integration fixtures.
internal static class FellowshipScenario
{
    public static MatchState Actions(IRandomSource random)
    {
        var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), random);
        TurnFlow.Start(state);
        state.Phase = GamePhase.ActionResolution;
        state.Fellowship.Progress = 4;
        foreach (var die in state.Dice.Where(d => d.Location == DieLocation.Unrolled))
        {
            die.Location = DieLocation.Available;
            die.Face = DieFace.Character;
        }
        TurnFlow.SelectNext(state, Side.FreePeoples);
        return state;
    }

    public static MatchState Declaration(IRandomSource random)
    {
        var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), random);
        TurnFlow.Start(state);
        state.Fellowship.Progress = 5;
        state.Fellowship.Corruption = 1;
        return state;
    }

    public static MatchState Hunt(IRandomSource random)
    {
        var state = Actions(random);
        state.Fellowship.RegionId = "moria";
        state.Fellowship.Progress = 2;
        var grey = CharacterCatalog.Companion(state, "base.character.gandalf-grey");
        grey.Location = "casualties";
        state.Characters.Add(grey);
        state.Fellowship.Companions.Remove(grey.Id);
        state.Fellowship.Guide = "base.character.strider";
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow).Take(2))
        {
            die.Location = DieLocation.Hunt;
            die.HuntOrigin = "allocated";
        }
        Pool(state, "base.hunt.standard.2.1.1");
        // Later draws refill only old discards, never a tile spent earlier in this same action.
        foreach (var id in new[] { "base.hunt.standard.2.0.1", "base.hunt.standard.2.0.2" })
            state.HuntTiles.Single(t => t.Id == id).Location = "discard";
        var engine = new GameEngine(new HuntCommands());
        var result = engine.Execute(state, Side.FreePeoples, new()
        {
            MatchId = state.Id,
            CommandId = Guid.NewGuid().ToString("N"),
            ExpectedRevision = state.Revision,
            DecisionId = state.Pending!.Id,
            Kind = CommandKind.MoveFellowship,
            Payload = new() { DieId = state.Dice.First(d => d.Owner == Side.FreePeoples).Id }
        }, new RecordedRoll());
        if (!result.Receipt.Accepted || result.State.Pending?.Kind != DecisionKind.HuntReroll)
            throw new InvalidOperationException("Hunt fixture must stop at the reroll decision.");
        return result.State;
    }

    public static MatchState Entry(IRandomSource random)
    {
        var state = Declaration(random);
        state.Fellowship.RegionId = "morannon";
        state.Fellowship.Progress = 0;
        state.Fellowship.Hidden = false;
        return state;
    }

    public static MatchState Mordor(IRandomSource random, int corruption)
    {
        var state = Actions(random);
        state.Fellowship.RegionId = MordorRules.Track;
        state.Fellowship.MordorStep = 4;
        state.Fellowship.Progress = 0;
        state.Fellowship.Corruption = corruption;
        Pool(state, "base.hunt.standard.1.0.1");
        return state;
    }

    private static void Pool(MatchState state, string id)
    {
        if (state.HuntTiles.All(t => t.Id != id))
            throw new InvalidOperationException("Unknown fixture tile.");
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Id == id ? "pool" : "removed";
    }

    private sealed class RecordedRoll : IRandomSource
    {
        private readonly Queue<int> rolls = new([0, 5]);
        public int Next(int maximum) => rolls.Dequeue();
    }
}
