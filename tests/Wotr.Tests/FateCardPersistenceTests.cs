using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.FateCardsFixture;
using static Wotr.Tests.LordsPersistenceTests;

namespace Wotr.Tests;

public class FateCardPersistenceTests
{
    [PostgresFact]
    public async Task EveryEventPersistsItsExactCostDiscardAndFirstDecisionBeforeAnIdempotentRetry()
    {
        foreach (var card in Cards)
        {
            var state = State(card);
            await Restart(state, CommandKind.PlayEvent, Cost(state, card));
        }
    }

    [PostgresFact]
    public async Task ThereRestartsSeparationAndGuideWithoutRepeatingPoliticalEffects()
    {
        var state = FateCompanionCardTests.There();
        state.Fellowship.Guide = CharacterHuntTests.Merry;
        state = Play(state, FateCards.ThereAndBack);
        var separation = new CommandPayload { Ids = [CharacterHuntTests.Merry], Route = ["rivendell", "fords-of-bruinen"] };
        await Restart(state, CommandKind.SeparateCompanions, separation);
        state = KingsFixture.Do(state, CommandKind.SeparateCompanions, separation);
        await Restart(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider });
    }

    [PostgresFact]
    public async Task BookRestartsJourneyDestinationAndUnitChoiceAfterItsPoliticalAdvance()
    {
        var state = Play(FateCompanionCardTests.Book(second: true), FateCards.Book);
        await Restart(state, CommandKind.MoveCompanions, new());
        state = KingsFixture.Do(state, CommandKind.MoveCompanions);
        var region = new CommandPayload { Ids = ["iron-hills"] };
        await Restart(state, CommandKind.ChooseEffect, region);
        state = KingsFixture.Do(state, CommandKind.ChooseEffect, region);
        var elite = Option(state).EligibleIds.Single(id => state.Units.Single(u => u.Id == id).Kind == UnitKind.Elite);
        await Restart(state, CommandKind.ChooseEffect, new() { Ids = [elite] });
    }

    [PostgresFact]
    public async Task BothFreeRecruitmentCardsPersistPlacementsAndPrivateDrawThenRestoreTheHandLimitDecision()
    {
        foreach (var card in new[] { FateCards.Bardings, FateCards.Dwarves })
        {
            var state = State(card);
            foreach (var other in CardCatalog.ConfiguredDeck(state.Configuration)
                .Where(c => c.Side == Side.FreePeoples && c.Id != card).Take(6))
                EventTests.GiveCard(state, Side.FreePeoples, other.Id);
            state = Play(state, card);
            var nation = card == FateCards.Bardings ? "north" : "dwarves";
            var plan = Plan(state, (nation, UnitKind.Elite, "iron-hills", 1), (nation, UnitKind.Leader, "iron-hills", 1));
            await Restart(state, CommandKind.ChooseEffect, plan);
            state = KingsFixture.Do(state, CommandKind.ChooseEffect, plan);
            Assert.Equal(DecisionKind.DiscardCards, state.Pending!.Kind);
            await Restart(state, CommandKind.DiscardCards, new() { Ids = [state.Player(Side.FreePeoples).Hand[0]] });
        }
    }

    [PostgresFact]
    public async Task OrcsRestartsTheAlternativeAndTheSharedReservePlan()
    {
        var state = State(FateCards.Orcs);
        Reserve(state, "sauron", UnitKind.Regular, 4);
        state = Play(state, FateCards.Orcs);
        await Restart(state, CommandKind.ChooseEffect, new() { Ids = ["erebor"] });
        state = Choose(state, "erebor");
        await Restart(state, CommandKind.ChooseEffect, Plan(state,
            ("sauron", UnitKind.Regular, "dol-guldur", 1), ("sauron", UnitKind.Regular, "erebor", 3)));
    }

    [PostgresFact]
    public async Task MonstersRestartsBothBranchProgramsAndTheEliteDestinationWithoutReplayingRecruitment()
    {
        foreach (var branch in new[] { FateCards.MonstersWest, FateCards.MonstersEast })
        {
            var state = State(FateCards.Monsters);
            Reserve(state, "sauron", UnitKind.Regular, 1);
            state = Play(state, FateCards.Monsters);
            await Restart(state, CommandKind.ChooseEffect, new() { Ids = [branch] });
            state = Choose(state, branch);
            var region = branch == FateCards.MonstersWest ? "trollshaws" : "withered-heath";
            if (branch == FateCards.MonstersEast)
            {
                await Restart(state, CommandKind.ChooseEffect, new() { Ids = [region] });
                state = Choose(state, region);
            }
            await Restart(state, CommandKind.ChooseEffect, Plan(state,
                ("sauron", UnitKind.Regular, branch == FateCards.MonstersWest ? "angmar" : "northern-mirkwood", 1),
                ("sauron", UnitKind.Elite, region, 1)));
        }
    }

    [PostgresFact]
    public async Task OnslaughtRestartsVoluntaryLossesItsServerRollAndOpposingLosses()
    {
        var state = NoRerolls(Reveal(Combat(FateCards.Orcs), FateCards.Orcs, samples: new int[6]));
        var battle = BattleRuntime.Active(state)!;
        var loss = new CommandPayload
        {
            Losses = [BattleFixture.Loss(BattleRules.Units(state, battle.Force(Side.Shadow)).First(BoardRules.IsArmyUnit).Id)]
        };
        await Restart(state, CommandKind.TakeBattleLosses, loss, 3);
        state = KingsFixture.Do(state, CommandKind.TakeBattleLosses, loss, 3);
        battle = BattleRuntime.Active(state)!;
        var regular = BattleRules.Units(state, battle.Force(Side.FreePeoples)).First(u => u.Kind == UnitKind.Regular);
        await Restart(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(regular.Id)] });
    }
}
