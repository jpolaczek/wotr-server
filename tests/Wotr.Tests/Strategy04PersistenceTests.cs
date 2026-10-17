using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards04;
using static Wotr.Tests.CardPackPersistenceTests;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Strategy01Fixture;
using static Wotr.Tests.Strategy04RecruitmentTests;

namespace Wotr.Tests;

public class Strategy04PersistenceTests
{
    [PostgresFact]
    public async Task SwordsRestoresBothGroupsThenDrawAndGreyHandLimitWithoutDuplicates()
    {
        var state = BattleFixture.State();
        EventTests.GiveCard(state, Side.FreePeoples, SwordsInEriador);
        var player = state.Player(Side.FreePeoples);
        foreach (var card in player.Decks["character"].Take(5).ToArray())
        {
            player.Decks["character"].Remove(card);
            HandRules.Add(player, card);
        }
        var payload = Cost(state, DieFace.Event);
        payload.CardId = SwordsInEriador;
        payload.CardCost = DieFace.Event;
        state = await RestartDecision(state, CommandKind.PlayEvent, payload, new SequenceRandom());
        state = await RestartDecision(state, CommandKind.ChooseEffect,
            new() { Ids = [Pick(state, "north", UnitKind.Elite), Pick(state, "dwarves", UnitKind.Regular)] }, new SequenceRandom());
        Assert.Equal(6, state.Player(Side.FreePeoples).Hand.Count);
        Assert.Equal("event.completed", state.Pending!.Window);
        state = await RestartDecision(state, CommandKind.RespondEffect, new() { ReactionId = "character.grey-draw" }, new SequenceRandom());
        Assert.Equal(DecisionKind.DiscardCards, state.Pending!.Kind);
        Assert.Equal(7, state.Player(Side.FreePeoples).Hand.Count);
        state = await RestartDecision(state, CommandKind.DiscardCards, new() { Ids = [state.Player(Side.FreePeoples).Hand[0]] }, new SequenceRandom());
        Assert.Equal(2, state.Events.Count(e => e.Kind == "EventRecruited"));
        Assert.Equal(2, state.Events.Count(e => e.Kind == "CardDrawn"));
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Empty(state.Resolution.Frames);
    }

    [PostgresFact]
    public async Task FixedRecruitmentCardsRestoreEveryChoiceAndAutomaticBrandDoesNotRepeat()
    {
        foreach (var card in new[] { KindredOfGlorfindel, DainIronfoot, ThranduilsArchers })
        {
            var state = Play(BattleFixture.State(), card);
            state = await RestartDecision(state, CommandKind.ChooseEffect,
                new() { Ids = Strategy03Fixture.Recruits(state, UnitKind.Elite) }, new SequenceRandom());
            Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
            Assert.Equal(card == DainIronfoot ? 2 : 1, state.Events.Count(e => e.Kind == "EventRecruited"));
        }
        var brand = BattleFixture.State();
        EventTests.GiveCard(brand, Side.FreePeoples, KingBrandsMen);
        var cost = Cost(brand, DieFace.Muster);
        cost.CardId = KingBrandsMen;
        cost.CardCost = DieFace.Muster;
        brand = await RestartDecision(brand, CommandKind.PlayEvent, cost, new SequenceRandom());
        Assert.Equal(2, brand.Events.Count(e => e.Kind == "EventRecruited"));
        Assert.Single(brand.Events, e => e.Kind == "CardDrawn");
    }

    [PostgresFact]
    public async Task EomerRestoresRegionBeforeRecruitsAndValourRestoresCombatModifier()
    {
        var state = Play(BattleFixture.State(), Eomer);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ["westemnet"] }, new SequenceRandom());
        state = await RestartDecision(state, CommandKind.ChooseEffect,
            new() { Ids = [Pick(state, "rohan", UnitKind.Elite)] }, new SequenceRandom());
        Assert.Equal(2, state.Units.Count(u => u.Location == "westemnet" && u.Nation == "rohan"));
        state = CardPackBattleFixture.Reveal(Strategy03Fixture.Charge(Eomer), Eomer, dice: new int[8]);
        var id = BattleRuntime.Active(state)!.Attacker.Round.Dice[0].Id;
        state = await RestartDecision(state, CommandKind.RerollBattle, new() { Ids = [id] }, new SequenceRandom());
        state = await RestartDecision(state, CommandKind.RerollBattle, new(), new SequenceRandom(3));
        var die = BattleRuntime.Active(state)!.Attacker.Round.Dice.Single(d => d.Id == id);
        Assert.Equal(1, die.Modifier);
        Assert.Equal(0, die.RerollModifier);
        Assert.False(die.RerollHit);
    }
}
