using Wotr.Contracts;
using Wotr.Domain.Content;
using static Wotr.Domain.ExpansionBoard;

namespace Wotr.Domain;

// UI-348 and UI-276 (EXP-15 D7, D17-D20; owner 2026-10-26): the Kings decisions of the owner's playtest the test menu
// lacked — Dismay after the roll, Far-sighted on Thranduil's picture, Brand's
// Combat card replacement after the reveal (Heir of Bard with Brand Awakened in the North's army, Shadows in the North
// for a Corrupted Brand) and the Palantír of Anárion's extra card, whose skip is the console's Pass. The client's network
// tests start from the same builders.
public static class KingsDecisionScenarios
{
    private const string Group = "Kings · Decisions";
    private const string Dale = "dale";
    private const string Target = "northern-rhovanion";
    private const string FreePeoplesCard = "base.fp.strategy.01";
    private const string ShadowCard = KingsCards03.Valinor;

    private static MatchConfiguration Kings => new() { Kings = true };

    public static IEnumerable<TestScenario> All() =>
    [
        new("kings-brand-heir", Group, "Heir of Bard",
            "Brand, King of Dale, and the North's army attack from Dale; after the reveal the gilt chip with Brand's arms " +
            "beside your Combat card cancels the Shadow card at initiative 0, Pass keeps your card's effects.",
            Side.FreePeoples, (id, random) => Brand(id, random, true)) { Modules = [ExpansionModules.Kings] },
        new("kings-brand-shadows", Group, "Shadows in the North",
            "Brand is Corrupted and the North's army attacks from Dale; after the reveal the Shadow's gilt chip with " +
            "Brand's arms cancels the Free Peoples card, Pass keeps the Shadow card's effects.",
            Side.Shadow, (id, random) => Brand(id, random, false)) { Modules = [ExpansionModules.Kings] },
        new("kings-anarion", Group, "Palantír of Anárion",
            "Denethor, Lord Steward, stands in Minas Tirith and the Ruler die drew a card: click a deck for the extra " +
            "card, or Pass to finish the action without it.",
            Side.FreePeoples, Anarion) { Modules = [ExpansionModules.Kings] },
        new("kings-chieftain-substitution", Group, "A Chieftain's substitution",
            "The North's army attacks the Black Serpent's Sauron regulars beside Dale; before the reveal the Shadow's gilt " +
            "chip with the Southrons' arms beside its Combat card substitutes Red Wrath, Pass keeps the card's effects.",
            Side.Shadow, Chieftain) { Modules = [ExpansionModules.Kings] },
        new("kings-dismay", Group, "Dismay",
            "Turn 2 after the roll: Théoden fell as an Awakened king, so the Shadow holds his Dismay token; a click on the " +
            "Free Peoples' glowing Ruler die spends it and re-rolls the die, Pass keeps the token.",
            Side.Shadow, Dismay) { Modules = [ExpansionModules.Lords, ExpansionModules.Kings] },
        new("kings-far-sighted", Group, "Far-sighted",
            "Thranduil is Awakened in the Woodland Realm and the Free Peoples' Ruler die shows a Character: with it selected, " +
            "Thranduil's glowing picture moves the Fellowship or companions with Far-sighted; the usual move needs no click.",
            Side.FreePeoples, FarSighted) { Modules = [ExpansionModules.Kings] }
    ];

