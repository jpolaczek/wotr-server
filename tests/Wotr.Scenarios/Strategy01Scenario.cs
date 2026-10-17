using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using static Wotr.Domain.FreePeoplesStrategyCards01;
using static CardPackScenario;

internal static class Strategy01Scenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["strategy-protection"] = Protection(random),
        ["strategy-book"] = Book(random),
        ["strategy-spirit"] = Spirit(random),
        ["strategy-rangers"] = Rangers(random),
        ["strategy-scouts"] = Scouts(random)
    };

    private static MatchState BeforeEvent(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        return state;
    }

    private static MatchState Play(MatchState state, string card, params int[] rolls)
    {
        Give(state, card);
        var die = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available);
        die.Face = CardCatalog.Base[card].EventType;
        return Execute(state, CommandKind.PlayEvent, new() { DieId = die.Id, CardId = card, CardCost = die.Face }, rolls);
    }

    private static MatchState Protection(IRandomSource random)
    {
        var state = Play(BeforeEvent(random), PowerTooGreat);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        state = Play(state, TomBombadil);
        var shadow = state.Player(Side.Shadow);
        shadow.Hand.Clear();
        foreach (var card in new[] { "base.sh.strategy.01", "base.sh.character.01", "base.sh.strategy.05" })
        {
            foreach (var deck in shadow.Decks.Values)
                deck.Remove(card);
            HandRules.Add(shadow, card);
        }
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available))
            die.Face = DieFace.Muster;
        return state;
    }

    private static MatchState Book(IRandomSource random)
    {
        var state = BeforeEvent(random);
        Character(state, "base.character.gimli", "iron-hills");
        Character(state, "base.character.legolas", "dale");
        return Play(state, BookOfMazarbul);
    }

    private static MatchState Spirit(IRandomSource random)
    {
        var state = BeforeEvent(random);
        foreach (var region in new[] { "orthanc", "morannon" })
        {
            Unit(state, "isengard", UnitKind.Regular, region);
            Unit(state, "sauron", UnitKind.Elite, region);
        }
        Character(state, CharacterRules.WitchKing, "orthanc");
        Unit(state, "sauron", UnitKind.Nazgul, "orthanc");
        return Play(state, SpiritOfMordor);
    }

    private static MatchState Rangers(IRandomSource random)
    {
        var state = BeforeEvent(random);
        Unit(state, "rohan", UnitKind.Regular, "osgiliath");
        Unit(state, "sauron", UnitKind.Regular, "north-ithilien");
        return Play(state, FaramirsRangers, 0, 0, 0);
    }

    private static MatchState Scouts(IRandomSource random)
    {
        var state = BeforeEvent(random);
        foreach (var nation in state.Nations)
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
        Unit(state, "isengard", UnitKind.Regular, "westemnet");
        Unit(state, "rohan", UnitKind.Regular, "helms-deep");
        Give(state, SpiritOfMordor);
        TurnFlow.SelectNext(state, Side.Shadow);
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        die.Face = DieFace.Army;
        state = Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = die.Id,
            MovementCost = DieFace.Army,
            Moves = [new() { From = "westemnet", To = "helms-deep", UnitIds = state.Units.Where(u => u.Location == "westemnet").Select(u => u.Id).ToList() }]
        });
        state = Execute(state, CommandKind.FightFieldBattle, new());
        state = Execute(state, CommandKind.CommitCombatCard, new());
        return Execute(state, CommandKind.CommitCombatCard, new() { CardId = SpiritOfMordor });
    }
}
