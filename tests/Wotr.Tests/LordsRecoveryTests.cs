using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsFixture;

namespace Wotr.Tests;

public class LordsRecoveryTests
{
    [Fact]
    public void SetupCreatesNamedRingsAndDormantDiceOnlyOnce()
    {
        var state = Setup();
        Assert.Equal(5, state.Dice.Count(d => d.Special is not null));
        Assert.All(state.Dice.Where(d => d.Special is not null), d => Assert.Equal(DieLocation.Removed, d.Location));
        Assert.Equal(new[] { LordsDiceSetup.VilyaRing, LordsDiceSetup.NenyaRing, LordsDiceSetup.NaryaRing }, state.Rings.Select(r => r.Id));
        Assert.Throws<InvalidOperationException>(() => LordsDiceSetup.Initialize(state));
        Assert.Throws<InvalidOperationException>(() => LordsDiceSetup.Initialize(KernelTests.NewGame()));
    }

    [Theory]
    [InlineData(1, false, true, true)]
    [InlineData(1, false, false, false)]
    [InlineData(2, false, true, false)]
    [InlineData(2, true, true, true)]
    [InlineData(2, true, false, false)]
    public void NaryaRequiresKeeperGuideAtRecoveryAndActualFellowshipMovementExceptFirstTurn(int turn, bool moved, bool keeperGuide, bool added)
    {
        var state = Setup();
        state.Turn = turn;
        var keeper = Put(state, LordsDiceCatalog.KeeperGandalf, "fellowship");
        if (keeperGuide)
            state.Fellowship.Guide = keeper.Id;
        var die = state.Dice[0];
        die.Location = DieLocation.Hunt;
        die.HuntOrigin = moved ? DiceRules.FellowshipMove : LordsDiceCatalog.KeeperEye;
        DiceRules.Recover(state);
        Assert.Equal(added ? DieLocation.Unrolled : DieLocation.Removed, Die(state, LordsDiceCatalog.Narya).Location);
        Assert.Equal(moved, state.MandatoryHuntAllocation);
        state.Fellowship.Guide = keeper.Id;
        Assert.Equal(added ? DieLocation.Unrolled : DieLocation.Removed, Die(state, LordsDiceCatalog.Narya).Location);
    }

    [Theory]
    [InlineData(LordsDiceCatalog.Vilya, "lords.character.elrond", CharacterRules.White, true, true, true)]
    [InlineData(LordsDiceCatalog.Nenya, "lords.character.galadriel", CharacterRules.White, true, true, true)]
    [InlineData(LordsDiceCatalog.Vilya, "lords.character.elrond", CharacterRules.White, false, true, false)]
    [InlineData(LordsDiceCatalog.Vilya, "lords.character.elrond", CharacterRules.White, true, false, false)]
    [InlineData(LordsDiceCatalog.Balrog, "lords.character.balrog", CharacterRules.WitchKing, true, true, true)]
    [InlineData(LordsDiceCatalog.Balrog, "lords.character.balrog", LordsDiceCatalog.ChiefWitchKing, true, true, true)]
    [InlineData(LordsDiceCatalog.Gothmog, "lords.character.gothmog", LordsDiceCatalog.ChiefWitchKing, true, true, true)]
    [InlineData(LordsDiceCatalog.Gothmog, "lords.character.gothmog", LordsDiceCatalog.ChiefWitchKing, false, true, false)]
    [InlineData(LordsDiceCatalog.Balrog, "lords.character.balrog", LordsDiceCatalog.ChiefWitchKing, true, false, false)]
    public void V12OnlyChosenRemoveFacesCheckWhiteOrWitchKingAtRecovery(string id, string source, string condition, bool chosen, bool alive, bool removed)
    {
        var state = Actions();
        Put(state, source, "rivendell");
        Put(state, condition, alive ? "rivendell" : "casualties");
        var die = Die(state, id);
        die.Special!.Chosen = chosen;
        die.Special.RemoveSymbol = true;
        die.Location = chosen ? DieLocation.Spent : DieLocation.Discarded;
        Assert.False(die.Special.PermanentlyRemoved);
        state = StateCodec.Copy(state);
        DiceRules.Recover(state);
        die = Die(state, id);
        Assert.Equal(removed, die.Special!.PermanentlyRemoved);
        Assert.Equal(removed ? DieLocation.Removed : DieLocation.Unrolled, die.Location);
        var view = Views().For(state, Side.FreePeoples).Dice.Single(d => d.Id == id);
        Assert.Equal(removed, view.PermanentlyRemoved);
        Assert.False(view.SpecialDieChosen);
        Assert.False(view.RemoveSymbol);
        Assert.Null(view.Face);
        Put(state, condition, "casualties");
        DiceRules.Recover(state);
        Assert.Equal(removed ? DieLocation.Removed : DieLocation.Unrolled, die.Location);
    }

    [Fact]
    public void V12EnteringOrDyingDuringActionsChangesSpecialDiceOnlyAtRecovery()
    {
        var state = Actions();
        var die = Die(state, LordsDiceCatalog.Nenya);
        Put(state, "lords.character.galadriel", "lorien");
        Assert.Equal(DieLocation.Removed, die.Location);
        DiceRules.Recover(state);
        Assert.Equal(DieLocation.Unrolled, die.Location);
        LordsDiceRoll.Roll(die, new SequenceRandom(2));
        LordsDiceRoll.Select(state, Side.FreePeoples, die.Id);
        Put(state, "lords.character.galadriel", "casualties");
        Assert.Equal(DieLocation.Available, die.Location);
        DiceRules.Recover(state);
        Assert.Equal(DieLocation.Removed, die.Location);
        Assert.True(die.Special!.PermanentlyRemoved);
    }

    [Theory]
    [InlineData(CharacterRules.WitchKing, true)]
    [InlineData(LordsDiceCatalog.ChiefWitchKing, false)]
    public void BlackCaptainRemovesGothmogsDieAtRecoveryWithoutRemovingHisFigure(string witchKing, bool removed)
    {
        var state = Actions();
        var gothmog = Put(state, "lords.character.gothmog", "minas-morgul");
        Put(state, witchKing, "minas-morgul");
        DiceRules.Recover(state);
        Assert.Equal(removed, Die(state, LordsDiceCatalog.Gothmog).Special!.PermanentlyRemoved);
        Assert.Equal("minas-morgul", gothmog.Location);
    }
}