    // Turn 2's roll after Théoden fell as an Awakened king: the Shadow holds his Dismay token, and Thranduil's Awakening
    // keeps the Free Peoples' Ruler die in their pool.
    public static MatchState Dismay(string id, IRandomSource random)
    {
        // Dismay needs Lords as well as Kings (rules X02, KINGS pp. 17-18).
        var state = Actions(id, random, new() { Lords = true, Kings = true });
        // Thranduil, still Awakened, keeps the Free Peoples' Ruler die in play.
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Thranduil), "woodland-realm");
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Theoden), "edoras");
        CharacterLifecycle.Eliminate(state, state.Characters.Single(c => c.Id == SovereignCatalog.Theoden));
        state.Turn++;
        DiceRules.Recover(state);
        TurnFlow.RollAndBeginActions(state, new SequenceRandom(new int[64]));
        while (state.Pending?.Kind is DecisionKind.SpecialDieChoice or DecisionKind.RulerDiscard)
        {
            var special = state.Pending.Kind == DecisionKind.SpecialDieChoice;
            var choices = special ? LordsDiceRoll.LegalChoices(state, state.Pending.Owner) : KingsDice.DiscardCandidates(state, state.Pending.Owner);
            state = Execute(state, special ? CommandKind.ChooseSpecialDie : CommandKind.DiscardRulerDie, new() { DieId = choices[0] });
        }
        if (state.Pending?.Kind != DecisionKind.Dismay)
            throw new InvalidOperationException("Dismay must wait for the Shadow after the roll.");
        return state;
    }

    // The first action of turn 1 with Thranduil Awakened at home and the Ruler die showing a Character.
    public static MatchState FarSighted(string id, IRandomSource random)
    {
        var state = Actions(id, random, Kings);
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Thranduil), "woodland-realm");
        var ruler = KingsDice.Ruler(state, Side.FreePeoples)!;
        Show(state, ruler.Id, DieFace.Character);
        return state;
    }

    // The Free Peoples draw a card with their Ruler die (Event) while Denethor is in Minas Tirith: the Palantír of
    // Anárion offers either deck or no extra card.
    public static MatchState Anarion(string id, IRandomSource random)
    {
        var state = Actions(id, random, Kings);
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Denethor), "minas-tirith");
        var ruler = state.Dice.Single(d => d.Owner == Side.FreePeoples && d.Source == SovereignCatalog.RulerSource);
        Show(state, ruler.Id, DieFace.Event);
        state = Execute(state, CommandKind.DrawCard, new() { DieId = ruler.Id, Deck = "strategy" });
        if (state.Pending?.Presentation != "Deck" || state.Pending.SourceId != SovereignCatalog.Denethor)
            throw new InvalidOperationException("The Palantír of Anárion must offer its extra card.");
        return state;
    }

    // The North's army in Dale — with Brand Awakened, or with Brand Corrupted at home — attacks two Sauron regulars next
    // door; both sides commit a Combat card, and the reveal offers Brand's replacement to his owner.
    public static MatchState Brand(string id, IRandomSource random, bool awakened)
    {
        var board = Battle(id, random);
        var state = board.State;
        var brand = Sovereign(state, SovereignCatalog.Brand);
        if (awakened)
            SovereignLifecycle.Awaken(state, brand, Dale);
        else
        {
            brand.Status = SovereignStatus.Corrupted;
            brand.CorruptedRegion = SovereignCatalog.All[SovereignCatalog.Brand].Home;
            state.Units.RemoveAll(u => u.Id == SovereignCatalog.Brand);
        }
        state = Commit(board, awakened ? Side.FreePeoples : Side.Shadow);
        return state;
    }

    // The same battle with the Black Serpent beside the Sauron regulars: before the reveal the Shadow may substitute his
    // Red Wrath for its Combat card's effects (KINGS, Kings/KingsChieftainCombat.cs).
    public static MatchState Chieftain(string id, IRandomSource random)
    {
        var board = Battle(id, random);
        var serpent = new CharacterState { Id = KingsChieftains.Serpent, Location = Target };
        CharacterLifecycle.ApplyVersion(serpent, CharacterCatalog.Kings[KingsChieftains.Serpent]);
        board.State.Characters.Add(serpent);
        var state = Commit(board, Side.Shadow);
        if (BattleRuntime.Active(state)!.CardsRevealed)
            throw new InvalidOperationException("A Chieftain's substitution comes before the reveal.");
        return state;
    }

    private static ScenarioBoard Battle(string id, IRandomSource random) => ScenarioBoard.Setup(id, random, Kings)
        .AtWar("north", "sauron")
        .Place("north", UnitKind.Regular, Dale, 2)
        .Place("sauron", UnitKind.Regular, Target, 2)
        .Dice(Side.FreePeoples, DieFace.Army, DieFace.Character, DieFace.Muster, DieFace.Event)
        .Hand(Side.FreePeoples, FreePeoplesCard)
        .Hand(Side.Shadow, ShadowCard);

    // The North's army in Dale attacks; both sides commit their Combat card, and the replacement waits for its owner.
    private static MatchState Commit(ScenarioBoard board, Side owner)
    {
        board.Act(Side.FreePeoples);
        var state = Attack(board.State);
        while (state.Pending?.Kind == DecisionKind.CombatCard)
            state = Execute(state, CommandKind.CommitCombatCard,
                new() { CardId = state.Pending.Owner == Side.FreePeoples ? FreePeoplesCard : ShadowCard });
        if (state.Pending?.Presentation != "CombatReplacement" || state.Pending.Owner != owner)
            throw new InvalidOperationException("The Combat card replacement must wait for its owner.");
        return state;
    }

    // The whole army in Dale attacks, as its legal option lists it (the North's Leader and, Awakened, Brand with it).
    private static MatchState Attack(MatchState state)
    {
        var army = GameRuntime.Projection(EffectCatalog.All).For(state, Side.FreePeoples).Options
            .First(o => o.Kind == CommandKind.AttackArmy && o.Enabled).Armies.Single(a => a.RegionId == Dale);
        var die = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Army);
        return Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = die.Id,
            MovementCost = DieFace.Army,
            Moves = [new() { From = Dale, To = Target, UnitIds = [.. army.UnitIds], CharacterIds = [.. army.CharacterIds] }]
        });
    }
}
