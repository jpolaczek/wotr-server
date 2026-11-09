using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.FateScenarioDriver;

namespace Wotr.Tests;

// TEST-14: each Fate card's scenarios let every clause act. The event scenarios (UI-266) are played to the end of the
// card, each clause's result on the board; the Combat scenarios offer the card at its owner's choice and its effect
// stands in the revealed round; Erebor held and Kings with Fate change which choices and cards the game offers.
public class FateCardEndToEndScenarioTests
{
    private const string Dale = "dale";
    private const string Erebor = "erebor";

    [Fact]
    public void ThereAndBackAgainSeparatesOneRegionFurtherThenAdvancesTheNorthTheDwarvesAndTheElves()
    {
        var state = Play(Create(Event(FateCards.ThereAndBack)), FateCards.ThereAndBack);
        Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);
        var merry = Option(state, CommandKind.SeparateCompanions).Travel.Single(t => t.Id == "base.character.merry");
        Assert.Equal(2, merry.Range);
        var route = TwoSteps(merry.Origin, merry.Targets);
        state = Do(state, CommandKind.SeparateCompanions, new() { Ids = [merry.Id], Route = route });
        Assert.Equal(route[^1], state.Characters.Single(c => c.Id == merry.Id).Location);
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        state = Do(state, CommandKind.SelectGuide, new() { GuideId = state.Fellowship.Guide });
        Assert.True(Nation(state, "north").Active);
        Assert.All(new[] { "dwarves", "elves", "north" }, id => Assert.Equal(2, Nation(state, id).StepsToWar));
    }

    [Fact]
    public void BookOfMazarbulBringsTheDwarvesToWarAndRecruitsWhereACompanionStands()
    {
        var state = Play(Create(Event(FateCards.Book)), FateCards.Book);
        state = Do(state, CommandKind.MoveCompanions);
        Assert.Equal(0, Nation(state, "dwarves").StepsToWar);
        Assert.Equal(["ered-luin", "iron-hills"], Option(state, CommandKind.ChooseEffect).EligibleIds.Order());
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["iron-hills"] });
        var unit = Option(state, CommandKind.ChooseEffect).EligibleIds[0];
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [unit] });
        Assert.Equal("iron-hills", state.Units.Single(u => u.Id == unit).Location);
    }

    // EXP-15 D8: with Legolas moved out of Ered Luin only the Iron Hills remain, so the next choice is the unit.
    [Fact]
    public void BookOfMazarbulTakesTheOnlyCompanionRegionWithoutAClick()
    {
        var state = Play(Create(Event(FateCards.Book)), FateCards.Book);
        state = Do(state, CommandKind.MoveCompanions, new()
        {
            CompanionMoves = [new() { Ids = ["base.character.legolas"], Route = ["ered-luin", "grey-havens"] }]
        });
        var units = Option(state, CommandKind.ChooseEffect).EligibleIds;
        Assert.All(units, id => Assert.Contains(state.Units, u => u.Id == id && u.Nation == "dwarves" && u.Location == "reserve"));
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [units[0]] });
        Assert.Equal("iron-hills", state.Units.Single(u => u.Id == units[0]).Location);
    }

    [Theory]
    [InlineData(FateCards.Bardings, "north")]
    [InlineData(FateCards.Dwarves, "dwarves")]
    public void TheDaleCardsRecruitInDaleItselfAndDrawAStrategyCard(string cardId, string nation)
    {
        var state = Create(Event(cardId));
        var deck = state.Player(Side.FreePeoples).Decks["strategy"].Count;
        state = Play(state, cardId);
        var option = Option(state, CommandKind.ChooseEffect);
        Assert.Contains(Dale, option.Targets);
        var kind = option.Recruitment.First(g => g.Nation == nation && g.Kind != UnitKind.Leader).Kind;
        var plan = Plan(option, (nation, kind, Dale, 1), (nation, UnitKind.Leader, Dale, 1));
        state = Do(state, CommandKind.ChooseEffect, plan);
        Assert.All(plan.Placements, p => Assert.Equal(Dale, state.Units.Single(u => u.Id == p.UnitId).Location));
        Assert.Equal(deck - 1, state.Player(Side.FreePeoples).Decks["strategy"].Count);
        Assert.Null(state.Control[Dale]);
    }

    [Fact]
    public void OrcsMultiplyingAgainSplitsTheFourRegularsBetweenDolGuldurAndTheChosenRegion()
    {
        var state = Play(Create(Event(FateCards.Orcs)), FateCards.Orcs);
        Assert.Equal([Erebor, "mount-gundabad"], Option(state, CommandKind.ChooseEffect).EligibleIds.Order());
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [Erebor] });
        var option = Option(state, CommandKind.ChooseEffect);
        Assert.Equal(4, option.Recruitment.Single(g => g.Kind == UnitKind.Regular).ReserveIds.Count);
        var plan = Plan(option, ("sauron", UnitKind.Regular, "dol-guldur", 2), ("sauron", UnitKind.Regular, Erebor, 2));
        state = Do(state, CommandKind.ChooseEffect, plan);
        Assert.All(plan.Placements, p => Assert.Equal(p.RegionId, state.Units.Single(u => u.Id == p.UnitId).Location));
        Assert.Equal(4, state.Units.Count(u => u.Location == Erebor && u.Nation == "sauron"));
    }

    [Fact]
    public void MonstersRousedChoosesWestThenPlacesTheLastRegularAndTheEliteInTrollshaws()
    {
        var state = Play(Create(Event(FateCards.Monsters)), FateCards.Monsters);
        Assert.Equal([FateCards.MonstersEast, FateCards.MonstersWest], Option(state, CommandKind.ChooseEffect).EligibleIds.Order());
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [FateCards.MonstersWest] });
        var option = Option(state, CommandKind.ChooseEffect);
        var plan = Plan(option, ("sauron", UnitKind.Regular, "angmar", 1), ("sauron", UnitKind.Elite, "trollshaws", 1));
        state = Do(state, CommandKind.ChooseEffect, plan);
        Assert.All(plan.Placements, p => Assert.Equal(p.RegionId, state.Units.Single(u => u.Id == p.UnitId).Location));
    }

    [Theory]
    [InlineData(FateCards.ThereAndBack)]
    [InlineData(FateCards.Book)]
    [InlineData(FateCards.Bardings)]
    [InlineData(FateCards.Dwarves)]
    [InlineData(FateCards.Orcs)]
    [InlineData(FateCards.Monsters)]
    public void EveryFateCardHasACombatScenarioAtItsOwnersChoice(string cardId)
    {
        var card = CardCatalog.All[cardId];
        var scenario = TestScenarioCatalog.Find(FateCardCombatScenarios.Id(cardId))!;
        Assert.Equal((FateCardScenarios.Group, card.Side), (scenario.Group, scenario.StartingSide));
        Assert.StartsWith(card.Title + " · Combat: ", scenario.Title);
        var state = Create(scenario.Id);
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        Assert.Contains(cardId, Option(state, CommandKind.CommitCombatCard).EligibleIds);
        Assert.True(Reveal(state, cardId).Revision > state.Revision);
    }

    [Fact]
    public void BraveStandLeavesTheShadowOneDieAgainstFourCompanions()
    {
        var battle = BattleRuntime.Active(Reveal(Create(FateCardCombatScenarios.Id(FateCards.ThereAndBack)), FateCards.ThereAndBack))!;
        Assert.Single(battle.Force(Side.Shadow).Round.Dice);
    }

    [Fact]
    public void AdvantageousPositionPenalizesEveryShadowDieAtTheFordsOfRohan()
    {
        var state = Reveal(Create(FateCardCombatScenarios.Id(FateCards.Book)), FateCards.Book);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal("rohan", BoardRules.Region(state, battle.Region).Nation);
        Assert.Equal(-1, battle.Force(Side.Shadow).Round.CombatModifier);
    }

    [Fact]
    public void ValourAddsOneToEveryFreePeoplesDieWithTheRohanElite()
    {
        var battle = BattleRuntime.Active(Reveal(Create(FateCardCombatScenarios.Id(FateCards.Dwarves)), FateCards.Dwarves))!;
        Assert.Equal(1, battle.Force(Side.FreePeoples).Round.CombatModifier);
    }

    [Fact]
    public void OnslaughtOffersTheShadowVoluntaryHitsAfterTheCasualties()
    {
        var state = NoRerolls(Reveal(Create(FateCardCombatScenarios.Id(FateCards.Orcs)), FateCards.Orcs));
        Assert.Equal((DecisionKind.Losses, Side.Shadow), (state.Pending!.Kind, state.Pending.Owner));
        Assert.True(BattleLosses.Current(state)!.Voluntary);
        state = Do(state, CommandKind.TakeBattleLosses);
        Assert.NotEqual(DecisionKind.Losses, state.Pending?.Kind);
    }

    [Fact]
    public void DesperateBattleRaisesBothSidesCombatAndReroll()
    {
        var battle = BattleRuntime.Active(Reveal(Create(FateCardCombatScenarios.Id(FateCards.Monsters)), FateCards.Monsters))!;
        Assert.All(new[] { battle.Attacker, battle.Defender }, force =>
        {
            Assert.Equal(1, force.Round.CombatModifier);
            Assert.Equal(1, force.Round.RerollModifier);
        });
    }

    [Fact]
    public void ShieldWallCancelsOneOfTheShadowsHitsBeforeTheCasualties()
    {
        var state = Create("fate-card-bardings-shield-wall");
        Assert.Equal((DecisionKind.Losses, Side.FreePeoples), (state.Pending!.Kind, state.Pending.Owner));
        var battle = BattleRuntime.Active(state)!;
        var shadowHits = BattleRoll.HitCount(battle.Force(Side.Shadow));
        Assert.True(shadowHits >= 2);
        Assert.Equal(1, battle.Force(Side.FreePeoples).Round.CancelHits);
        Assert.Equal(shadowHits - 1, BattleLosses.Current(state)!.Hits);
        var losses = BattleRules.Units(state, battle.Force(Side.FreePeoples)).Where(BoardRules.IsArmyUnit)
            .Select(u => new BattleLossChoice { UnitId = u.Id, Kind = BattleLossKind.Remove }).ToList();
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = losses });
        Assert.All(losses, loss => Assert.NotEqual("fords-of-isen", state.Units.Single(u => u.Id == loss.UnitId).Location));
    }

    [Fact]
    public void OrcsMultiplyingAgainTakesMountGundabadWithoutAClickWhileTheFreePeoplesHoldErebor()
    {
        var state = Create("fate-card-orcs-erebor-held");
        Assert.Equal(Side.FreePeoples, state.Control[Erebor]);
        var before = Regulars(state);
        state = Play(state, FateCards.Orcs);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        var after = Regulars(state);
        Assert.Equal((3, 3, 0), (after["dol-guldur"] - before["dol-guldur"], after["mount-gundabad"] - before["mount-gundabad"],
            after.GetValueOrDefault(Erebor)));
    }

    [Fact]
    public void KingsWithFateHasNoBrandOrDainAndPlaysTheFateDaleCards()
    {
        var state = Create("fate-kings-dale-cards");
        Assert.True(state.Configuration.Kings);
        Assert.Equal(3, state.Kings!.Sovereigns.Count);
        Assert.DoesNotContain(state.Kings.Sovereigns, s => s.Id is SovereignCatalog.Brand or SovereignCatalog.Dain);
        var free = state.Player(Side.FreePeoples);
        var cards = free.Decks.Values.SelectMany(deck => deck).Concat(free.Hand).Concat(free.Discard).ToList();
        Assert.DoesNotContain("kings.fp.strategy.19", cards);
        Assert.DoesNotContain("kings.fp.strategy.22", cards);
        var events = Option(state, CommandKind.PlayEvent).Events.Select(e => e.CardId).ToList();
        Assert.Contains(FateCards.Bardings, events);
        Assert.Contains(FateCards.Dwarves, events);
        state = Play(state, FateCards.Dwarves);
        var option = Option(state, CommandKind.ChooseEffect);
        Assert.Contains(Dale, option.Targets);
        var plan = Plan(option, ("dwarves", UnitKind.Elite, Dale, 1), ("dwarves", UnitKind.Leader, Dale, 1));
        state = Do(state, CommandKind.ChooseEffect, plan);
        Assert.All(plan.Placements, p => Assert.Equal(Dale, state.Units.Single(u => u.Id == p.UnitId).Location));
    }

    private static string Event(string cardId) => FateCardScenarios.Id(CardCatalog.All[cardId]);

    // A route to a reachable region two steps from the origin, beyond a level 1 Companion's own range.
    private static List<string> TwoSteps(string origin, IReadOnlyCollection<string> targets)
    {
        var near = BoardRules.Regions[origin].Neighbors;
        return near.Where(targets.Contains).SelectMany(step => BoardRules.Regions[step].Neighbors
                .Where(far => far != origin && !near.Contains(far) && targets.Contains(far))
                .Select(far => new List<string> { origin, step, far }))
            .First();
    }

    // Sauron Regulars on the map by region.
    private static Dictionary<string, int> Regulars(MatchState state) => state.Units
        .Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && BoardRules.Regions.ContainsKey(u.Location))
        .GroupBy(u => u.Location).ToDictionary(g => g.Key, g => g.Count());

    // The scenario's side commits the card and the other side none; every die then rolls a 1.
    private static MatchState Reveal(MatchState state, string cardId)
    {
        var owner = state.Pending!.Owner;
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = cardId }, new LowestFace());
        while (state.Pending?.Kind == DecisionKind.CombatCard && state.Pending.Owner != owner)
            state = Do(state, CommandKind.CommitCombatCard, new() { CardId = "" }, new LowestFace());
        return state;
    }
}
