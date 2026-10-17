using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Tests;

internal static class CouncilFixture
{
    public static MatchState Actions()
    {
        var state = LordsFixture.Actions();
        state.Configuration.Council = true;
        state.Lords!.Council = new() { HomelandsChosen = true };
        return state;
    }

    public static CharacterState Inside(MatchState state, string name)
    {
        var character = CharacterCatalog.Companion(state, "base.character." + name);
        CharacterLifecycle.ApplyVersion(character, CharacterCatalog.Lords["lords.character." + name]);
        if (!state.Characters.Contains(character))
            state.Characters.Add(character);
        return character;
    }

    public static MatchState Hunt(string tile, params string[] names)
    {
        var state = Actions();
        state.Fellowship.RegionId = "hollin";
        foreach (var name in names)
            Inside(state, name);
        state.Fellowship.Companions = names.Select(name => "base.character." + name).ToList();
        state.Fellowship.Guide = state.Fellowship.Companions[0];
        LordsCompanionSetup.Reveal(state, CharacterCatalog.Companion(state, state.Fellowship.Guide));
        state.Dice.First(d => d.Owner == Side.Shadow).Location = DieLocation.Hunt;
        HuntTests.Pool(state, tile);
        return state;
    }

    public static MatchState Move(MatchState state, params int[] samples) => LordsFixture.Do(state, CommandKind.MoveFellowship,
        new() { DieId = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Id }, new SequenceRandom(samples));
}
