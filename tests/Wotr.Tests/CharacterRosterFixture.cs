using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Tests;

internal static class CharacterRosterFixture
{
    public static MatchState Actions(int expansions = 0, Side actor = Side.FreePeoples, bool council = false)
    {
        var configuration = new MatchConfiguration
        {
            Lords = (expansions & 1) != 0,
            Kings = (expansions & 2) != 0,
            Fate = (expansions & 4) != 0,
            Council = council
        };
        // Setup shuffles the real configured decks; later projection/command entropy is measured separately.
        var state = MatchFactory.Create("roster-test", new SequenceRandom(new int[512]), configuration);
        if (configuration.Kings)
            KingsSetup.Initialize(state);
        if (configuration.Lords)
        {
            LordsCompanionSetup.Begin(state);
            LordsCompanionSetup.Choose(state, [CharacterRules.Grey], CharacterRules.Strider);
        }
        state.Turn = 2;
        state.Phase = GamePhase.ActionResolution;
        foreach (var die in state.Dice.Where(d => d.Source == "standard" && d.Location != DieLocation.Removed))
        {
            die.Location = DieLocation.Available;
            die.Face = die.Owner == Side.FreePeoples ? DieFace.WillOfTheWest : DieFace.Muster;
        }
        TurnFlow.SelectNext(state, actor);
        return state;
    }

    public static List<CharacterRosterView> Roster(MatchState state, Side side) =>
        LordsFixture.Views().For(state, side).OwnCharacterRoster;

    public static CharacterRosterView Row(MatchState state, Side side, string figure) =>
        Roster(state, side).Single(c => c.FigureId == figure);

    public static CharacterState Put(MatchState state, string definitionId, string region, string? figureId = null)
    {
        var id = figureId ?? definitionId;
        var character = state.Characters.Find(c => c.Id == id) ?? new CharacterState { Id = id };
        CharacterLifecycle.ApplyVersion(character, CharacterCatalog.All[definitionId]);
        character.Location = region;
        state.Fellowship.Companions.Remove(id);
        if (!state.Characters.Contains(character))
            state.Characters.Add(character);
        return character;
    }
}
