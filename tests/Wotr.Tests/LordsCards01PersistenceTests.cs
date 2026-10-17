using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.LordsFreePeoplesCards;
using static Wotr.Tests.LordsCardsFixture;

namespace Wotr.Tests;

public class LordsCards01PersistenceTests
{
    [PostgresFact]
    public async Task EveryEventPersistsItsPaidCostAndCompleteOrPendingEffect()
    {
        foreach (var card in new[] { YouKnowTheWay, SafePaths, Wind, ThreeRings, EveryLittle, Rangers })
        {
            var state = State();
            await LordsPersistenceTests.Restart(state, CommandKind.PlayEvent, Cost(state, card), card == ThreeRings ? [0, 0, 0] : []);
        }
    }

    [PostgresFact]
    public async Task EveryCombatCardRestartsThePrivateCommitAndSharedReveal()
    {
        foreach (var card in new[] { YouKnowTheWay, SafePaths, Wind, ThreeRings, EveryLittle, Rangers })
        {
            var state = Battle(card, companion: "base.character.merry");
            state.Fellowship.RegionId = BattleFixture.Target;
            var commit = new CommandPayload { CardId = card };
            await LordsPersistenceTests.Restart(state, CommandKind.CommitCombatCard, commit);
            state = LordsFixture.Do(state, CommandKind.CommitCombatCard, commit);
            var battle = BattleRuntime.Active(state)!;
            var dice = BattleRoll.DiceCount(state, battle.Attacker) + BattleRoll.DiceCount(state, battle.Defender);
            await LordsPersistenceTests.Restart(state, CommandKind.CommitCombatCard, new(), new int[dice]);
        }
    }

    [PostgresFact]
    public async Task WindRestartsBeforeInspectionDuringAccessAndAfterRevokingIt()
    {
        var state = State();
        EventTests.GiveCard(state, Side.Shadow, "base.sh.character.01");
        EventTests.GiveCard(state, Side.Shadow, "base.sh.strategy.02");
        state = Play(state, Wind);
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [WindFromWestStep.Inspect] });
        state = Choose(state, WindFromWestStep.Inspect);
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = ["base.sh.character.01"] });
    }

    [PostgresFact]
    public Task WindTableChoicePersistsRemovalWithoutHandPermission()
    {
        var state = State();
        state.Player(Side.Shadow).Table.AddRange([SmeagolRules.WeShallGetIt, ShadowCharacterCards04.Palantir]);
        state = Play(state, Wind);
        return LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [SmeagolRules.WeShallGetIt] });
    }

    [PostgresFact]
    public async Task RingsPersistTheRollBeforeRecoveringChosenIdentities()
    {
        var state = State();
        state.Rings[0].Owner = null;
        state.Rings[1].Owner = Side.Shadow;
        state.Rings[2].Owner = Side.Shadow;
        var cost = Cost(state, ThreeRings);
        await LordsPersistenceTests.Restart(state, CommandKind.PlayEvent, cost, 4, 5, 0);
        state = LordsFixture.Do(state, CommandKind.PlayEvent, cost, new SequenceRandom(4, 5, 0));
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [state.Rings[0].Id, state.Rings[2].Id] });
    }

    [PostgresFact]
    public Task EveryLittleRestartsSharedReservePlacementBeforeDrawing()
    {
        var state = State();
        foreach (var unit in state.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular).Skip(1))
            unit.Location = "casualties";
        state = Play(state, EveryLittle);
        return LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new()
        {
            Placements = [new() { UnitId = state.Pending!.PrivateChoices.Single(), RegionId = "lamedon" }]
        });
    }

    [PostgresFact]
    public async Task RangersRestartsTargetRollShadowLossNorthDestinationAndUnitChoice()
    {
        var state = State();
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "bree");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "bree");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "arnor");
        state = Play(state, Rangers);
        var target = new CommandPayload { Ids = ["bree"] };
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, target, 4, 0, 0);
        state = LordsFixture.Do(state, CommandKind.ChooseEffect, target, new SequenceRandom(4, 0, 0));
        var loss = new CommandPayload { Losses = [BattleFixture.Loss(EventLosses.Current(state)!.Target.Participants[0])] };
        await LordsPersistenceTests.Restart(state, CommandKind.TakeEventLosses, loss);
        state = LordsFixture.Do(state, CommandKind.TakeEventLosses, loss);
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = ["buckland"] });
        state = Choose(state, "buckland");
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [state.Pending!.PrivateChoices[0]] });
    }

    [PostgresFact]
    public async Task OrderlyWithdrawalRestartsCancellationLossesAndForcedRetreat()
    {
        await RestartWithdrawal(true);
        await RestartWithdrawal(false);
    }

    private static async Task RestartWithdrawal(bool fpAttacks)
    {
        var state = LordsWithdrawalTests.Window(fpAttacks);
        var choice = new CommandPayload { Ids = [OrderlyWithdrawalStep.Cancel] };
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, choice);
        state = Choose(state, OrderlyWithdrawalStep.Cancel);
        while (state.Pending?.Kind == DecisionKind.Losses)
        {
            var battle = BattleRuntime.Active(state)!;
            var unit = BattleRules.Units(state, battle.Force(state.Pending.Owner)).First(BoardRules.IsArmyUnit);
            var loss = new CommandPayload { Losses = [BattleFixture.Loss(unit.Id)] };
            await LordsPersistenceTests.Restart(state, CommandKind.TakeBattleLosses, loss);
            state = LordsFixture.Do(state, CommandKind.TakeBattleLosses, loss);
        }
        if (!fpAttacks)
            await LordsPersistenceTests.Restart(state, CommandKind.RetreatBattle, new() { RegionId = "pelargir" });
    }
}
