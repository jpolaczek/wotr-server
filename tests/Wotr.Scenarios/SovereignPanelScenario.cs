using Wotr.Contracts;
using Wotr.Domain;
using static ExpansionUiFixture;

internal static class SovereignPanelScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["ui59-awaken"] = Awakening(random, false),
        ["ui59-awaken-variant"] = Awakening(random, true),
        // UI-101: its own copy, so the Characters panel test never sees another test's awakening.
        ["ui101-sovereigns"] = Awakening(random, false),
        ["ui59-cleanse-remove"] = Cleansing(random),
        ["ui59-cleanse-decline"] = Cleansing(random),
        ["ui59-sequential"] = Sequential(random),
        ["ui59-replacement-kings"] = Replacement(random)
    };

    private static MatchState Awakening(IRandomSource random, bool variant)
    {
        var configuration = Full(variant);
        configuration.RemovingRulerTokens = variant;
        configuration.Dismay = true;
        var state = Actions(random, configuration);
        foreach (var nation in state.Nations)
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
        Die(state, Side.FreePeoples).Face = DieFace.Muster;
        var id = variant ? SovereignCatalog.Thranduil : SovereignCatalog.Dain;
        CorruptionRules.Attach(state, Sovereign(state, id), "base.hunt.standard.1.0.1");
        return state;
    }

    private static MatchState Cleansing(IRandomSource random)
    {
        var state = Actions(random, new() { Kings = true });
        Put(state, "base.character.gimli", "erebor");
        var nation = state.Nations.Single(n => n.Id == "dwarves");
        nation.Active = true;
        nation.StepsToWar = 2;
        var die = Die(state, Side.FreePeoples);
        die.Face = DieFace.Muster;
        CorruptionRules.Attach(state, Sovereign(state, SovereignCatalog.Dain), "base.hunt.standard.3.0.1");
        CorruptionRules.Attach(state, Sovereign(state, SovereignCatalog.Dain), KingsTiles.One);
        state = Execute(state, CommandKind.AdvancePolitics, new() { DieId = die.Id, NationId = "dwarves" });
        if (state.Pending?.Presentation != "CorruptionRemoval")
            throw new InvalidOperationException("Sovereign panel fixture must await the optional random tile return.");
        return state;
    }

    // The Shadow to attempt Thranduil with four Eyes in the Hunt Box and only this tile in the Hunt Pool.
    private static MatchState Ready(IRandomSource random, bool sequential, string first)
    {
        var state = Actions(random, new() { Kings = true, SequentialCorruption = sequential, VisibleCorruption = true });
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Take(4))
        {
            die.Face = DieFace.Eye;
            die.Location = DieLocation.Hunt;
            die.HuntOrigin = "allocated";
        }
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Id == first ? "pool" : "removed";
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    private static MatchState Attempt(IRandomSource random, bool sequential, string first)
    {
        var state = Ready(random, sequential, first);
        return Execute(state, CommandKind.AttemptCorruption,
            new() { CharacterId = SovereignCatalog.Thranduil, DieId = CorruptionRules.Eyes(state).First().Id }, 0);
    }

    private static MatchState Sequential(IRandomSource random)
    {
        var state = Attempt(random, true, "base.hunt.standard.3.0.1");
        state.HuntTiles.Single(t => t.Id == "base.hunt.standard.1.0.1").Location = "pool";
        return state;
    }

    // UI-442: before the standard attempt, whose own command draws every tile. The pool holds only an Eye tile and the
    // discard pile the Kings tile, so the server's draws are fixed: the Eye is revealed and replaced, the emptied pool
    // refills with the Kings tile, and the Kings tile ends the attempt on Thranduil.
    private static MatchState Replacement(IRandomSource random)
    {
        var state = Ready(random, false, "base.hunt.standard.eye.1.1");
        state.HuntTiles.Single(t => t.Id == KingsTiles.One).Location = "discard";
        return state;
    }
}
