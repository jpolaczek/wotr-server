using Wotr.Contracts;
using Wotr.Domain;
using static ExpansionUiFixture;

internal static class ExpansionUiScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["ui09-council"] = Start(random, new() { Lords = true, Council = true, Kings = true, Fate = true, NewCities = true }),
        ["ui09-dice"] = Rolling(random),
        ["ui267-tie"] = SpecialRoll(random, 3, (LordsDiceCatalog.Vilya, 0), (LordsDiceCatalog.Nenya, 2),
            (LordsDiceCatalog.Balrog, 0), (LordsDiceCatalog.Gothmog, 2)),
        ["ui267-forced-eye"] = SpecialRoll(random, 0, (LordsDiceCatalog.Vilya, 5), (LordsDiceCatalog.Nenya, 0),
            (LordsDiceCatalog.Balrog, 1), (LordsDiceCatalog.Gothmog, 4)),
        ["ui09-dismay"] = Dismay(random),
        ["ui09-ruler-recruit"] = Recruitment(random),
        ["ui09-ruler-event"] = TokenEvent(random),
        ["ui09-ruler-draw"] = TokenEvent(random),
        ["ui09-travel"] = Travel(random),
        ["ui09-travel-token"] = Travel(random),
        ["ui22-separate"] = Travel(random),
        ["ui22-separate-token"] = Travel(random),
        ["ui09-council-politics"] = Council(random, CouncilTokens.Politics),
        ["ui09-council-travel"] = Council(random, CouncilTokens.Travel),
        ["ui09-entry"] = Council(random, CouncilTokens.Politics),
        ["ui09-balrog"] = Balrog(random),
        ["ui09-narya"] = Narya(random),
        ["ui09-vilya"] = Vilya(random),
        ["ui09-awaken"] = Awaken(random),
        ["ui09-remove"] = Remove(random),
        ["ui09-corruption-hidden"] = Corruption(random, false, false),
        ["ui09-corruption-visible"] = Corruption(random, true, false),
        ["ui09-corruption-stop"] = Corruption(random, false, true),
        ["ui09-corruption-draw"] = Corruption(random, true, true)
    };

    private static MatchState Rolling(IRandomSource random)
    {
        var state = Actions(random);
        foreach (var (id, region) in new[] { ("lords.character.elrond", "rivendell"), ("lords.character.galadriel", "lorien"),
            ("lords.character.balrog", "moria"), ("lords.character.gothmog", "minas-morgul"), (KingsChieftains.Serpent, "near-harad") })
            Put(state, id, region);
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Thranduil), "woodland-realm");
        state.Turn++;
        DiceRules.Recover(state);
        var samples = state.Dice.Where(d => d.Location == DieLocation.Unrolled).Select(d => d.Special is null ? 0 : 5).ToArray();
        // Each sample is valid but no gameplay outcome is automatically chosen by the client.
        TurnFlow.RollAndBeginActions(state, new Values(samples));
        return state;
    }

    // UI-267: a Lords game in turn 2 with Elrond, Galadriel, the Balrog and Gothmog in play; the Shadow first puts
    // `hunt` standard dice in the Hunt Box and the special dice roll the given faces (indices of LORDS p. 9/11:
    // Vilya 0 = Army★, 5 = Eye★; Nenya 0 = Character★, 2 = Event; Balrog 0 = Character★, 1 = Use Balrog; Gothmog
    // 2 = Muster, 4 = Card Draw). Three dice in the Hunt tie the rolled dice, so the Free Peoples choose first.
    private static MatchState SpecialRoll(IRandomSource random, int hunt, params (string Die, int Face)[] faces)
    {
        var state = Actions(random, new() { Lords = true });
        foreach (var (id, region) in new[] { (KeeperRings.Elrond, "rivendell"), (KeeperRings.Galadriel, "lorien"),
            (LordsCharacters.Balrog, "moria"), (LordsCharacters.Gothmog, "minas-morgul") })
            Put(state, id, region);
        state.Turn++;
        DiceRules.Recover(state);
        foreach (var id in DiceRules.HuntCandidates(state).Take(hunt))
            state.Dice.Single(d => d.Id == id).Location = DieLocation.Hunt;
        var chosen = faces.ToDictionary(f => f.Die, f => f.Face);
        var samples = state.Dice.Where(d => d.Location == DieLocation.Unrolled)
            .Select(d => d.Special is null ? 0 : chosen[d.Id]).ToArray();
        TurnFlow.RollAndBeginActions(state, new Values(samples));
        return state;
    }

    internal static MatchState Dismay(IRandomSource random)
    {
        var state = Rolling(random);
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Theoden), "edoras");
        CharacterLifecycle.Eliminate(state, state.Characters.Single(c => c.Id == SovereignCatalog.Theoden));
        while (state.Pending!.Kind is DecisionKind.SpecialDieChoice or DecisionKind.RulerDiscard)
        {
            var special = state.Pending.Kind == DecisionKind.SpecialDieChoice;
            var choices = special ? LordsDiceRoll.LegalChoices(state, state.Pending.Owner) : KingsDice.DiscardCandidates(state, state.Pending.Owner);
            state = Execute(state, special ? CommandKind.ChooseSpecialDie : CommandKind.DiscardRulerDie, new() { DieId = choices[0] });
        }
        return state;
    }

    private static MatchState Recruitment(IRandomSource random)
    {
        var state = Actions(random);
        state.Nations.Single(n => n.Id == "rohan").StepsToWar = 0;
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Theoden), "edoras");
        KingsDice.Ruler(state, Side.FreePeoples)!.Location = DieLocation.Available;
        KingsDice.Ruler(state, Side.FreePeoples)!.Face = DieFace.Character;
        return state;
    }

    private static MatchState TokenEvent(IRandomSource random)
    {
        var state = Actions(random);
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Denethor), "minas-tirith");
        KingsDice.Ruler(state, Side.FreePeoples)!.Location = DieLocation.Available;
        KingsDice.Ruler(state, Side.FreePeoples)!.Face = DieFace.Muster;
        CardPackScenario.Give(state, "base.fp.character.02");
        return state;
    }

    private static MatchState Travel(IRandomSource random)
    {
        var state = Actions(random);
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Thranduil), "woodland-realm");
        KingsDice.Ruler(state, Side.FreePeoples)!.Location = DieLocation.Available;
        KingsDice.Ruler(state, Side.FreePeoples)!.Face = DieFace.Character;
        return state;
    }

    private static MatchState Council(IRandomSource random, string token)
    {
        var state = Actions(random);
        state.Lords!.Council!.GrantedTokens = [token];
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available))
            die.Face = DieFace.Muster;
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    private static MatchState Balrog(IRandomSource random)
    {
        var state = Actions(random);
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available))
            die.Face = DieFace.Muster;
        state.Nations.Single(n => n.Id == "elves").StepsToWar = 0;
        state.Nations.Single(n => n.Id == "dwarves").StepsToWar = 0;
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    private static MatchState Narya(IRandomSource random)
    {
        var state = Actions(random);
        Put(state, LordsCharacters.Gandalf, "bree");
        return state;
    }

    internal static MatchState Vilya(IRandomSource random)
    {
        var state = Actions(random);
        Put(state, KeeperRings.Elrond, "rivendell");
        var die = Die(state, Side.FreePeoples);
        die.Face = DieFace.Event;
        return Execute(state, CommandKind.DrawCard, new() { DieId = die.Id, Deck = "strategy" });
    }

    private static MatchState Awaken(IRandomSource random)
    {
        var state = Actions(random, Full(false));
        foreach (var nation in state.Nations)
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
        Die(state, Side.FreePeoples).Face = DieFace.Muster;
        state.Units.Single(u => u.Id == SovereignCatalog.Dain).Location = "iron-hills";
        return state;
    }

    private static MatchState Remove(IRandomSource random)
    {
        var state = Actions(random);
        var sovereign = Sovereign(state, SovereignCatalog.Denethor);
        state.Units.RemoveAll(u => u.Id == sovereign.Id);
        sovereign.Status = SovereignStatus.Corrupted;
        sovereign.CorruptedRegion = "minas-tirith";
        Put(state, "base.character.boromir", "minas-tirith");
        return state;
    }

    private static MatchState Corruption(IRandomSource random, bool visible, bool sequential)
    {
        var config = Full();
        config.VisibleCorruption = visible;
        config.SequentialCorruption = sequential;
        var state = Actions(random, config);
        Put(state, KeeperRings.Galadriel, "lorien");
        var keeper = state.Dice.Single(d => d.Id == LordsDiceCatalog.Nenya);
        keeper.Face = DieFace.Eye;
        keeper.Location = DieLocation.Hunt;
        keeper.HuntOrigin = LordsDiceCatalog.KeeperEye;
        keeper.Special!.Chosen = true;
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Take(2))
        {
            die.Face = DieFace.Eye;
            die.Location = DieLocation.Hunt;
            die.HuntOrigin = "allocated";
        }
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Id.StartsWith("base.hunt.standard.1.", StringComparison.Ordinal) ? "pool" : "set-aside";
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    private sealed class Values(int[] samples) : IRandomSource
    {
        private readonly Queue<int> values = new(samples);
        public int Next(int upperBound) => values.Dequeue();
    }
}
