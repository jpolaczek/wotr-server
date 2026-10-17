using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class CasualtyTests
{
    [Theory]
    [InlineData(Side.FreePeoples, CasualtyCause.Event)]
    [InlineData(Side.Shadow, CasualtyCause.Event)]
    [InlineData(Side.FreePeoples, CasualtyCause.Battle)]
    [InlineData(Side.Shadow, CasualtyCause.Battle)]
    public void LastUnitUsesCauseToResolveLeadersAndCharacters(Side side, CasualtyCause cause)
    {
        var state = TurnTests.Actions();
        foreach (var unit in state.Units)
            unit.Location = "reserve";
        var regular = state.Units.First(u => BoardRules.Owner(u) == side && u.Kind == UnitKind.Regular);
        var leader = state.Units.First(u => BoardRules.Owner(u) == side && u.Kind ==
            (side == Side.FreePeoples ? UnitKind.Leader : UnitKind.Nazgul));
        regular.Location = leader.Location = "minas-tirith";
        state.Characters.Add(new() { Id = "character", Side = side, Location = "minas-tirith" });
        var participants = new HashSet<string> { regular.Id, leader.Id, "character" };
        CasualtyRules.Eliminate(state, [regular.Id], cause, participants);
        Assert.Equal(side == Side.FreePeoples ? "casualties" : "reserve", regular.Location);
        Assert.Equal(cause == CasualtyCause.Battle ? "casualties" : "minas-tirith", state.Characters[0].Location);
        Assert.Equal(side == Side.FreePeoples ? "casualties" : cause == CasualtyCause.Battle ? "reserve" : "minas-tirith", leader.Location);
    }

    [Fact]
    public void BattleRearguardNeitherProtectsNorDiesWithAttackingForce()
    {
        var state = TurnTests.Actions();
        var army = state.Units.Where(u => u.Location == "minas-tirith" && BoardRules.IsArmyUnit(u)).ToArray();
        var attacker = army[0];
        state.Characters.Add(new() { Id = "attacking-character", Side = Side.FreePeoples, Location = "minas-tirith" });
        state.Characters.Add(new() { Id = "rear-character", Side = Side.FreePeoples, Location = "minas-tirith" });
        CasualtyRules.Eliminate(state, [attacker.Id], CasualtyCause.Battle, new HashSet<string> { attacker.Id, "attacking-character" });
        Assert.Equal("casualties", state.Characters[0].Location);
        Assert.Equal("minas-tirith", state.Characters[1].Location);
        Assert.All(army.Skip(1), u => Assert.Equal("minas-tirith", u.Location));
    }
}
