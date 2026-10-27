using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.FateScenarioDriver;

namespace Wotr.Tests;

// TEST-14: the Fate map scenarios act through the engine: Erebor's capture scores 2 and ends the game, Sauron musters in
// Erebor, Dale fights as a neutral Fortification, the active Dwarves reach war and muster in the Iron Hills City, whose
// capture scores 1 and whose declaration heals, and New Cities make Ered Luin and South Rhûn worth a point.
public class FateBoardScenarioTests
{
    private const string Erebor = "erebor";
    private const string Dale = "dale";
    private const string IronHills = "iron-hills";

    [Fact]
    public void CapturingEreborScoresTwoAndTheVictoryCheckEndsTheGame()
    {
        var state = Create("fate-erebor-victory");
        Assert.Equal(2, MilitaryVictory.Points(state, Side.FreePeoples));
        state = March(state, CommandKind.MoveArmies, IronHills, Erebor);
        Assert.Equal(Side.FreePeoples, state.Control[Erebor]);
        Assert.Equal(4, MilitaryVictory.Points(state, Side.FreePeoples));
        Assert.Equal((Side.FreePeoples, VictoryCondition.FreePeoplesMilitary), (state.Winner, state.Victory));
    }

    [Fact]
    public void SauronMustersInErebor()
    {
        var state = Create("fate-erebor-muster");
        var regulars = Option(state, CommandKind.Recruit).Recruitment.Single(g => g.Nation == "sauron" && g.Kind == UnitKind.Regular);
        Assert.Contains(Erebor, regulars.Targets);
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available && d.Face == DieFace.Muster);
        state = Do(state, CommandKind.Recruit, new()
        {
            DieId = die.Id,
            Placements = [new() { UnitId = regulars.ReserveIds[0], RegionId = Erebor }]
        });
        Assert.Equal(3, state.Units.Count(u => u.Location == Erebor && u.Nation == "sauron"));
        var dwarf = state.Units.First(u => u.Nation == "dwarves" && u.Location == "reserve");
        Assert.Equal(RecruitmentBlock.Settlement, RecruitmentRules.MusterBlocker(state, Side.FreePeoples, dwarf, Erebor));
    }

    [Fact]
    public void TheMouthOfSauronEntersInErebor()
    {
        var state = Create("fate-erebor-muster");
        Assert.Contains(CharacterEntry.Candidates(state, Side.Shadow),
            plan => plan.DefinitionId == CharacterRules.Mouth && plan.Region == Erebor);
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available && d.Face == DieFace.Muster);
        state = Do(state, CommandKind.EnterCharacter, new() { DieId = die.Id, CharacterId = CharacterRules.Mouth, RegionId = Erebor });
        Assert.Equal(Erebor, state.Characters.Single(c => c.DefinitionId == CharacterRules.Mouth).Location);
    }

    [Theory]
    [InlineData(Erebor, 1)]
    [InlineData(Dale, 0)]
    public void RevealingTheFellowshipIntoEreborDrawsOneMoreHuntTile(string region, int tiles)
    {
        var state = Create("fate-erebor-revealed");
        var targets = Option(state, CommandKind.RevealFellowship).Targets;
        Assert.Contains(Erebor, targets);
        Assert.DoesNotContain(IronHills, targets);
        var route = region == Dale ? new List<string> { Dale } : [Dale, region];
        var revealed = Do(state, CommandKind.RevealFellowship, new() { Route = route });
        Assert.Equal(region, revealed.Fellowship.RegionId);
        Assert.Equal(tiles, revealed.Events.Count(e => e.Kind == "HuntTileDrawn" && e.Revision > state.Revision));
    }

    [Theory]
    [InlineData("fate-dale-fortification", Erebor, Dale)]
    [InlineData("fate-new-cities-defence", "north-ered-luin", "ered-luin")]
    public void TheDefenceHitsOnlyOnSixInTheFirstRoundWithoutASiege(string id, string from, string region)
    {
        var state = March(Create(id), CommandKind.AttackArmy, from, region);
        Assert.Equal((DecisionKind.CombatCard, Side.Shadow), (state.Pending!.Kind, state.Pending.Owner));
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal((region, BattleMode.Field), (battle.Region, battle.Mode));
        Assert.Equal(6, BattleRules.HitTarget(state, battle, battle.Attacker));
        Assert.Equal(5, BattleRules.HitTarget(state, battle, battle.Defender));
        Assert.False(StrongholdRules.CanShelter(state, battle));
    }

    [Fact]
    public void DaleBelongsToNoNationAndStaysUncontrolled()
    {
        var state = Create("fate-dale-fortification");
        var dale = BoardRules.Region(state, Dale);
        Assert.Equal(("Fortification", null, 0), (dale.Settlement, dale.Nation, dale.VictoryPoints));
        Assert.Null(state.Control[Dale]);
        Assert.Contains(state.Units, u => u.Location == Dale && u.Nation == "dwarves");
    }

    [Fact]
    public void TheActiveDwarvesGoToWarAndMusterInTheIronHills()
    {
        var state = Create("fate-dwarves-to-war");
        var dice = state.Dice.Where(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Muster)
            .Select(d => d.Id).ToList();
        Assert.Contains("dwarves", Option(state, CommandKind.AdvancePolitics).Targets);
        state = Do(state, CommandKind.AdvancePolitics, new() { DieId = dice[0], NationId = "dwarves" });
        Assert.Equal(0, Nation(state, "dwarves").StepsToWar);
        Assert.Equal((DecisionKind.Action, Side.FreePeoples), (state.Pending!.Kind, state.Pending.Owner));
        var dwarves = Option(state, CommandKind.Recruit).Recruitment.First(g => g.Nation == "dwarves" && g.Kind == UnitKind.Elite);
        Assert.Contains(IronHills, dwarves.Targets);
        Assert.DoesNotContain(Erebor, dwarves.Targets);
        state = Do(state, CommandKind.Recruit, new()
        {
            DieId = dice[1],
            Placements = [new() { UnitId = dwarves.ReserveIds[0], RegionId = IronHills }]
        });
        Assert.Equal(IronHills, state.Units.Single(u => u.Id == dwarves.ReserveIds[0]).Location);
    }

    [Fact]
    public void CapturingTheIronHillsScoresOneForTheShadow()
    {
        var state = Create("fate-iron-hills-captured");
        Assert.Equal(0, MilitaryVictory.Points(state, Side.Shadow));
        state = March(state, CommandKind.MoveArmies, Erebor, IronHills);
        Assert.Equal(Side.Shadow, state.Control[IronHills]);
        Assert.Equal(1, MilitaryVictory.Points(state, Side.Shadow));
    }

    [Fact]
    public void DeclaringTheFellowshipInTheIronHillsHealsOneCorruption()
    {
        var state = Create("fate-iron-hills-sanctuary");
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
        Assert.Equal(2, state.Fellowship.Corruption);
        state = Do(state, CommandKind.DeclareFellowship, new() { Route = [Dale, IronHills] });
        Assert.Equal((IronHills, 1), (state.Fellowship.RegionId, state.Fellowship.Corruption));
        Assert.False(TravelRules.Sanctuary(state, Erebor));
        Assert.False(TravelRules.Sanctuary(state, Dale));
        Assert.False(TravelRules.Sanctuary(state, "ered-luin"));
        Assert.True(TravelRules.Sanctuary(Create("fate-new-cities-defence"), "ered-luin"));
    }

    [Theory]
    [InlineData("fate-new-cities-ered-luin", Side.Shadow, "north-ered-luin", "ered-luin")]
    [InlineData("fate-new-cities-south-rhun", Side.FreePeoples, "east-rhun", "south-rhun")]
    public void ANewCityCapturedScoresOne(string id, Side side, string from, string city)
    {
        var state = Create(id);
        Assert.True(state.Configuration.NewCities);
        Assert.Equal("City", BoardRules.Region(state, city).Settlement);
        Assert.Equal(0, MilitaryVictory.Points(state, side));
        state = March(state, CommandKind.MoveArmies, from, city);
        Assert.Equal(side, state.Control[city]);
        Assert.Equal(1, MilitaryVictory.Points(state, side));
    }
}
