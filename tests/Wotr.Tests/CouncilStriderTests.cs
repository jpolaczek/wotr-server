using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class CouncilStriderTests
{
    internal static MatchState State(int progress)
    {
        var state = CouncilFixture.Actions();
        CouncilFixture.Inside(state, "strider");
        state.Fellowship.Guide = CharacterRules.Strider;
        state.Fellowship.RegionId = "hollin";
        state.Fellowship.Progress = progress;
        return state;
    }

    [Theory]
    [InlineData(0, DieLocation.Hunt)]
    [InlineData(1, DieLocation.FellowshipBox)]
    public void OnlyFirstMovementDieQualifiesUsingProgressBeforeMovement(int progress, DieLocation location)
    {
        var state = CouncilFixture.Move(State(progress));
        Assert.Equal(location, state.Dice[0].Location);
        Assert.Equal(progress + 1, state.Fellowship.Progress);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        state = CouncilFixture.Move(state);
        Assert.Equal(DieLocation.Hunt, state.Dice[1].Location);
    }

    [Fact]
    public void HuntStillOccursAndCasualtyOrGuideChangeDoesNotLoseTheSavedDieDestination()
    {
        var state = State(1);
        state.Dice.First(d => d.Owner == Side.Shadow).Location = DieLocation.Hunt;
        HuntTests.Pool(state, HuntTests.Three);
        state = CouncilFixture.Move(state, 5, 0);
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        Assert.Equal(DieLocation.Resolving, state.Dice[0].Location);
        Assert.Equal(3, HuntRuntime.Active(state)!.Damage);
        state = LordsFixture.Do(StateCodec.Copy(state), CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Guide });
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == CharacterRules.Strider).Location);
        Assert.Equal(DieLocation.FellowshipBox, state.Dice[0].Location);
        Assert.Equal(0, state.Fellowship.Corruption);
    }

    [Fact]
    public void ChangingGuideDoesNotResetFirstMoveAndTheNextTurnDoes()
    {
        var state = State(1);
        state.Fellowship.Guide = CharacterRules.Grey;
        state = CouncilFixture.Move(state);
        Assert.Equal(DieLocation.Hunt, state.Dice[0].Location);
        state.Fellowship.Guide = CharacterRules.Strider;
        TurnFlow.SelectNext(state, Side.FreePeoples);
        state = CouncilFixture.Move(state);
        Assert.Equal(DieLocation.Hunt, state.Dice[1].Location);
        state.Turn++;
        TurnFlow.SelectNext(state, Side.FreePeoples);
        state = CouncilFixture.Move(state);
        Assert.Equal(DieLocation.FellowshipBox, state.Dice[2].Location);
    }

    [Fact]
    public void FellowshipBoxDieRecoversWithoutActivatingNaryaOrMandatoryHunt()
    {
        var state = CouncilFixture.Move(State(1));
        CouncilFixture.Inside(state, "gandalf-grey");
        state.Fellowship.Guide = CharacterRules.Grey;
        state.Turn++;
        DiceRules.Recover(state);
        Assert.Equal(DieLocation.Unrolled, state.Dice[0].Location);
        Assert.False(state.MandatoryHuntAllocation);
        Assert.Equal(DieLocation.Removed, LordsFixture.Die(state, LordsDiceCatalog.Narya).Location);
    }

    [Fact]
    public void VilyaMayKeepTheDieButDoesNotResetStridersFirstMovement()
    {
        var state = State(1);
        LordsCharacterFixture.Put(state, LordsCharacters.Elrond, "rivendell");
        state = CouncilFixture.Move(state);
        Assert.Equal(DecisionKind.KeepActionDie, state.Pending!.Kind);
        state = LordsFixture.Do(state, CommandKind.UseVilya, new() { DieId = state.Dice[0].Id, RingId = LordsDiceSetup.VilyaRing });
        Assert.Equal(DieLocation.Available, state.Dice[0].Location);
        Assert.Equal("", state.Lords!.Council!.FellowshipBoxDieId);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        state = CouncilFixture.Move(state);
        Assert.Equal(DieLocation.Hunt, state.Dice[0].Location);
    }

    [Fact]
    public void AlternateStriderCannotHideWithAnyDieAndTransformsUsingTheSameFigure()
    {
        var state = State(1);
        state.Fellowship.Hidden = false;
        var p = CharacterFixture.Cost(state, DieFace.Muster);
        LordsFixture.Reject(state, CommandKind.HideFellowship, p);
        p = CharacterFixture.Cost(state, DieFace.Character);
        state = LordsFixture.Do(state, CommandKind.HideFellowship, p);
        var strider = state.Characters.Single(c => c.Id == CharacterRules.Strider);
        strider.Location = "minas-tirith";
        state.Fellowship.Companions.Remove(strider.Id);
        state.Fellowship.Guide = CharacterRules.Grey;
        TurnFlow.SelectNext(state, Side.FreePeoples);
        p = CharacterFixture.Cost(state, DieFace.WillOfTheWest);
        p.CharacterId = CharacterRules.Aragorn;
        p.RegionId = "minas-tirith";
        state = LordsFixture.Do(state, CommandKind.EnterCharacter, p);
        Assert.Equal(CharacterRules.Aragorn, state.Characters.Single(c => c.Id == CharacterRules.Strider).DefinitionId);
        Assert.Equal(1, state.Player(Side.FreePeoples).ExtraDiceNextRecovery);
    }
}
