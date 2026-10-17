using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.KingsFreePeoplesCards01;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsCards01PersistenceTests
{
    [PostgresFact]
    public async Task EveryEventRestartsItsPaidCostAndInitialDecision()
    {
        foreach (var card in Ids)
        {
            var state = State();
            Companion(state, Boromir, "pelargir");
            await LordsPersistenceTests.Restart(state, CommandKind.PlayEvent, Cost(state, card));
        }
    }

    [PostgresFact]
    public async Task EveryCombatRestartsPrivateCommitAndRevealWithoutDuplicatingRolls()
    {
        foreach (var card in Ids)
        {
            var state = Battle();
            KingsCards01CombatTests.AddCompanion(state, Merry);
            state.Player(Side.FreePeoples).Hand = [card];
            state.Player(Side.Shadow).Hand.Clear();
            var payload = new CommandPayload { CardId = card };
            await LordsPersistenceTests.Restart(state, CommandKind.CommitCombatCard, payload);
            state = Do(state, CommandKind.CommitCombatCard, payload);
            var count = card == Dark ? 5 : card == Stewards ? 3 : 4;
            await LordsPersistenceTests.Restart(state, CommandKind.CommitCombatCard, new(), new int[count]);
        }
    }

    [PostgresFact]
    public async Task RandomRemovalPersistsTheChosenTileAndFollowingDrawForEveryBranch()
    {
        foreach (var (card, sovereign) in new[] { (Stewards, SovereignCatalog.Denethor), (Riders, SovereignCatalog.Theoden), (Brand, SovereignCatalog.Brand) })
        {
            var state = State();
            Companion(state, Boromir, "pelargir");
            Tile(state, sovereign);
            state = Play(state, card);
            await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = ["corruption"] }, 0);
        }
    }

    [PostgresFact]
    public async Task InspectionRestartsBeforeAccessDuringAccessAndAfterReturningOrKeeping()
    {
        foreach (var card in new[] { Wisdom, Dark })
        {
            var state = KingsInspectionTests.Ready(card);
            var select = new CommandPayload { Ids = [SovereignCatalog.Dain] };
            await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, select);
            state = Do(state, CommandKind.ChooseEffect, select);
            await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [One] });
            if (card == Wisdom)
                await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = ["keep"] });
        }
    }

    [PostgresFact]
    public async Task WisdomRestartsTheExclusivePoliticalBranchAndNationChoice()
    {
        var state = Play(State(), Wisdom);
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [WisdomBranchStep.Politics] });
        state = Choose(state, WisdomBranchStep.Politics);
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = ["north"] });
    }

    [PostgresFact]
    public async Task RecruitmentRestartsTheBranchAndAllPlacementsBeforeDrawing()
    {
        var state = Play(State(), Riders);
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = ["recruit"] });
        state = Choose(state, "recruit");
        var ids = new EffectOptions().GetOptions(state, Side.FreePeoples).Single().EligibleIds
            .Where(id => state.Units.Single(u => u.Id == id).Kind != UnitKind.Elite);
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new()
        {
            Placements = ids.Select(id => new UnitPlacementChoice { UnitId = id, RegionId = "edoras" }).ToList()
        });
        state = State();
        Companion(state, Boromir, "pelargir");
        state = Choose(Play(state, Stewards), "recruit");
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [state.Pending!.PrivateChoices[0]] });
    }

    [PostgresFact]
    public async Task AidRestartsJourneyThenTheAwakeningChoiceInPlace()
    {
        var state = State();
        Companion(state, Gimli, "dale");
        Companion(state, Merry, "edoras");
        state = Play(state, Aid);
        var journey = new CommandPayload { CompanionMoves = [new() { Ids = [Gimli], Route = ["dale", "erebor"] }] };
        await LordsPersistenceTests.Restart(state, CommandKind.MoveCompanions, journey);
        state = Do(state, CommandKind.MoveCompanions, journey);
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Dain] });
    }

    [PostgresFact]
    public Task BannerRestartsTheLeadershipChoiceBeforeLeaderRerolls()
    {
        var state = Battle();
        KingsCards01CombatTests.AddSovereign(state, SovereignCatalog.Theoden);
        state = Cards(state, Aid, 0, 0, 0, 0);
        return LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Theoden] });
    }

    [PostgresFact]
    public async Task GodOfOldRestartsTheChosenSovereignsRollAndSeparateLossBeforeCombat()
    {
        var state = Battle();
        KingsCards01CombatTests.AddSovereign(state, SovereignCatalog.Theoden);
        state = Cards(state, Dark);
        var chosen = new CommandPayload { Ids = [SovereignCatalog.Dain] };
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, chosen, 2);
        state = Do(state, CommandKind.ChooseEffect, chosen, 2);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        var loss = BattleRules.Units(state, force).First(BoardRules.IsArmyUnit).Id;
        await LordsPersistenceTests.Restart(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(loss)] }, 0, 0, 0);
    }
}
