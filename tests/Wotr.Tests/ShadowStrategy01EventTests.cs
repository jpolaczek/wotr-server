using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards01;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy01Fixture;

namespace Wotr.Tests;

public class ShadowStrategy01EventTests
{
    [Fact]
    public void EveryEventValidatesOwnershipWindowPrintedCostAndSingleUse()
    {
        foreach (var card in Cards)
        {
            var state = State(card);
            var cost = EventCost(state, card);
            var command = BattleFixture.Command(state, CommandKind.PlayEvent, cost);
            Assert.Null(Engine().Validate(state, Side.Shadow, command));
            Assert.NotNull(Engine().Validate(state, Side.FreePeoples, command));
            var die = state.Dice.Single(d => d.Id == cost.DieId);
            die.Face = DieFace.Character;
            Reject(state, CommandKind.PlayEvent, cost);
            die.Face = cost.CardCost;
            die.Location = DieLocation.Spent;
            Reject(state, CommandKind.PlayEvent, cost);
            die.Location = DieLocation.Available;
            HandRules.Remove(state.Player(Side.Shadow), card);
            Reject(state, CommandKind.PlayEvent, cost);
            HandRules.Add(state.Player(Side.Shadow), card);
            state.Pending!.Kind = DecisionKind.Reaction;
            Reject(state, CommandKind.PlayEvent, cost);
        }
    }

    [Fact]
    public void DawnDiscardsOnlyUnusedWillAndRecoveryRestoresThem()
    {
        var state = State(Dawn);
        var dice = state.Dice.Where(d => d.Owner == Side.FreePeoples).ToArray();
        dice[0].Face = dice[1].Face = DieFace.WillOfTheWest;
        dice[2].Face = DieFace.Character;
        var spent = dice[0].Id;
        state.Nations.Single(n => n.Id == "isengard").StepsToWar = 1;
        Reject(state, CommandKind.PlayEvent, EventCost(state, Dawn));
        state.Nations.Single(n => n.Id == "isengard").StepsToWar = 0;
        state = Play(state, Dawn);
        Assert.All(state.Dice.Where(d => d.Owner == Side.FreePeoples && d.Face == DieFace.WillOfTheWest), d => Assert.Equal(DieLocation.Spent, d.Location));
        Assert.Equal(DieLocation.Available, state.Dice.Single(d => d.Id == dice[2].Id).Location);
        DiceRules.Recover(state);
        Assert.Equal(DieLocation.Unrolled, state.Dice.Single(d => d.Id == spent).Location);
    }

    [Fact]
    public void ThreatsNeedsActualAttackOrCompanionAdvanceAndRespectsWormtongue()
    {
        var state = State(Threats);
        var rohan = state.Nations.Single(n => n.Id == "rohan");
        rohan.Active = false;
        rohan.StepsToWar = 1;
        Shadow04Fixture.Table(state, ShadowCharacterCards04.Wormtongue);
        state = Play(state, Threats);
        rohan = state.Nations.Single(n => n.Id == "rohan");
        Assert.False(PoliticalRules.CanMusterAdvance(state, rohan));
        var unit = BattleFixture.Put(state, "rohan", UnitKind.Regular, "westemnet");
        PoliticalRules.ArmyAttacked(state, [unit], PoliticalAttack.ArmyBattle);
        PoliticalReactions.Complete(state);
        Assert.Contains(Threats, state.Player(Side.Shadow).Table);
        Assert.False(rohan.Active);
        unit.Location = "edoras";
        PoliticalRules.ArmyAttacked(state, [unit], PoliticalAttack.ArmyBattle);
        PoliticalReactions.Complete(state);
        Assert.Empty(state.Player(Side.Shadow).Table);
        Assert.Equal(0, rohan.StepsToWar);
        state = Play(State(Threats), Threats);
        var north = state.Nations.Single(n => n.Id == "north");
        north.StepsToWar = 3;
        north.Active = false;
        PoliticalRules.Activate(state, "north");
        PoliticalRules.Advance(state, "north", PoliticalAdvanceCause.Event);
        Assert.Contains(Threats, state.Player(Side.Shadow).Table);
        Assert.True(PoliticalRules.CanMusterAdvance(state, north));
        PoliticalRules.Advance(state, "north", PoliticalAdvanceCause.CompanionAbility);
        Assert.DoesNotContain(Threats, state.Player(Side.Shadow).Table);
    }

    [Fact]
    public void StormcrowTargetsNationThenLetsOpponentEliminateWholeFigureAnywhere()
    {
        var state = State(Stormcrow);
        var elite = BattleFixture.Put(state, "gondor", UnitKind.Elite, "dol-amroth");
        var leader = BattleFixture.Put(state, "gondor", UnitKind.Leader, "dol-amroth");
        Put(state, CharacterRules.Grey, "dol-amroth");
        Put(state, "base.character.legolas", "woodland-realm");
        state.Nations.Single(n => n.Id == "elves").StepsToWar = 1;
        var condition = EffectCatalog.Base.Events[Stormcrow].Condition;
        Assert.Equal(new[] { "elves", "gondor" }, condition.NationTargets(state, Side.Shadow).Order());
        var cost = EventCost(state, Stormcrow);
        cost.NationId = "north";
        Reject(state, CommandKind.PlayEvent, cost);
        state = Play(state, Stormcrow);
        Assert.Equal(3, state.Nations.Single(n => n.Id == "gondor").StepsToWar);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        var options = Shadow01Fixture.Choices(state);
        Assert.Contains(elite.Id, options.EligibleIds);
        Assert.Contains(leader.Id, options.EligibleIds);
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [CharacterRules.Grey] });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [elite.Id] });
        Assert.Equal("casualties", state.Units.Single(u => u.Id == elite.Id).Location);
        Assert.Equal("casualties", state.Units.Single(u => u.Id == leader.Id).Location);
        Assert.Equal("dol-amroth", CharacterRules.InPlay(state, CharacterRules.Grey)!.Location);
        Assert.Empty(state.PoliticalReactions);
    }

    [Fact]
    public void StormcrowAllowsPartialEffectAtTrackEndAndRejectsAtWarOrMordorOnly()
    {
        var state = State(Stormcrow);
        foreach (var u in state.Units.Where(u => u.Nation == "gondor"))
            BoardRules.ReturnToReserve(u);
        state.Nations.Single(n => n.Id == "gondor").StepsToWar = 3;
        state = Play(state, Stormcrow);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(3, state.Nations.Single(n => n.Id == "gondor").StepsToWar);
        state = State(Stormcrow);
        state.Nations.Single(n => n.Id == "gondor").StepsToWar = 0;
        Assert.False(new StormcrowCondition().CanPlay(state, Side.Shadow));
        state.Nations.Single(n => n.Id == "gondor").StepsToWar = 2;
        state.Fellowship.RegionId = MordorRules.Track;
        Assert.False(new StormcrowCondition().CanPlay(state, Side.Shadow));
    }
}
