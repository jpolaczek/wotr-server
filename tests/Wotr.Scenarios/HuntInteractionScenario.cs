using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

internal static class HuntInteractionScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random)
    {
        var result = new Dictionary<string, MatchState>
        {
            ["ui34-hunt-allocation"] = Allocation(random),
            ["ui34-hunt-zero"] = Allocation(random),
            ["ui50-hunt-required"] = RequiredAllocation(random),
            ["ui50-hunt-no-dice"] = Rolled(random, false, empty: true),
            ["ui34-hunt-reroll"] = FellowshipScenario.Hunt(random),
            ["ui34-hunt-keep"] = Rolled(random, true),
            ["ui34-hunt-miss"] = Rolled(random, false)
        };
        foreach (var response in Enum.GetValues<HuntResponse>())
            foreach (var gesture in new[] { "click", "double", "drop" })
                result.Add("ui34-response-" + response + "-" + gesture, Damage(random));
        return result;
    }

    private static MatchState Damage(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        state.Fellowship.RegionId = "hollin";
        state.Fellowship.Guide = "base.character.strider";
        var grey = CharacterCatalog.Companion(state, "base.character.gandalf-grey");
        grey.Location = "casualties";
        state.Characters.Add(grey);
        state.Fellowship.Companions.Remove(grey.Id);
        state.Fellowship.Corruption = 4;
        state.Dice.First(d => d.Owner == Side.Shadow).Location = DieLocation.Hunt;
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Id == "base.hunt.standard.3.0.1" ? "pool" : "removed";
        var result = ExpansionUiFixture.Execute(state, CommandKind.MoveFellowship,
            new() { DieId = state.Dice.First(d => d.Owner == Side.FreePeoples).Id }, 5, 0);
        if (result.Pending?.Kind != DecisionKind.HuntDamage)
            throw new InvalidOperationException("Response fixture must stop before choosing a casualty.");
        return result;
    }

    private static MatchState Allocation(IRandomSource random)
    {
        var state = FellowshipScenario.Declaration(random);
        TurnFlow.OpenHuntAllocation(state);
        return state;
    }

    private static MatchState RequiredAllocation(IRandomSource random)
    {
        var state = Allocation(random);
        state.MandatoryHuntAllocation = true;
        state.Fellowship.Companions = [state.Fellowship.Guide];
        return state;
    }

    internal static MatchState Rolled(IRandomSource random, bool reroll, bool empty = false)
    {
        var state = FellowshipScenario.Actions(random);
        state.Fellowship.RegionId = reroll ? "moria" : "rivendell";
        state.Fellowship.Progress = 0;
        var huntDie = state.Dice.First(d => d.Owner == Side.Shadow);
        if (!empty)
        {
            huntDie.Location = DieLocation.Hunt;
            huntDie.HuntOrigin = "allocated";
        }
        return ExpansionUiFixture.Execute(state, CommandKind.MoveFellowship,
            new() { DieId = state.Dice.First(d => d.Owner == Side.FreePeoples).Id }, 0);
    }
}
