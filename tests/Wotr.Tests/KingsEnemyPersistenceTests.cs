using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.KingsCards04;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsEnemyPersistenceTests
{
    [PostgresFact]
    public async Task EventRestartsPaymentTargetRollArmyChoiceAndFreePeoplesLosses()
    {
        var state = KingsEnemyTests.Ready();
        await LordsPersistenceTests.Restart(state, CommandKind.PlayEvent, Cost(state, Enemy, DieFace.Muster));
        state = Play(state, Enemy);
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Denethor] }, 4, 0, 0);
        state = KingsEnemyTests.ArmyChoice();
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = ["minas-tirith:garrison"] }, 4, 0, 0);
        state = Choose(state, "minas-tirith:garrison", 4, 0, 0);
        var victim = EventLosses.Current(state)!.Target.Units(state).Single(BoardRules.IsArmyUnit).Id;
        await LordsPersistenceTests.Restart(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(victim)] });
    }

    [PostgresFact]
    public async Task CombatRestartsSecretCommitsAndMixedSovereignChoice()
    {
        var state = KingsEnemyCombatTests.Combat();
        state.Player(Side.FreePeoples).Hand.Clear();
        state.Player(Side.Shadow).Hand = [Enemy];
        await LordsPersistenceTests.Restart(state, CommandKind.CommitCombatCard, new());
        state = Do(state, CommandKind.CommitCombatCard, new());
        await LordsPersistenceTests.Restart(state, CommandKind.CommitCombatCard, new() { CardId = Enemy }, new int[4]);
        var region = BattleRuntime.Active(state)!.Region;
        KingsCards03Fixture.Corrupt(state, SovereignCatalog.Theoden);
        Sovereign(state, SovereignCatalog.Theoden).CorruptedRegion = region;
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = Enemy });
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Theoden] }, new int[4]);
    }
}
