using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards03;
using static Wotr.Tests.CardPackPersistenceTests;
using static Wotr.Tests.Strategy01Fixture;
using static Wotr.Tests.Strategy03Fixture;

namespace Wotr.Tests;

public class Strategy03PersistenceTests
{
    [PostgresFact]
    public async Task CirdanRestoresRegionAndTwoFigureDecisionsWithoutRepeatingRecruits()
    {
        var state = Play(Coasts(), CirdansShips);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ["grey-havens"] }, new SequenceRandom());
        var ids = Recruits(state, UnitKind.Regular, UnitKind.Elite);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ids }, new SequenceRandom());
        Assert.Equal(2, state.Events.Count(e => e.Kind == "EventRecruited"));
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [PostgresFact]
    public async Task EverySingleRecruitmentRestoresAndChargesOnlyOnce()
    {
        foreach (var card in new[] { GuardsOfCitadel, CelebornsGaladhrim, RidersOfTheoden, Grimbeorn, Imrahil })
        {
            var state = Play(BattleFixture.State(), card);
            state = await RestartDecision(state, CommandKind.ChooseEffect,
                new() { Ids = Recruits(state, UnitKind.Elite) }, new SequenceRandom());
            Assert.Equal(card == CelebornsGaladhrim ? 1 : 2, state.Events.Count(e => e.Kind == "EventRecruited"));
            Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        }
    }

    [PostgresFact]
    public async Task RidersRestoresSelectedRegionBeforeArmyAndLeaderArriveTogether()
    {
        var state = BattleFixture.State();
        CharacterFixture.Put(state, "base.character.gimli", "eastemnet");
        state = Play(state, RidersOfTheoden);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ["eastemnet"] }, new SequenceRandom());
        state = await RestartDecision(state, CommandKind.ChooseEffect,
            new() { Ids = Recruits(state, UnitKind.Regular) }, new SequenceRandom());
        Assert.Equal(2, state.Units.Count(u => u.Location == "eastemnet" && u.Nation == "rohan"));
    }

    [PostgresFact]
    public async Task ChargeRestoresImmediateLossAndResumesCombatWithoutRepeatingSpecialRoll()
    {
        var state = CardPackBattleFixture.Reveal(Charge(), CirdansShips, dice: [5, 0]);
        var victim = BattleRules.Units(state, BattleRuntime.Active(state)!.Defender).First(BoardRules.IsArmyUnit).Id;
        state = await RestartDecision(state, CommandKind.TakeBattleLosses,
            new() { Losses = [BattleFixture.Loss(victim)] }, new SequenceRandom(new int[7]));
        Assert.Single(BattleRuntime.Active(state)!.SpecialAttacks);
        Assert.Equal(2, BattleRuntime.Active(state)!.Defender.Round.Dice.Count);
        Assert.Equal(9, state.RandomSamples.Count);
    }

    [PostgresFact]
    public async Task GrimbeornRestoresScoutsRetreatWithoutRolling()
    {
        var state = CardPackBattleFixture.Reveal(CardPackBattleFixture.BeforeCards(Grimbeorn, fpAttacks: false), Grimbeorn);
        state = await RestartDecision(state, CommandKind.ChooseEffect,
            new() { Ids = [state.Pending!.PrivateChoices[0]] }, new SequenceRandom());
        Assert.Empty(state.RandomSamples);
        Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
    }
}
