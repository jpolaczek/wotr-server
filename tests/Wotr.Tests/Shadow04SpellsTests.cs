using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards04;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow04Fixture;

namespace Wotr.Tests;

public class Shadow04SpellsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TwoNazgulFiguresRollTwoDiceAndLossesPreserveCompanionsWithoutPoliticalAttack(bool siege)
    {
        var state = State(Spells, siege);
        var companion = Put(state, "base.character.boromir", "minas-tirith");
        companion.Garrison = siege;
        var nation = state.Nations.Single(n => n.Id == "gondor");
        nation.Active = false;
        nation.StepsToWar = 3;
        var defender = state.Units.First(u => u.Location == "minas-tirith" && BoardRules.Owner(u) == Side.FreePeoples);
        foreach (var unit in state.Units.Where(u => u.Location == "minas-tirith" && BoardRules.Owner(u) == Side.FreePeoples && u.Id != defender.Id))
            BoardRules.ReturnToReserve(unit);
        BattleFixture.Put(state, "gondor", UnitKind.Leader, "minas-tirith").Garrison = siege;
        state = Play(state, Spells, 4, 3);
        Assert.Equal(2, state.RandomSamples.Count);
        Assert.Equal(1, EventLosses.Current(state)!.Hits);
        Assert.Null(BattleRuntime.Active(state));
        Assert.Empty(state.PoliticalReactions);
        state = Do(state, CommandKind.TakeEventLosses, new() { Losses = [new() { UnitId = defender.Id }] });
        Assert.Equal("minas-tirith", state.Characters.Single(c => c.Id == companion.Id).Location);
        Assert.False(state.Characters.Single(c => c.Id == companion.Id).Garrison);
        Assert.DoesNotContain(state.Units, u => u.Location == "minas-tirith" && u.Kind == UnitKind.Leader);
        Assert.False(state.Nations.Single(n => n.Id == "gondor").Active);
        Assert.Equal(siege ? 2 : 3, state.Nations.Single(n => n.Id == "gondor").StepsToWar);
        Assert.Equal(siege ? Side.Shadow : Side.FreePeoples, state.Control["minas-tirith"]);
    }

    [Theory]
    [InlineData("minas-tirith", false)]
    [InlineData("osgiliath", true)]
    public void BesiegedSourceCanTargetAdjacentArmyAndFiveDiceIsTheMaximum(string source, bool besieged)
    {
        var state = State(Spells, true);
        foreach (var unit in state.Units.Where(u => BoardRules.Owner(u) == Side.Shadow && u.Location == "minas-tirith"))
        {
            unit.Location = source;
            unit.Garrison = besieged;
        }
        var king = CharacterRules.InPlay(state, CharacterRules.WitchKing)!;
        king.Location = source;
        king.Garrison = besieged;
        for (var i = 0; i < 5; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Nazgul, source).Garrison = besieged;
        state = Play(state, Spells, 0, 1, 2, 3, 0);
        Assert.Equal(5, state.RandomSamples.Count);
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.Events, e => e.Kind == "NationActivated");
    }

    [Fact]
    public void NoArmyNoNazgulOrBlackBorderCannotProvideAnAttack()
    {
        var state = State(Spells);
        foreach (var unit in state.Units.Where(u => BoardRules.Owner(u) == Side.FreePeoples && BoardRules.IsArmyUnit(u)))
            unit.Location = "gorgoroth";
        Reject(state, CommandKind.PlayEvent, EventCost(state, Spells));
        foreach (var unit in state.Units.Where(u => u.Location == "gorgoroth"))
            unit.Location = "minas-tirith";
        foreach (var unit in state.Units.Where(u => u.Kind == UnitKind.Nazgul))
            BoardRules.ReturnToReserve(unit);
        CharacterRules.InPlay(state, CharacterRules.WitchKing)!.Location = "orthanc";
        Reject(state, CommandKind.PlayEvent, EventCost(state, Spells));
        CharacterRules.InPlay(state, CharacterRules.WitchKing)!.Location = "osgiliath";
        foreach (var unit in state.Units.Where(u => u.Location == "osgiliath" && BoardRules.IsArmyUnit(u)))
            BoardRules.ReturnToReserve(unit);
        Reject(state, CommandKind.PlayEvent, EventCost(state, Spells));
    }

    [Fact]
    public void SourceAndTargetChoicesAreIndependentAndInvalidSelectionsDoNotRoll()
    {
        var state = State(Spells);
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "north-ithilien");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "lossarnach");
        BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "lossarnach");
        state = Play(state, Spells);
        Assert.Equal(2, Choices(state).EligibleIds.Count);
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["minas-tirith:field"] });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["osgiliath:field"] });
        Assert.Equal(2, Choices(state).EligibleIds.Count);
        Assert.Empty(state.RandomSamples);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["north-ithilien:field"] }, new SequenceRandom(4, 0));
        Assert.Equal("north-ithilien", EventLosses.Current(state)!.Target.Region);
    }
}
