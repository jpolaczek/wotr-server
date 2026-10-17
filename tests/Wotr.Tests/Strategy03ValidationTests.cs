using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards03;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

public class Strategy03ValidationTests
{
    [Theory]
    [InlineData(CirdansShips)]
    [InlineData(GuardsOfCitadel)]
    [InlineData(CelebornsGaladhrim)]
    [InlineData(RidersOfTheoden)]
    [InlineData(Grimbeorn)]
    [InlineData(Imrahil)]
    public void EveryEventRejectsWrongCostSpentDieMissingCardAndWrongWindow(string card)
    {
        var state = Strategy03Fixture.Coasts();
        EventTests.GiveCard(state, Side.FreePeoples, card);
        var payload = Cost(state, DieFace.Muster);
        payload.CardId = card;
        payload.CardCost = DieFace.Character;
        Reject(state, CommandKind.PlayEvent, payload);
        payload.CardCost = DieFace.Muster;
        var die = state.Dice.Single(d => d.Id == payload.DieId);
        die.Location = DieLocation.Spent;
        Reject(state, CommandKind.PlayEvent, payload);
        die.Location = DieLocation.Available;
        HandRules.Remove(state.Player(Side.FreePeoples), card);
        Reject(state, CommandKind.PlayEvent, payload);
        HandRules.Add(state.Player(Side.FreePeoples), card);
        Assert.Null(Engine().Validate(state, Side.FreePeoples, BattleFixture.Command(state, CommandKind.PlayEvent, payload)));
        state.Pending!.Kind = DecisionKind.Losses;
        Reject(state, CommandKind.PlayEvent, payload);
    }

    [Theory]
    [InlineData(CirdansShips)]
    [InlineData(GuardsOfCitadel)]
    [InlineData(CelebornsGaladhrim)]
    [InlineData(RidersOfTheoden)]
    [InlineData(Grimbeorn)]
    [InlineData(Imrahil)]
    public void EveryCombatRequiresOwnedCardAndCorrectWindow(string card)
    {
        var state = Strategy03Fixture.Charge(card, fpAttacks: false);
        state = Do(state, CommandKind.CommitCombatCard);
        var payload = new CommandPayload { CardId = card };
        Assert.Null(Engine().Validate(state, Side.FreePeoples, BattleFixture.Command(state, CommandKind.CommitCombatCard, payload)));
        HandRules.Remove(state.Player(Side.FreePeoples), card);
        Reject(state, CommandKind.CommitCombatCard, payload);
        HandRules.Add(state.Player(Side.FreePeoples), card);
        state.Pending!.Kind = DecisionKind.Action;
        Reject(state, CommandKind.CommitCombatCard, payload);
    }

    [Fact]
    public void RecruitPreviewIsPureShowsBothPlacementsAndKeepsOtherPlayersOptionsPrivate()
    {
        var state = Strategy01Fixture.Play(Strategy03Fixture.Coasts(), CirdansShips);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["grey-havens"] });
        var command = BattleFixture.Command(state, CommandKind.ChooseEffect,
            new() { Ids = Strategy03Fixture.Recruits(state, UnitKind.Regular, UnitKind.Elite) });
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine());
        var result = preview.Preview(state, Side.FreePeoples, command);
        Assert.True(result.Legal);
        Assert.Equal(2, result.Placements.Count);
        Assert.All(result.Placements, p => Assert.Equal("grey-havens", p.RegionId));
        Assert.Empty(new EffectOptions().GetOptions(state, Side.Shadow));
        Assert.False(preview.Preview(state, Side.Shadow, command).Legal);
        Assert.Equal(before, StateCodec.Write(state));
    }

    [Fact]
    public void CirdanTargetsEveryCoastalArmyButNotLoneLeadersOrInlandArmies()
    {
        var state = BattleFixture.State();
        var coasts = BoardRules.Regions.Values.Where(r => r.Coastal).Select(r => r.Id).ToArray();
        foreach (var region in coasts)
        {
            var nation = region is "umbar" or "lamedon" ? "elves" : "gondor";
            BattleFixture.Put(state, nation, UnitKind.Regular, region);
            state.Control[region] = Side.FreePeoples;
        }
        BattleFixture.Put(state, "elves", UnitKind.Regular, "lorien");
        state = Strategy01Fixture.Play(state, CirdansShips);
        Assert.Equal(coasts.Order(), state.Pending!.PrivateChoices.Order());
        state.Units.Single(u => u.Location == "arnor").Location = "reserve";
        BattleFixture.Put(state, "north", UnitKind.Leader, "arnor");
        Assert.DoesNotContain("arnor", new EffectOptions().GetOptions(state, Side.FreePeoples).Single().EligibleIds);
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["arnor"] });
    }
}
