using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class SmeagolRules
{
    public const string WeShallGetIt = "lords.sh.special.05";
    public static bool Present(MatchState state) => state.Fellowship.Companions.Contains(LordsCharacters.Smeagol);
    public static bool CanEnter(MatchState state) => state.Configuration.Lords &&
        state.Lords is { SmeagolLeft: false, GollumEntered: false } && state.Fellowship.Guide != CharacterRules.Gollum;

    public static int Level(MatchState state) => state.Fellowship.Companions.Where(id => id != LordsCharacters.Smeagol)
        .Select(id => CharacterCatalog.Companion(state, id).Level).DefaultIfEmpty(0).Max();

    public static void Enter(MatchState state)
    {
        if (Present(state))
            return;
        var character = new CharacterState { Id = LordsCharacters.Smeagol, Location = "fellowship" };
        CharacterLifecycle.ApplyVersion(character, CharacterCatalog.All[LordsCharacters.Smeagol]);
        state.Characters.Add(character);
        state.Fellowship.Companions.Add(character.Id);
        state.Fellowship.Guide = character.Id;
        state.Events.Add(new() { Revision = state.Revision, Kind = "GuideChanged", Text = "Sméagol joined the Fellowship as its Guide." });
        Reconcile(state);
    }

    public static void Discard(MatchState state, bool enterEvent = true)
    {
        state.Fellowship.Companions.Remove(LordsCharacters.Smeagol);
        var character = state.Characters.Single(c => c.DefinitionId == LordsCharacters.Smeagol);
        character.Location = "removed";
        character.Garrison = false;
        state.Lords!.SmeagolLeft = true;
        state.Events.Add(new() { Revision = state.Revision, Kind = "CompanionDiscarded", Text = "Sméagol left the Fellowship." });
        var shadow = state.Player(Side.Shadow);
        if (!enterEvent || shadow.Table.Contains(WeShallGetIt))
            return;
        shadow.Table.Add(WeShallGetIt);
        state.Events.Add(new() { Revision = state.Revision, Kind = "SpecialEventEntered", Text = $"{WeShallGetIt} entered play." });
    }

    public static void Reconcile(MatchState state)
    {
        if (!Present(state) || state.Fellowship.Companions.Count != 1)
            return;
        Discard(state, enterEvent: false);
        state.Fellowship.Guide = CharacterRules.Gollum;
        state.Lords!.GollumEntered = true;
        state.Events.Add(new() { Revision = state.Revision, Kind = "GuideChanged", Text = "Gollum replaced Sméagol as Guide." });
    }

    public static bool Declared(MatchState state)
    {
        if (!Present(state) || !TravelRules.Sanctuary(state, state.Fellowship.RegionId))
            return false;
        Discard(state);
        return true;
    }
}
