using Wotr.Contracts;
using Wotr.Domain;
using static ExpansionUiFixture;

internal static class BoardCompletionScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random)
    {
        var characters = CharacterScenario.Create(random);
        return new()
        {
            ["ui71-history"] = Transformation(characters["entry"]),
            ["ui71-setup"] = Setup(random),
            ["ui71-hobbits"] = characters["hobbits"],
            ["ui71-white-rider"] = characters["white-rider"],
            ["ui71-awakened"] = Awakened(random),
            ["ui71-council-device"] = CouncilEntry(random)
        };
    }

    public static MatchState CouncilEntry(IRandomSource random) => ExpansionUiScenario.Create(random)["ui09-entry"];

    private static MatchState Transformation(MatchState state)
    {
        state = Execute(state, CommandKind.EnterCharacter, new()
        {
            CharacterId = CharacterRules.Aragorn, RegionId = "pelargir",
            DieId = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Face == DieFace.WillOfTheWest).Id
        });
        state = Execute(state, CommandKind.DiscardDie, new() { DieId = Die(state, Side.Shadow).Id });
        if (state.Pending?.Owner != Side.FreePeoples ||
            state.Characters.Single(c => c.Id == CharacterRules.Strider).DefinitionId != CharacterRules.Aragorn)
            throw new InvalidOperationException("UI-71 requires a recorded transformation before the next FP choice.");
        return state;
    }

    private static MatchState Setup(IRandomSource random)
    {
        var state = Start(random, Full());
        state = Execute(state, CommandKind.ChooseCompanionVersions, new()
        {
            GuideId = CharacterRules.Strider,
            Ids = [LordsCharacters.Gandalf, "fate.character.gimli-dwarven-lord", "lords.character.legolas", "lords.character.strider"]
        });
        if (state.Pending?.Kind != DecisionKind.CouncilHomelands)
            throw new InvalidOperationException("UI-71 setup history must precede the separate homeland choice.");
        return state;
    }

    private static MatchState Awakened(IRandomSource random)
    {
        var state = SovereignPanelScenario.Create(random)["ui59-awaken"];
        return Execute(state, CommandKind.AwakenSovereign, new()
            { CharacterId = SovereignCatalog.Dain, DieId = Die(state, Side.FreePeoples).Id, Route = ["erebor"] });
    }
}
