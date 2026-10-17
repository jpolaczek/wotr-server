using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.FateCardsFixture;

namespace Wotr.Tests;

public class FateFreeRecruitmentTests
{
    [Theory]
    [InlineData(FateCards.Bardings, "north", UnitKind.Regular)]
    [InlineData(FateCards.Bardings, "north", UnitKind.Elite)]
    [InlineData(FateCards.Dwarves, "dwarves", UnitKind.Regular)]
    [InlineData(FateCards.Dwarves, "dwarves", UnitKind.Elite)]
    public void RecruitmentBeforeWarPlacesUnitAndLeaderTogetherThenPrivatelyDrawsStrategy(string card, string nation, UnitKind kind)
    {
        var state = State(card);
        var cost = Cost(state, card);
        var drawn = state.Player(Side.FreePeoples).Decks["strategy"][0];
        var steps = state.Nations.Single(n => n.Id == nation).StepsToWar;
        Assert.True(steps > 0);
        state = KingsFixture.Do(state, CommandKind.PlayEvent, cost);
        var option = Option(state);
        Assert.Equal(1, option.EventRecruitment!.MaximumRegions);
        Assert.DoesNotContain("dale", option.Targets);
        Assert.DoesNotContain("erebor", option.Targets);
        var plan = Plan(state, (nation, kind, "iron-hills", 1), (nation, UnitKind.Leader, "iron-hills", 1));
        var wrong = new CommandPayload { Placements = plan.Placements.Select(p => new UnitPlacementChoice { UnitId = p.UnitId, RegionId = p.RegionId }).ToList() };
        wrong.Placements[1].RegionId = "woodland-realm";
        LordsFixture.Reject(state, CommandKind.ChooseEffect, wrong);
        Assert.DoesNotContain(drawn, state.Player(Side.FreePeoples).Hand);
        state = KingsFixture.Do(state, CommandKind.ChooseEffect, plan);
        Assert.All(plan.Placements, p => Assert.Equal(p.RegionId, state.Units.Single(u => u.Id == p.UnitId).Location));
        Assert.Equal(new[] { drawn }, state.Player(Side.FreePeoples).Hand);
        Assert.DoesNotContain(drawn, KingsFixture.Json(state, Side.Shadow));
        Assert.Equal(steps, state.Nations.Single(n => n.Id == nation).StepsToWar);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == cost.DieId).Location);
    }

    [Theory]
    [InlineData(FateCards.Bardings, "north", UnitKind.Leader, false)]
    [InlineData(FateCards.Bardings, "north", UnitKind.Regular, true)]
    [InlineData(FateCards.Bardings, "north", UnitKind.Elite, true)]
    [InlineData(FateCards.Dwarves, "dwarves", UnitKind.Leader, false)]
    [InlineData(FateCards.Dwarves, "dwarves", UnitKind.Regular, true)]
    public void DaleRequiresAnActualFreePeoplesArmyRatherThanALeaderOrCompanion(string card, string nation, UnitKind present, bool legal)
    {
        var state = State(card);
        CharacterFixture.Put(state, "base.character.legolas", "dale");
        BattleFixture.Put(state, "north", present, "dale");
        state = Play(state, card);
        Assert.Equal(legal, Option(state).Targets.Contains("dale"));
        var plan = Plan(state, (nation, UnitKind.Elite, "dale", 1), (nation, UnitKind.Leader, "dale", 1));
        if (legal)
        {
            state = KingsFixture.Do(state, CommandKind.ChooseEffect, plan);
            Assert.All(plan.Placements, p => Assert.Equal("dale", state.Units.Single(u => u.Id == p.UnitId).Location));
            Assert.Null(state.Control["dale"]);
        }
        else
            LordsFixture.Reject(state, CommandKind.ChooseEffect, plan);
    }

    [Theory]
    [InlineData(FateCards.Bardings, "north", UnitKind.Leader)]
    [InlineData(FateCards.Bardings, "north", UnitKind.Regular)]
    [InlineData(FateCards.Dwarves, "dwarves", UnitKind.Leader)]
    [InlineData(FateCards.Dwarves, "dwarves", UnitKind.Elite)]
    public void APartiallyEmptyReserveDoesNotSkipTheRemainingRecruitOrTheDraw(string card, string nation, UnitKind available)
    {
        var state = State(card);
        foreach (var kind in new[] { UnitKind.Regular, UnitKind.Elite, UnitKind.Leader })
            Reserve(state, nation, kind, kind == available ? 1 : 0);
        state = Play(state, card);
        var plan = Plan(state, (nation, available, "iron-hills", 1));
        Assert.Equal(1, Option(state).Minimum);
        state = KingsFixture.Do(state, CommandKind.ChooseEffect, plan);
        Assert.Equal("iron-hills", state.Units.Single(u => u.Id == plan.Placements[0].UnitId).Location);
        Assert.Single(state.Player(Side.FreePeoples).Hand);
    }

    [Theory]
    [InlineData(FateCards.Bardings, "north")]
    [InlineData(FateCards.Dwarves, "dwarves")]
    public void NoReserveStillDrawsAndAnEmptyDeckNeverReshuffles(string card, string nation)
    {
        foreach (var emptyDeck in new[] { false, true })
        {
            var state = State(card);
            foreach (var kind in new[] { UnitKind.Regular, UnitKind.Elite, UnitKind.Leader })
                Reserve(state, nation, kind, 0);
            if (emptyDeck)
                state.Player(Side.FreePeoples).Decks["strategy"].Clear();
            state = Play(state, card);
            Assert.Equal(emptyDeck ? 0 : 1, state.Player(Side.FreePeoples).Hand.Count);
            Assert.Empty(state.Resolution.Frames);
            Assert.Contains(card, state.Player(Side.FreePeoples).Discard);
        }
    }

    [Theory]
    [InlineData(FateCards.Bardings, "north")]
    [InlineData(FateCards.Dwarves, "dwarves")]
    public void AdjacentFriendlyGarrisonIsLegalAndThePaidDieWaitsForFinalStackExcess(string card, string nation)
    {
        var state = State(card);
        state.Control["erebor"] = Side.FreePeoples;
        FateFixture.Clear(state, "erebor");
        for (var i = 0; i < 5; i++)
            BattleFixture.Put(state, "gondor", UnitKind.Regular, "erebor").Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "erebor");
        var cost = Cost(state, card);
        state = KingsFixture.Do(state, CommandKind.PlayEvent, cost);
        var plan = Plan(state, (nation, UnitKind.Elite, "erebor", 1), (nation, UnitKind.Leader, "erebor", 1));
        state = KingsFixture.Do(state, CommandKind.ChooseEffect, plan);
        Assert.All(plan.Placements, p => Assert.True(state.Units.Single(u => u.Id == p.UnitId).Garrison));
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.Single(state.Player(Side.FreePeoples).Hand);
        Assert.Equal(DieLocation.Resolving, state.Dice.Single(d => d.Id == cost.DieId).Location);
    }
}
