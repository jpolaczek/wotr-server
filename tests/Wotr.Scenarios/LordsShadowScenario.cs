using Wotr.Contracts;
using Wotr.Domain;

internal static class LordsShadowScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["lords-lore-choice"] = Lore(random),
        ["lords-lore-remove"] = Table(random, LordsShadowCards.Lore),
        ["lords-gates-remove"] = Table(random, LordsShadowCards.Gates),
        ["lords-smeagol-cost"] = Smeagol(random)
    };

    internal static MatchState State(IRandomSource random)
    {
        var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), random, new() { Lords = true });
        LordsDiceSetup.Initialize(state);
        SmeagolTiles.Initialize(state);
        state.Turn = 2;
        state.Phase = GamePhase.ActionResolution;
        state.Fellowship.Guide = CharacterRules.Grey;
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        foreach (var die in state.Dice.Where(d => d.Source == "standard"))
        {
            die.Location = DieLocation.Available;
            die.Face = DieFace.Character;
        }
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    internal static MatchState Play(MatchState state, string card, DieFace face = DieFace.Character)
    {
        foreach (var deck in state.Player(Side.Shadow).Decks.Values)
            deck.Remove(card);
        HandRules.Add(state.Player(Side.Shadow), card);
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        die.Face = face;
        return CardPackScenario.Execute(state, CommandKind.PlayEvent, new()
        {
            CardId = card, DieId = die.Id, CardCost = face
        });
    }

    private static MatchState Table(IRandomSource random, string card)
    {
        var state = Play(State(random), card);
        var dice = state.Dice.Where(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).ToArray();
        dice[0].Face = DieFace.WillOfTheWest;
        dice[1].Face = DieFace.Army;
        return state;
    }

    private static MatchState Lore(IRandomSource random)
    {
        var state = Table(random, LordsShadowCards.Lore);
        state.Phase = GamePhase.ActionRoll;
        var keeper = state.Dice.Single(d => d.Id == LordsDiceCatalog.Nenya);
        keeper.Face = DieFace.Eye;
        keeper.Location = DieLocation.Hunt;
        keeper.HuntOrigin = LordsDiceCatalog.KeeperEye;
        var shadow = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        shadow.Face = DieFace.Eye;
        shadow.Location = DieLocation.Hunt;
        shadow.HuntOrigin = "rolled-eye";
        TurnFlow.BeginActions(state);
        new EffectRuntime(EffectCatalog.All).Continue(state, random);
        return state;
    }

    private static MatchState Smeagol(IRandomSource random)
    {
        var state = State(random);
        SmeagolRules.Enter(state);
        state = Play(state, LordsShadowCards.WeWontGoBack);
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Id == SmeagolTiles.Definitions[1].Id ? "pool" : "set-aside";
        var shadow = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        shadow.Face = DieFace.Eye;
        shadow.Location = DieLocation.Hunt;
        shadow.HuntOrigin = "allocated";
        return CardPackScenario.Execute(state, CommandKind.MoveFellowship, new()
        {
            DieId = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Id
        }, 5, 0);
    }
}
