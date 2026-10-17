using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesCharacterCards04;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.FellowshipEventCardTests;

namespace Wotr.Tests;

public class GondorRecruitCardTests
{
    internal static MatchState StewardsState(string region = "minas-tirith")
    {
        var state = BattleFixture.State();
        Put(state, "base.character.boromir", region);
        return state;
    }

    internal static MatchState CompanyState()
    {
        var state = BattleFixture.State();
        Put(state, CharacterRules.Strider, "fangorn");
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "fangorn");
        BattleFixture.Put(state, "rohan", UnitKind.Regular, "fangorn");
        return state;
    }

    [Theory]
    [InlineData(Stewards)]
    [InlineData(GreyCompany)]
    public void MissingPrintedCompanionConditionRejectsWithoutDrawing(string card)
    {
        var state = card == Stewards ? StewardsState("osgiliath") : CompanyState();
        if (card == GreyCompany)
            state.Characters.Single().Garrison = true;
        EventTests.GiveCard(state, Side.FreePeoples, card);
        var payload = Cost(state, DieFace.Character);
        payload.CardId = card;
        payload.CardCost = DieFace.Character;
        Reject(state, CommandKind.PlayEvent, payload);
    }

    [Theory]
    [InlineData("erech", UnitKind.Regular)]
    [InlineData("anfalas", UnitKind.Elite)]
    [InlineData("minas-tirith", UnitKind.Elite)]
    public void StewardsRecruitsChoiceThenDrawsTwoEvenOutsideSettlement(string region, UnitKind kind)
    {
        var state = StewardsState(region);
        var deck = state.Player(Side.FreePeoples).Decks["strategy"].Count;
        state = Play(state, Stewards);
        Assert.Equal(2, state.Pending!.PrivateChoices.Count);
        var id = state.Pending.PrivateChoices.Single(id => state.Units.Single(u => u.Id == id).Kind == kind);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [id] });
        Assert.Equal(kind, state.Units.Single(u => u.Id == id).Kind);
        Assert.Equal(StewardsCondition.Boromir(state)!.Location, state.Units.Single(u => u.Id == id).Location);
        Assert.Equal(deck - 2, state.Player(Side.FreePeoples).Decks["strategy"].Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StewardsCanRecruitOnEitherFriendlySideOfSiege(bool boromirInside)
    {
        var state = StewardsState();
        state.Characters.Single().Garrison = boromirInside;
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-tirith").Garrison = boromirInside;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "minas-tirith").Garrison = !boromirInside;
        state.Control["minas-tirith"] = boromirInside ? Side.FreePeoples : Side.Shadow;
        state = Play(state, Stewards);
        var id = state.Pending!.PrivateChoices[0];
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [id] });
        Assert.Equal(boromirInside, state.Units.Single(u => u.Id == id).Garrison);
    }

    [Theory]
    [InlineData("pelargir", false)]
    [InlineData("erech", true)]
    public void IllegalRecruitmentStillDrawsTwo(string region, bool enemyArmy)
    {
        var state = StewardsState(region);
        if (enemyArmy)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, region);
        else
            state.Control[region] = Side.Shadow;
        var deck = state.Player(Side.FreePeoples).Decks["strategy"].Count;
        state = Play(state, Stewards);
        Assert.Equal(deck - 2, state.Player(Side.FreePeoples).Decks["strategy"].Count);
        Assert.DoesNotContain(state.Units, u => u.Location == region && u.Nation == "gondor");
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void GreyCompanyReplacesLastRegularAtomicallyAndCannotUseAnotherNationsElite()
    {
        var state = CompanyState();
        foreach (var unit in state.Units.Where(u => u.Nation == "rohan" && u.Kind == UnitKind.Regular))
            BoardRules.ReturnToReserve(unit);
        var regular = state.Units.Single(u => u.Location == "fangorn");
        var leader = BattleFixture.Put(state, "gondor", UnitKind.Leader, "fangorn");
        state = Play(state, GreyCompany);
        Assert.Equal("casualties", state.Units.Single(u => u.Id == regular.Id).Location);
        Assert.Equal("fangorn", state.Units.Single(u => u.Id == leader.Id).Location);
        Assert.Equal("fangorn", KingArmyRules.King(state)!.Location);
        Assert.Contains(state.Units, u => u.Nation == "gondor" && u.Kind == UnitKind.Elite && u.Location == "fangorn");
    }

    [Theory]
    [InlineData(Stewards)]
    [InlineData(GreyCompany)]
    public void MissingReservesSkipsRecruitmentWithoutLosingRegularAndStillDraws(string card)
    {
        var state = card == Stewards ? StewardsState() : CompanyState();
        foreach (var unit in state.Units.Where(u => u.Location == "reserve" && (card == Stewards ? u.Nation == "gondor" : u.Kind == UnitKind.Elite)))
            unit.Location = "casualties";
        var army = state.Units.Where(u => BoardRules.Regions.ContainsKey(u.Location)).Select(u => u.Id).ToList();
        var deck = state.Player(Side.FreePeoples).Decks["strategy"].Count;
        state = Play(state, card);
        Assert.Equal(army, state.Units.Where(u => BoardRules.Regions.ContainsKey(u.Location)).Select(u => u.Id));
        Assert.Equal(deck - 2, state.Player(Side.FreePeoples).Decks["strategy"].Count);
    }

    [Fact]
    public void GreyCompanyChoiceStaysInKingsArmyAndPreservesSiegePlacement()
    {
        var state = CompanyState();
        foreach (var unit in state.Units.Where(u => u.Location == "fangorn"))
        {
            unit.Location = "helms-deep";
            unit.Garrison = true;
        }
        state.Characters.Single().Location = "helms-deep";
        state.Characters.Single().Garrison = true;
        BattleFixture.Put(state, "isengard", UnitKind.Regular, "helms-deep");
        var outside = BattleFixture.Put(state, "gondor", UnitKind.Regular, "edoras");
        state = Play(state, GreyCompany);
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [outside.Id] });
        var regular = state.Units.Single(u => u.Location == "helms-deep" && u.Nation == "rohan");
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [regular.Id] });
        Assert.Contains(state.Units, u => u.Location == "helms-deep" && u.Nation == "rohan" && u.Kind == UnitKind.Elite && u.Garrison);
    }

    [Theory]
    [InlineData(Stewards)]
    [InlineData(GreyCompany)]
    public void BothStrategyCardsPrecedeDiscardAndGreyGuideDraw(string card)
    {
        var state = card == Stewards ? StewardsState() : CompanyState();
        var player = state.Player(Side.FreePeoples);
        player.Hand.Clear();
        foreach (var id in new[] { card, Treebeard, Huorns, Entmoot, DeadMen, FreePeoplesCharacterCards03.Eagles })
            EventTests.GiveCard(state, Side.FreePeoples, id);
        state.Fellowship.Guide = CharacterRules.Grey;
        var payload = Cost(state, DieFace.Event);
        payload.CardId = card;
        payload.CardCost = DieFace.Event;
        state = Do(state, CommandKind.PlayEvent, payload);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [state.Pending!.PrivateChoices[0]] });
        Assert.Equal(DecisionKind.DiscardCards, state.Pending!.Kind);
        Assert.Equal(7, state.Player(Side.FreePeoples).Hand.Count);
        state = Do(state, CommandKind.DiscardCards, new() { Ids = [Treebeard] });
        Assert.Equal("event.completed", state.Pending!.Window);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = state.Pending.PrivateChoices[0] });
        Assert.Equal(DecisionKind.DiscardCards, state.Pending!.Kind);
        Assert.Equal(7, state.Player(Side.FreePeoples).Hand.Count);
        state = Do(state, CommandKind.DiscardCards, new() { Ids = [Huorns] });
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }
}
