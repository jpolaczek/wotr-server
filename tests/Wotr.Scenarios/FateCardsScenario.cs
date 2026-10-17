using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using static CardPackScenario;

internal static class FateCardsScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["fate-there"] = There(random),
        ["fate-book"] = Book(random),
        ["fate-bardings"] = KingsCards01Scenario.Play(State(random, Side.FreePeoples), FateCards.Bardings),
        ["fate-dwarves"] = KingsCards01Scenario.Play(State(random, Side.FreePeoples), FateCards.Dwarves),
        ["fate-orcs"] = Shadow(random, FateCards.Orcs, 4),
        ["fate-monsters"] = Shadow(random, FateCards.Monsters, 1)
    };

    private static MatchState State(IRandomSource random, Side actor)
    {
        var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), random, new() { Fate = true, NewCities = true });
        state.Phase = GamePhase.ActionResolution;
        state.Turn = 2;
        state.Fellowship.Guide = CharacterRules.Strider;
        foreach (var die in state.Dice.Where(d => d.Location == DieLocation.Unrolled))
        {
            die.Location = DieLocation.Available;
            die.Face = DieFace.Event;
        }
        TurnFlow.SelectNext(state, actor);
        return state;
    }

    private static MatchState There(IRandomSource random)
    {
        var state = State(random, Side.FreePeoples);
        state.Fellowship.Guide = "base.character.merry";
        Character(state, "base.character.legolas", "carrock");
        return KingsCards01Scenario.Play(state, FateCards.ThereAndBack);
    }

    private static MatchState Book(IRandomSource random)
    {
        var state = State(random, Side.FreePeoples);
        Character(state, "base.character.gimli", "iron-hills");
        Character(state, "base.character.legolas", "ered-luin");
        state = KingsCards01Scenario.Play(state, FateCards.Book);
        return Execute(state, CommandKind.MoveCompanions, new());
    }

    private static MatchState Shadow(IRandomSource random, string card, int regulars)
    {
        var state = State(random, Side.Shadow);
        foreach (var unit in state.Units.Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == "reserve").Skip(regulars))
            unit.Location = "removed";
        return KingsCards01Scenario.Play(state, card);
    }
}
