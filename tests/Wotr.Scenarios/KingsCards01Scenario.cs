using Wotr.Contracts;
using Wotr.Domain;
using static CardPackScenario;
using static Wotr.Domain.KingsFreePeoplesCards01;

internal static class KingsCards01Scenario
{
    private const string One = "base.hunt.standard.1.0.1";

    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["kings-wisdom-branch"] = Inspection(random, Wisdom, false),
        ["kings-wisdom-inspection"] = Inspection(random, Wisdom, true),
        ["kings-dark-inspection"] = Inspection(random, Dark, true),
        ["kings-riders-recruit"] = Choose(Play(State(random), Riders), "recruit"),
        ["kings-aid-awakening"] = Awakening(random)
    };

    internal static MatchState State(IRandomSource random)
    {
        var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), random, new() { Kings = true });
        KingsSetup.Initialize(state);
        state.Turn = 2;
        state.Phase = GamePhase.ActionResolution;
        state.Fellowship.Guide = CharacterRules.Strider;
        state.Player(Side.FreePeoples).Hand.Clear();
        foreach (var die in state.Dice.Where(d => d.Location == DieLocation.Unrolled))
        {
            die.Location = DieLocation.Available;
            die.Face = DieFace.Character;
        }
        foreach (var unit in state.Units.Where(u => !u.Id.StartsWith("kings.")))
            BoardRules.ReturnToReserve(unit);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        return state;
    }

    private static MatchState Inspection(IRandomSource random, string card, bool inspect)
    {
        var state = State(random);
        var dain = SovereignLifecycle.Find(state, SovereignCatalog.Dain)!;
        CorruptionRules.Attach(state, dain, One);
        CorruptionRules.Attach(state, dain, KingsTiles.One);
        CorruptionRules.Attach(state, SovereignLifecycle.Find(state, SovereignCatalog.Denethor)!, "base.hunt.standard.1.0.2");
        state = Play(state, card);
        if (!inspect)
            return state;
        state = Choose(state, card == Wisdom ? WisdomBranchStep.Inspect : "north");
        return Choose(state, SovereignCatalog.Dain);
    }

    private static MatchState Awakening(IRandomSource random)
    {
        var state = State(random);
        Character(state, "base.character.gimli", "dale");
        Character(state, "base.character.merry", "edoras");
        state = Play(state, Aid);
        return Execute(state, CommandKind.MoveCompanions, new()
        {
            CompanionMoves = [new() { Ids = ["base.character.gimli"], Route = ["dale", "erebor"] }]
        });
    }

    internal static MatchState Play(MatchState state, string card)
    {
        var owner = Wotr.Domain.Content.CardCatalog.All[card].Side;
        Give(state, card);
        var die = state.Dice.First(d => d.Owner == owner && d.Location == DieLocation.Available);
        die.Face = DieFace.Event;
        return Execute(state, CommandKind.PlayEvent, new() { CardId = card, CardCost = DieFace.Event, DieId = die.Id });
    }

    private static MatchState Choose(MatchState state, string id) => Execute(state, CommandKind.ChooseEffect, new() { Ids = [id] });
}
