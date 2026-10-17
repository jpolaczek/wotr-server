using Wotr.Domain;

internal static class CharacterRosterActionScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random)
    {
        var characters = CharacterScenario.Create(random);
        var abilities = ExpansionAbilityScenario.Create(random);
        var resources = ExpansionUiScenario.Create(random);
        ExpansionUiFixture.Put(abilities["t34-travel-sh"], CharacterRules.Saruman, "orthanc");
        ExpansionUiFixture.Put(resources["ui09-council-travel"], CharacterRules.WitchKing, "barad-dur");
        return new()
        {
            ["ui67-travel-fp"] = characters["travel-drag"],
            ["ui67-entry"] = characters["entry"],
            ["ui67-travel-sh"] = abilities["t34-travel-sh"],
            ["ui67-balrog"] = abilities["ui22-balrog-travel"],
            ["ui67-ruler"] = resources["ui09-ruler-recruit"],
            ["ui67-council-entry"] = resources["ui09-entry"],
            ["ui67-council-travel"] = resources["ui09-council-travel"],
            ["ui67-fate"] = FateGimliScenario.Create(random)["fate-gimli-messenger"]
        };
    }
}
