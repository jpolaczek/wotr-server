using Wotr.Contracts;

namespace Wotr.Domain;

public static class FateGimli
{
    public const string Figure = "base.character.gimli";
    public const string Standard = "fate.character.gimli";
    public const string Alternate = "fate.character.gimli-dwarven-lord";
    public const string Renown = "politics-dwarves-at-fate-homelands";
    public const string GuideReaction = "fate.gimli-guide";
    public static IReadOnlyList<string> Homelands { get; } = ["iron-hills", "ered-luin"];

    public static string DefaultVersion(MatchConfiguration configuration, string figureId) =>
        configuration.Fate && figureId == Figure ? Standard : figureId;

    public static bool MatchesName(CharacterState character, string definitionId) =>
        definitionId == Figure && character.DefinitionId is Standard or Alternate;

    public static EffectReaction Reaction { get; } = new(GuideReaction, "hunt.guide", "council.gimli-guide",
        new CouncilSeparationCondition(), RequiredGuideId: Alternate);
}
