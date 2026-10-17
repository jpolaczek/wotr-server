using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsChieftainHuntTests
{
    internal static MatchState Window()
    {
        var state = Actions();
        state.Fellowship.RegionId = "moria";
        KingsDiceTests.Chieftain(state, KingsChieftains.Ugluk, "dimrill-dale");
        Eyes(state, 2);
        Pool(state, HuntTests.Three);
        var die = Die(state);
        die.Face = DieFace.Character;
        return Do(state, CommandKind.MoveFellowship, new() { DieId = die.Id }, 4, 4);
    }

    [Fact]
    public void UglukAddsOneOnlyToSelectedHuntRerollsAndPreservesNaturalResults()
    {
        var state = Window();
        var hunt = HuntRuntime.Active(state)!;
        Assert.Equal(DecisionKind.HuntReroll, state.Pending!.Kind);
        Assert.Equal(0, hunt.EyeDamage);
        Assert.Equal(0, hunt.Bonus);
        Assert.Equal(1, hunt.RerollBonus);
        Assert.All(HuntOptions.View(state)!.Dice, d => Assert.False(d.Success));
        var selected = hunt.Dice[0].Id;
        state = Do(StateCodec.Copy(state), CommandKind.RerollHunt, new() { Ids = [selected] }, 4, 0);
        hunt = HuntRuntime.Active(state)!;
        Assert.Equal(1, hunt.EyeDamage);
        Assert.Equal(3, hunt.Damage);
        Assert.Equal(KingsFixture.SetupSamples + 4, state.RandomSamples.Count);
        var view = HuntOptions.View(state)!;
        Assert.Equal(1, view.RerollBonus);
        Assert.All(view.Dice, d => Assert.Equal(5, d.Result));
        Assert.Single(view.Dice, d => d.Success && d.Rerolled && d.Id == selected);
        Assert.Single(view.Dice, d => !d.Success && !d.Rerolled);
    }

    [Theory]
    [InlineData("moria", true, 1)]
    [InlineData("dimrill-dale", true, 1)]
    [InlineData("dimrill-dale", false, 1)]
    [InlineData("orthanc", true, 0)]
    [InlineData("casualties", true, 0)]
    public void HuntAbilityWorksWithHiddenFellowshipButRequiresAStillPresentUglukNearby(string region, bool hidden, int bonus)
    {
        var state = Actions();
        state.Fellowship.RegionId = "moria";
        state.Fellowship.Hidden = hidden;
        KingsDiceTests.Chieftain(state, KingsChieftains.Ugluk, region);
        Assert.Equal(bonus, KingsChieftainHunt.RerollBonus(state));
    }

    [Fact]
    public void UglukAloneGrantsNoRerollSourceOrExtraRandomness()
    {
        var state = Actions();
        state.Fellowship.RegionId = "hollin";
        KingsDiceTests.Chieftain(state, KingsChieftains.Ugluk, "hollin");
        Eyes(state, 1);
        var die = Die(state);
        die.Face = DieFace.Character;
        state = Do(state, CommandKind.MoveFellowship, new() { DieId = die.Id }, 4);
        Assert.Null(HuntRuntime.Active(state));
        Assert.Equal(KingsFixture.SetupSamples + 1, state.RandomSamples.Count);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        Assert.DoesNotContain(state.Events, e => e.Kind == "HuntRerolled");
        Assert.DoesNotContain(state.Events, e => e.Kind == "HuntTileDrawn");
    }

    [Fact]
    public void PlayerCanDeclineAnImprovedRerollWithoutSpendingRandomness()
    {
        var state = Do(Window(), CommandKind.RerollHunt);
        Assert.Null(HuntRuntime.Active(state));
        Assert.Equal(KingsFixture.SetupSamples + 2, state.RandomSamples.Count);
    }

    [Fact]
    public void NaturalOneStillFailsWithUglukAndEarlierMovementModifiers()
    {
        var state = Actions();
        state.Fellowship.RegionId = "moria";
        KingsDiceTests.Chieftain(state, KingsChieftains.Ugluk, "moria");
        Eyes(state, 1);
        var hunt = new HuntProcedure { RollBonus = 5 };
        var random = new SequenceRandom(0, 0);
        HuntRoll.Roll(state, hunt, random);
        HuntRoll.Reroll(state, hunt, [hunt.Dice[0].Id], random);
        Assert.Equal(0, hunt.EyeDamage);
        Assert.False(HuntRoll.Success(hunt, hunt.Dice[0]));
        Assert.Equal(2, random.Calls);
    }

    [PostgresFact]
    public Task UgluksHuntBonusAndSelectedRerollSurviveRestartWithoutNewEntropy()
    {
        var state = Window();
        return LordsPersistenceTests.Restart(state, CommandKind.RerollHunt,
            new() { Ids = [HuntRuntime.Active(state)!.Dice[0].Id] }, 4, 0);
    }
}
