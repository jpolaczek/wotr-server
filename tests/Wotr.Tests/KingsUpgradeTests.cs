using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsUpgradeTests
{
    [Theory]
    [InlineData(SovereignCatalog.Dain, false)]
    [InlineData(SovereignCatalog.Dain, true)]
    [InlineData(SovereignCatalog.Denethor, false)]
    [InlineData(SovereignCatalog.Denethor, true)]
    [InlineData(SovereignCatalog.Thranduil, false)]
    [InlineData(SovereignCatalog.Thranduil, true)]
    public void UpgradeUsesAnyMusterAndPreservesTheArmyIncludingItsGarrison(string id, bool besieged)
    {
        var state = Actions();
        var definition = SovereignCatalog.All[id];
        state.Nations.Single(n => n.Id == definition.Nation).StepsToWar = 0;
        if (besieged)
        {
            foreach (var unit in state.Units.Where(u => u.Location == definition.Home))
                unit.Garrison = true;
            BattleFixture.Put(state, "sauron", UnitKind.Regular, definition.Home);
        }
        SovereignLifecycle.Awaken(state, Sovereign(state, id), definition.Home);
        var option = Assert.Single(KingsUpgrades.Options(state, Side.FreePeoples));
        var payload = new CommandPayload { CharacterId = id, AbilityId = option.AbilityId, DieId = option.Dice[0], Ids = option.Targets };
        var plan = CharacterAbilities.Plan(state, Side.FreePeoples, payload)!;
        var before = state.Units.Count(u => u.Location == definition.Home && u.Garrison == besieged && BoardRules.IsArmyUnit(u) && BoardRules.Owner(u) == Side.FreePeoples);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.FreePeoples,
            BattleFixture.Command(state, CommandKind.UseCharacterAbility, payload));
        Assert.True(preview.Legal);
        Assert.Empty(preview.Excess);
        Assert.Equal(plan.ReturnedIds, preview.RemovedIds);
        state = Do(state, CommandKind.UseCharacterAbility, payload);
        Assert.Equal("reserve", state.Units.Single(u => u.Id == plan.ReturnedIds.Single()).Location);
        var elite = state.Units.Single(u => u.Id == plan.Placements.Single().UnitId);
        Assert.Equal(UnitKind.Elite, elite.Kind);
        Assert.Equal(definition.Home, elite.Location);
        Assert.Equal(besieged, elite.Garrison);
        Assert.Equal(before, state.Units.Count(u => u.Location == definition.Home && u.Garrison == besieged && BoardRules.IsArmyUnit(u) && BoardRules.Owner(u) == Side.FreePeoples));
    }

    [Theory]
    [InlineData("peace")]
    [InlineData("away")]
    [InlineData("no-elite")]
    [InlineData("other-army")]
    [InlineData("other-nation")]
    public void UpgradeRejectsMissingWarHomeReserveOrMatchingRegular(string reason)
    {
        var state = Actions();
        state.Nations.Single(n => n.Id == "dwarves").StepsToWar = reason == "peace" ? 1 : 0;
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Dain), reason == "away" ? "dale" : "erebor");
        if (reason == "no-elite")
            foreach (var unit in state.Units.Where(u => u.Nation == "dwarves" && u.Kind == UnitKind.Elite && u.Location == "reserve"))
                unit.Location = "casualties";
        if (reason is "other-army" or "other-nation")
            foreach (var unit in state.Units.Where(u => u.Location == "erebor" && u.Kind == UnitKind.Regular))
            {
                unit.Garrison = reason == "other-army";
                unit.Nation = reason == "other-nation" ? "north" : unit.Nation;
            }
        Assert.Empty(KingsUpgrades.Options(state, Side.FreePeoples));
        LordsFixture.Reject(state, CommandKind.UseCharacterAbility, new()
        {
            CharacterId = SovereignCatalog.Dain, AbilityId = "kings.dwarvenking", DieId = Die(state).Id, Ids = ["erebor"]
        });
    }
}
