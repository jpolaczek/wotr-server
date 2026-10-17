using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsSovereignTests
{
    [Fact]
    public void SetupAddsFiveDistinctLeadersAndTwoNumericalKingsTilesWithoutReplacingBaseFigures()
    {
        var plain = KernelTests.NewGame();
        var state = Setup();
        Assert.Equal(plain.Units.Count + 5, state.Units.Count);
        Assert.Equal(5, state.Kings!.Sovereigns.Count);
        Assert.Empty(state.Characters);
        Assert.Empty(state.Kings.Tokens);
        foreach (var definition in SovereignCatalog.All.Values)
        {
            var unit = state.Units.Single(u => u.Id == definition.Id);
            Assert.Equal(UnitKind.Leader, unit.Kind);
            Assert.Equal(definition.Nation, unit.Nation);
            Assert.Equal(definition.Home, unit.Location);
            Assert.Equal(SovereignStatus.Initial, Sovereign(state, unit.Id).Status);
        }
        Assert.Equal(plain.HuntTiles.Count + 2, state.HuntTiles.Count);
        Assert.All(state.HuntTiles.Where(t => KingsTiles.IsKings(t.Id)), t => Assert.Equal("pool", t.Location));
        Assert.Throws<InvalidOperationException>(() => KingsSetup.Initialize(state));
        Assert.Null(plain.Kings);
        Assert.DoesNotContain(plain.Units, u => u.Id.StartsWith("kings."));
    }

    [Theory]
    [InlineData(SovereignCatalog.Brand, "north", "dale", 1, 2)]
    [InlineData(SovereignCatalog.Dain, "dwarves", "erebor", 1, 4)]
    [InlineData(SovereignCatalog.Denethor, "gondor", "minas-tirith", 1, 3)]
    [InlineData(SovereignCatalog.Theoden, "rohan", "edoras", 2, 3)]
    [InlineData(SovereignCatalog.Thranduil, "elves", "woodland-realm", 2, 4)]
    public void AwakeningChangesOneFigureToACompanionReturnsTilesAndGrantsOneToken(string id, string nation, string home, int level, int resistance)
    {
        var state = Actions();
        state.Nations.Single(n => n.Id == nation).Active = true;
        if (nation == "elves")
            state.Nations.Single(n => n.Id == nation).StepsToWar = 0;
        CorruptionRules.Attach(state, Sovereign(state, id), KingsTiles.One);
        var cost = Die(state).Id;
        state = Do(StateCodec.Copy(state), CommandKind.AwakenSovereign, new() { CharacterId = id, DieId = cost, Route = [home] });
        Assert.DoesNotContain(state.Units, u => u.Id == id);
        var character = Assert.Single(state.Characters, c => c.Id == id);
        Assert.True(CharacterRules.IsCompanion(character));
        Assert.Equal(level, character.Level);
        Assert.Equal(1, character.Leadership);
        Assert.Equal(nation, character.NationIcon);
        Assert.Equal(resistance, SovereignCatalog.All[id].Resistance);
        Assert.Equal(SovereignStatus.Awakened, Sovereign(state, id).Status);
        Assert.Empty(Sovereign(state, id).Tiles);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == KingsTiles.One).Location);
        Assert.Single(state.Kings!.Tokens, t => t.SovereignId == id);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == cost).Location);
        Assert.DoesNotContain(CorruptionRules.Targets(state), s => s.Id == id);
    }

    [Fact]
    public void AwakeningRequiresTheNationCostAndLegalRouteRatherThanTeleporting()
    {
        var state = Actions();
        var unit = state.Units.Single(u => u.Id == SovereignCatalog.Dain);
        var payload = new CommandPayload { CharacterId = unit.Id, DieId = Die(state).Id, Route = ["erebor"] };
        LordsFixture.Reject(state, CommandKind.AwakenSovereign, payload);
        state.Nations.Single(n => n.Id == "dwarves").Active = true;
        unit.Location = "iron-hills";
        LordsFixture.Reject(state, CommandKind.AwakenSovereign, payload);
        payload.Route = ["iron-hills", "erebor"];
        Assert.Null(LordsFixture.Engine().Validate(state, Side.FreePeoples, BattleFixture.Command(state, CommandKind.AwakenSovereign, payload)));
        Die(state).Face = DieFace.Character;
        LordsFixture.Reject(state, CommandKind.AwakenSovereign, payload);
        Die(state).Face = DieFace.Muster;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "erebor");
        LordsFixture.Reject(state, CommandKind.AwakenSovereign, payload);
        state.Units.Single(u => u.Id == unit.Id).Location = "erebor";
        state.Units.Single(u => u.Id == unit.Id).Garrison = true;
        payload.Route = ["erebor"];
        state = Do(state, CommandKind.AwakenSovereign, payload);
        Assert.True(state.Characters.Single(c => c.Id == unit.Id).Garrison);
    }

    [Fact]
    public void BrandMayReachAnotherNationsUnconqueredCityButThranduilNeedsWar()
    {
        var state = Actions();
        state.Nations.Single(n => n.Id == "north").Active = true;
        var brand = Sovereign(state, SovereignCatalog.Brand);
        Assert.Contains("erebor", SovereignAwakening.Destinations(state, brand));
        state.Control["erebor"] = Side.Shadow;
        Assert.DoesNotContain("erebor", SovereignAwakening.Destinations(state, brand));
        state.Units.Single(u => u.Id == brand.Id).Location = "dagorlad";
        state.Control["morannon"] = Side.FreePeoples;
        Assert.DoesNotContain("morannon", SovereignAwakening.Destinations(state, brand));
        state.Nations.Single(n => n.Id == "elves").Active = true;
        Assert.Empty(SovereignAwakening.Destinations(state, Sovereign(state, SovereignCatalog.Thranduil)));
        state.Nations.Single(n => n.Id == "elves").StepsToWar = 0;
        Assert.Contains("woodland-realm", SovereignAwakening.Destinations(state, Sovereign(state, SovereignCatalog.Thranduil)));
    }

    [Fact]
    public void LosingTheInitialLeaderReturnsItsTilesWhileCorruptedDoesNotDieWithTheArmy()
    {
        var state = Actions();
        var initial = Sovereign(state, SovereignCatalog.Brand);
        CorruptionRules.Attach(state, initial, KingsTiles.One);
        var army = state.Units.Where(u => u.Location == "dale" && BoardRules.IsArmyUnit(u)).Select(u => u.Id).ToArray();
        CasualtyRules.Eliminate(state, army, CasualtyCause.Event);
        Assert.Equal(SovereignStatus.Removed, initial.Status);
        Assert.Empty(initial.Tiles);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == KingsTiles.One).Location);
        var corrupted = Sovereign(state, SovereignCatalog.Denethor);
        CorruptionRules.Attach(state, corrupted, HuntTests.Three);
        Assert.Equal(SovereignStatus.Corrupted, corrupted.Status);
        Assert.DoesNotContain(state.Units, u => u.Id == corrupted.Id);
        Assert.DoesNotContain(state.Characters, c => c.Id == corrupted.Id);
        army = state.Units.Where(u => u.Location == "minas-tirith" && BoardRules.IsArmyUnit(u)).Select(u => u.Id).ToArray();
        CasualtyRules.Eliminate(state, army, CasualtyCause.Event);
        Assert.Equal(SovereignStatus.Corrupted, corrupted.Status);
        Assert.Equal("minas-tirith", SovereignLifecycle.Region(state, corrupted));
    }

    [Fact]
    public void PlanningArmyDestructionDoesNotReturnTilesUntilThePlanIsApplied()
    {
        var state = Actions();
        var sovereign = Sovereign(state, SovereignCatalog.Brand);
        CorruptionRules.Attach(state, sovereign, KingsTiles.One);
        var target = ArmyTarget.At(state, Side.FreePeoples, "dale")!;
        var choices = target.Units(state).Where(BoardRules.IsArmyUnit)
            .Select(u => new BattleLossChoice { UnitId = u.Id, Kind = BattleLossKind.Remove }).ToArray();
        var before = StateCodec.Write(state);
        var plan = ArmyLossRules.Plan(state, target, 10, choices, CasualtyCause.Battle);
        Assert.NotNull(plan);
        Assert.Equal(before, StateCodec.Write(state));
        ArmyLossRules.ApplyFigures(state, plan);
        Assert.Equal(SovereignStatus.Removed, sovereign.Status);
        Assert.Empty(sovereign.Tiles);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == KingsTiles.One).Location);
    }

    [Theory]
    [InlineData(DieFace.Army)]
    [InlineData(DieFace.Character)]
    [InlineData(DieFace.Event)]
    [InlineData(DieFace.Muster)]
    [InlineData(DieFace.WillOfTheWest)]
    public void AwakeningAcceptsEveryAvailableRulerFace(DieFace face)
    {
        var state = Actions();
        state.Nations.Single(n => n.Id == "dwarves").Active = true;
        var cost = Ruler(state, face).Id;
        state = Do(state, CommandKind.AwakenSovereign, new() { CharacterId = SovereignCatalog.Dain, DieId = cost, Route = ["erebor"] });
        Assert.Equal(SovereignStatus.Awakened, Sovereign(state, SovereignCatalog.Dain).Status);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == cost).Location);
    }
}
