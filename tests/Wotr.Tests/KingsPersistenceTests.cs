using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsPersistenceTests
{
    [PostgresFact]
    public Task RestartBetweenDrawsKeepsEyeCostTargetAndHiddenCursor()
    {
        var state = Attempt(3, tiles: [HuntTests.Three, KingsTiles.One]);
        return LordsPersistenceTests.Restart(state, CommandKind.DrawCorruptionTile, new(), 0);
    }

    [PostgresFact]
    public Task RestartBeforePrivateChoiceAndRetryDoNotExposeOrRedrawTheTile() =>
        LordsPersistenceTests.Restart(Attempt(), CommandKind.ChooseCorruptionTile, new() { Ids = [HuntTests.Three] });

    [PostgresFact]
    public Task RestartOnLastNormalDrawKeepsBothPrivateChoices()
    {
        var one = HuntPool.Definitions.Values.First(t => t.Pool == "standard" && t.Value == "1" && !KingsTiles.IsKings(t.Id)).Id;
        var state = Attempt(3, tiles: [HuntTests.Three, one]);
        return LordsPersistenceTests.Restart(state, CommandKind.DrawCorruptionTile, new(), 0);
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
