using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

internal static class FateGimliScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["fate-gimli-renown"] = Actions(random, FateGimli.Standard, "iron-hills"),
        ["fate-gimli-messenger"] = Actions(random, FateGimli.Alternate, "ered-luin"),
        ["fate-gimli-guide"] = Hunt(random)
    };

    private static MatchState Actions(IRandomSource random, string definition, string region)
    {
        var alternate = definition == FateGimli.Alternate;
        var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), random,
            new() { Fate = true, Lords = alternate, Council = alternate });
        if (alternate)
        {
            LordsDiceSetup.Initialize(state);
            state.Lords!.Council = new() { HomelandsChosen = true };
        }
        state.Turn = 2;
        state.Phase = GamePhase.ActionResolution;
        foreach (var die in state.Dice.Where(d => d.Location == DieLocation.Unrolled))
        {
            die.Location = DieLocation.Available;
            die.Face = DieFace.Character;
        }
        var gimli = CharacterCatalog.Companion(state, FateGimli.Figure);
        CharacterLifecycle.ApplyVersion(gimli, CharacterCatalog.Fate[definition]);
        gimli.Location = region;
        state.Characters.Add(gimli);
        if (region != "fellowship")
            state.Fellowship.Companions.Remove(gimli.Id);
        LordsCompanionSetup.Reveal(state, gimli);
        state.Nations.Single(n => n.Id == "dwarves").Active = false;
        state.Nations.Single(n => n.Id == "dwarves").StepsToWar = 2;
        TurnFlow.SelectNext(state, Side.FreePeoples);
        return state;
    }

    private static MatchState Hunt(IRandomSource random)
    {
        var state = Actions(random, FateGimli.Alternate, "fellowship");
        state.Fellowship.Companions = [FateGimli.Figure];
        state.Fellowship.Guide = FateGimli.Figure;
        state.Fellowship.RegionId = "hollin";
        state.Dice.First(d => d.Owner == Side.Shadow).Location = DieLocation.Hunt;
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Id == "base.hunt.standard.3.0.1" ? "pool" : "removed";
        return CardPackScenario.Execute(state, CommandKind.MoveFellowship,
            new() { DieId = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Id }, 5, 0);
    }
}
