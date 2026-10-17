using Wotr.Contracts;
using Wotr.Domain;
using static ExpansionUiFixture;

internal static class InGameContextScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random)
    {
        var sovereigns = SovereignPanelScenario.Create(random);
        var expansions = ExpansionUiScenario.Create(random);
        var effects = EffectsScenario.Create(random);
        return new()
        {
            ["ui62-effect"] = RemovePalantir(effects["effects-palantir"]),
            ["ui62-effect-cost"] = effects["effects-protection"],
            ["ui62-ring"] = Ring(FellowshipScenario.Actions(random)),
            ["ui62-council"] = Council(expansions["ui09-council-politics"]),
            ["ui62-dismay"] = Dismay(expansions["ui09-dismay"]),
            ["ui62-ruler"] = Ruler(expansions["ui09-ruler-draw"]),
            ["ui62-sovereign"] = Awaken(sovereigns["ui59-awaken"]),
            ["ui62-cleanse"] = Execute(sovereigns["ui59-cleanse-remove"], CommandKind.ChooseEffect, new() { Ids = ["remove"] }, 0),
            ["ui62-private-tile"] = PrivateTileChoice(expansions["ui09-corruption-hidden"]),
            ["ui62-victory"] = Victory(BaseBoundaryScenario.Create(random)["boundary-military-fp"])
        };
    }

    public static MatchState RecoveryRing(IRandomSource random) => FellowshipScenario.Actions(random);

    public static MatchState RecoveryTile(IRandomSource random) =>
        PrivateTileChoice(ExpansionUiScenario.Create(random)["ui09-corruption-hidden"]);

    private static MatchState PrivateTileChoice(MatchState state)
    {
        state = Execute(state, CommandKind.AttemptCorruption,
            new() { CharacterId = SovereignCatalog.Thranduil, DieId = LordsDiceCatalog.Nenya }, 0);
        state = Execute(state, CommandKind.DrawCorruptionTile, samples: [0]);
        var attempt = CorruptionRuntime.Current(state);
        if (attempt is not { Stage: CorruptionStage.Choosing, DrawLimit: 2 } || attempt.Drawn.Count != 2)
            throw new InvalidOperationException("UI-62 requires a completed normal draw with two legal private tile choices.");
        return state;
    }

    public static MatchState RecoveryVictory(IRandomSource random)
    {
        var state = BaseBoundaryScenario.Create(random)["boundary-military-fp"];
        return Execute(state, CommandKind.DiscardDie, new() { DieId = Die(state, Side.FreePeoples).Id });
    }

    private static MatchState RemovePalantir(MatchState state) => Execute(state, CommandKind.DiscardPalantir, new()
    {
        DieId = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Army).Id,
        RingId = state.Rings.First(r => r.Owner == Side.FreePeoples).Id
    });

    private static MatchState Ring(MatchState state) => Execute(state, CommandKind.UseRing, new()
    {
        DieId = Die(state, Side.FreePeoples).Id, RingId = state.Rings.First(r => r.Owner == Side.FreePeoples).Id, Face = DieFace.Muster
    });

    private static MatchState Council(MatchState state) => Execute(state, CommandKind.UseCouncilToken, new()
    {
        AbilityId = CouncilTokens.Politics, NationId = CouncilTokens.Nations(state).First()
    });

    private static MatchState Dismay(MatchState state) => Execute(state, CommandKind.UseDismay, new()
    {
        Ids = [KingsRulerTokens.Id(KingsDismay.Tokens(state).First().SovereignId)],
        DieId = KingsDismay.Dice(state).First().Id
    }, 0);

    private static MatchState Ruler(MatchState state) => Execute(state, CommandKind.DrawCard, new()
    {
        DieId = KingsDice.Ruler(state, Side.FreePeoples)!.Id, RulerTokenId = "kings.token.denethor", Deck = "character"
    });

    private static MatchState Awaken(MatchState state) => Execute(state, CommandKind.AwakenSovereign, new()
    {
        DieId = Die(state, Side.FreePeoples).Id, CharacterId = SovereignCatalog.Dain, Route = ["erebor"]
    });

    private static MatchState Victory(MatchState state)
    {
        state = Execute(state, CommandKind.DiscardDie, new() { DieId = Die(state, Side.FreePeoples).Id });
        return Execute(state, CommandKind.DiscardDie, new() { DieId = Die(state, Side.Shadow).Id });
    }
}
