using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

internal static class CharacterFixture
{
    public static GameEngine Engine(EffectCatalog? catalog = null) => new(new TurnCommands(), new MusterCommands(), new CharacterCommands(), new MovementCommands(),
        new FellowshipCommands(), new HuntCommands(catalog), new BattleCommands(catalog), new StrongholdCommands(), new EffectCommands(catalog), new DeclineReactionHandler());

    public static MatchState Do(MatchState state, CommandKind kind, CommandPayload? payload = null, IRandomSource? random = null, EffectCatalog? catalog = null)
    {
        var result = Engine(catalog).Execute(state, state.Pending!.Owner, BattleFixture.Command(state, kind, payload), random ?? new SequenceRandom());
        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        return result.State;
    }

    public static CharacterState Put(MatchState state, string definitionId, string region)
    {
        var character = CharacterCatalog.Companion(state, definitionId);
        CharacterLifecycle.ApplyVersion(character, CharacterCatalog.Base[definitionId]);
        character.Location = region;
        state.Fellowship.Companions.Remove(character.Id);
        if (!state.Characters.Contains(character))
            state.Characters.Add(character);
        CharacterLifecycle.ReconcileDice(state);
        return character;
    }

    public static CommandPayload Cost(MatchState state, DieFace face)
    {
        var die = state.Dice.First(d => d.Owner == state.Pending!.Owner && d.Location == DieLocation.Available);
        die.Face = face;
        return new() { DieId = die.Id };
    }

    public static void Reject(MatchState state, CommandKind kind, CommandPayload payload)
    {
        var before = StateCodec.Write(state);
        var random = new SequenceRandom();
        var result = Engine().Execute(state, state.Pending!.Owner, BattleFixture.Command(state, kind, payload), random);
        Assert.False(result.Receipt.Accepted);
        Assert.Equal(before, StateCodec.Write(state));
        Assert.Equal(0, random.Calls);
    }
}
