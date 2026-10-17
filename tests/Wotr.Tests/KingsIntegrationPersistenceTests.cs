using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsIntegrationFixture;

namespace Wotr.Tests;

public class KingsIntegrationPersistenceTests
{
    [PostgresFact]
    public async Task RestartsEveryLordsKingsRollBoundaryWithTheSameDiscardQueue()
    {
        var state = KingsLordsIntegrationTests.Rolling();
        for (var i = 0; i < 2; i++)
        {
            var p = new CommandPayload { DieId = LordsDiceRoll.LegalChoices(state, state.Pending!.Owner)[0] };
            await LordsPersistenceTests.Restart(state, CommandKind.ChooseSpecialDie, p);
            state = KingsFixture.Do(state, CommandKind.ChooseSpecialDie, p);
        }
        foreach (var side in new[] { Side.Shadow, Side.FreePeoples })
        {
            var p = new CommandPayload { DieId = KingsDice.DiscardCandidates(state, side).First(id => id != KingsDice.Ruler(state, side)!.Id) };
            await LordsPersistenceTests.Restart(state, CommandKind.DiscardRulerDie, p);
            state = KingsFixture.Do(state, CommandKind.DiscardRulerDie, p);
        }
        Assert.Equal(DecisionKind.Dismay, state.Pending!.Kind);
        await LordsPersistenceTests.Restart(state, CommandKind.UseDismay, Dismay(state), 5);
        await LordsPersistenceTests.Restart(state, CommandKind.DeclineDismay, new());
    }

    [PostgresFact]
    public async Task LoreAndDismayOrderPersistsBeforeAndAfterEitherEffect()
    {
        var state = DismayWindow(lore: true);
        await LordsPersistenceTests.Restart(state, CommandKind.UseLoreBeforeActions, new());
        var lore = KingsFixture.Do(state, CommandKind.UseLoreBeforeActions);
        await LordsPersistenceTests.Restart(lore, CommandKind.ChooseEffect,
            new() { Ids = [KingsDice.Shadow, "lore.Muster.keep"] });
        lore = LordsCardsFixture.Choose(lore, KingsDice.Shadow, "lore.Muster.keep");
        await LordsPersistenceTests.Restart(lore, CommandKind.UseDismay, Dismay(lore), 5);
        state.Kings!.Tokens.RemoveAt(1);
        var dismay = KingsFixture.Do(state, CommandKind.UseDismay, Dismay(state), 5);
        await LordsPersistenceTests.Restart(dismay, CommandKind.ChooseEffect,
            new() { Ids = [KingsDice.Shadow, "lore.Muster.keep"] });
    }

    [PostgresFact]
    public async Task SequentialDrawStopIgnoredReplacementAndAutomaticCompletionRestartPrivately()
    {
        foreach (var visible in new[] { false, true })
        {
            var state = Sequential(visible);
            await LordsPersistenceTests.Restart(state, CommandKind.ChooseCorruptionTile, new() { Ids = [HuntTests.Three] });
            state.HuntTiles.Single(t => t.Id == "lords.hunt.smeagol.1").Location = "pool";
            await LordsPersistenceTests.Restart(state, CommandKind.DrawCorruptionTile, new(), 0);
            state = KingsFixture.Do(state, CommandKind.DrawCorruptionTile, new(), 0);
            state.HuntTiles.Single(t => t.Id == KingsCorruptionTests.One).Location = "pool";
            await LordsPersistenceTests.Restart(state, CommandKind.DrawCorruptionTile, new(), 0);
            state = KingsFixture.Do(state, CommandKind.DrawCorruptionTile, new(), 0);
            await LordsPersistenceTests.Restart(state, CommandKind.ChooseCorruptionTile, new() { Ids = [KingsCorruptionTests.One] });
            state.HuntTiles.Single(t => t.Id == KingsTiles.Two).Location = "pool";
            await LordsPersistenceTests.Restart(state, CommandKind.DrawCorruptionTile, new(), 0);
        }
    }

    [PostgresFact]
    public async Task SpecialEyeCostAndCouncilDualCostNeverRepeatAfterRetry()
    {
        foreach (var id in new[] { LordsDiceCatalog.Nenya, LordsDiceCatalog.Gothmog })
        {
            var state = Actions();
            KingsFixture.Eyes(state, 1);
            SpecialEye(state, id);
            KingsFixture.Pool(state, KingsTiles.One);
            await LordsPersistenceTests.Restart(state, CommandKind.AttemptCorruption,
                new() { CharacterId = SovereignCatalog.Dain, DieId = id }, 0);
        }
        foreach (var token in CouncilTokens.All)
        {
            var state = Actions();
            await LordsPersistenceTests.Restart(state, CommandKind.EnterCharacter,
                KingsCouncilIntegrationTests.Payload(state, token: token));
        }
    }

    [PostgresFact]
    public async Task RulerTokenTransferAndLastNumericEventDrawPersistTheirFinalEffects()
    {
        var (state, payload) = KingsRulerTokenTests.Recruitment();
        state.Configuration.Lords = true;
        state.Configuration.Dismay = true;
        await LordsPersistenceTests.Restart(state, CommandKind.UseCharacterAbility, payload);
        state = KingsWestTests.Ready(2);
        state.Configuration.SequentialCorruption = true;
        KingsFixture.Pool(state, HuntTests.Three);
        state = KingsCardsFixture.Play(state, KingsCards02.West);
        state = KingsCardsFixture.Choose(state, SovereignCatalog.Dain, 0);
        state.HuntTiles.Single(t => t.Id == KingsCorruptionTests.One).Location = "pool";
        await LordsPersistenceTests.Restart(state, CommandKind.DrawCorruptionTile, new(), 0);
    }
}
