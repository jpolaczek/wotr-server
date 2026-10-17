using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesCharacterCards04;
using static Wotr.Tests.CardPackPersistenceTests;

namespace Wotr.Tests;

public class CardPack04PersistenceTests
{
    [PostgresFact]
    public async Task EntsRestoresLossesThenFreeEventWithoutRepeatingDamageOrPayment()
    {
        var state = EntsCardTests.BeforeAttack(true);
        EventTests.GiveCard(state, Side.FreePeoples, FreePeoplesCharacterCards02.Bilbo);
        state.Fellowship.Corruption = 2;
        state = FellowshipEventCardTests.Play(state, Treebeard, 3, 0, 0);
        var id = EventLosses.Current(state)!.Target.Units(state).First(BoardRules.IsArmyUnit).Id;
        state = await RestartDecision(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(id)] }, new SequenceRandom());
        Assert.Equal("ents.follow-up", state.Pending!.Window);
        state = await RestartDecision(state, CommandKind.PlayEvent, new() { CardId = FreePeoplesCharacterCards02.Bilbo, CardCost = DieFace.Character }, new SequenceRandom());
        Assert.Equal(1, state.Fellowship.Corruption);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Equal(3, state.RandomSamples.Count);
    }

    [PostgresFact]
    public async Task DeadMenRestoresEveryOwnerHandoffAndRecruitment()
    {
        var state = FellowshipEventCardTests.Play(DeadMenCardTests.BeforeJourney(), DeadMen);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ["pelargir", "base.character.gimli"] }, new SequenceRandom(0));
        var id = EventLosses.Current(state)!.Target.Units(state).First(BoardRules.IsArmyUnit).Id;
        state = await RestartDecision(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(id)] }, new SequenceRandom());
        var destination = state.Pending!.PrivateChoices[0];
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = [destination] }, new SequenceRandom());
        var recruits = state.Pending!.PrivateChoices.ToList();
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = recruits }, new SequenceRandom());
        Assert.Equal(Side.FreePeoples, state.Control["pelargir"]);
        Assert.Single(state.Events, e => e.Kind == "DeadMenAttack");
        Assert.Single(state.Events, e => e.Kind == "EventRetreat");
        Assert.Single(state.RandomSamples);
        Assert.Equal(3, state.Units.Count(u => u.Location == "pelargir" && BoardRules.IsArmyUnit(u)));
    }

    [PostgresFact]
    public async Task StewardsRestoresRegionAndChoiceAndDrawsExactlyTwice()
    {
        var state = FellowshipEventCardTests.Play(GondorRecruitCardTests.StewardsState(), Stewards);
        var before = state.Player(Side.FreePeoples).Decks["strategy"].Count;
        var id = state.Pending!.PrivateChoices[1];
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = [id] }, new SequenceRandom());
        Assert.Equal("minas-tirith", state.Units.Single(u => u.Id == id).Location);
        Assert.Equal(before - 2, state.Player(Side.FreePeoples).Decks["strategy"].Count);
    }

    [PostgresFact]
    public async Task GreyCompanyRestoresChoiceAndReplacementCannotRepeat()
    {
        var state = FellowshipEventCardTests.Play(GondorRecruitCardTests.CompanyState(), GreyCompany);
        var id = state.Pending!.PrivateChoices[1];
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = [id] }, new SequenceRandom());
        Assert.Equal("casualties", state.Units.Single(u => u.Id == id).Location);
        Assert.Single(state.Units, u => u.Location == "fangorn" && u.Kind == UnitKind.Elite);
        Assert.Single(state.Events, e => e.Kind == "EventUnitReplaced");
    }
}
