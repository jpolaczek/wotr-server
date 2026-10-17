using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards01;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Strategy01Fixture;

namespace Wotr.Tests;

public class StrategyArmyCardTests
{
    [Fact]
    public void SpiritRequiresTwoArmyNationsAndCannotCountNazgulOrMinion()
    {
        var state = MixedArmy();
        Assert.True(new MixedShadowArmyCondition().CanPlay(state, Side.FreePeoples));
        BoardRules.ReturnToReserve(state.Units.Single(u => u.Location == "orthanc" && u.Kind == UnitKind.Elite));
        Assert.False(new MixedShadowArmyCondition().CanPlay(state, Side.FreePeoples));
        EventTests.GiveCard(state, Side.FreePeoples, SpiritOfMordor);
        var payload = Cost(state, DieFace.Army);
        payload.CardId = SpiritOfMordor;
        payload.CardCost = DieFace.Army;
        Reject(state, CommandKind.PlayEvent, payload);
    }

    [Theory]
    [InlineData(SpiritOfMordor, "orthanc", 5)]
    [InlineData(FaramirsRangers, "osgiliath", 3)]
    [InlineData(FaramirsRangers, "north-ithilien", 3)]
    [InlineData(FaramirsRangers, "south-ithilien", 3)]
    public void ArmyDestroyedLeavesNazgulAndMinionAndDoesNotAdvancePolitics(string card, string region, int dice)
    {
        var state = Play(MixedArmy(region), card, Enumerable.Repeat(5, dice).ToArray());
        var loss = EventLosses.Current(state)!;
        Assert.Equal(CasualtyCause.Event, loss.Cause);
        Assert.Equal(dice, loss.Hits);
        Assert.Equal(dice, state.RandomSamples.Count);
        var choices = loss.Target.Units(state).Where(BoardRules.IsArmyUnit).Select(u => BattleFixture.Loss(u.Id)).ToList();
        state = Do(state, CommandKind.TakeEventLosses, new() { Losses = choices });
        Assert.DoesNotContain(state.Units, u => u.Location == region && BoardRules.IsArmyUnit(u));
        Assert.Contains(state.Units, u => u.Location == region && u.Kind == UnitKind.Nazgul);
        Assert.Equal(region, CharacterRules.InPlay(state, CharacterRules.WitchKing)!.Location);
        Assert.Equal(2, state.Nations.Single(n => n.Id == "isengard").StepsToWar);
        Assert.DoesNotContain(state.Events, e => e.Kind == "PoliticsAdvanced");
    }

    [Fact]
    public void SpiritTargetChoiceRejectsSingleNationAndZeroHitsDoNotOpenLosses()
    {
        var state = MixedArmy();
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "morannon");
        BattleFixture.Put(state, "isengard", UnitKind.Regular, "morannon");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "moria");
        state = Play(state, SpiritOfMordor);
        Assert.Equal(2, state.Pending!.PrivateChoices.Count);
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["moria"] });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["morannon"] }, new SequenceRandom(0, 1, 2, 3, 0));
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(5, state.RandomSamples.Count);
    }

    [Fact]
    public void BesiegedArmyDestructionReleasesSurvivingNazgulAndCapturesStronghold()
    {
        var state = MixedArmy("orthanc");
        foreach (var unit in state.Units.Where(u => u.Location == "orthanc"))
            unit.Garrison = true;
        state.Characters.Single().Garrison = true;
        BattleFixture.Put(state, "rohan", UnitKind.Regular, "orthanc");
        state = Play(state, SpiritOfMordor, 5, 5, 5, 5, 5);
        state = Do(state, CommandKind.TakeEventLosses, new()
        {
            Losses = EventLosses.Current(state)!.Target.Units(state)
            .Where(BoardRules.IsArmyUnit).Select(u => BattleFixture.Loss(u.Id)).ToList()
        });
        Assert.Equal(Side.FreePeoples, state.Control["orthanc"]);
        Assert.False(state.Units.Single(u => u.Location == "orthanc" && u.Kind == UnitKind.Nazgul).Garrison);
        Assert.False(CharacterRules.InPlay(state, CharacterRules.WitchKing)!.Garrison);
    }

    [Theory]
    [InlineData(UnitKind.Regular)]
    [InlineData(UnitKind.Elite)]
    public void RangersRecruitWithAnyFreeArmyEvenWithoutShadowTarget(UnitKind kind)
    {
        var state = BattleFixture.State();
        BattleFixture.Put(state, "rohan", UnitKind.Regular, "osgiliath");
        state = Play(state, FaramirsRangers);
        var id = state.Pending!.PrivateChoices.Single(id => state.Units.Single(u => u.Id == id).Kind == kind);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [id] });
        Assert.Contains(state.Units, u => u.Location == "osgiliath" && u.Nation == "gondor" && u.Kind == kind);
        Assert.Contains(state.Units, u => u.Location == "osgiliath" && u.Nation == "gondor" && u.Kind == UnitKind.Leader);
        Assert.Empty(state.RandomSamples);
    }

    [Fact]
    public void RangersRespectPartialReservesAndResolveWithoutAnyTarget()
    {
        var state = BattleFixture.State();
        state = Play(state, FaramirsRangers);
        Assert.Empty(state.Resolution.Frames);
        Assert.Empty(state.RandomSamples);
        state = BattleFixture.State();
        BattleFixture.Put(state, "rohan", UnitKind.Regular, "osgiliath");
        foreach (var unit in state.Units.Where(u => u.Nation == "gondor" && BoardRules.IsArmyUnit(u)))
            unit.Location = "casualties";
        state = Play(state, FaramirsRangers);
        Assert.Contains(state.Units, u => u.Location == "osgiliath" && u.Kind == UnitKind.Leader);
        Assert.Empty(state.Resolution.Frames);
    }
}
