using Wotr.Contracts;
using Wotr.Domain.Content;
using static Wotr.Domain.ExpansionBoard;

namespace Wotr.Domain;

// UI-278 (EXP-15 D15-D16; owner 2026-10-26): the Kings cards whose choices stand on their objects — Wisdom of Elrond and
// It Is Not So Dark Here on the nation tokens and the kings, the five recruit-or-cleanse cards on the Reserve and the
// king, and There Is Hope for Victory and His Power Waxes on a king who takes part in the battle; the Palantír of
// Barad-dûr's Muster-as-Event starts at its card on the table (owner's decision 17, 2026-10-19). The client's network
// tests start from the same builders.
public static class KingsCardDecisionScenarios
{
    private const string Group = "Kings · Decisions";
    private const string Boromir = "base.character.boromir";
    private const string MinasTirith = "minas-tirith";
    private const string Osgiliath = "osgiliath";
    private const string Edoras = "edoras";
    private const string Westemnet = "westemnet";

    private static MatchConfiguration Kings => new() { Kings = true };

    public static IEnumerable<TestScenario> All() =>
    [
        new("kings-card-wisdom", Group, "Wisdom of Elrond",
            "Dáin and Denethor carry Corruption tiles. Play Wisdom of Elrond: a glowing nation token activates and advances " +
            "that nation, a glowing king shows his tiles to return one, Pass keeps them. It Is Not So Dark Here advances " +
            "the nation whose token you click, then shows a king's tiles.",
            Side.FreePeoples, Wisdom) { Modules = [ExpansionModules.Kings] },
        new("kings-card-recruit-or-cleanse", Group, "Recruit or remove corruption",
            "Every king carries a Corruption tile and Boromir stands in Minas Tirith. Each card in your hand recruits or " +
            "cleanses: a glowing figure of the king's nation in the Reserve recruits, the glowing king returns one random " +
            "tile. Four dice: play any four of the five cards.",
            Side.FreePeoples, RecruitOrCleanse) { Modules = [ExpansionModules.Kings] },
        new("kings-card-hope", Group, "There Is Hope for Victory",
            "Denethor and Théoden attack Osgiliath with Gondor's army and Ever at Your Service; after the reveal both kings " +
            "glow in the battle window's army: the one you click awakens and adds one to the Leader re-roll.",
            Side.FreePeoples, Hope) { Modules = [ExpansionModules.Kings] },
        new("kings-card-power-waxes", Group, "His Power Waxes",
            "The Shadow attacks Edoras with the Palantír of Barad-dûr as its Combat card; Denethor defends Edoras, the " +
            "Corrupted Théoden's home. After the reveal Denethor glows in the battle window's army and Théoden on his portrait.",
            Side.Shadow, PowerWaxes) { Modules = [ExpansionModules.Kings] },
        new("kings-card-palantir", Group, "The Palantír of Barad-dûr",
            "The Palantír of Barad-dûr stands on your table and two dice show Muster: click the card, then a glowing Muster " +
            "die (or the die, then the card), to spend it as an Event die once this turn; a card in the hand or a deck then " +
            "takes it.",
            Side.Shadow, Palantir) { Modules = [ExpansionModules.Kings] }
    ];

    // The Palantír of Barad-dûr on the Shadow's table, two Muster dice and an Event card in the hand.
    public static MatchState Palantir(string id, IRandomSource random) => ScenarioBoard.Setup(id, random, Kings)
        .Table(Side.Shadow, KingsCards03.Palantir)
        .Dice(Side.Shadow, DieFace.Muster, DieFace.Muster, DieFace.Army)
        .Hand(Side.Shadow, KingsCards03.Threats)
        .Act(Side.Shadow);

    // Wisdom of Elrond and It Is Not So Dark Here in the hand, an Event and a Character die to play them.
    public static MatchState Wisdom(string id, IRandomSource random)
    {
        var board = ScenarioBoard.Setup(id, random, Kings)
            .Dice(Side.FreePeoples, DieFace.Event, DieFace.Character)
            .Change(state => state.Player(Side.FreePeoples).Hand.Clear())
            .Hand(Side.FreePeoples, KingsFreePeoplesCards01.Wisdom, KingsFreePeoplesCards01.Dark);
        Tiles(board.State, SovereignCatalog.Dain, 2);
        Tiles(board.State, SovereignCatalog.Denethor, 1);
        return board.Act(Side.FreePeoples);
    }

