using Wotr.Contracts;
using Wotr.Domain;
using static ExpansionUiFixture;

internal static class UI63SiegeScenario
{
    private const string Home = "erebor";
    private const string Tile = "base.hunt.standard.1.0.1";

    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["ui63-siege-muster"] = Siege(random, ruler: false, outside: false),
        ["ui63-siege-ruler"] = Siege(random, ruler: true, outside: false),
        ["ui63-siege-outside"] = Siege(random, ruler: false, outside: true)
    };

    private static MatchState Siege(IRandomSource random, bool ruler, bool outside)
    {
        var state = Actions(random, new() { Kings = true });
        if (ruler)
            state = ReachRulerTurn(state);
        else
            Die(state, Side.FreePeoples).Face = DieFace.Muster;
        state.Nations.Single(n => n.Id == "dwarves").Active = true;
        foreach (var unit in state.Units.Where(u => BoardRules.Owner(u) == Side.FreePeoples && u.Location == Home))
            unit.Garrison = true;
        var besieger = state.Units.First(u => BoardRules.Owner(u) == Side.Shadow && u.Kind == UnitKind.Regular &&
            BoardRules.Regions.ContainsKey(u.Location));
        besieger.Location = Home;
        besieger.Garrison = false;
        state.Control[Home] = Side.FreePeoples;
        if (outside)
        {
            var dain = state.Units.Single(u => u.Id == SovereignCatalog.Dain);
            dain.Location = "iron-hills";
            dain.Garrison = false;
        }
        var sovereign = Sovereign(state, SovereignCatalog.Dain);
        CorruptionRules.Attach(state, sovereign, Tile);
        CorruptionRules.Attach(state, sovereign, KingsTiles.One);
        Verify(state, ruler, outside);
        return state;
    }

    // An available Ruler is reached through the previous turn's awakening,
    // normal Recover/roll and explicit standard-die discard (KINGS pp.6–7).
    private static MatchState ReachRulerTurn(MatchState state)
    {
        state.Nations.Single(n => n.Id == "rohan").Active = true;
        var die = Die(state, Side.FreePeoples);
        die.Face = DieFace.Muster;
        state = Execute(state, CommandKind.AwakenSovereign,
            new() { CharacterId = SovereignCatalog.Theoden, DieId = die.Id, Route = ["edoras"] });
        var turn = state.Turn;
        for (var step = 0; state.Turn == turn; step++)
        {
            if (step > 20 || state.Pending?.Kind != DecisionKind.Action)
                throw new InvalidOperationException("Siege fixture failed to finish the prior action round.");
            state = Execute(state, CommandKind.DiscardDie, new() { DieId = Die(state, state.Pending.Owner).Id });
        }
        if (state.Pending?.Kind != DecisionKind.FellowshipPhase)
            throw new InvalidOperationException("Siege fixture must reach the next Fellowship Phase.");
        state = Execute(state, CommandKind.ContinueFellowship);
        var rolls = new int[state.Dice.Count(d => d.Location == DieLocation.Unrolled)];
        state = Execute(state, CommandKind.AllocateHunt, samples: rolls);
        if (state.Pending?.Kind != DecisionKind.RulerDiscard || state.Pending.Owner != Side.FreePeoples)
            throw new InvalidOperationException("Siege fixture must explicitly discard after its Ruler roll.");
        var discard = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Source == "standard" &&
            d.Location == DieLocation.Available);
        return Execute(state, CommandKind.DiscardRulerDie, new() { DieId = discard.Id });
    }

    private static void Verify(MatchState state, bool ruler, bool outside)
    {
        if (!BoardRules.Besieged(state, Home, Side.FreePeoples) || state.Pending?.Owner != Side.FreePeoples)
            throw new InvalidOperationException("Siege fixture must expose a real FP decision and garrison.");
        var die = ruler ? KingsDice.Ruler(state, Side.FreePeoples)! : Die(state, Side.FreePeoples);
        var payload = new CommandPayload
        {
            DieId = die.Id, CharacterId = SovereignCatalog.Dain,
            Route = outside ? ["iron-hills", Home] : [Home]
        };
        if (SovereignAwakening.Valid(state, payload) == outside)
            throw new InvalidOperationException("Awakening must allow staying, but not entering the siege from outside.");
        if (outside)
            return;
        var result = Execute(StateCodec.Copy(state), CommandKind.AwakenSovereign, payload);
        if (!result.Characters.Single(c => c.Id == SovereignCatalog.Dain).Garrison ||
            result.HuntTiles.Any(t => (t.Id == Tile || t.Id == KingsTiles.One) && t.Location != "pool"))
            throw new InvalidOperationException("Awakening must preserve the garrison and return both physical tiles.");
    }
}
