using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class CharacterRules
{
    public const string Grey = "base.character.gandalf-grey";
    public const string White = "base.character.gandalf-white";
    public const string Strider = "base.character.strider";
    public const string Aragorn = "base.character.aragorn";
    public const string Saruman = "base.character.saruman";
    public const string WitchKing = "base.character.witch-king";
    public const string Mouth = "base.character.mouth-of-sauron";
    public const string Gollum = "base.character.gollum";
    public const string Captain = "combat-strength-plus-one";
    public const string TakenAlive = "return-after-fellowship-elimination-except-mordor";
    public const string HobbitGuide = "guide-separate-to-reduce-hunt-one";
    public const string MouthArmy = "muster-as-army-once-per-turn";

    public static bool Has(CharacterState character, string ability) =>
        CharacterCatalog.All.TryGetValue(character.DefinitionId, out var definition) && definition.Abilities.Contains(ability);

    public static bool OnBoard(CharacterState character) => BoardRules.Regions.ContainsKey(character.Location);
    public static bool IsCompanion(CharacterState character) => CharacterCatalog.All.GetValueOrDefault(character.DefinitionId)?.Role == "Companion";
    public static CharacterState? InPlay(MatchState state, string definitionId) =>
        state.Characters.FirstOrDefault(c => c.DefinitionId == definitionId && OnBoard(c));
    public static bool Mobile(CharacterState character) => character.Level > 0 ||
        CharacterCatalog.All.TryGetValue(character.DefinitionId, out var definition) && definition.UnlimitedMovement;

    public static bool SameIdentity(CharacterState character, string definitionId) =>
        CharacterCatalog.All.TryGetValue(character.DefinitionId, out var current) &&
        CharacterCatalog.All.TryGetValue(definitionId, out var target) && current.Identity == target.Identity;

    // Card names can refer to alternate versions, without granting the original version's abilities.
    public static bool MatchesCardName(CharacterState character, string definitionId) => character.DefinitionId == definitionId ||
        CouncilCompanions.MatchesName(character, definitionId) || FateGimli.MatchesName(character, definitionId) ||
        (character.DefinitionId switch
        {
            LordsCharacters.Gandalf => definitionId == Grey,
            LordsCharacters.WitchKing => definitionId == WitchKing,
            _ => false
        });

    public static bool IsWitchKing(CharacterState character) => SameIdentity(character, WitchKing);
    public static CharacterState? WitchKingInPlay(MatchState state) =>
        state.Characters.FirstOrDefault(c => IsWitchKing(c) && OnBoard(c));
    public static int CombatLevel(CharacterState character) => Has(character, "combat-card-level-three") ? 3 : character.Level;
    public static int MovementLevel(CharacterState character) => Has(character, CouncilCompanions.MovementLevelTwo) ? 2 : character.Level;
    public static bool IsHobbit(CharacterState character) => SameIdentity(character, "base.character.merry") || SameIdentity(character, "base.character.pippin");
    public static bool IsStriderOrAragorn(CharacterState character) => MatchesCardName(character, Strider) || character.DefinitionId == Aragorn;

    public static string GuideDefinition(MatchState state) => state.Characters.Find(c => c.Id == state.Fellowship.Guide)?.DefinitionId
        ?? FateGimli.DefaultVersion(state.Configuration, state.Fellowship.Guide);

    public static int CompanionRange(IReadOnlyList<CharacterState> group)
    {
        if (group.Count <= 2 && group.Any(c => c.DefinitionId == White) &&
            group.All(c => c.DefinitionId == White || IsHobbit(c)))
            return 4;
        return group.Max(MovementLevel);
    }

    public static bool Leads(MatchState state, UnitState unit) => unit.Kind is UnitKind.Leader or UnitKind.Nazgul ||
        unit.Kind == UnitKind.Elite && unit.Nation == "isengard" && InPlay(state, Saruman) is not null;

    public static bool CanHide(MatchState state, string dieId) => DiceRules.AvailableDie(state, Side.FreePeoples, dieId) is { } die &&
        (DiceRules.CanPay(die, DieFace.Character) || GuideDefinition(state) == Strider);

    public static void PayAny(MatchState state, Side actor, string dieId)
    {
        var die = DiceRules.AvailableDie(state, actor, dieId) ?? throw new InvalidOperationException("Missing available die.");
        DiceRules.Pay(state, actor, dieId, die.Face);
    }
}
