using Wotr.Contracts;
using Wotr.Domain;

internal static class FellowshipGuideScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["ui34-guide-change"] = FellowshipScenario.Declaration(random),
        ["ui34-guide-keep"] = FellowshipScenario.Declaration(random),
        ["ui34-guide-council"] = Council(random),
        ["ui34-separate-siege"] = Siege(random),
        ["ui34-separate-mordor"] = Mordor(random)
    };

    private static MatchState Council(IRandomSource random)
    {
        var state = ExpansionUiFixture.Start(random, new() { Lords = true, Council = true });
        state = ExpansionUiFixture.Execute(state, CommandKind.ChooseCompanionVersions,
            new() { GuideId = CharacterRules.Strider, Ids = [LordsCharacters.Gandalf, "lords.character.strider"] });
        return ExpansionUiFixture.Execute(state, CommandKind.ChooseCouncilHomelands);
    }

    private static MatchState Siege(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        state.Fellowship.RegionId = "minas-tirith";
        state.Fellowship.Progress = 0;
        foreach (var unit in state.Units.Where(u => u.Location == "minas-tirith" && BoardRules.Owner(u) == Side.FreePeoples))
            unit.Garrison = true;
        state.Units.First(u => BoardRules.Owner(u) == Side.Shadow && u.Kind == UnitKind.Regular).Location = "minas-tirith";
        return state;
    }

    private static MatchState Mordor(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        state.Fellowship.RegionId = MordorRules.Track;
        state.Fellowship.MordorStep = 2;
        state.Fellowship.Progress = 0;
        DiceRules.Pay(state, Side.FreePeoples, state.Dice.First(d => d.Owner == Side.FreePeoples).Id, DieFace.Character);
        FellowshipFlow.BeginSeparation(state, "ui34-forced-separation", FellowshipReturn.FinishAction);
        return state;
    }
}
