using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class LordsCharacters
{
    public const string Elrond = "lords.character.elrond";
    public const string Galadriel = "lords.character.galadriel";
    public const string Gandalf = "lords.character.gandalf-grey";
    public const string Smeagol = "lords.character.smeagol";
    public const string Balrog = "lords.character.balrog";
    public const string Gothmog = "lords.character.gothmog";
    public const string Mouth = "lords.character.mouth-of-sauron";
    public const string WitchKing = "lords.character.witch-king";
    public const string Household = "household-of-elrond";
    public const string Flame = "flame-of-udun";
    public const string EvilKnowledge = "evil-knowledge";
    public const string GreatSorcery = "great-sorcery";

    public static bool QualifiesForWhite(CharacterDefinition definition) =>
        definition.Role == "Minion" && !definition.Abilities.Contains("not-minion-for-gandalf-white");

    public static bool HouseholdLeader(MatchState state, BattleForce force, UnitState unit) =>
        unit.Nation == "elves" && unit.Kind == UnitKind.Elite &&
        CharacterRules.InPlay(state, Elrond) is { } elrond && elrond.Location == unit.Location &&
        !force.Round.CancelledAbilities.Contains(elrond.Id);

    public static bool CanMusterInSiege(MatchState state, Side actor, string region) =>
        actor == Side.FreePeoples && region == "lorien" && CharacterRules.InPlay(state, Galadriel) is not null;

    public static void SettlementCaptured(MatchState state, Side actor, string region)
    {
        if (actor != Side.Shadow)
            return;
        var id = region switch { "rivendell" => Elrond, "lorien" => Galadriel, _ => "" };
        if (CharacterRules.InPlay(state, id) is not { } character)
            return;
        CharacterLifecycle.Eliminate(state, character);
        state.Events.Add(new()
        {
            Revision = state.Revision,
            Kind = "CharacterEliminated",
            Text = $"{CharacterCatalog.All[id].Title} was eliminated when {region} was captured."
        });
    }
}
