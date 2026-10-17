using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.FellowshipEventCardTests;
using static Wotr.Domain.FreePeoplesCharacterCards03;

namespace Wotr.Tests;

public class ThereAndBackCardTests
{
    [Theory]
    [InlineData("base.character.gimli", "dale")]
    [InlineData("base.character.gimli", "erebor")]
    [InlineData("base.character.legolas", "woodland-realm")]
    public void ExistingCompanionCanTriggerPoliticsWithoutAnyCompanionsLeftToSeparate(string companion, string region)
    {
        var state = BeforeCard(companion, region);
        state.Fellowship.Companions.Clear();
        state.Fellowship.Guide = CharacterRules.Gollum;
        state = Play(state, ThereAndBack);
        AssertPolitics(state, true);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void PoliticsWaitsForSeparationAndGuideAndDoesNotActivateElves()
    {
        var state = BeforeCard("base.character.legolas", "woodland-realm");
        state = Play(state, ThereAndBack);
        AssertPolitics(state, false);
        var option = new FellowshipOptions().GetOptions(state, Side.FreePeoples).Single();
        Assert.Equal(2, option.Travel.Single(t => t.Id == CharacterHuntTests.Merry).Range);
        state = Do(state, CommandKind.SeparateCompanions, new() { Ids = [CharacterHuntTests.Merry], Route = ["rivendell", "fords-of-bruinen"] });
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        AssertPolitics(state, false);
        state = Do(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider });
        AssertPolitics(state, true);
    }

    [Theory]
    [InlineData("dale")]
    [InlineData("erebor")]
    [InlineData("woodland-realm")]
    public void CapturedSettlementDoesNotTriggerCardOrOrdinaryCompanionActivation(string region)
    {
        var state = BeforeCard("base.character.gimli", region);
        state.Control[region] = Side.Shadow;
        PoliticalRules.CompanionArrived(state, region, "free-peoples");
        PoliticalRules.FellowshipDeclared(state, region);
        AssertPolitics(state, false);
        state.Fellowship.Companions.Clear();
        state = Play(state, ThereAndBack);
        AssertPolitics(state, false);
    }

    [Fact]
    public void NewlySeparatedGimliQualifiesButThisCardDoesNotPermitEnteringSiege()
    {
        var state = BeforeCard(CharacterHuntTests.Merry, "dale");
        state.Fellowship.RegionId = "old-forest-road";
        state = Play(state, ThereAndBack);
        state = Do(state, CommandKind.SeparateCompanions, new() { Ids = ["base.character.gimli"], Route = ["old-forest-road", "dale"] });
        state = Do(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider });
        AssertPolitics(state, true);
        state = TurnTests.Actions();
        state.Fellowship.RegionId = "dimrill-dale";
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "lorien");
        state = Play(state, ThereAndBack);
        Reject(state, CommandKind.SeparateCompanions, new() { Ids = ["base.character.gimli"], Route = ["dimrill-dale", "lorien"] });
    }

    [Fact]
    public void NoEligibleFigureStillAllowsSeparationAndNoPolitics()
    {
        var state = BeforeCard(CharacterHuntTests.Merry, "dale");
        state.Fellowship.MordorStep = 0;
        state.Fellowship.RegionId = MordorRules.Track;
        state = Play(state, ThereAndBack);
        state = Do(state, CommandKind.SeparateCompanions, new() { Ids = [.. state.Fellowship.Companions] });
        AssertPolitics(state, false);
        Assert.Equal(CharacterRules.Gollum, state.Fellowship.Guide);
    }

    private static MatchState BeforeCard(string companion, string region)
    {
        var state = TurnTests.Actions();
        Put(state, companion, region);
        foreach (var nation in state.Nations.Where(n => n.Id is "dwarves" or "north" or "elves"))
        {
            nation.Active = false;
            nation.StepsToWar = 2;
        }
        return state;
    }

    private static void AssertPolitics(MatchState state, bool advanced)
    {
        Assert.False(state.Nations.Single(n => n.Id == "elves").Active);
        foreach (var nation in state.Nations.Where(n => n.Id is "dwarves" or "north" or "elves"))
        {
            Assert.Equal(advanced ? 1 : 2, nation.StepsToWar);
            if (nation.Id != "elves")
                Assert.Equal(advanced, nation.Active);
        }
    }
}
