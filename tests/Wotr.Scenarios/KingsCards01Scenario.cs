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
        ["kings-aid-awakening"] = Awakening(random),
        // UI-278 (D15): the branches and the Nation step on their objects; each test has its own seeded match.
        ["ui278-wisdom-politics"] = Inspection(random, Wisdom, false),
        ["ui278-dark-nation"] = Inspection(random, Dark, false),
        ["ui278-stewards-cleanse"] = StewardsCleanse(random),
        ["ui278-riders-branch"] = Play(State(random), Riders),
        ["ui278-thranduil-recruit"] = Play(State(random), KingsCards02.Thranduil),
        // UI-278 (D16): His Power Waxes with Denethor in the defending army and the Corrupted Théoden at home.
        ["ui278-power-defender"] = KingsCardDecisionScenarios.PowerWaxes(Guid.NewGuid().ToString("N"), random)
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

    // House of the Stewards with Boromir in Minas Tirith and a tile on Denethor.
    private static MatchState StewardsCleanse(IRandomSource random)
    {
        var state = State(random);
        Character(state, "base.character.boromir", "minas-tirith");
        CorruptionRules.Attach(state, SovereignLifecycle.Find(state, SovereignCatalog.Denethor)!, One);
        return Play(state, KingsFreePeoplesCards01.Stewards);
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
