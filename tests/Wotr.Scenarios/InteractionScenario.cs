using Wotr.Contracts;
using Wotr.Domain;

internal static class InteractionScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random)
    {
        var result = new[] { "receipt", "race", "late-view", "reconnect-press" }
            .ToDictionary(name => name, _ => Actions(random));
        result.Add("notice-battle", BattleScenario.Create(random)["field"]);
        result.Add("ui34-panel-move", FellowshipActions(random, true));
        result.Add("ui34-panel-hide", FellowshipActions(random, false));
        result.Add("ui34-route-stay", FellowshipStay(random));
        foreach (var pair in FellowshipGuideScenario.Create(random))
            result.Add(pair.Key, pair.Value);
        foreach (var pair in HuntInteractionScenario.Create(random))
            result.Add(pair.Key, pair.Value);
        foreach (var pair in HuntAftermathScenario.Create(random))
            result.Add(pair.Key, pair.Value);
        foreach (var pair in HuntActivityScenario.Create(random))
            result.Add(pair.Key, pair.Value);
        foreach (var pair in SovereignPanelScenario.Create(random))
            result.Add(pair.Key, pair.Value);
        foreach (var pair in MordorInteractionScenario.Create(random))
            result.Add(pair.Key, pair.Value);
        foreach (var pair in MapContourScenario.Create(random))
            result.Add(pair.Key, pair.Value);
        foreach (var pair in MapOverrideScenario.Create(random))
            result.Add(pair.Key, pair.Value);
        foreach (var pair in MapAcceptanceScenario.Create(random))
            result.Add(pair.Key, pair.Value);
        foreach (var pair in CharacterRosterScenario.Create(random))
            result.Add(pair.Key, pair.Value);
        foreach (var pair in CharacterRosterActionScenario.Create(random))
            result.Add(pair.Key, pair.Value);
        foreach (var pair in ContextHelpScenario.Create(random))
            result.Add(pair.Key, pair.Value);
        foreach (var pair in InGameContextScenario.Create(random))
            result.Add(pair.Key, pair.Value);
        foreach (var pair in CouncilHomelandScenario.Create(random))
            result.Add(pair.Key, pair.Value);
        result.Add("ui63-dense", InGameProfileScenario.Dense(random));
        foreach (var pair in UI63SiegeScenario.Create(random))
            result.Add(pair.Key, pair.Value);
        foreach (var pair in BoardCompletionScenario.Create(random))
            result.Add(pair.Key, pair.Value);
        return result;
    }

    private static MatchState FellowshipStay(IRandomSource random)
    {
        var state = FellowshipScenario.Declaration(random);
        state.Fellowship.RegionId = "minas-tirith";
        state.Fellowship.Progress = 0;
        state.Nations.Single(n => n.Id == "gondor").Active = false;
        return state;
    }

    private static MatchState FellowshipActions(IRandomSource random, bool hidden)
    {
        var state = FellowshipScenario.Actions(random);
        state.Fellowship.Hidden = hidden;
        state.Dice.First(d => d.Owner == Side.FreePeoples).Face = DieFace.WillOfTheWest;
        return state;
    }

    private static MatchState Actions(IRandomSource random)
    {
        var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), random);
        TurnFlow.Start(state);
        state.Phase = GamePhase.ActionResolution;
        foreach (var die in state.Dice.Where(d => d.Location == DieLocation.Unrolled))
        {
            die.Location = DieLocation.Available;
            die.Face = DieFace.Event;
        }
        TurnFlow.SelectNext(state, Side.FreePeoples);
        return state;
    }
}
