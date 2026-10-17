using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using static Wotr.Domain.KingsCards03;
using static Wotr.Tests.KingsCards03Fixture;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsCards03PersistenceTests
{
    [PostgresFact]
    public async Task EveryEventRestartsPaymentAndItsFirstDecision()
    {
        foreach (var card in Ids)
        {
            var state = Ready(card);
            await LordsPersistenceTests.Restart(state, CommandKind.PlayEvent, EventCost(state, card), card == Stormcrow ? [0] : []);
        }
    }

    [PostgresFact]
    public async Task ValinorRestartsRegionOrderEachRollAndEachOwnersLosses()
    {
        var state = Ready(Valinor);
        var first = BattleFixture.Put(state, "elves", UnitKind.Regular, "lorien").Id;
        var second = BattleFixture.Put(state, "elves", UnitKind.Regular, "rivendell").Id;
        state = Play(state, Valinor);
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = ["lorien"] }, 5);
        state = Choose(state, "lorien", 5);
        var loss = new CommandPayload { Losses = [BattleFixture.Loss(first)] };
        await LordsPersistenceTests.Restart(state, CommandKind.TakeEventLosses, loss, 5);
        state = Do(state, CommandKind.TakeEventLosses, loss, 5);
        await LordsPersistenceTests.Restart(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(second)] });
    }

    [PostgresFact]
    public Task StormcrowRestartsPrivateTileAndFreePeoplesWholeEliteLoss()
    {
        var state = KingsStormcrowTests.Choice();
        var elite = state.Units.Single(u => u.Location == "bree" && u.Kind == UnitKind.Elite).Id;
        return LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [elite] });
    }

    [PostgresFact]
    public async Task KingRestartsBonusChoiceAndResultingStackLimit()
    {
        var state = Ready(King);
        for (var i = 0; i < 5; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, "minas-morgul");
        state = Play(state, King);
        var id = state.Pending!.PrivateChoices.First(id => state.Units.Single(u => u.Id == id).Kind == UnitKind.Regular);
        var choice = new CommandPayload { Ids = [id] };
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, choice);
        state = Do(state, CommandKind.ChooseEffect, choice);
        await LordsPersistenceTests.Restart(state, CommandKind.RemoveExcess, choice);
    }

    [PostgresFact]
    public async Task PalantirRestartsDrawAndEventConversionWithSharedTurnLimit()
    {
        var state = KingsPalantirTests.WithPalantir();
        var cost = CardDrawCost.Options(state, Side.Shadow).First(c => c.AbilityId == KingsPalantir.Conversion);
        await LordsPersistenceTests.Restart(state, CommandKind.DrawCard,
            new() { DieId = cost.DieId, Deck = "strategy", AbilityId = cost.AbilityId });
        var payload = Cost(state, KingsCards02.Welcome, DieFace.Muster);
        payload.CardCost = DieFace.Event;
        payload.AbilityId = KingsPalantir.Conversion;
        await LordsPersistenceTests.Restart(state, CommandKind.PlayEvent, payload);
    }

    [PostgresFact]
    public async Task FollyAndPalantirRestartBothRemovalCosts()
    {
        foreach (var card in new[] { Denethor, Palantir })
            foreach (var will in new[] { true, false })
            {
                var state = Table(card);
                Companion(state, Boromir, "minas-tirith");
                var die = Die(state);
                die.Face = will ? DieFace.WillOfTheWest : DieFace.Army;
                var payload = new CommandPayload { DieId = die.Id };
                if (!will && card == Palantir)
                    payload.RingId = state.Rings.First(r => r.Owner == Side.FreePeoples).Id;
                await LordsPersistenceTests.Restart(state, card == Denethor ? CommandKind.DiscardDenethor : CommandKind.DiscardBaradPalantir, payload);
            }
    }

    [PostgresFact]
    public async Task EveryCombatRestartsSecretCommitAndReveal()
    {
        foreach (var card in Ids)
        {
            var state = KingsCards03CombatTests.Combat(card);
            state.Player(Side.FreePeoples).Hand.Clear();
            state.Player(Side.Shadow).Hand = [card];
            await LordsPersistenceTests.Restart(state, CommandKind.CommitCombatCard, new());
            state = Do(state, CommandKind.CommitCombatCard, new());
            await LordsPersistenceTests.Restart(state, CommandKind.CommitCombatCard, new() { CardId = card }, card == King ? [] : new int[4]);
        }
    }

    [PostgresFact]
    public async Task PowerRestartsSovereignChoiceAndRelentlessRestartsVoluntaryLoss()
    {
        var state = KingsCards03CombatTests.MixedPower();
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Denethor] }, new int[4]);
        state = KingsChieftainBattleFixture.Cards(KingsCards03CombatTests.Combat(King), King);
        await LordsPersistenceTests.Restart(state, CommandKind.TakeBattleLosses, new(), new int[4]);
        var id = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.Shadow)).First().Id;
        await LordsPersistenceTests.Restart(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(id)] }, new int[3]);
    }
}
