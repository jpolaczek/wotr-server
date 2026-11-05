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

    // UI-442: a standard attempt restarts as one command drawing every tile, then its private choice; Sequential
    // restarts each draw.
    [PostgresFact]
    public async Task FullCompositionPrivateCorruptionDrawAndAttachmentRestartInAllFourTileVariants()
    {
        foreach (var visible in new[] { false, true })
            foreach (var sequential in new[] { false, true })
            {
                var state = ExpansionInteractionTests.CorruptionReady(visible, sequential);
                var payload = ExpansionInteractionTests.CorruptionPayload;
                string[] first = sequential ? [ExpansionInteractionTests.SmeagolTile]
                    : [ExpansionInteractionTests.SmeagolTile, HuntTests.Three, KingsCorruptionTests.One];
                state = await RestartAndDo(state, CommandKind.AttemptCorruption, payload, first);
                if (sequential)
                {
                    state = await RestartAndDo(state, CommandKind.DrawCorruptionTile, new(), HuntTests.Three);
                    await LordsPersistenceTests.Restart(state, CommandKind.DrawCorruptionTile, new(),
                        KingsFixture.DrawOrder(state, KingsCorruptionTests.One));
                }
                else
                    await LordsPersistenceTests.Restart(state, CommandKind.ChooseCorruptionTile,
                        new() { Ids = [KingsCorruptionTests.One] });
            }
    }

    private static async Task<MatchState> RestartAndDo(MatchState state, CommandKind kind, CommandPayload payload, params string[] draws)
    {
        var samples = KingsFixture.DrawOrder(state, draws);
        await LordsPersistenceTests.Restart(state, kind, payload, samples);
        return KingsFixture.Do(state, kind, payload, samples);
    }
}
