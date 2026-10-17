using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CardPackPersistenceTests;
using static Wotr.Domain.FreePeoplesCharacterCards03;

namespace Wotr.Tests;

public class CardPack03PersistenceTests
{
    [PostgresFact]
    public async Task MirrorRestoresSpecificDieSelectionAndDoesNotHealTwice()
    {
        var state = MirrorAndChallengeTests.MirrorChoice();
        var id = state.Pending!.PrivateChoices[1];
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = [id] }, new SequenceRandom());
        Assert.Equal(DieFace.WillOfTheWest, state.Dice.Single(d => d.Id == id).Face);
        Assert.Equal(1, state.Fellowship.Corruption);
        Assert.Single(state.Events, e => e.Kind == "ActionDieChanged");
    }

    [PostgresFact]
    public async Task ChallengePersistsThreeTileOutcomesAndRetryDoesNotDrawAgain()
    {
        var state = MirrorAndChallengeTests.ChallengeState();
        HuntTests.Pool(state, HuntTests.Eye, HuntTests.Three, "base.hunt.standard.0.1.1");
        EventTests.GiveCard(state, Side.FreePeoples, Challenge);
        var payload = CharacterFixture.Cost(state, DieFace.Character);
        payload.CardCost = DieFace.Character;
        payload.CardId = Challenge;
        state = await RestartDecision(state, CommandKind.PlayEvent, payload, new SequenceRandom(0, 0, 0));
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Equal("removed", state.HuntTiles.Single(t => t.Id == HuntTests.Eye).Location);
        Assert.Single(state.Events, e => e.Kind == "ChallengeDraw");
    }

    [PostgresFact]
    public async Task GwaihirRestoresFixedRangeAndPlacesCompanionInsideSiege()
    {
        var state = await RestartDecision(CompanionJourneyCardTests.BesiegedDestination(), CommandKind.MoveCompanions,
            new() { CompanionMoves = [new() { Ids = ["base.character.gimli"], Route = ["dimrill-dale", "lorien"] }] }, new SequenceRandom());
        Assert.True(state.Characters.Single().Garrison);
        Assert.Equal("lorien", state.Characters.Single().Location);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [PostgresFact]
    public async Task SwifterRestoresBonusSeparationAndThenGuideChoice()
    {
        var state = FellowshipEventCardTests.Play(TurnTests.Actions(), Swifter);
        state = await RestartDecision(state, CommandKind.SeparateCompanions,
            new() { Ids = [CharacterHuntTests.Merry], Route = ["rivendell", "fords-of-bruinen", "hollin", "north-dunland"] }, new SequenceRandom());
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        state = await RestartDecision(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider }, new SequenceRandom());
        Assert.Equal("north-dunland", state.Characters.Single().Location);
        Assert.Empty(state.Resolution.Frames);
    }

    [PostgresFact]
    public async Task ThereAndBackRestoresGuideChoiceBeforePoliticalAdvancement()
    {
        var state = TurnTests.Actions();
        state.Fellowship.RegionId = "old-forest-road";
        state = FellowshipEventCardTests.Play(state, ThereAndBack);
        var before = state.Nations.Single(n => n.Id == "north").StepsToWar;
        state = await RestartDecision(state, CommandKind.SeparateCompanions,
            new() { Ids = ["base.character.gimli"], Route = ["old-forest-road", "dale"] }, new SequenceRandom());
        Assert.Equal(before, state.Nations.Single(n => n.Id == "north").StepsToWar);
        state = await RestartDecision(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider }, new SequenceRandom());
        Assert.Equal(before - 1, state.Nations.Single(n => n.Id == "north").StepsToWar);
        Assert.True(state.Nations.Single(n => n.Id == "north").Active);
    }

    [PostgresFact]
    public async Task EaglesRestoresBothOwnersAndRetryCannotRepeatRollsOrRetreat()
    {
        var state = FellowshipEventCardTests.Play(EaglesCardTests.BeforeAttack(), Eagles);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = [EaglesCardTests.Target] }, new SequenceRandom(0, 4, 3));
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ["dol-guldur"] }, new SequenceRandom());
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Equal(2, state.Units.Count(u => u.Kind == UnitKind.Nazgul && u.Location == "dol-guldur"));
        Assert.Single(state.Events, e => e.Kind == "EaglesAttack");
        Assert.Single(state.Events, e => e.Kind == "NazgulRetreated");
    }

    [PostgresFact]
    public async Task SuddenStrikeRestoresOpponentsSpecialLossesBeforeOrdinaryRolls()
    {
        var state = LeadershipAttackCardTests.SuddenLosses();
        var casualty = BattleRules.Units(state, BattleRuntime.Active(state)!.Defender).First(BoardRules.IsArmyUnit);
        state = await RestartDecision(state, CommandKind.TakeBattleLosses,
            new() { Losses = [BattleFixture.Loss(casualty.Id)] }, new SequenceRandom(new int[7]));
        Assert.Equal(11, state.RandomSamples.Count);
        Assert.Single(BattleRuntime.Active(state)!.SpecialAttacks);
    }
}
