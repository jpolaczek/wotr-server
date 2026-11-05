using Wotr.Contracts;
using Wotr.Domain;
using static ExpansionUiFixture;

// UI-271: the Lords characters on their objects: entries from the Characters panel (the Mouth's and the Witch-king's
// versions as rows of their own), the Chief's reactions at the Character deck and his figure, the Balrog card's order.
internal static class LordsCharacterScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["ui271-entries-fp"] = FreePeoplesEntries(random),
        ["ui271-entries-sh"] = ShadowEntries(random),
        ["ui271-witch-king-switch"] = WitchKingSwitch(random),
        ["ui271-spear"] = Spear(random),
        ["ui271-he-sees"] = HeSees(random),
        ["ui271-balrog-order"] = BalrogOrder(random)
    };

    private static MatchState State(IRandomSource random, Side actor, DieFace face = DieFace.Muster)
    {
        var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), random, new() { Lords = true });
        LordsDiceSetup.Initialize(state);
        SmeagolTiles.Initialize(state);
        state.Turn = 2;
        state.Phase = GamePhase.ActionResolution;
        state.Fellowship.Guide = CharacterRules.Grey;
        // The Chief may enter or follow only outside a Free Peoples sanctuary.
        state.Fellowship.RegionId = "hollin";
        foreach (var die in state.Dice.Where(d => d.Source == "standard"))
        {
            die.Location = DieLocation.Available;
            die.Face = face;
        }
        TurnFlow.SelectNext(state, actor);
        return state;
    }

    private static void War(MatchState state, params string[] nations)
    {
        foreach (var nation in state.Nations.Where(n => nations.Contains(n.Id)))
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
    }

    // Elrond and Galadriel enter from their rows with a Muster die.
    private static MatchState FreePeoplesEntries(IRandomSource random)
    {
        var state = State(random, Side.FreePeoples);
        War(state, "elves");
        return state;
    }

    // The Balrog, Gothmog, both Mouths and both Witch-kings can enter: every Free Peoples nation is At War for the base
    // Mouth, and the Free Peoples hold Angmar for the Lords Mouth.
    private static MatchState ShadowEntries(IRandomSource random)
    {
        var state = State(random, Side.Shadow);
        War(state, "sauron", "gondor", "rohan", "elves", "dwarves", "north");
        foreach (var unit in state.Units.Where(u => u.Location == "angmar"))
            BoardRules.ReturnToReserve(unit);
        state.Control["angmar"] = Side.FreePeoples;
        return state;
    }

    // The Black Captain stands in Minas Morgul; the Chief's row turns him into the other version in place.
    private static MatchState WitchKingSwitch(IRandomSource random)
    {
        var state = State(random, Side.Shadow);
        War(state, "sauron", "gondor");
        Put(state, CharacterRules.WitchKing, "minas-morgul");
        return state;
    }

    // The Chief stands with the Fellowship; a Shadow Character-deck event opens Spear of Terror.
    private static MatchState Spear(IRandomSource random)
    {
        var state = State(random, Side.Shadow, DieFace.Character);
        Chief(state, "hollin");
        return Play(state, ShadowCharacterCards03.Lidless);
    }

    // The Free Peoples declare the Fellowship in Edoras; He Sees, He Knows offers the Chief's flight there.
    private static MatchState HeSees(IRandomSource random)
    {
        var state = State(random, Side.FreePeoples);
        Chief(state, "minas-morgul");
        state.Phase = GamePhase.Fellowship;
        state.Fellowship.RegionId = "edoras";
        state.Control["edoras"] = Side.FreePeoples;
        TurnFlow.OpenFellowshipPhase(state);
        return Execute(state, CommandKind.DeclareFellowship, new() { Route = ["edoras"] });
    }

    // The active Balrog in Moria; his card asks whether he moves alone, moves or attacks with his army, or finishes.
    private static MatchState BalrogOrder(IRandomSource random)
    {
        var state = State(random, Side.Shadow, DieFace.Character);
        Put(state, LordsCharacters.Balrog, "moria").Level = 2;
        return Play(state, LordsShadowCards.Balrog);
    }

    // The Chief stands on the Witch-king's figure, as his entry places him.
    private static void Chief(MatchState state, string region) => Put(state, LordsCharacters.WitchKing, region).Id = CharacterRules.WitchKing;

    private static MatchState Play(MatchState state, string card)
    {
        foreach (var deck in state.Player(Side.Shadow).Decks.Values)
            deck.Remove(card);
        HandRules.Add(state.Player(Side.Shadow), card);
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        return Execute(state, CommandKind.PlayEvent, new() { CardId = card, DieId = die.Id, CardCost = die.Face });
    }
}
