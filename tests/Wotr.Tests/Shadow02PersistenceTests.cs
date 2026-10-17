using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards02;
using static Wotr.Domain.FreePeoplesCharacterCards01;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow02TravelTests;
using static Wotr.Tests.CardPackPersistenceTests;

namespace Wotr.Tests;

public class Shadow02PersistenceTests
{
    [PostgresFact]
    public async Task EveryEventRestoresBeforePaymentAndRetryCannotRepeatDamageOrCost()
    {
        foreach (var card in new[] { FoulThing, Candles, Search, Weather, Strike, Wound })
        {
            var state = BeforeTravel(card);
            state.Fellowship.Hidden = false;
            Pool(state, HuntTests.Eye);
            state = await RestartDecision(state, CommandKind.PlayEvent, EventCost(state, card),
                new SequenceRandom(card == Candles ? [3, 0, 5] : card == FoulThing ? [0] : []));
            if (card is Search or Strike)
                state = await RestartDecision(state, CommandKind.MoveShadowCharacters, new(), new SequenceRandom());
            if (card == Weather)
                state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = [Choices(state).EligibleIds[0]] }, new SequenceRandom());
            Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
            Assert.Single(state.Player(Side.Shadow).Discard, id => id == card);
            Assert.Equal(card is Candles or Wound ? 2 : 0, state.Fellowship.Corruption);
            Assert.Empty(state.Resolution.Frames);
        }
    }

    [PostgresFact]
    public async Task FoulThingRestoresReplacementProtectionRandomCasualtyAndGuide()
    {
        var state = BeforeTravel(FoulThing);
        state.Player(Side.FreePeoples).Table.AddRange([Mithril, Axe]);
        state.Fellowship.Guide = "base.character.legolas";
        var pick = state.Fellowship.Companions.IndexOf("base.character.legolas");
        Pool(state, ShadowCharacterCards01.SpecialTiles[0].Tile, RevealOne);
        state = Play(state, FoulThing, 0);
        state = await RestartDecision(state, CommandKind.DeclineReaction, new(), new SequenceRandom(5));
        state = await RestartDecision(state, CommandKind.RespondEffect, new() { ReactionId = ProtectReaction }, new SequenceRandom(pick));
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        Assert.Equal(3, HuntRuntime.Active(state)!.Damage);
        state = await RestartDecision(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider }, new SequenceRandom());
        Assert.Equal(3, state.Fellowship.Corruption);
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Events, e => e.Kind == "HuntCasualty");
    }

    [PostgresFact]
    public async Task SearchRestoresFlightThenFpRevelationWithoutReapplyingMovement()
    {
        var state = Play(BeforeTravel(Search), Search);
        var id = NazgulEventTravel.Option(state).EligibleIds[0];
        state = await RestartDecision(state, CommandKind.MoveShadowCharacters, Flight(state, "hollin", id), new SequenceRandom());
        state = await RestartDecision(state, CommandKind.RevealFellowship, new() { Route = ["hollin", "north-dunland"] }, new SequenceRandom());
        Assert.Equal("north-dunland", state.Fellowship.RegionId);
        Assert.Single(state.Events, e => e.Kind == "ShadowCharactersMoved");
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Empty(state.Resolution.Frames);
    }

    [PostgresFact]
    public async Task StrikeRestoresEffectChoiceRolledHuntRerollAndDamage()
    {
        var state = Shadow02StrikeTests.StrikeChoice(1);
        Pool(state, HuntTests.Three);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = [NazgulStrikeStep.Hunt] }, new SequenceRandom(0));
        Assert.True(HuntRuntime.Active(state)!.Rolled);
        state = await RestartDecision(state, CommandKind.RerollHunt, new() { Ids = ["hunt-die-0"] }, new SequenceRandom(5, 0));
        state = await RestartDecision(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }, new SequenceRandom());
        Assert.Equal(3, state.Fellowship.Corruption);
        Assert.Equal(2, state.Fellowship.Progress);
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Empty(state.Resolution.Frames);
    }

    [PostgresFact]
    public async Task EachLeadershipCostRestoresSelectedPointsAndRoundModifiers()
    {
        foreach (var card in new[] { FoulThing, Candles, Weather })
        {
            var state = Reveal(BeforeCombat(card), card);
            var count = card == FoulThing ? 1 : 2;
            var points = Choices(state).EligibleIds.Take(count).ToList();
            state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = points },
                new SequenceRandom(card == FoulThing ? [] : new int[8]));
            var battle = BattleRuntime.Active(state)!;
            Assert.Equal(count, battle.Force(Side.Shadow).Round.ForfeitedLeadership.Values.Sum());
            Assert.Equal(card == FoulThing ? 1 : 0, battle.Force(Side.Shadow).Round.RerollModifier);
            Assert.Equal(card == Candles ? 3 : 5, battle.Force(Side.FreePeoples).Round.Dice.Count);
            Assert.Single(state.Events, e => e.Kind == "NazgulLeadershipForfeited");
        }
    }

    [PostgresFact]
    public async Task BothBlackBreathCardsRestoreEliminationDecisionWithoutSpendingTheirHits()
    {
        foreach (var card in new[] { Strike, Wound })
        {
            var state = Shadow02BlackBreathTests.Choice(card);
            state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ["base.character.gimli"] }, new SequenceRandom());
            Assert.Equal("casualties", state.Characters.Single(c => c.Id == "base.character.gimli").Location);
            Assert.Single(state.Events, e => e.Kind == "BlackBreath");
            Assert.Equal(2, BattleLosses.Current(state)!.Hits);
        }
    }
}
