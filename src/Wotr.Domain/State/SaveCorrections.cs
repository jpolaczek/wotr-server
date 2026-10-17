namespace Wotr.Domain;

internal static class SaveCorrections
{
    // The pre-release SRC-02 catalog misread Strider's North icon as the general FP icon.
    // Fix the copied statistic in existing saves, never Aragorn sharing the same figure ID.
    public static void Apply(MatchState state)
    {
        foreach (var character in state.Characters)
            if (character.DefinitionId == CharacterRules.Strider && character.NationIcon == "free-peoples")
                character.NationIcon = "north";
    }
}
