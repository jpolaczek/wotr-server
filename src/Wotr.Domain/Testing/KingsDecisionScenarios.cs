using Wotr.Contracts;
using static Wotr.Domain.ExpansionBoard;

namespace Wotr.Domain;

// UI-348 (EXP-15 D7, D17; owner 2026-10-26): the Kings decisions of the owner's playtest the test menu lacked — Brand's
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
            Side.FreePeoples, Anarion) { Modules = [ExpansionModules.Kings] }
    ];

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
        var board = ScenarioBoard.Setup(id, random, Kings)
            .AtWar("north", "sauron")
            .Place("north", UnitKind.Regular, Dale, 2)
            .Place("sauron", UnitKind.Regular, Target, 2)
            .Dice(Side.FreePeoples, DieFace.Army, DieFace.Character, DieFace.Muster, DieFace.Event)
            .Hand(Side.FreePeoples, FreePeoplesCard)
            .Hand(Side.Shadow, ShadowCard);
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
        board.Act(Side.FreePeoples);
        state = Attack(state);
        while (state.Pending?.Kind == DecisionKind.CombatCard)
            state = Execute(state, CommandKind.CommitCombatCard,
                new() { CardId = state.Pending.Owner == Side.FreePeoples ? FreePeoplesCard : ShadowCard });
        var owner = awakened ? Side.FreePeoples : Side.Shadow;
        if (state.Pending?.Presentation != "CombatReplacement" || state.Pending.Owner != owner)
            throw new InvalidOperationException("Brand's replacement must wait for his owner after the reveal.");
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
