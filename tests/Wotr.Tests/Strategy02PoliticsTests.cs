using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards02;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Strategy01Fixture;

namespace Wotr.Tests;

public class Strategy02PoliticsTests
{
    [Theory]
    [InlineData("the-shire", true, true)]
    [InlineData("bree", true, true)]
    [InlineData("carrock", true, false)]
    [InlineData("dale", true, false)]
    [InlineData("the-shire", false, false)]
    public void FearOnlyActivatesNorthInExactUnconqueredSettlements(string region, bool friendly, bool activates)
    {
        var state = BattleFixture.State();
        Put(state, "base.character.legolas", region);
        state.Control[region] = friendly ? Side.FreePeoples : Side.Shadow;
        var nation = state.Nations.Single(n => n.Id == "north");
        nation.Active = false;
        nation.StepsToWar = 3;
        state = Play(state, FearFireFoes);
        Reject(state, CommandKind.SeparateCompanions, new() { Ids = [CharacterRules.Grey], Route = ["rivendell"] });
        state = Do(state, CommandKind.MoveCompanions);
        Assert.Equal(activates, state.Nations.Single(n => n.Id == "north").Active);
        Assert.Equal(activates ? 0 : 3, state.Nations.Single(n => n.Id == "north").StepsToWar);
    }

    [Fact]
    public void FearMovesIndependentGroupsThenChecksNorthAndAllowsNoCompanions()
    {
        var state = BattleFixture.State();
        Put(state, "base.character.legolas", "north-downs");
        Put(state, "base.character.gimli", "old-forest");
        state.Nations.Single(n => n.Id == "north").Active = false;
        state.Nations.Single(n => n.Id == "north").StepsToWar = 3;
        state = Play(state, FearFireFoes);
        state = Do(state, CommandKind.MoveCompanions, new()
        {
            CompanionMoves = [new() { Ids = ["base.character.legolas"], Route = ["north-downs", "bree"] },
                new() { Ids = ["base.character.gimli"], Route = ["old-forest", "the-shire"] }]
        });
        Assert.Equal(0, state.Nations.Single(n => n.Id == "north").StepsToWar);
        Assert.Equal(2, state.Events.Count(e => e.Kind == "CompanionsMoved"));
        Assert.Empty(Play(BattleFixture.State(), FearFireFoes).Resolution.Frames);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WisdomActivatesThenAdvancesEvenAlreadyActiveNation(bool active)
    {
        var state = BattleFixture.State();
        var nation = state.Nations.Single(n => n.Id == "rohan");
        nation.Active = active;
        nation.StepsToWar = 1;
        EventTests.GiveCard(state, Side.FreePeoples, WisdomOfElrond);
        var payload = Cost(state, DieFace.Muster);
        payload.CardId = WisdomOfElrond;
        payload.CardCost = DieFace.Muster;
        payload.NationId = "sauron";
        Reject(state, CommandKind.PlayEvent, payload);
        payload.NationId = "rohan";
        state = Do(state, CommandKind.PlayEvent, payload);
        Assert.True(state.Nations.Single(n => n.Id == "rohan").Active);
        Assert.Equal(0, state.Nations.Single(n => n.Id == "rohan").StepsToWar);
    }

    [Theory]
    [InlineData(UnitKind.Regular)]
    [InlineData(UnitKind.Elite)]
    public void ArrowRequiresActiveGondorDoesNotActivateRohanAndRecruitsChoice(UnitKind kind)
    {
        var state = BattleFixture.State();
        var gondor = state.Nations.Single(n => n.Id == "gondor");
        gondor.Active = false;
        Assert.False(EffectCatalog.Base.Events[RedArrow].Condition.CanPlay(state, Side.FreePeoples));
        gondor.Active = true;
        state.Nations.Single(n => n.Id == "rohan").Active = false;
        state.Nations.Single(n => n.Id == "rohan").StepsToWar = 1;
        state = Play(state, RedArrow);
        var id = state.Pending!.PrivateChoices.Single(id => state.Units.Single(u => u.Id == id).Kind == kind);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [id] });
        Assert.False(state.Nations.Single(n => n.Id == "rohan").Active);
        Assert.Equal(1, state.Nations.Single(n => n.Id == "rohan").StepsToWar);
        Assert.Contains(state.Units, u => u.Location == "edoras" && u.Kind == kind);
        Assert.Contains(state.Units, u => u.Location == "edoras" && u.Kind == UnitKind.Leader);
    }

    [Fact]
    public void ArrowStillAdvancesWithoutRecruitsAndPreservesThreatsAndPromises()
    {
        var state = BattleFixture.State();
        state.Nations.Single(n => n.Id == "rohan").StepsToWar = 2;
        state.Nations.Single(n => n.Id == "rohan").Active = false;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "edoras");
        state.Player(Side.Shadow).Table.Add("base.sh.strategy.05");
        state.ActiveEffects.Add(new()
        {
            Owner = Side.Shadow,
            SourceId = "base.sh.strategy.05",
            Modifier = EffectModifier.PreventPassiveMusterAdvance,
            ExpiresWhen = EffectExpiry.FreeNationAdvancedByAttackOrCompanion
        });
        state = Play(state, RedArrow);
        Assert.Equal(1, state.Nations.Single(n => n.Id == "rohan").StepsToWar);
        Assert.Empty(state.Resolution.Frames);
        Assert.Contains("base.sh.strategy.05", state.Player(Side.Shadow).Table);
    }
}
