using Wotr.Contracts;
using Wotr.Domain;

internal static class HuntAftermathScenario
{
    internal const string Gimli = "base.character.gimli";
    internal const string Merry = "base.character.merry";
    internal const string Pippin = "base.character.pippin";

    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["ui52-hunt-aftermath"] = Followups(random)
    };

    private static MatchState Followups(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        state.Fellowship.RegionId = "moria";
        state.Fellowship.Progress = 2;
        state.Fellowship.Companions = [Gimli, Merry, Pippin];
        state.Fellowship.Guide = Gimli;
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow).Take(3))
        {
            die.Location = DieLocation.Hunt;
            die.HuntOrigin = "allocated";
        }
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Id == "base.hunt.standard.eye.1.1" ? "pool" : "removed";
        foreach (var id in new[] { "base.hunt.standard.1.0.1", "base.hunt.standard.1.0.2" })
            state.HuntTiles.Single(t => t.Id == id).Location = "discard";
        var result = ExpansionUiFixture.Execute(state, CommandKind.MoveFellowship,
            new() { DieId = state.Dice.First(d => d.Owner == Side.FreePeoples).Id }, 5, 5, 5, 0);
        if (result.Pending?.Kind != DecisionKind.HuntDamage)
            throw new InvalidOperationException("Aftermath fixture must await the first casualty.");
        return result;
    }
}
