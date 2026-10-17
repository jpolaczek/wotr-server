using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards02;
using static Wotr.Tests.CardPackPersistenceTests;
using static Wotr.Tests.Strategy01Fixture;
using static Wotr.Tests.Strategy02Fixture;

namespace Wotr.Tests;

public class Strategy02PersistenceTests
{
    [PostgresFact]
    public async Task MarchRestoresRouteRulesAndDoesNotCaptureOrPayTwice()
    {
        var state = Play(March(), ThroughDayNight);
        state = await RestartDecision(state, CommandKind.MoveArmies, Move(state, "north-ithilien", "minas-morgul", "gorgoroth"), new SequenceRandom());
        Assert.Equal(Side.FreePeoples, state.Control["minas-morgul"]);
        Assert.Single(state.Events, e => e.Kind == "SettlementCaptured" && e.Text.Contains("Minas Morgul"));
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [PostgresFact]
    public async Task WosesRestoresGarrisonExitAndCapturedOrigin()
    {
        var state = Play(Passage(), PathsOfWoses);
        state = await RestartDecision(state, CommandKind.MoveArmies, Move(state, "helms-deep", "minas-tirith"), new SequenceRandom());
        Assert.Equal(Side.Shadow, state.Control["helms-deep"]);
        Assert.False(state.Characters.Single().Garrison);
    }

    [PostgresFact]
    public async Task ReliefRestoresNestedBattleAndReductionBeforeBothRolls()
    {
        var state = Play(Relief(), HelpUnlookedFor);
        state = await RestartDecision(state, CommandKind.AttackArmy, Attack(state), new SequenceRandom());
        Assert.Equal(EffectReturn.Parent, BattleRuntime.Current(state)!.Return);
        state = await RestartDecision(state, CommandKind.CommitCombatCard, new(), new SequenceRandom());
        state = await RestartDecision(state, CommandKind.CommitCombatCard, new(), new SequenceRandom(0, 0, 0, 0, 0, 0));
        Assert.Equal(3, BattleRuntime.Active(state)!.Defender.Round.Dice.Count);
        Assert.Equal(6, state.RandomSamples.Count);
    }

    [PostgresFact]
    public async Task FearRestoresIndependentCompanionGroupsAndRemainingPolitics()
    {
        var state = BattleFixture.State();
        CharacterFixture.Put(state, "base.character.gimli", "old-forest");
        state.Nations.Single(n => n.Id == "north").StepsToWar = 3;
        state = Play(state, FearFireFoes);
        state = await RestartDecision(state, CommandKind.MoveCompanions, new()
        {
            CompanionMoves = [new() { Ids = ["base.character.gimli"], Route = ["old-forest", "the-shire"] }]
        }, new SequenceRandom());
        Assert.Equal(0, state.Nations.Single(n => n.Id == "north").StepsToWar);
    }

    [PostgresFact]
    public async Task ArrowRestoresRecruitmentChoiceAfterPoliticalAdvance()
    {
        var state = BattleFixture.State();
        state.Nations.Single(n => n.Id == "rohan").StepsToWar = 2;
        state = Play(state, RedArrow);
        var elite = state.Pending!.PrivateChoices.Single(id => state.Units.Single(u => u.Id == id).Kind == UnitKind.Elite);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = [elite] }, new SequenceRandom());
        Assert.Equal(1, state.Nations.Single(n => n.Id == "rohan").StepsToWar);
        Assert.Contains(state.Units, u => u.Kind == UnitKind.Leader && u.Location == "edoras");
    }

    [PostgresFact]
    public async Task ConfusionRestoresForbiddenRerollsAndNormalDamageTiming()
    {
        var state = CardPackBattleFixture.Reveal(CardPackBattleFixture.BeforeCards(WisdomOfElrond), WisdomOfElrond,
            dice: [0, 0, 0, 0, 0, 0, 0, 1]);
        state = await RestartDecision(state, CommandKind.RerollBattle, new(), new SequenceRandom());
        state = await RestartDecision(state, CommandKind.RerollBattle, new(), new SequenceRandom());
        Assert.Equal(2, state.Pending!.RequiredCount);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        Assert.Equal(8, state.RandomSamples.Count);
    }
}
