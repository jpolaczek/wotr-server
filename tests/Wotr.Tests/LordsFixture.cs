using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

// Focused rule fixtures; full expansion lobby startup remains gated until T33/UI-09/T34.
internal static class LordsFixture
{
    public static GameEngine Engine(EffectCatalog? catalog = null) => new(
        new TurnCommands(),
        new LordsDiceCommands(),
        new EffectCommands(catalog),
        new FellowshipCommands(),
        new CharacterCommands(),
        new HuntCommands(),
        new BattleCommands(catalog),
        new StrongholdCommands(),
        new MusterCommands(),
        new MovementCommands(),
        new DeclineReactionHandler(),
        new KeeperRingCommands(),
        new LordsSetupCommands(),
        new CouncilCommands(),
        new SovereignCommands(),
        new CorruptionCommands(),
        new KingsDiceCommands(),
        new DismayCommands());

    public static Projection Views() => new(
        new TurnOptions(),
        new CoreOptions(),
        new LordsDiceCommands(),
        new EffectOptions(),
        new HuntOptions(),
        new BattleOptions(),
        new StrongholdCommands(),
        new KeeperRingCommands(),
        new LordsSetupCommands(),
        new CouncilCommands(),
        new SovereignCommands(),
        new CorruptionCommands(),
        new KingsDiceCommands(),
        new DismayCommands());

    public static MatchState Setup()
    {
        var state = KernelTests.NewGame();
        state.Configuration.Lords = true;
        LordsDiceSetup.Initialize(state);
        return state;
    }

    public static MatchState Actions()
    {
        var state = Setup();
        state.Turn = 2;
        state.Phase = GamePhase.ActionResolution;
        foreach (var die in state.Dice.Where(d => d.Source == "standard"))
        {
            die.Location = DieLocation.Available;
            die.Face = DieFace.Character;
        }
        TurnFlow.SelectNext(state, Side.FreePeoples);
        return state;
    }

    public static CharacterState Put(MatchState state, string definition, string location)
    {
        var character = state.Characters.Find(c => c.DefinitionId == definition);
        if (character is null)
        {
            character = new() { Id = definition, DefinitionId = definition, Side = definition.Contains("balrog") || definition.Contains("gothmog") ||
                definition.Contains("witch-king") ? Side.Shadow : Side.FreePeoples };
            state.Characters.Add(character);
        }
        character.Location = location;
        return character;
    }

    public static DieState Die(MatchState state, string id) => state.Dice.Single(d => d.Id == id);

    public static MatchState Do(MatchState state, CommandKind kind, CommandPayload? payload = null, IRandomSource? random = null)
    {
        var result = Engine().Execute(state, state.Pending!.Owner, BattleFixture.Command(state, kind, payload), random ?? new SequenceRandom());
        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        return result.State;
    }

    public static void Reject(MatchState state, CommandKind kind, CommandPayload payload)
    {
        var before = StateCodec.Write(state);
        var random = new SequenceRandom();
        var result = Engine().Execute(state, state.Pending!.Owner, BattleFixture.Command(state, kind, payload), random);
        Assert.False(result.Receipt.Accepted);
        Assert.Equal(before, StateCodec.Write(result.State));
        Assert.Equal(0, random.Calls);
    }
}
