using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Tests;

public class ExpansionPersistenceTests
{
    [PostgresFact]
    public async Task All93ConfigurationsRestartTheirFirstSetupDecisionWithPrivateViewsAndIdempotentRetry()
    {
        foreach (var row in ExpansionConfigurations.Legal())
        {
            var state = ExpansionFixture.Start(ExpansionConfigurations.FromFlags((int)row[0]));
            var choice = ExpansionFixture.NextSetupChoice(state);
            await LordsPersistenceTests.Restart(state, choice.Kind, choice.Payload);
        }
    }

    [PostgresFact]
    public async Task All93ConfigurationsRestartTheirFirstPaidActionWithoutResettingSetupOrShufflingAgain()
    {
        foreach (var row in ExpansionConfigurations.Legal())
        {
            var state = ExpansionFixture.CompleteSetup(ExpansionFixture.Start(ExpansionConfigurations.FromFlags((int)row[0])));
            var die = state.Dice.First(candidate => candidate.Owner == Side.FreePeoples && candidate.Location == DieLocation.Available);
            await LordsPersistenceTests.Restart(state, CommandKind.DiscardDie, new() { DieId = die.Id });
        }
    }

    [PostgresFact]
    public async Task FateCouncilBookAndChieftainPersistEachDecisionAndBothSeparateCosts()
    {
        foreach (var cities in new[] { false, true })
        {
            var state = ExpansionInteractionTests.Book(cities);
            await LordsPersistenceTests.Restart(state, CommandKind.MoveCompanions, new());
            state = KingsFixture.Do(state, CommandKind.MoveCompanions);
            var elite = FateCardsFixture.Option(state).EligibleIds
                .Single(id => state.Units.Single(unit => unit.Id == id).Kind == UnitKind.Elite);
            await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [elite] });
            state = FateCardsFixture.Choose(state, elite);
            var payload = KingsCouncilIntegrationTests.Payload(state);
            state.Dice.Single(die => die.Id == payload.DieId).Face = DieFace.Muster;
            await LordsPersistenceTests.Restart(state, CommandKind.EnterCharacter, payload);
        }
    }

    [PostgresFact]
    public async Task FullRosterDiceChoicesRulerDiscardsAndDismayPersistWithEitherTokenVariant()
    {
        foreach (var removing in new[] { false, true })
        {
            var state = ExpansionDiceIntegrationTests.Rolling(cities: true, removing);
            foreach (var id in new[] { LordsDiceCatalog.Gothmog, LordsDiceCatalog.Nenya })
            {
                var payload = new CommandPayload { DieId = id };
                await LordsPersistenceTests.Restart(state, CommandKind.ChooseSpecialDie, payload);
                state = KingsFixture.Do(state, CommandKind.ChooseSpecialDie, payload);
            }
            foreach (var side in new[] { Side.Shadow, Side.FreePeoples })
            {
                var payload = new CommandPayload { DieId = KingsDice.DiscardCandidates(state, side)
                    .First(id => state.Dice.Single(die => die.Id == id).Source == "standard") };
                await LordsPersistenceTests.Restart(state, CommandKind.DiscardRulerDie, payload);
                state = KingsFixture.Do(state, CommandKind.DiscardRulerDie, payload);
            }
            await LordsPersistenceTests.Restart(state, CommandKind.UseDismay,
                new() { DieId = KingsDice.Shadow, Ids = [KingsRulerTokens.Id(SovereignCatalog.Theoden)] }, 5);
        }
    }

    [PostgresFact]
    public async Task FullCompositionPrivateCorruptionDrawAndAttachmentRestartInAllFourTileVariants()
    {
        foreach (var visible in new[] { false, true })
            foreach (var sequential in new[] { false, true })
            {
                var state = ExpansionInteractionTests.Corruption(visible, sequential);
                state.HuntTiles.Single(tile => tile.Id == HuntTests.Three).Location = "pool";
                await LordsPersistenceTests.Restart(state, CommandKind.DrawCorruptionTile, new(), 0);
                state = KingsFixture.Do(state, CommandKind.DrawCorruptionTile, new(), 0);
                state.HuntTiles.Single(tile => tile.Id == KingsCorruptionTests.One).Location = "pool";
                await LordsPersistenceTests.Restart(state, CommandKind.DrawCorruptionTile, new(), 0);
                state = KingsFixture.Do(state, CommandKind.DrawCorruptionTile, new(), 0);
                if (!sequential)
                    await LordsPersistenceTests.Restart(state, CommandKind.ChooseCorruptionTile,
                        new() { Ids = [KingsCorruptionTests.One] });
            }
    }
}
