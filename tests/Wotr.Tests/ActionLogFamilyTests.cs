using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.ActionLogFixture;

namespace Wotr.Tests;

public class ActionLogFamilyTests
{
    [Fact]
    public void EveryCommandHasAnExplicitClassificationAndDescription()
    {
        foreach (var command in Enum.GetValues<CommandKind>())
        {
            Assert.True(Enum.IsDefined(ActionCommandCatalog.Context(command)));
            Assert.NotEmpty(ActionCommandText.Describe(command));
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => ActionCommandCatalog.Context((CommandKind)int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => ActionCommandText.Describe((CommandKind)int.MaxValue));
    }

    [Theory]
    [InlineData(UnitKind.Regular, "gondor", "minas-tirith", "minas-tirith")]
    [InlineData(UnitKind.Leader, "gondor", "minas-tirith", "casualties")]
    [InlineData(UnitKind.Elite, "sauron", "barad-dur", "barad-dur")]
    public void RecruitmentKeepsPlacedFigureEvenIfAnUnaccompaniedLeaderImmediatelyDies(UnitKind kind, string nation, string region, string outcome)
    {
        var side = nation == "sauron" ? Side.Shadow : Side.FreePeoples;
        var state = ActionSequenceFixture.Board(side);
        var die = state.Dice.First(d => d.Owner == side);
        die.Face = DieFace.WillOfTheWest;
        var unit = state.Units.First(u => u.Nation == nation && u.Kind == kind);
        state = LordsFixture.Do(state, CommandKind.Recruit, new()
        {
            DieId = die.Id, Stepwise = true, Placements = [new() { UnitId = unit.Id, RegionId = region }]
        });
        var step = Steps(state, side).Single(s => s.Command == CommandKind.Recruit);
        Assert.Equal(DieFace.WillOfTheWest, Assert.Single(step.Costs).Face);
        Assert.Equal(DieFace.Muster, step.Costs[0].Use);
        Assert.Contains(step.Facts.Figures, f => f.To == region && f.After?.UnitKind == kind && f.After.NationId == nation);
        Assert.Equal(outcome, state.Units.Single(u => u.Id == unit.Id).Location);
        if (outcome == "casualties")
            Assert.Contains(step.Facts.Figures, f => f.To == "casualties" && f.Before?.UnitKind == UnitKind.Leader);
        if (kind != UnitKind.Elite)
            Assert.Equal(1, step.Facts.Budget!.Remaining);
    }

    [Theory]
    [InlineData(false, 2, 1)]
    [InlineData(true, 1, 0)]
    public void PoliticalHistorySeparatesActivationFromDistance(bool active, int before, int after)
    {
        var state = TurnTests.Actions();
        var nation = state.Nations.Single(n => n.Id == "gondor");
        nation.Active = active;
        nation.StepsToWar = before;
        state.Dice[0].Face = DieFace.ArmyMuster;
        state = LordsFixture.Do(state, CommandKind.AdvancePolitics, new() { DieId = state.Dice[0].Id, NationId = nation.Id });
        var step = Steps(state).Single(s => s.Command == CommandKind.AdvancePolitics);
        var fact = Assert.Single(step.Facts.Nations);
        Assert.Equal(active, fact.WasActive);
        Assert.Equal(active, fact.Active);
        Assert.Equal(before, fact.PreviousStepsToWar);
        Assert.Equal(after, fact.StepsToWar);
        Assert.Equal(nation.Id, step.Context.NationId);
        Assert.Equal(DieFace.Muster, Assert.Single(step.Costs).Use);
    }

    [Fact]
    public void RingConversionAndSubsequentActionHaveSeparateRealCosts()
    {
        var state = TurnTests.Actions();
        var die = state.Dice.First(d => d.Owner == Side.FreePeoples);
        var ring = state.Rings.First(r => r.Owner == Side.FreePeoples);
        die.Face = DieFace.Army;
        state = LordsFixture.Do(state, CommandKind.UseRing, new() { DieId = die.Id, RingId = ring.Id, Face = DieFace.Event });
        var conversion = Steps(state).Single(s => s.Command == CommandKind.UseRing);
        Assert.Equal(ActionCostKind.Ring, Assert.Single(conversion.Costs).Kind);
        Assert.Equal(DieFace.Army, Assert.Single(conversion.Facts.Dice).Before);
        Assert.Equal(DieFace.Event, conversion.Facts.Dice[0].After);
        Assert.Equal(DieLocation.Available, conversion.Facts.Dice[0].To);
        state = LordsFixture.Do(state, CommandKind.DrawCard, new() { DieId = die.Id, Deck = "strategy" });
        var draw = Steps(state).Single(s => s.Command == CommandKind.DrawCard);
        Assert.NotEqual(conversion.ActionId, draw.ActionId);
        Assert.Equal(ActionCostKind.Die, Assert.Single(draw.Costs).Kind);
        Assert.Equal(DieFace.Event, draw.Costs[0].Face);
    }

    [Fact]
    public void DrawAndExcessDiscardPublishCountsWhileOnlyTheirOwnerReceivesIdentities()
    {
        var state = TurnTests.Actions();
        var player = state.Player(Side.FreePeoples);
        while (player.Hand.Count < 6)
            CardFlow.Draw(state, Side.FreePeoples, "strategy");
        var card = player.Decks["strategy"][0];
        var die = state.Dice.First(d => d.Owner == Side.FreePeoples);
        die.Face = DieFace.Event;
        state = LordsFixture.Do(state, CommandKind.DrawCard, new() { DieId = die.Id, Deck = "strategy" });
        var id = state.Resolution.ActionLog!.Action.Id;
        Assert.Contains(Action(state, id).Steps.SelectMany(s => s.Facts.Cards), c => c.Id == card && c.Use == ActionCardUse.Draw);
        Assert.DoesNotContain(card, Json(Action(state, id, Side.Shadow)));
        state = LordsFixture.Do(StateCodec.Copy(state), CommandKind.DiscardCards, new() { Ids = [card] });
        Assert.Equal(ActionLogStatus.Completed, Action(state, id).Action.Status);
        Assert.Contains(Action(state, id).Steps.SelectMany(s => s.Facts.Cards), c => c.Id == card && c.Use == ActionCardUse.Discard);
        Assert.DoesNotContain(card, Json(Action(state, id, Side.Shadow)));
        Assert.Contains(Action(state, id, Side.Shadow).Steps.SelectMany(s => s.Facts.Cards), c => c.Use == ActionCardUse.Discard && c.Count == 1 && c.Id == "");
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(4)]
    [InlineData(3)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public void SetupAcrossAllModuleCombinationsRecordsPublicDiceWithoutPrivateDecks(int modules)
    {
        var config = new MatchConfiguration { Lords = (modules & 1) != 0, Kings = (modules & 2) != 0, Fate = (modules & 4) != 0 };
        var state = ExpansionFixture.CompleteSetup(ExpansionFixture.Start(config));
        foreach (var side in Enum.GetValues<Side>())
        {
            var steps = Steps(state, side);
            Assert.All(steps, s => Assert.False(s.Legacy));
            Assert.Contains(steps.SelectMany(s => s.Facts.Dice), d => d.After.HasValue && d.From == DieLocation.Unrolled);
            var raw = Json(steps);
            foreach (var card in state.Player(TurnFlow.Other(side)).Hand)
                Assert.DoesNotContain(card, raw);
            Assert.DoesNotContain("randomSamples", raw);
            Assert.DoesNotContain("privateCompanionVersions", raw);
        }
    }

    internal static List<ActionLogStep> Steps(MatchState state, Side side = Side.FreePeoples) =>
        Records(state, side).GroupBy(r => r.ActionId)
            .SelectMany(g => ActionLogProjection.Action(state.Id, g, side, ActionLogFilter.All)!.Steps).ToList();

    internal static string Json<T>(T value) => JsonSerializer.Serialize(value, BoardCatalog.Json);
}
