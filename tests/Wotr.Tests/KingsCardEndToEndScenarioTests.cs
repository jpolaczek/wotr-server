using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

// TEST-13: every Kings card has an event scenario (a new one, or a decision scenario that already shows the event whole)
// and a combat scenario in the Kings tab. The event is a legal play paid by a die of the turn whose play runs through the
// engine and changes the board once the demo CPU answers its decisions; the board meets each clause that could
// otherwise do nothing. The combat scenario waits at the owner's Combat card choice, which offers the card in a battle
// where its combat condition holds and its effect has something to act on, and committing it runs through the engine.
public class KingsCardEndToEndScenarioTests
{
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);
    private static readonly DemoBot Bot = new(Engine, Views);
    private static readonly int[] Seeds = [1, 2, 3, 4, 5, 7];
    private const int StepLimit = 200;
    private const string Boromir = "base.character.boromir";

    // The events a decision scenario of UI-278 already shows whole; the four national cards show their recruit-or-cleanse
    // branch there and their removal of a Corrupted king in their own event scenario.
    private static readonly IReadOnlyDictionary<string, string> ReusedEvents = new Dictionary<string, string>
    {
        [KingsFreePeoplesCards01.Stewards] = "kings-card-recruit-or-cleanse",
        [KingsFreePeoplesCards01.Dark] = "kings-card-wisdom",
        [KingsFreePeoplesCards01.Wisdom] = "kings-card-wisdom",
        [KingsFreePeoplesCards01.Riders] = "kings-card-recruit-or-cleanse",
        [KingsFreePeoplesCards01.Brand] = "kings-card-recruit-or-cleanse",
        [KingsCards02.Dain] = "kings-card-recruit-or-cleanse",
        [KingsCards02.Thranduil] = "kings-card-recruit-or-cleanse"
    };

    // The clauses of the Kings events whose standard board would leave them nothing to act on.
    private static readonly Dictionary<string, (string Clause, Func<MatchState, bool> Holds)> Clauses = new()
    {
        [KingsFreePeoplesCards01.Aid] = ("Companions outside the Fellowship a move from an Initial king, one with tiles", s =>
            Outside(s).Count() >= 2 && Initial(s).Any(k => k.Tiles.Count > 0 && Outside(s).Any(c => Near(c.Location, Region(s, k))))),
        [KingsFreePeoplesCards01.Riders] = ("a Corrupted Théoden", s => Status(s, SovereignCatalog.Theoden) == SovereignStatus.Corrupted),
        [KingsFreePeoplesCards01.Brand] = ("a Corrupted Brand", s => Status(s, SovereignCatalog.Brand) == SovereignStatus.Corrupted),
        [KingsCards02.Dain] = ("a Corrupted Dáin", s => Status(s, SovereignCatalog.Dain) == SovereignStatus.Corrupted),
        [KingsCards02.Thranduil] = ("a Corrupted Thranduil", s => Status(s, SovereignCatalog.Thranduil) == SovereignStatus.Corrupted),
        [KingsCards02.Service] = ("a king with hidden tiles", s => Initial(s).Any(k => k.Tiles.Count > 0)),
        [KingsCards02.Villages] = ("four nations with a Regular or Leader in the reserve", s => s.Units
            .Where(u => u.Location == "reserve" && BoardRules.Owner(u) == Side.FreePeoples && u.Kind is UnitKind.Regular or UnitKind.Leader &&
                !u.Id.StartsWith("kings.", StringComparison.Ordinal)).Select(u => u.Nation).Distinct().Count() >= 4),
        [KingsCards02.Welcome] = ("an awakenable king of an active nation not at war, with tiles, beside a Companion", s =>
            SovereignAwakening.Destinations(s, Sovereign(s, SovereignCatalog.Denethor)).Count > 0 && !AtWar(s, "gondor") &&
            Sovereign(s, SovereignCatalog.Denethor).Tiles.Count > 0 && Location(s, Boromir) == "minas-tirith"),
        [KingsCards02.West] = ("three Eyes, an Initial king beside a Nazgûl and no Free Peoples die", s =>
            CorruptionRules.Eyes(s).Count() >= 3 && Free(s, Side.FreePeoples) == 0 &&
            Initial(s).Any(k => s.Units.Any(u => u.Kind == UnitKind.Nazgul && Near(u.Location, Region(s, k))))),
        [KingsCards03.Valinor] = ("two Corrupted kings", s => KingsValinorCondition.Corrupted(s) >= 2),
        [KingsCards03.Denethor] = ("Denethor awakenable, Boromir beside him and a Shadow army and Army die for Minas Tirith", s =>
            SovereignAwakening.Destinations(s, Sovereign(s, SovereignCatalog.Denethor)).Count > 0 && KingsDenethorRules.AnyDie(s) &&
            Army(s, "osgiliath", Side.Shadow) && Free(s, Side.Shadow, DieFace.Army) >= 2),
        [KingsCards03.Threats] = ("an awakenable king, two Eyes and a king one counting tile from his resistance", s =>
            s.Kings!.Sovereigns.Any(k => SovereignAwakening.Destinations(s, k).Count > 0) && CorruptionRules.CanAttempt(s) &&
            Sovereign(s, SovereignCatalog.Brand).Tiles.Count == 1 && PoolCounts(s)),
        [KingsCards03.Stormcrow] = ("a Companion in a nation that can step back, with an Initial king", s =>
            Location(s, Boromir) == "minas-tirith" && s.Nations.Single(n => n.Id == "gondor").StepsToWar is > 0 and < 3 &&
            Status(s, SovereignCatalog.Denethor) == SovereignStatus.Initial),
        [KingsCards03.King] = ("Denethor and another king Awakened", s => Status(s, SovereignCatalog.Denethor) == SovereignStatus.Awakened &&
            s.Kings!.Sovereigns.Count(k => k.Status == SovereignStatus.Awakened) >= 2),
        [KingsCards03.Palantir] = ("a second Muster die, a Will of the West die and an Elven Ring", s =>
            Free(s, Side.Shadow, DieFace.Muster) >= 2 && Free(s, Side.FreePeoples, DieFace.WillOfTheWest) > 0 &&
            s.Rings.Any(r => r.Owner == Side.FreePeoples)),
        [KingsCards04.Enemy] = ("a Corrupted king in a region with a Free Peoples army", s =>
            s.Kings!.Sovereigns.Any(k => k.Status == SovereignStatus.Corrupted && Army(s, k.CorruptedRegion, Side.FreePeoples)))
    };

    // What each Kings combat effect needs in its battle to act, beyond its printed condition.
    private static readonly Dictionary<string, (string Clause, Func<MatchState, BattleState, bool> Holds)> CombatClauses = new()
    {
        ["combat.brave-stand"] = ("Companions and several Shadow dice", (s, b) => Companions(s, b) > 0 && ArmyUnits(s, b, Side.Shadow) > 1),
        [KingsFreePeoplesCards01.Banner] = ("an Awakened king and a Shadow Leader re-roll", (s, b) =>
            Awakened(s, b).Any() && BattleLeadership.Total(s, b.Force(Side.Shadow)) > 0),
        [KingsFreePeoplesCards01.GodOfOld] = ("an Awakened king with Leadership", (s, b) => Awakened(s, b).Any(c => c.Leadership > 0)),
        ["combat.daylight"] = ("more than three Shadow dice", (s, b) => ArmyUnits(s, b, Side.Shadow) > 3),
        ["combat.valour"] = ("a Free Peoples Elite", (s, b) => BattleRules.Units(s, b.Force(Side.FreePeoples)).Any(u => u.Kind == UnitKind.Elite)),
        [KingsCards02.Hope] = ("an Initial king who awakens", (s, b) => b.Force(Side.FreePeoples).Participants
            .Any(id => SovereignLifecycle.Find(s, id)?.Status == SovereignStatus.Initial)),
        ["combat.servant-of-the-shadow"] = ("a Minion with Leadership and several Free Peoples dice", (s, b) =>
            BattleRules.Characters(s, b.Force(Side.Shadow)).Any(c => c.Leadership > 0) && ArmyUnits(s, b, Side.FreePeoples) > 1),
        [KingsCards02.Vain] = ("Free Peoples Leadership to reduce", (s, b) => BattleLeadership.Total(s, b.Force(Side.FreePeoples)) > 0),
        ["combat.great-host"] = ("twice the defenders whatever the casualties", (s, b) =>
            ArmyUnits(s, b, Side.Shadow) - ArmyUnits(s, b, Side.FreePeoples) >= 2 * ArmyUnits(s, b, Side.FreePeoples)),
        [KingsCards03.Power] = ("a king in the battle's region", (s, b) => HisPowerWaxesCondition.Sovereigns(s, b).Any())
    };

    public static TheoryData<string> Cards => new(CardCatalog.Kings.Keys.Order(StringComparer.Ordinal));

    public static TheoryData<string> EventScenarios => new(CardCatalog.Kings.Values.Where(c => KingsCardEvents.Setups.ContainsKey(c.Id))
        .Select(KingsCardScenarios.EventId).Concat(ReusedEvents.Values).Distinct().Order(StringComparer.Ordinal));

    public static TheoryData<string> ConditionalCards => new(Clauses.Keys.Order(StringComparer.Ordinal));

    [Fact]
    public void EveryKingsCardHasAnEventAndACombatScenarioInTheKingsTab()
    {
        Assert.Equal(19, CardCatalog.Kings.Count);
        foreach (var card in CardCatalog.Kings.Values)
        {
            var combat = TestScenarioCatalog.Find(KingsCardScenarios.CombatId(card))!;
            Assert.Equal(card.Title + " · Combat: " + CardTextCatalog.CombatTitle(card.Id), combat.Title);
            Assert.Equal((card.Side, ExpansionModules.Kings), (combat.StartingSide, combat.Modules[0]));
            var play = KingsCardEvents.Setups.ContainsKey(card.Id) ? TestScenarioCatalog.Find(KingsCardScenarios.EventId(card))! : null;
            Assert.True(play is not null || ReusedEvents.ContainsKey(card.Id), $"{card.Id}: no event scenario");
            if (play is null)
                continue;
            Assert.StartsWith(card.Title + " · Event", play.Title);
            Assert.Equal((card.Side, ExpansionModules.Kings), (play.StartingSide, play.Modules[0]));
            Assert.Contains(card.Title, play.Description);
        }
    }

    [Theory]
    [MemberData(nameof(EventScenarios))]
    public void TheEventScenarioOffersItsCardsAndTheirPlayRuns(string id)
    {
        var scenario = TestScenarioCatalog.Find(id)!;
        var cards = CardCatalog.Kings.Values.Where(c => KingsCardScenarios.EventId(c) == id || ReusedEvents.GetValueOrDefault(c.Id) == id);
        foreach (var card in cards)
        {
            var state = scenario.Create("kings-event-" + card.Id);
            Assert.Equal(scenario.StartingSide, state.Pending?.Owner);
            Accepted(state, scenario.StartingSide, Play(state, scenario.StartingSide, card.Id, Nation(card.Id)));
        }
    }

    [Fact]
    public void EveryNewEventScenarioLetsItsCardChangeTheBoard()
    {
        var idle = KingsCardEvents.Setups.Keys.Order(StringComparer.Ordinal).Where(id => !Seeds.Any(seed => ChangesTheBoard(id, seed))).ToList();
        Assert.True(idle.Count == 0, "These Kings event scenarios leave their card nothing to act on: " + string.Join(", ", idle));
    }

    [Theory]
    [MemberData(nameof(ConditionalCards))]
    public void TheEventScenarioMeetsTheCardsClauses(string cardId)
    {
        var state = Event(cardId);
        var (clause, holds) = Clauses[cardId];
        Assert.True(holds(state), $"{cardId} ({CardCatalog.All[cardId].Title}) needs {clause}.");
    }

    [Theory]
    [MemberData(nameof(Cards))]
    public void TheCombatScenarioOffersTheCardWhereItsEffectActs(string cardId)
    {
        var card = CardCatalog.Kings[cardId];
        var scenario = TestScenarioCatalog.Find(KingsCardScenarios.CombatId(card))!;
        var state = scenario.Create("kings-combat-" + cardId);
        Assert.Equal((DecisionKind.CombatCard, scenario.StartingSide), (state.Pending?.Kind, state.Pending!.Owner));
        var view = Views.For(state, scenario.StartingSide);
        Assert.Contains(cardId, view.Options.Single(o => o.Kind == CommandKind.CommitCombatCard).EligibleIds);
        var battle = BattleRuntime.Active(state)!;
        foreach (var effect in EffectCatalog.All.Combats[cardId].Effects.Where(e => CombatClauses.ContainsKey(e.ProgramId)))
        {
            var (clause, holds) = CombatClauses[effect.ProgramId];
            Assert.True(holds(state, battle), $"{cardId} ({effect.ProgramId}) needs {clause}.");
        }
        Accepted(state, scenario.StartingSide, Command(state, view, CommandKind.CommitCombatCard, new() { CardId = cardId }));
    }

    // The removal branch takes the Corrupted king off the board and draws nothing.
    [Theory]
    [InlineData(KingsFreePeoplesCards01.Riders, SovereignCatalog.Theoden)]
    [InlineData(KingsFreePeoplesCards01.Brand, SovereignCatalog.Brand)]
    [InlineData(KingsCards02.Dain, SovereignCatalog.Dain)]
    [InlineData(KingsCards02.Thranduil, SovereignCatalog.Thranduil)]
    public void TheNationalCardRemovesItsCorruptedKing(string cardId, string sovereign)
    {
        var state = Event(cardId);
        var hand = state.Player(Side.FreePeoples).Hand.Count;
        state = Run(state, Side.FreePeoples, Play(state, Side.FreePeoples, cardId));
        Assert.Equal(SovereignStatus.Removed, Status(state, sovereign));
        Assert.Equal(hand - 1, state.Player(Side.FreePeoples).Hand.Count);
    }

    // Aid in Time of Need: Gimli walks from Dale to Erebor, and Dáin awakens there with his tile returned.
    [Fact]
    public void AidMovesACompanionToAKingWhoAwakensThere()
    {
        var state = Event(KingsFreePeoplesCards01.Aid);
        state = Run(state, Side.FreePeoples, Play(state, Side.FreePeoples, KingsFreePeoplesCards01.Aid));
        state = Run(state, Side.FreePeoples, Command(state, Views.For(state, Side.FreePeoples), CommandKind.MoveCompanions,
            new() { CompanionMoves = [new() { Ids = ["base.character.gimli"], Route = ["dale", "erebor"] }] }));
        state = ChooseIfOffered(state, Side.FreePeoples, SovereignCatalog.Dain);
        Assert.Equal(SovereignStatus.Awakened, Status(state, SovereignCatalog.Dain));
        Assert.Equal("erebor", Location(state, SovereignCatalog.Dain));
        Assert.Empty(Sovereign(state, SovereignCatalog.Dain).Tiles);
    }

    // Ever at Your Service: Dáin recruits in Erebor, then one of his two tiles returns.
    [Fact]
    public void ServiceRecruitsWithTheKingAndReturnsOneOfHisTiles()
    {
        var state = Event(KingsCards02.Service);
        var erebor = state.Units.Count(u => u.Location == "erebor");
        state = Run(state, Side.FreePeoples, Play(state, Side.FreePeoples, KingsCards02.Service));
        state = ChooseIfOffered(state, Side.FreePeoples, SovereignCatalog.Dain);
        state = Resolve(state, 1);
        Assert.Equal(erebor + 1, state.Units.Count(u => u.Location == "erebor"));
        Assert.Single(Sovereign(state, SovereignCatalog.Dain).Tiles);
    }

    // Your Welcome Is Doubtful: the awakening goes, and the Character die discards it because Boromir stands with Denethor.
    [Fact]
    public void WelcomeBlocksTheAwakeningUntilTheCharacterDieDiscardsIt()
    {
        var state = Event(KingsCards02.Welcome);
        Assert.Contains(Views.For(StateFor(state, Side.FreePeoples), Side.FreePeoples).Options, o => o.Kind == CommandKind.AwakenSovereign);
        state = Run(state, Side.Shadow, Play(state, Side.Shadow, KingsCards02.Welcome));
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        var options = Views.For(state, Side.FreePeoples).Options;
        Assert.DoesNotContain(options, o => o.Kind == CommandKind.AwakenSovereign);
        Assert.False(KingsCardRestrictions.CanRemoveCorruption(state));
        var discard = options.Single(o => o.Kind == CommandKind.DiscardWelcome);
        var die = discard.EligibleDice.Single(id => state.Dice.Single(d => d.Id == id).Face == DieFace.Character);
        state = Run(state, Side.FreePeoples, Command(state, Views.For(state, Side.FreePeoples), CommandKind.DiscardWelcome, new() { DieId = die }));
        Assert.DoesNotContain(KingsCards02.Welcome, state.Player(Side.Shadow).Table);
    }

    // The West Has Failed: three tiles without spending an Eye while the Free Peoples have no die, and Brand is corrupted.
    [Fact]
    public void TheWestHasFailedCorruptsBrandWithoutSpendingAnEye()
    {
        var state = Event(KingsCards02.West);
        var eyes = CorruptionRules.Eyes(state).Count();
        state = Run(state, Side.Shadow, Play(state, Side.Shadow, KingsCards02.West));
        state = ChooseIfOffered(state, Side.Shadow, SovereignCatalog.Brand);
        Assert.Equal(3, CorruptionRuntime.Current(state)!.DrawLimit);
        state = FinishAttempt(state);
        Assert.Equal(eyes, CorruptionRules.Eyes(state).Count());
        Assert.Equal(SovereignStatus.Corrupted, Status(state, SovereignCatalog.Brand));
    }

    // Denethor's Folly: no awakening, a discard by Boromir's presence, and no Free Peoples Combat card at Minas Tirith.
    [Fact]
    public void FollyForbidsTheAwakeningAndTheCombatCardsAtMinasTirith()
    {
        var state = Event(KingsCards03.Denethor);
        state = Run(state, Side.Shadow, Play(state, Side.Shadow, KingsCards03.Denethor));
        var options = Views.For(state, Side.FreePeoples).Options;
        Assert.DoesNotContain(options, o => o.Kind == CommandKind.AwakenSovereign);
        Assert.Contains(options, o => o.Kind == CommandKind.DiscardDenethor && o.EligibleDice.Count == Free(state, Side.FreePeoples));
        state = Draw(state);
        state = Run(state, Side.Shadow, Command(state, Views.For(state, Side.Shadow), CommandKind.AttackArmy, new()
        {
            DieId = FreeDie(state, Side.Shadow, DieFace.Army).Id, MovementCost = DieFace.Army,
            Moves = [new() { From = "osgiliath", To = "minas-tirith", UnitIds = Units(state, "osgiliath") }]
        }));
        Assert.Equal("minas-tirith", BattleRuntime.Active(state)!.Region);
        for (var step = 0; step < StepLimit && BattleRuntime.Active(state) is not null; step++)
        {
            Assert.False(state.Pending is { Kind: DecisionKind.CombatCard, Owner: Side.FreePeoples }, "The Free Peoples may play no Combat card.");
            state = Run(state, state.Pending!.Owner, Bot.Next(state, state.Pending.Owner)!, step);
        }
        Assert.Null(BattleRuntime.Active(state));
    }

    // Threats and Promises: a Muster die no longer awakens Thranduil, and corrupting Brand discards the card.
    [Fact]
    public void ThreatsBlocksTheMusterAwakeningUntilAKingIsCorrupted()
    {
        var state = Event(KingsCards03.Threats);
        state = Run(state, Side.Shadow, Play(state, Side.Shadow, KingsCards03.Threats));
        Assert.DoesNotContain(Views.For(state, Side.FreePeoples).Options, o => o.Kind == CommandKind.AwakenSovereign);
        state = Draw(state);
        state = Run(state, Side.Shadow, Command(state, Views.For(state, Side.Shadow), CommandKind.AttemptCorruption,
            new() { DieId = CorruptionRules.Eyes(state).First().Id, CharacterId = SovereignCatalog.Brand }));
        state = FinishAttempt(state);
        Assert.Equal(SovereignStatus.Corrupted, Status(state, SovereignCatalog.Brand));
        Assert.DoesNotContain(KingsCards03.Threats, state.Player(Side.Shadow).Table);
    }

    // Stormcrow: Gondor steps back, Denethor draws a tile, and the Free Peoples choose a Gondor unit to lose.
    [Fact]
    public void StormcrowCorruptsTheKingOfTheNationItSetsBack()
    {
        var state = Event(KingsCards03.Stormcrow);
        state = Run(state, Side.Shadow, Play(state, Side.Shadow, KingsCards03.Stormcrow, "gondor"));
        Assert.Equal(3, state.Nations.Single(n => n.Id == "gondor").StepsToWar);
        // A tile of 3 reaches Denethor's resistance at once and corrupts him.
        var denethor = Sovereign(state, SovereignCatalog.Denethor);
        Assert.True(denethor.Tiles.Count == 1 || denethor.Status == SovereignStatus.Corrupted);
        Assert.Contains(state.Events, e => e.Kind == "CorruptionAdded");
        Assert.Equal((DecisionKind.EffectChoice, Side.FreePeoples), (state.Pending!.Kind, state.Pending.Owner));
        Assert.All(state.Pending.PrivateChoices, id => Assert.Equal("gondor", state.Units.Single(u => u.Id == id).Nation));
    }

    // The King Is Revealed: five Regulars and a Nazgûl, then one figure for each of the two Awakened kings.
    [Fact]
    public void TheKingIsRevealedRecruitsOneMoreFigurePerAwakenedKing()
    {
        var state = Event(KingsCards03.King);
        state = Resolve(Run(state, Side.Shadow, Play(state, Side.Shadow, KingsCards03.King)), 1);
        Assert.Equal(8, state.Units.Count(u => u.Location == KingBonusRecruitStep.Region));
    }

    // The Palantír of Barad-dûr: on the table, the Free Peoples may discard it with Will of the West or a die and a Ring.
    [Fact]
    public void ThePalantirStaysUntilTheFreePeoplesDiscardIt()
    {
        var state = Event(KingsCards03.Palantir);
        state = Run(state, Side.Shadow, Play(state, Side.Shadow, KingsCards03.Palantir));
        Assert.Contains(KingsCards03.Palantir, state.Player(Side.Shadow).Table);
        var discard = Views.For(state, Side.FreePeoples).Options.Single(o => o.Kind == CommandKind.DiscardBaradPalantir);
        Assert.True(discard.Enabled);
        Assert.Contains(FreeDie(state, Side.FreePeoples, DieFace.WillOfTheWest).Id, discard.EligibleDice);
        Assert.Contains(state.Dice, d => d.Owner == Side.Shadow && KingsPalantir.CanConvert(state, Side.Shadow, d));
    }

    // Work of the Enemy: the Corrupted Théoden's region hits on 4 or more, the others on 5.
    [Fact]
    public void WorkOfTheEnemyHitsHarderBesideACorruptedKing()
    {
        var state = Event(KingsCards04.Enemy);
        state = Run(state, Side.Shadow, Play(state, Side.Shadow, KingsCards04.Enemy));
        Assert.Contains(SovereignCatalog.Theoden, state.Pending!.PrivateChoices);
        Assert.Equal(4, EnemySovereignStep.Threshold(state, SovereignCatalog.Theoden));
        Assert.Equal(5, EnemySovereignStep.Threshold(state, SovereignCatalog.Denethor));
    }

    private static MatchState Event(string cardId) =>
        TestScenarioCatalog.Find(KingsCardScenarios.EventId(CardCatalog.Kings[cardId]))!.Create("kings-clause-" + cardId);

    // Stormcrow's nation: Gondor, where Boromir stands.
    private static string Nation(string cardId) => cardId == KingsCards03.Stormcrow ? "gondor" : "";

    private static GameCommand Play(MatchState state, Side side, string cardId, string nation = "")
    {
        var view = Views.For(state, side);
        var option = view.Options.FirstOrDefault(o => o.Kind == CommandKind.PlayEvent);
        var play = option?.Events.FirstOrDefault(e => e.CardId == cardId);
        Assert.True(option is { Enabled: true } && play is not null,
            $"{cardId}: no legal event play ({option?.Reason ?? "no PlayEvent option"}; offered {string.Join(", ", option?.EligibleIds ?? [])})");
        var costs = play!.Costs.Where(c => c.AbilityId.Length == 0).ToList();
        var cost = costs.FirstOrDefault(c => state.Dice.Single(d => d.Id == c.DieId).Face != DieFace.Event) ?? costs[0];
        return Command(state, view, CommandKind.PlayEvent, new()
        {
            CardId = cardId, DieId = cost.DieId, CardCost = cost.Face,
            NationId = nation.Length > 0 ? nation : play.NationTargets.FirstOrDefault() ?? ""
        });
    }

    private static bool ChangesTheBoard(string cardId, int seed)
    {
        var state = Event(cardId);
        var side = CardCatalog.Kings[cardId].Side;
        var command = Play(state, side, cardId, Nation(cardId));
        var before = Board(state, cardId, command.Payload.DieId);
        state = Resolve(Run(state, side, command, seed), seed);
        return Board(state, cardId, command.Payload.DieId) != before;
    }

    // The demo CPU answers every decision until the effect is over and an action is due again.
    private static MatchState Resolve(MatchState state, int seed)
    {
        for (var step = 0; step < StepLimit && state.Winner is null && state.Pending is { } pending; step++)
        {
            if (EffectRuntime.Current(state) is null && CorruptionRuntime.Current(state) is null && pending.Kind == DecisionKind.Action)
                return state;
            var command = Bot.Next(state, pending.Owner);
            if (command is null)
                return state;
            state = Run(state, pending.Owner, command, seed + step);
        }
        return state;
    }

    // The Free Peoples draw a Strategy card with their Event die; the CPU discards down to the hand limit if needed.
    private static MatchState Draw(MatchState state)
    {
        state = Run(state, Side.FreePeoples, Command(state, Views.For(state, Side.FreePeoples), CommandKind.DrawCard,
            new() { DieId = FreeDie(state, Side.FreePeoples, DieFace.Event).Id, Deck = "strategy" }));
        state = Resolve(state, 1);
        Assert.Equal((DecisionKind.Action, Side.Shadow), (state.Pending!.Kind, state.Pending.Owner));
        return state;
    }

    // The Shadow draws until it may choose, then keeps the first tile offered.
    private static MatchState FinishAttempt(MatchState state)
    {
        for (var step = 0; step < 12 && state.Pending?.Kind == DecisionKind.CorruptionTile; step++)
        {
            var options = Views.For(state, Side.Shadow).Options;
            var choose = options.FirstOrDefault(o => o.Kind == CommandKind.ChooseCorruptionTile);
            state = Run(state, Side.Shadow, choose is null
                ? Command(state, Views.For(state, Side.Shadow), CommandKind.DrawCorruptionTile, new())
                : Command(state, Views.For(state, Side.Shadow), CommandKind.ChooseCorruptionTile, new() { Ids = [choose.EligibleIds[0]] }));
        }
        return state;
    }

    // A single legal choice resolves without a decision (EffectChoiceStep), so the step is answered only when it waits.
    private static MatchState ChooseIfOffered(MatchState state, Side side, string id) =>
        state.Pending is { Kind: DecisionKind.EffectChoice } pending && pending.Owner == side && pending.PrivateChoices.Contains(id)
            ? Run(state, side, Command(state, Views.For(state, side), CommandKind.ChooseEffect, new() { Ids = [id] }))
            : state;

    // The same board with the other side to act, as after a switch of side before the play.
    private static MatchState StateFor(MatchState state, Side side)
    {
        var copy = StateCodec.Copy(state);
        TurnFlow.SelectNext(copy, side);
        return copy;
    }

    private static GameCommand Command(MatchState state, PlayerView view, CommandKind kind, CommandPayload payload) => new()
    {
        MatchId = state.Id,
        CommandId = "kings-card-" + kind + "-" + state.Revision,
        ExpectedRevision = state.Revision,
        DecisionId = view.Decision!.Id,
        Kind = kind,
        Payload = payload
    };

    private static MatchState Run(MatchState state, Side side, GameCommand command, int seed = 7)
    {
        var result = Engine.Execute(state, side, command, new SeededRandom(seed));
        Assert.True(result.Receipt.Accepted, $"{command.Kind}: {result.Receipt.Error}");
        return result.State;
    }

    private static void Accepted(MatchState state, Side side, GameCommand command)
    {
        var error = Engine.Validate(state, side, command);
        Assert.True(error is null, $"{command.Kind} {command.Payload.CardId}: {error}");
        Assert.True(Run(state, side, command).Revision > state.Revision);
    }

    // What a card can change; the played card's way to the discard pile and the die that paid for it do not count.
    private static string Board(MatchState state, string cardId, string dieId) => JsonSerializer.Serialize(new
    {
        state.Units,
        state.Characters,
        state.Nations,
        state.Control,
        state.HuntTiles,
        state.Kings!.Sovereigns,
        Players = state.Players.Select(p => new
        {
            p.Side,
            Hand = p.Hand.Where(id => id != cardId).Order(StringComparer.Ordinal),
            p.Table
        }),
        Dice = state.Dice.Where(d => d.Id != dieId)
    });

    private static SovereignState Sovereign(MatchState state, string id) => state.Kings!.Sovereigns.Single(s => s.Id == id);

    private static SovereignStatus Status(MatchState state, string id) => Sovereign(state, id).Status;

    private static IEnumerable<SovereignState> Initial(MatchState state) => state.Kings!.Sovereigns.Where(s => s.Status == SovereignStatus.Initial);

    private static string Region(MatchState state, SovereignState sovereign) => SovereignLifecycle.Region(state, sovereign);

    private static IEnumerable<CharacterState> Outside(MatchState state) => state.Characters.Where(c =>
        c.Side == Side.FreePeoples && CharacterRules.IsCompanion(c) && CharacterRules.OnBoard(c) && !state.Fellowship.Companions.Contains(c.Id));

    private static string Location(MatchState state, string id) =>
        state.Characters.SingleOrDefault(c => c.Id == id)?.Location ?? state.Units.Single(u => u.Id == id).Location;

    private static bool Near(string region, string target) =>
        BoardRules.Regions.ContainsKey(region) && (region == target || BoardRules.Regions[region].Neighbors.Contains(target));

    private static bool Army(MatchState state, string region, Side side) =>
        state.Units.Any(u => u.Location == region && BoardRules.Owner(u) == side && BoardRules.IsArmyUnit(u));

    private static bool AtWar(MatchState state, string nation) => state.Nations.Single(n => n.Id == nation).StepsToWar == 0;

    private static int Free(MatchState state, Side side, DieFace? face = null) =>
        state.Dice.Count(d => d.Owner == side && d.Location == DieLocation.Available && (face is null || d.Face == face));

    private static DieState FreeDie(MatchState state, Side side, DieFace face) =>
        state.Dice.First(d => d.Owner == side && d.Location == DieLocation.Available && d.Face == face);

    private static bool PoolCounts(MatchState state) => state.HuntTiles.Where(t => t.Location == "pool")
        .All(t => !KingsTiles.IsNumeric(t.Id) || !KingsTiles.IsKings(t.Id) && KingsTiles.CorruptionValue(t.Id) > 0);

    private static int Companions(MatchState state, BattleState battle) => BattleRules.Characters(state, battle.Force(Side.FreePeoples))
        .Count(c => CharacterCatalog.All.GetValueOrDefault(c.DefinitionId)?.Role == "Companion");

    private static IEnumerable<CharacterState> Awakened(MatchState state, BattleState battle) => BattleRules.Characters(state,
        battle.Force(Side.FreePeoples)).Where(c => SovereignLifecycle.Find(state, c.Id)?.Status == SovereignStatus.Awakened);

    private static List<string> Units(MatchState state, string region) => state.Units.Where(u => u.Location == region).Select(u => u.Id).ToList();

    private static int ArmyUnits(MatchState state, BattleState battle, Side side) =>
        BattleRules.Units(state, battle.Force(side)).Count(BoardRules.IsArmyUnit);
}