    // The five recruit-or-cleanse cards in the hand, each king with one tile, Boromir in Minas Tirith for the Stewards.
    public static MatchState RecruitOrCleanse(string id, IRandomSource random)
    {
        var board = ScenarioBoard.Setup(id, random, Kings)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Event, DieFace.Event, DieFace.Event)
            .Change(state => state.Player(Side.FreePeoples).Hand.Clear())
            .Hand(Side.FreePeoples, KingsFreePeoplesCards01.Stewards, KingsFreePeoplesCards01.Riders, KingsFreePeoplesCards01.Brand,
                KingsCards02.Dain, KingsCards02.Thranduil)
            .Companion(Boromir, MinasTirith);
        foreach (var sovereign in SovereignCatalog.All.Keys)
            Tiles(board.State, sovereign, 1);
        return board.Act(Side.FreePeoples);
    }

    // Gondor's army with Denethor and Théoden attacks two Sauron regulars in Osgiliath; Ever at Your Service's Combat side
    // (There Is Hope for Victory) waits for the Free Peoples' choice of a king after the reveal.
    public static MatchState Hope(string id, IRandomSource random)
    {
        var board = ScenarioBoard.Setup(id, random, Kings)
            // Every attacking figure's nation is at war, Théoden's Rohan as well.
            .AtWar("gondor", "rohan", "sauron")
            .Clear(Osgiliath)
            .Place("sauron", UnitKind.Regular, Osgiliath, 2)
            .Dice(Side.FreePeoples, DieFace.Army, DieFace.Character)
            .Hand(Side.FreePeoples, KingsCards02.Service)
            .Change(state => state.Units.Single(u => u.Id == SovereignCatalog.Theoden).Location = MinasTirith);
        board.Act(Side.FreePeoples);
        var state = Commit(Attack(board.State, Side.FreePeoples, MinasTirith, Osgiliath), KingsCards02.Service, "");
        return Expect(state, Side.FreePeoples);
    }

    // Three Sauron regulars attack Edoras, which Rohan and Denethor defend, while Théoden is Corrupted at home there
    // (a Corrupted Brand would offer the Shadow his replacement first); the Palantír of Barad-dûr's Combat side (His Power
    // Waxes) waits for the Shadow's choice of a king after the reveal.
    public static MatchState PowerWaxes(string id, IRandomSource random)
    {
        var board = ScenarioBoard.Setup(id, random, Kings)
            .AtWar("sauron")
            .Clear(Westemnet)
            .Place("sauron", UnitKind.Regular, Westemnet, 3)
            .Place("rohan", UnitKind.Regular, Edoras)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster)
            .Hand(Side.Shadow, KingsCards03.Palantir)
            .Change(state =>
            {
                var theoden = Sovereign(state, SovereignCatalog.Theoden);
                theoden.Status = SovereignStatus.Corrupted;
                theoden.CorruptedRegion = SovereignCatalog.All[SovereignCatalog.Theoden].Home;
                state.Units.RemoveAll(u => u.Id == SovereignCatalog.Theoden);
                state.Units.Single(u => u.Id == SovereignCatalog.Denethor).Location = Edoras;
            });
        board.Act(Side.Shadow);
        var state = Commit(Attack(board.State, Side.Shadow, Westemnet, Edoras), "", KingsCards03.Palantir);
        return Expect(state, Side.Shadow);
    }

    // Value-1 Corruption tiles from the Hunt Pool on a king.
    private static void Tiles(MatchState state, string sovereign, int count)
    {
        var tiles = state.HuntTiles.Where(t => t.Location == "pool" && KingsTiles.IsNumeric(t.Id) && !KingsTiles.IsKings(t.Id) &&
            KingsTiles.CorruptionValue(t.Id) == 1).Take(count).Select(t => t.Id).ToList();
        foreach (var tile in tiles)
            CorruptionRules.Attach(state, Sovereign(state, sovereign), tile);
    }

    // The whole army of a region attacks, as its legal option lists it.
    private static MatchState Attack(MatchState state, Side side, string from, string to)
    {
        var army = GameRuntime.Projection(EffectCatalog.All).For(state, side).Options
            .First(o => o.Kind == CommandKind.AttackArmy && o.Enabled).Armies.Single(a => a.RegionId == from);
        var die = state.Dice.First(d => d.Owner == side && d.Location == DieLocation.Available && d.Face == DieFace.Army);
        return Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = die.Id,
            MovementCost = DieFace.Army,
            Moves = [new() { From = from, To = to, UnitIds = [.. army.UnitIds], CharacterIds = [.. army.CharacterIds] }]
        });
    }

    private static MatchState Commit(MatchState state, string freePeoples, string shadow)
    {
        while (state.Pending?.Kind == DecisionKind.CombatCard)
            state = Execute(state, CommandKind.CommitCombatCard,
                new() { CardId = state.Pending.Owner == Side.FreePeoples ? freePeoples : shadow });
        return state;
    }

    private static MatchState Expect(MatchState state, Side owner)
    {
        if (state.Pending?.Presentation != "Sovereign" || state.Pending.Owner != owner)
            throw new InvalidOperationException("The card's choice of a king must wait for its owner after the reveal, not " +
                $"{state.Pending?.Kind} {state.Pending?.Presentation} of {state.Pending?.Owner}.");
        return state;
    }
}
