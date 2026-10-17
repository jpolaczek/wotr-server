using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards01;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Strategy01Fixture;

namespace Wotr.Tests;

public class LastBattleCardTests
{
    [Theory]
    [InlineData("fangorn", true)]
    [InlineData("osgiliath", true)]
    [InlineData("orthanc", true)]
    [InlineData("minas-tirith", false)]
    [InlineData("erebor", false)]
    public void RequiresAragornWithArmyOutsideFreeNationRegardlessOfControl(string region, bool legal)
    {
        var state = KingArmy(region);
        state.Control[region] = Side.Shadow;
        Assert.Equal(legal, new LastBattleCondition().CanPlay(state, Side.FreePeoples));
        state.Characters.Single().Garrison = true;
        Assert.False(new LastBattleCondition().CanPlay(state, Side.FreePeoples));
        state.Characters.Single().Garrison = false;
        CharacterLifecycle.ApplyVersion(state.Characters.Single(), CharacterCatalog.Base[CharacterRules.Strider]);
        Assert.False(new LastBattleCondition().CanPlay(state, Side.FreePeoples));
    }

    [Fact]
    public void AlreadyRevealedFellowshipDoesNotPreventPlayAndConditionWaitsForEndOfAction()
    {
        var state = KingArmy();
        state.Fellowship.Hidden = false;
        state = Play(state, LastBattle);
        Assert.Contains(LastBattle, state.Player(Side.FreePeoples).Table);
        state.Characters.Single().Location = "edoras";
        ActiveEffects.CheckConditions(state);
        Assert.Contains(LastBattle, state.Player(Side.FreePeoples).Table);
        state.Characters.Single().Location = "fangorn";
        ActiveEffects.EndAction(state);
        Assert.Contains(LastBattle, state.Player(Side.FreePeoples).Table);
        state.Characters.Single().Location = "casualties";
        ActiveEffects.EndAction(state);
        Assert.Contains(LastBattle, state.Player(Side.FreePeoples).Discard);
    }

    [Fact]
    public void ActualMoveSpendsDieWithoutIncreasingHuntAndDoesNotForceNextTurnsAllocation()
    {
        var state = Play(KingArmy(), LastBattle);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        var payload = Cost(state, DieFace.Character);
        state = Do(state, CommandKind.MoveFellowship, payload);
        Assert.Equal(1, state.Fellowship.Progress);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == payload.DieId).Location);
        Assert.DoesNotContain(state.Dice, d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Hunt);
        DiceRules.Recover(state);
        Assert.False(state.MandatoryHuntAllocation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RevelationDiscardsBeforeResolvingMovementDie(bool mordor)
    {
        var state = Play(KingArmy(), LastBattle);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        var payload = Cost(state, DieFace.Character);
        DiceRules.Pay(state, Side.FreePeoples, payload.DieId, DieFace.Character);
        if (mordor)
        {
            state.Fellowship.MordorStep = 0;
            state.Fellowship.RegionId = MordorRules.Track;
            // The shared hook is used by HuntRuntime on the Mordor Track too.
            ActiveEffects.FellowshipLocated(state);
            state.Fellowship.Hidden = false;
        }
        else
            FellowshipFlow.BeginReveal(state, "test.reveal");
        Assert.DoesNotContain(LastBattle, state.Player(Side.FreePeoples).Table);
        DiceRules.CompleteCost(state, Side.FreePeoples, true);
        Assert.Equal(DieLocation.Hunt, state.Dice.Single(d => d.Id == payload.DieId).Location);
    }

    [Fact]
    public void DeclarationRemovesProtectionEvenWhenItStaysInSameRegion()
    {
        var state = Play(KingArmy(), LastBattle);
        state.Phase = GamePhase.Fellowship;
        TurnFlow.OpenFellowshipPhase(state);
        state = Do(state, CommandKind.DeclareFellowship, new() { Route = [state.Fellowship.RegionId] });
        Assert.DoesNotContain(LastBattle, state.Player(Side.FreePeoples).Table);
    }
}
