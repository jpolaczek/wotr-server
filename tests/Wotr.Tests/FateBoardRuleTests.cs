using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class FateBoardRuleTests
{
    [Theory]
    [InlineData("erebor", false, Side.FreePeoples, 2)]
    [InlineData("iron-hills", false, Side.Shadow, 1)]
    [InlineData("dale", false, Side.Shadow, 0)]
    [InlineData("ered-luin", false, Side.Shadow, 0)]
    [InlineData("ered-luin", true, Side.Shadow, 1)]
    [InlineData("south-rhun", false, Side.FreePeoples, 0)]
    [InlineData("south-rhun", true, Side.FreePeoples, 1)]
    public void OnlyEnemyConquestScoresAndRecaptureRemovesThosePoints(string region, bool cities, Side actor, int points)
    {
        var state = FateFixture.Setup(cities);
        Assert.Equal(0, MilitaryVictory.Points(state, Side.Shadow));
        Assert.Equal(0, MilitaryVictory.Points(state, Side.FreePeoples));
        FateFixture.Clear(state, region);
        TerritoryRules.Capture(state, actor, region);
        Assert.Equal(points, MilitaryVictory.Points(state, actor));
        if (region == "dale")
            Assert.Null(state.Control[region]);
        TerritoryRules.Capture(state, TurnFlow.Other(actor), region);
        Assert.Equal(0, MilitaryVictory.Points(state, actor));
        Assert.Equal(0, MilitaryVictory.Points(state, TurnFlow.Other(actor)));
    }

    [Theory]
    [InlineData("sauron", "erebor", true)]
    [InlineData("dwarves", "erebor", false)]
    [InlineData("north", "dale", false)]
    [InlineData("dwarves", "iron-hills", true)]
    public void RecruitmentUsesTheNewNationAndSettlementEvenWhenControlledByTheRecruitingSide(string nation, string region, bool legal)
    {
        var side = BoardRules.Nations[nation].Side;
        var state = FateFixture.Actions(side);
        state.Nations.Single(n => n.Id == nation).StepsToWar = 0;
        FateFixture.Clear(state, region);
        state.Control[region] = side;
        var reserve = state.Units.First(u => u.Nation == nation && u.Kind == UnitKind.Regular && u.Location == "reserve");
        var payload = new CommandPayload
        {
            DieId = KingsFixture.Die(state, side).Id,
            Placements = [new() { UnitId = reserve.Id, RegionId = region }]
        };
        Assert.Equal(legal, RecruitmentRules.CanPlaceMuster(state, side, reserve, region));
        Assert.Equal(legal, new PreviewService(LordsFixture.Engine()).Preview(state, side,
            BattleFixture.Command(state, CommandKind.Recruit, payload)).Legal);
        if (legal)
        {
            var next = KingsFixture.Do(state, CommandKind.Recruit, payload);
            Assert.Equal(region, next.Units.Single(u => u.Id == reserve.Id).Location);
        }
        else
            LordsFixture.Reject(state, CommandKind.Recruit, payload);
    }

    [Fact]
    public void NeutralDaleDoesNotBlockPeacefulNationalMovementAndDoesNotChangeControl()
    {
        var state = FateFixture.Actions();
        var units = state.Units.Where(u => u.Location == "iron-hills").ToArray();
        Assert.True(state.Nations.Single(n => n.Id == "dwarves").StepsToWar > 0);
        var die = KingsFixture.Die(state);
        die.Face = DieFace.Army;
        var payload = new CommandPayload { DieId = die.Id, MovementCost = DieFace.Army,
            Moves = [new() { From = "iron-hills", To = "dale", UnitIds = units.Select(u => u.Id).ToList() }] };
        var before = StateCodec.Write(state);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.FreePeoples,
            BattleFixture.Command(state, CommandKind.MoveArmies, payload));
        Assert.True(preview.Legal);
        Assert.Empty(preview.ControlChanges);
        Assert.Equal(before, StateCodec.Write(state));
        state = KingsFixture.Do(state, CommandKind.MoveArmies, payload);
        Assert.All(units, u => Assert.Equal("dale", state.Units.Single(n => n.Id == u.Id).Location));
        Assert.Null(state.Control["dale"]);
    }

    [Fact]
    public void SauronMayEnterItsNewHomelandAtPeaceAndTheMovementCopyPreservesFate()
    {
        var state = FateFixture.Actions(Side.Shadow);
        state.Nations.Single(n => n.Id == "sauron").StepsToWar = 1;
        var unit = BattleFixture.Put(state, "sauron", UnitKind.Regular, "dale");
        var die = KingsFixture.Die(state, Side.Shadow);
        die.Face = DieFace.Army;
        var payload = new CommandPayload { DieId = die.Id, MovementCost = DieFace.Army,
            Moves = [new() { From = "dale", To = "erebor", UnitIds = [unit.Id] }] };
        Assert.NotNull(MovementRules.Plan(state, Side.Shadow, payload));
        state = KingsFixture.Do(state, CommandKind.MoveArmies, payload);
        Assert.Equal("erebor", state.Units.Single(u => u.Id == unit.Id).Location);
        Assert.Equal(0, MilitaryVictory.Points(state, Side.Shadow));
        var dwarf = state.Units.First(u => u.Location == "iron-hills");
        Assert.False(MovementRules.CanCrossNationalBorder(state, dwarf, "erebor"));
    }

    [Theory]
    [InlineData("dale", false, 6)]
    [InlineData("iron-hills", false, 6)]
    [InlineData("ered-luin", false, 5)]
    [InlineData("ered-luin", true, 6)]
    [InlineData("south-rhun", false, 5)]
    [InlineData("south-rhun", true, 6)]
    public void CityAndFortificationDefenceAppliesOnlyToTheFirstFieldRound(string region, bool cities, int target)
    {
        var state = FateFixture.Setup(cities);
        var battle = new BattleState { Region = region, Round = 1, Mode = BattleMode.Field };
        Assert.Equal(target, BattleRules.HitTarget(state, battle, battle.Attacker));
        Assert.Equal(5, BattleRules.HitTarget(state, battle, battle.Defender));
        Assert.False(StrongholdRules.CanShelter(state, battle));
        battle.Round = 2;
        Assert.Equal(5, BattleRules.HitTarget(state, battle, battle.Attacker));
    }

    [Fact]
    public void EreborUsesShadowHuntsAndEntriesWhileIronHillsIsNowASanctuary()
    {
        var state = FateFixture.Setup();
        Assert.Equal(new[] { "erebor" }, TravelRules.AdditionalHunts(state, ["dale", "erebor", "iron-hills"]));
        Assert.Contains("erebor", CharacterEntry.SauronStrongholds(state));
        Assert.True(TravelRules.Sanctuary(state, "iron-hills"));
        Assert.False(TravelRules.Sanctuary(state, "dale"));
        Assert.False(TravelRules.Sanctuary(state, "erebor"));
        state.Fellowship.RegionId = "dale";
        Assert.True(new FellowshipOutsideFreeSettlement().CanPlay(state, Side.Shadow));
        state.Fellowship.RegionId = "erebor";
        Assert.True(new FellowshipOutsideFreeSettlement().CanPlay(state, Side.Shadow));
        FateFixture.Clear(state, "erebor");
        TerritoryRules.Capture(state, Side.FreePeoples, "erebor");
        Assert.Empty(TravelRules.AdditionalHunts(state, ["erebor"]));
        Assert.False(TravelRules.Sanctuary(state, "erebor"));
    }

    [Fact]
    public void WhiteHandAndFateOverlaysComposeWithoutChangingConquestPoints()
    {
        var state = KingsBoardTests.Captured();
        state.Configuration.Fate = true;
        state.Configuration.NewCities = true;
        state.Control["erebor"] = Side.Shadow;
        Assert.Equal("sauron", BoardRules.Region(state, "erebor").Nation);
        Assert.Equal("isengard", BoardRules.Region(state, "helms-deep").Nation);
        Assert.Equal(6, BoardProjection.Overrides(state).Count);
        Assert.Equal(2, MilitaryVictory.Points(state, Side.Shadow));
    }

    [PostgresFact]
    public Task RecruitmentPersistsMapOverridesAndPoliticalStateForBothPlayersAcrossRestartAndRetry()
    {
        var state = FateFixture.Actions(Side.Shadow, cities: true);
        state.Nations.Single(n => n.Id == "sauron").StepsToWar = 0;
        var regular = state.Units.First(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == "reserve");
        return LordsPersistenceTests.Restart(state, CommandKind.Recruit, new()
        {
            DieId = KingsFixture.Die(state, Side.Shadow).Id,
            Placements = [new() { UnitId = regular.Id, RegionId = "erebor" }]
        });
    }
}
