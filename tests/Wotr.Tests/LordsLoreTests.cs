using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class LordsLoreTests
{
    [Fact]
    public void ActualActionRollResolvesBothSpecialChoicesBeforeOfferingLoreOnce()
    {
        var state = LordsDiceTests.BothSidesRolling();
        state.Player(Side.Shadow).Table.Add(LordsShadowCards.Lore);
        TurnFlow.RollAndBeginActions(state, new SequenceRandom(Enumerable.Repeat(5, 15).ToArray()));
        Assert.Equal(DecisionKind.SpecialDieChoice, state.Pending!.Kind);
        state = LordsFixture.Do(state, CommandKind.ChooseSpecialDie, new() { DieId = LordsDiceCatalog.Gothmog });
        Assert.Equal(DecisionKind.SpecialDieChoice, state.Pending!.Kind);
        state = LordsFixture.Do(state, CommandKind.ChooseSpecialDie, new() { DieId = LordsDiceCatalog.Nenya });
        Assert.Equal("DieAndResult", state.Pending!.Presentation);
        Assert.Null(LordsFixture.Views().For(state, Side.FreePeoples).Decision);
        state = LordsCardsFixture.Choose(state, LordsDiceCatalog.Gothmog, "lore.Army.remove");
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        Assert.True(LordsFixture.Die(state, LordsDiceCatalog.Gothmog).Special!.RemoveSymbol);
    }

    internal static MatchState Window()
    {
        var state = LordsShadowCardsFixture.State();
        state.Player(Side.Shadow).Table.Add(LordsShadowCards.Lore);
        state.Phase = GamePhase.ActionRoll;
        var keeper = LordsFixture.Die(state, LordsDiceCatalog.Nenya);
        keeper.Face = DieFace.Eye;
        keeper.Location = DieLocation.Hunt;
        keeper.HuntOrigin = LordsDiceCatalog.KeeperEye;
        var shadow = state.Dice.First(d => d.Owner == Side.Shadow && d.Source == "standard");
        shadow.Face = DieFace.Eye;
        shadow.Location = DieLocation.Hunt;
        shadow.HuntOrigin = "rolled-eye";
        TurnFlow.BeginActions(state);
        new EffectRuntime(EffectCatalog.All).Continue(state, new SequenceRandom());
        return state;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChooseDieAndResultTogetherOrDeclineBeforeFreePeoplesFirstAction(bool decline)
    {
        var state = Window();
        Assert.Equal(GamePhase.ActionRoll, state.Phase);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.HuntOrigin == "rolled-eye");
        var id = die.Id;
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [id] });
        state = LordsCardsFixture.Choose(state, decline ? [LoreOfTheRings.Decline] : [id, "lore.Character.keep"]);
        Assert.Equal(decline ? DieLocation.Hunt : DieLocation.Available, state.Dice.Single(d => d.Id == id).Location);
        Assert.Equal(GamePhase.ActionResolution, state.Phase);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Contains(LordsShadowCards.Lore, state.Player(Side.Shadow).Table);
        Assert.Empty(state.Resolution.PaidDieIds);
    }

    [Fact]
    public void LesserMinionCanChangeToItsPrintedFaceAndRemoveSymbolChangesWithTheFace()
    {
        var state = Window();
        var die = LordsFixture.Die(state, LordsDiceCatalog.Gothmog);
        die.Special!.Chosen = true;
        die.Special.RemoveSymbol = true;
        die.Face = DieFace.Eye;
        die.Location = DieLocation.Hunt;
        die.HuntOrigin = LordsDiceCatalog.LesserEye;
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [die.Id, "lore.Character.keep"] });
        state = LordsCardsFixture.Choose(state, die.Id, "lore.Army.keep");
        die = LordsFixture.Die(state, die.Id);
        Assert.Equal(DieFace.Army, die.Face);
        Assert.Equal(DieLocation.Available, die.Location);
        Assert.True(die.Special!.Chosen);
        Assert.False(die.Special.RemoveSymbol);
    }

    [Fact]
    public void AllocatedEyesForeignDiceAndUnselectedLesserDiceCannotBeChanged()
    {
        var state = Window();
        var allocated = state.Dice.First(d => d.Owner == Side.Shadow && d.HuntOrigin == "rolled-eye");
        allocated.HuntOrigin = "allocated";
        foreach (var id in new[] { allocated.Id, LordsDiceCatalog.Nenya, LordsDiceCatalog.Balrog })
            LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [id, "lore.Army.keep"] });
    }

    [Fact]
    public void NoKeeperEyeMeansNoTriggerAndMultipleEyesStillGiveOneChange()
    {
        var state = LordsShadowCardsFixture.State();
        state.Player(Side.Shadow).Table.Add(LordsShadowCards.Lore);
        state.Phase = GamePhase.ActionRoll;
        state.Pending = null;
        TurnFlow.BeginActions(state);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        state = Window();
        var other = LordsFixture.Die(state, LordsDiceCatalog.Vilya);
        other.Face = DieFace.Eye;
        other.Location = DieLocation.Discarded;
        state = LordsCardsFixture.Choose(state, LoreOfTheRings.Decline);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        Assert.Equal(state.Turn, state.Lords!.LoreOfferedTurn);
    }
}
