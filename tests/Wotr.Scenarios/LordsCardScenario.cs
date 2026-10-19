using Wotr.Contracts;
using Wotr.Domain;
using static ExpansionUiFixture;

// UI-272: the Lords cards whose choices stand on their objects: The Gates are Closed answered at its × on a declaration,
// Wind from the West and Safe Paths in the Dark at the Shadow table cards' ×, Wind's inspected Shadow hand in the drawer,
// and Three Rings for the Elven-Kings at the Ring sockets.
internal static class LordsCardScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["ui272-gates-declaration"] = GatesDeclaration(random),
        ["ui272-wind-table"] = WindTable(random),
        ["ui272-wind-hand"] = WindHand(random),
        ["ui272-safe-paths"] = SafePaths(random),
        ["ui272-three-rings"] = ThreeRings(random)
    };

    private static MatchState State(IRandomSource random, Side actor)
    {
        var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), random, new() { Lords = true });
        LordsDiceSetup.Initialize(state);
        SmeagolTiles.Initialize(state);
        state.Turn = 2;
        state.Phase = GamePhase.ActionResolution;
        state.Fellowship.Guide = CharacterRules.Strider;
        foreach (var die in state.Dice.Where(d => d.Source == "standard"))
        {
            die.Location = DieLocation.Available;
            die.Face = DieFace.Event;
        }
        TurnFlow.SelectNext(state, actor);
        return state;
    }

    // The Free Peoples declare the Fellowship into Moria; the Shadow may answer with the Gates at its ×.
    private static MatchState GatesDeclaration(IRandomSource random)
    {
        var state = State(random, Side.FreePeoples);
        Table(state, LordsShadowCards.Gates);
        state.Fellowship.RegionId = "hollin";
        state.Fellowship.Progress = 2;
        state.Phase = GamePhase.Fellowship;
        TurnFlow.OpenFellowshipPhase(state);
        return Execute(state, CommandKind.DeclareFellowship, new() { Route = ["hollin", "moria"] });
    }

    // Wind from the West with Shadow table cards: one of them is discarded with its × in the Effects panel.
    private static MatchState WindTable(IRandomSource random)
    {
        var state = State(random, Side.FreePeoples);
        Table(state, LordsShadowCards.Gates);
        Table(state, LordsShadowCards.Lore);
        Hand(state, Side.Shadow, "base.sh.strategy.02");
        return Play(state, LordsFreePeoplesCards.Wind);
    }

    // Wind from the West with no Shadow table card: Inspect the hand or Finish, then a Shadow card in the drawer.
    private static MatchState WindHand(IRandomSource random)
    {
        var state = State(random, Side.FreePeoples);
        Hand(state, Side.Shadow, "base.sh.character.01");
        Hand(state, Side.Shadow, "base.sh.strategy.02");
        return Play(state, LordsFreePeoplesCards.Wind);
    }

    // Safe Paths in the Dark with both Sméagol cards on the Shadow table: one is discarded with its ×.
    private static MatchState SafePaths(IRandomSource random)
    {
        var state = State(random, Side.FreePeoples);
        state.Fellowship.Corruption = 2;
        Table(state, SmeagolRules.WeShallGetIt);
        Table(state, LordsFreePeoplesCards.WeWontGoBack);
        return Play(state, LordsFreePeoplesCards.SafePaths);
    }

    // Three Rings for the Elven-Kings rolls two successes: one Ring out of play and one held by the Shadow come back.
    private static MatchState ThreeRings(IRandomSource random)
    {
        var state = State(random, Side.FreePeoples);
        state.Rings[0].Owner = null;
        state.Rings[1].Owner = Side.Shadow;
        state.Rings[2].Owner = Side.Shadow;
        return Play(state, LordsFreePeoplesCards.ThreeRings, 4, 5, 0);
    }

    private static void Table(MatchState state, string card)
    {
        state.Player(Side.Shadow).Table.Add(card);
        state.ActiveEffects.Add(new() { SourceId = card, Owner = Side.Shadow, Duration = EffectDuration.UntilRemoved });
    }

    private static void Hand(MatchState state, Side side, string card)
    {
        foreach (var deck in state.Player(side).Decks.Values)
            deck.Remove(card);
        HandRules.Add(state.Player(side), card);
    }

    private static MatchState Play(MatchState state, string card, params int[] samples)
    {
        Hand(state, Side.FreePeoples, card);
        var die = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available);
        return Execute(state, CommandKind.PlayEvent, new() { CardId = card, DieId = die.Id, CardCost = die.Face }, samples);
    }
}
