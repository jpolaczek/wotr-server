using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsPersistenceTests
{
    // UI-442: a standard attempt draws in the command that spends the Eye, so that command restarts with all its draws.
    [PostgresFact]
    public Task RestartOfAnAttemptEndedByAKingsTileKeepsEyeCostTargetAndPlacement()
    {
        var state = Ready(3, HuntTests.Three, KingsTiles.One);
        return LordsPersistenceTests.Restart(state, CommandKind.AttemptCorruption, AttemptPayload(state),
            DrawOrder(state, HuntTests.Three, KingsTiles.One));
    }

    [PostgresFact]
    public Task RestartBeforePrivateChoiceAndRetryDoNotExposeOrRedrawTheTile() =>
        LordsPersistenceTests.Restart(Attempt(), CommandKind.ChooseCorruptionTile, new() { Ids = [HuntTests.Three] });

    [PostgresFact]
    public Task RestartOfAStandardAttemptKeepsBothPrivateChoices()
    {
        var state = Ready(3, HuntTests.Three, KingsCorruptionTests.One);
        return LordsPersistenceTests.Restart(state, CommandKind.AttemptCorruption, AttemptPayload(state),
            DrawOrder(state, HuntTests.Three, KingsCorruptionTests.One));
    }

    [PostgresFact]
    public Task RestartOfASingleTileAttemptPlacesItWithoutADecision()
    {
        var state = Ready(2, HuntTests.Three);
        return LordsPersistenceTests.Restart(state, CommandKind.AttemptCorruption, AttemptPayload(state), 0);
    }

    [PostgresFact]
    public Task RulerRecruitmentRestartsWithAllThreePlacementsAndOneCost()
    {
        var state = KingsRulerRecruitmentTests.Ready(SovereignCatalog.Brand);
        var payload = KingsRulerRecruitmentTests.Recruit(state, SovereignCatalog.Brand,
            (UnitKind.Regular, "dale"), (UnitKind.Regular, "bree"), (UnitKind.Regular, "dale"));
        return LordsPersistenceTests.Restart(state, CommandKind.UseCharacterAbility, payload);
    }

    [PostgresFact]
    public Task FarSightedPersistsItsChosenModeAndReducedRollWithoutRetryEntropy()
    {
        var state = KingsFarSightedTests.Ready();
        state.Fellowship.RegionId = "dol-guldur";
        Eyes(state, 2);
        var cost = state.Dice.Single(d => d.Owner == Side.FreePeoples && d.Source == SovereignCatalog.RulerSource).Id;
        return LordsPersistenceTests.Restart(state, CommandKind.MoveFellowship,
            new() { DieId = cost, AbilityId = KingsFarSighted.Ability }, 0);
    }

    [PostgresFact]
    public Task RestartBeforePoliticalRemovalRetainsTheActionCostAndRandomChoice()
    {
        var state = KingsPoliticsTests.Ready();
        state = Do(state, CommandKind.AdvancePolitics, new() { DieId = Die(state).Id, NationId = "dwarves" });
        return LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = ["remove"] }, 0);
    }
}
