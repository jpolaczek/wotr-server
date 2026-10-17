using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards02;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Strategy01Fixture;
using static Wotr.Tests.Strategy02Fixture;

namespace Wotr.Tests;

public class ArmyEventMovementTests
{
    [Fact]
    public void MarchCapturesAlongRouteAndPaysOnlyOriginalDie()
    {
        var state = Play(March(), ThroughDayNight);
        Assert.Equal(DecisionKind.ArmyEvent, state.Pending!.Kind);
        var command = BattleFixture.Command(state, CommandKind.MoveArmies, Move(state, "north-ithilien", "minas-morgul", "gorgoroth"));
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples, command);
        Assert.True(preview.Legal);
        Assert.Empty(preview.Costs);
        Assert.Equal(Side.FreePeoples, preview.ControlChanges["minas-morgul"]);
        Assert.All(preview.Placements, p => Assert.Equal("gorgoroth", p.RegionId));
        state = Do(state, command.Kind, command.Payload);
        Assert.Equal("gorgoroth", state.Characters.Single().Location);
        Assert.Equal(Side.FreePeoples, state.Control["minas-morgul"]);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData("no-companion")]
    [InlineData("pickup")]
    [InlineData("two-armies")]
    [InlineData("duplicate")]
    [InlineData("skip")]
    [InlineData("extra-step")]
    [InlineData("extra-die")]
    [InlineData("foreign")]
    public void MarchRejectsForgedGroupsCostsAndRoutesAtomically(string failure)
    {
        var state = March();
        var waiting = BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-morgul");
        var enemy = BattleFixture.Put(state, "sauron", UnitKind.Regular, "morannon");
        state = Play(state, ThroughDayNight);
        var payload = Move(state, "north-ithilien", "minas-morgul", "gorgoroth");
        switch (failure)
        {
            case "no-companion":
                payload.Moves[0].CharacterIds.Clear();
                break;
            case "pickup":
                payload.Moves[0].UnitIds.Add(waiting.Id);
                break;
            case "two-armies":
                payload.Moves.Add(new());
                break;
            case "duplicate":
                payload.Moves[0].UnitIds.Add(payload.Moves[0].UnitIds[0]);
                break;
            case "skip":
                payload.Route.RemoveAt(0);
                break;
            case "extra-step":
                payload.Route.Add("barad-dur");
                payload.Moves[0].To = "barad-dur";
                break;
            case "extra-die":
                payload.DieId = state.Dice.First().Id;
                break;
            case "foreign":
                payload.Moves[0].UnitIds.Add(enemy.Id);
                break;
        }
        Reject(state, CommandKind.MoveArmies, payload);
    }

    [Fact]
    public void MarchDoesNotPickUpFriendlyArmyAndChecksStackingOnlyAtEnd()
    {
        var state = March();
        for (var i = 0; i < 10; i++)
            BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-morgul");
        var waiting = state.Units.Where(u => u.Location == "minas-morgul").Select(u => u.Id).ToList();
        state = Play(state, ThroughDayNight);
        state = Do(state, CommandKind.MoveArmies, Move(state, "north-ithilien", "minas-morgul", "gorgoroth"));
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        Assert.All(waiting, id => Assert.Equal("minas-morgul", state.Units.Single(u => u.Id == id).Location));
        state = March();
        for (var i = 0; i < 10; i++)
            BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-morgul");
        state = Play(state, ThroughDayNight);
        state = Do(state, CommandKind.MoveArmies, Move(state, "north-ithilien", "minas-morgul"));
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        var old = state.Units.First(u => u.Location == "minas-morgul" && BoardRules.IsArmyUnit(u)).Id;
        state = Do(state, CommandKind.RemoveExcess, new() { Ids = [old] });
        Assert.Equal("reserve", state.Units.Single(u => u.Id == old).Location);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void MarchHonorsEnemyArmiesBordersAndBesiegedStrongholds()
    {
        var state = Play(March(), ThroughDayNight);
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "minas-morgul");
        Reject(state, CommandKind.MoveArmies, Move(state, "north-ithilien", "minas-morgul", "gorgoroth"));
        var nation = state.Nations.Single(n => n.Id == "gondor");
        nation.StepsToWar = 1;
        BoardRules.ReturnToReserve(state.Units.Single(u => u.Location == "minas-morgul"));
        Reject(state, CommandKind.MoveArmies, Move(state, "north-ithilien", "minas-morgul"));
        state = Play(March("osgiliath"), ThroughDayNight);
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-tirith").Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "minas-tirith");
        Reject(state, CommandKind.MoveArmies, Move(state, "osgiliath", "minas-tirith"));
        Assert.False(new ArmyOrderCondition(ArmyEventOrder.CompanionMarch).CanPlay(Passage(), Side.FreePeoples));
    }

    [Fact]
    public void MarchAllowsInitialSplitButCompanionMustMove()
    {
        var state = March();
        var rear = BattleFixture.Put(state, "gondor", UnitKind.Regular, "north-ithilien");
        var companion = Put(state, "base.character.legolas", "north-ithilien");
        state = Play(state, ThroughDayNight);
        var payload = Move(state, "north-ithilien", "minas-morgul");
        payload.Moves[0].UnitIds.Remove(rear.Id);
        payload.Moves[0].CharacterIds.Remove(companion.Id);
        state = Do(state, CommandKind.MoveArmies, payload);
        Assert.Equal("north-ithilien", state.Units.Single(u => u.Id == rear.Id).Location);
        Assert.Equal("north-ithilien", state.Characters.Single(c => c.Id == companion.Id).Location);
        Assert.Equal("minas-morgul", state.Characters.Single(c => c.Id == "base.character.gimli").Location);
    }
}
