using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.CardPackPersistenceTests;

namespace Wotr.Tests;

public class BaseCardBattleInteractionTests
{
    private const string Scouts = FreePeoplesStrategyCards01.SpiritOfMordor;

    [Fact]
    public void BothSwarmCardsCancelScoutsBeforeRetreatWithoutAwardingTheNoCardBonus()
    {
        foreach (var card in new[] { ShadowStrategyCards02.Moving, ShadowStrategyCards02.Return })
        {
            var state = BeforeScouts(card);
            state = Shadow01Fixture.Reveal(state, card, Scouts);
            var battle = BattleRuntime.Active(state)!;

            Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
            Assert.True(battle.Defender.Round.CardCancelled);
            Assert.Equal(Scouts, battle.Defender.Round.CardId);
            Assert.Equal(0, battle.Attacker.Round.RerollModifier);
            Assert.Equal(3, battle.Attacker.Round.Dice.Count);
            Assert.Equal(3, battle.Defender.Round.Dice.Count);
            Assert.Equal(6, state.RandomSamples.Count);
            Assert.All(BattleRules.Units(state, battle.Defender), unit => Assert.Equal(BattleFixture.Target, unit.Location));

            state = Do(state, CommandKind.StopBattle);
            Assert.Null(BattleRuntime.Active(state));
            Assert.Contains(Scouts, state.Player(Side.FreePeoples).Discard);
            Assert.Contains(card, state.Player(Side.Shadow).Discard);
        }
    }

    [Fact]
    public void ScoutsEndsOnslaughtAndRelentlessBeforeAnyVoluntaryLossOrCombatRoll()
    {
        foreach (var card in new[] { ShadowStrategyCards01.UrukHai, ShadowStrategyCards01.Dawn })
        {
            var state = BeforeScouts(card);
            var shadowIds = state.Units.Where(unit => unit.Location == BattleFixture.Origin).Select(unit => unit.Id).ToList();
            var freeIds = state.Units.Where(unit => unit.Location == BattleFixture.Target).Select(unit => unit.Id).ToList();
            state = Shadow01Fixture.Reveal(state, card, Scouts);

            Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
            Assert.Equal("RegionOrShelter", state.Pending.Presentation);
            var retreat = Shadow01Fixture.Choices(state).EffectChoices.First(choice => choice.Id != ScoutsStep.Shelter).Id;
            state = Do(StateCodec.Read(StateCodec.Write(state)), CommandKind.ChooseEffect, new() { Ids = [retreat] });

            Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
            Assert.Empty(state.RandomSamples);
            Assert.All(freeIds, id => Assert.Equal(retreat, state.Units.Single(unit => unit.Id == id).Location));
            Assert.All(shadowIds, id => Assert.Equal(BattleFixture.Origin, state.Units.Single(unit => unit.Id == id).Location));
            Assert.Contains(card, state.Player(Side.Shadow).Discard);
            Assert.Contains(Scouts, state.Player(Side.FreePeoples).Discard);

            state = Do(StateCodec.Read(StateCodec.Write(state)), CommandKind.AdvanceBattle, new() { Ids = shadowIds });
            Assert.Empty(state.Resolution.Frames);
            Assert.Empty(state.RandomSamples);
            Assert.All(shadowIds, id => Assert.Equal(BattleFixture.Target, state.Units.Single(unit => unit.Id == id).Location));
            Assert.Single(state.Dice, die => die.Location == DieLocation.Spent);
        }
    }

    [PostgresFact]
    public async Task CorsairsEndedByScoutsRestoresSorcererBeforePalantirWithoutAnotherDieOrRoll()
    {
        var state = CorsairFixture.State(defenders: 1);
        Put(state, CharacterRules.WitchKing, CorsairRules.Origin);
        Put(state, CharacterRules.Saruman, "orthanc");
        Shadow04Fixture.Table(state, ShadowCharacterCards04.Palantir);
        EventTests.GiveCard(state, Side.Shadow, ShadowCharacterCards04.Wormtongue);
        EventTests.GiveCard(state, Side.FreePeoples, Scouts);
        var cost = CorsairFixture.EventCost(state, DieFace.Event);

        state = await RestartDecision(state, CommandKind.PlayEvent, cost, new SequenceRandom());
        state = await RestartDecision(state, CommandKind.MoveArmies, CorsairFixture.Move(state), new SequenceRandom());
        state = await RestartDecision(state, CommandKind.CommitCombatCard,
            new() { CardId = ShadowCharacterCards04.Wormtongue }, new SequenceRandom());
        state = await RestartDecision(state, CommandKind.CommitCombatCard, new() { CardId = Scouts }, new SequenceRandom());
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ["lossarnach"] }, new SequenceRandom());

        Assert.Equal(DecisionKind.Reaction, state.Pending!.Kind);
        Assert.Equal("battle.round.closed", state.Pending.Window);
        Assert.Empty(state.Player(Side.Shadow).Hand);
        Assert.True(BattleRuntime.Active(state)!.ArrivalBeforeBattle);
        Assert.Equal(cost.DieId, Assert.Single(state.Dice, die => die.Location == DieLocation.Resolving).Id);
        var sorcererCard = state.Player(Side.Shadow).Decks["character"][0];

        state = await RestartDecision(state, CommandKind.RespondEffect,
            new() { ReactionId = "character.sorcerer" }, new SequenceRandom());

        Assert.Null(BattleRuntime.Active(state));
        Assert.Equal("Deck", state.Pending!.Presentation);
        Assert.Equal(sorcererCard, Assert.Single(state.Player(Side.Shadow).Hand));
        Assert.Equal(Side.Shadow, state.Control["pelargir"]);
        Assert.Equal("pelargir", CharacterRules.InPlay(state, CharacterRules.WitchKing)!.Location);
        Assert.Equal(2, state.Units.Count(unit => unit.Location == "pelargir" && BoardRules.IsArmyUnit(unit)));
        Assert.Equal(cost.DieId, Assert.Single(state.Dice, die => die.Location == DieLocation.Resolving).Id);
        var palantirCard = state.Player(Side.Shadow).Decks["strategy"][0];

        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ["strategy"] }, new SequenceRandom());

        Assert.Equal(new[] { sorcererCard, palantirCard }, state.Player(Side.Shadow).Hand);
        Assert.Empty(state.Resolution.Frames);
        Assert.Empty(state.RandomSamples);
        Assert.Equal(cost.DieId, Assert.Single(state.Dice, die => die.Location == DieLocation.Spent).Id);
        Assert.Single(state.Events, item => item.Kind == "BattleStarted");
        Assert.Single(state.Events, item => item.Kind == "BattleEnded");
    }

    private static MatchState BeforeScouts(string shadowCard)
    {
        var state = ShadowStrategy01Fixture.Combat(shadowCard);
        EventTests.GiveCard(state, Side.FreePeoples, Scouts);
        return state;
    }
}
