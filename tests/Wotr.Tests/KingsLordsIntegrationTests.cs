using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.KingsIntegrationFixture;

namespace Wotr.Tests;

public class KingsLordsIntegrationTests
{
    [Theory]
    [InlineData(LordsDiceCatalog.Vilya, Side.FreePeoples)]
    [InlineData(LordsDiceCatalog.Nenya, Side.FreePeoples)]
    [InlineData(LordsDiceCatalog.Narya, Side.FreePeoples)]
    [InlineData(LordsDiceCatalog.Balrog, Side.Shadow)]
    [InlineData(LordsDiceCatalog.Gothmog, Side.Shadow)]
    public void SpecialEyesPayCorruptionAndRemainOwnedButArePlacedWithFreePeoplesUsedDice(string id, Side owner)
    {
        var state = Actions();
        KingsFixture.Eyes(state, 2);
        SpecialEye(state, id);
        KingsFixture.Pool(state, KingsTiles.One);
        Assert.Equal(3, CorruptionRules.Eyes(state).Count());
        var next = KingsFixture.Do(state, CommandKind.AttemptCorruption,
            new() { CharacterId = SovereignCatalog.Thranduil, DieId = id }, 0);
        var die = LordsFixture.Die(next, id);
        Assert.Equal(owner, die.Owner);
        Assert.Equal(Side.FreePeoples, die.SpentWith);
        Assert.Equal(DieLocation.Spent, die.Location);
        Assert.True(die.Special!.Chosen);
        Assert.True(die.Special.RemoveSymbol);
        Assert.Empty(die.HuntOrigin);
        Assert.Equal(2, CorruptionRules.Eyes(next).Count());
        var view = LordsFixture.Views().For(next, Side.Shadow).Dice.Single(d => d.Id == id);
        Assert.Equal(Side.FreePeoples, view.SpentWith);
        Assert.Equal(owner, view.Owner);
        var source = CharacterCatalog.Lords[LordsDiceCatalog.Definitions[id].CharacterId];
        var character = new CharacterState { Id = source.Id, Location = "moria" };
        CharacterLifecycle.ApplyVersion(character, source);
        next.Characters.Add(character);
        LordsFixture.Put(next, owner == Side.FreePeoples ? CharacterRules.White : LordsCharacters.WitchKing, "minas-morgul");
        DiceRules.Recover(next);
        Assert.Null(die.SpentWith);
        if (id != LordsDiceCatalog.Narya)
        {
            Assert.Equal(DieLocation.Removed, die.Location);
            Assert.True(die.Special.PermanentlyRemoved);
        }
        Assert.False(next.MandatoryHuntAllocation);
    }

    [Fact]
    public void RulerEyeCountsButCaptainDiceNeverPayOrIncreaseDrawLimit()
    {
        var state = Actions();
        KingsFixture.Eyes(state, 2);
        var ruler = KingsDice.Ruler(state, Side.Shadow)!;
        ruler.Face = DieFace.Eye;
        ruler.Location = DieLocation.Hunt;
        ruler.HuntOrigin = "rolled-eye";
        var captain = KingsFixture.Die(state, Side.Shadow);
        captain.Face = DieFace.Character;
        state.Player(Side.Shadow).Table.Add(LordsShadowCards.Captain);
        CaptainOfDespair.RecordCost(state, captain);
        Assert.True(CaptainOfDespair.CompleteCost(state, captain));
        Assert.Equal(3, CorruptionRules.Eyes(state).Count());
        LordsFixture.Reject(state, CommandKind.AttemptCorruption,
            new() { CharacterId = SovereignCatalog.Thranduil, DieId = captain.Id });
        KingsFixture.Pool(state, KingsCorruptionTests.One);
        state = KingsFixture.Do(state, CommandKind.AttemptCorruption,
            new() { CharacterId = SovereignCatalog.Thranduil, DieId = ruler.Id }, 0);
        Assert.Equal(2, CorruptionRuntime.Current(state)!.DrawLimit);
        Assert.Equal(DieLocation.Hunt, LordsFixture.Die(state, captain.Id).Location);
        Assert.DoesNotContain(captain.Id, CorruptionRules.Eyes(state).Select(d => d.Id));
    }

    [Theory]
    [InlineData(0, Side.Shadow)]
    [InlineData(3, Side.FreePeoples)]
    [InlineData(4, Side.FreePeoples)]
    public void ActualRollUsesLordsOrderThenKingsShadowThenFreePeoplesThenDismay(int allocation, Side first)
    {
        var state = Rolling(allocation);
        Assert.Equal(first, state.Pending!.Owner);
        Assert.Equal(DecisionKind.SpecialDieChoice, state.Pending.Kind);
        foreach (var side in new[] { first, TurnFlow.Other(first) })
        {
            Assert.Equal(side, state.Pending!.Owner);
            var choices = LordsDiceRoll.LegalChoices(state, side);
            state = KingsFixture.Do(StateCodec.Copy(state), CommandKind.ChooseSpecialDie, new() { DieId = choices[0] });
        }
        Assert.Equal(DecisionKind.RulerDiscard, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        var shadow = KingsDice.DiscardCandidates(state, Side.Shadow).First(id => id != KingsDice.Shadow);
        Assert.DoesNotContain(LordsDiceCatalog.Gothmog, KingsDice.DiscardCandidates(state, Side.Shadow));
        state = KingsFixture.Do(StateCodec.Copy(state), CommandKind.DiscardRulerDie, new() { DieId = shadow });
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        var fp = KingsDice.DiscardCandidates(state, Side.FreePeoples).First(id => id != KingsDice.FreePeoples);
        Assert.DoesNotContain(LordsDiceCatalog.Nenya, KingsDice.DiscardCandidates(state, Side.FreePeoples));
        state = KingsFixture.Do(StateCodec.Copy(state), CommandKind.DiscardRulerDie, new() { DieId = fp });
        Assert.Equal(DecisionKind.Dismay, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        Assert.Equal(2, state.Dice.Count(d => d.Special?.Chosen == true && d.Location == DieLocation.Hunt));
        Assert.Empty(state.Kings!.RulerDiscardOrder);
        Assert.Empty(state.Lords!.ChoiceOrder);
        state = KingsFixture.Do(state, CommandKind.DeclineDismay);
        Assert.Equal(GamePhase.ActionResolution, state.Phase);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
    }

    internal static MatchState Rolling(int allocation = 0)
    {
        var state = Actions();
        foreach (var die in state.Dice.Where(d => d.Location == DieLocation.Available ||
            d.Source is LordsDiceCatalog.Vilya or LordsDiceCatalog.Nenya or LordsDiceCatalog.Balrog or LordsDiceCatalog.Gothmog or SovereignCatalog.RulerSource))
            die.Location = DieLocation.Unrolled;
        foreach (var id in DiceRules.HuntCandidates(state).Take(allocation))
        {
            var die = LordsFixture.Die(state, id);
            die.Location = DieLocation.Hunt;
            die.Face = DieFace.Eye;
            die.HuntOrigin = "allocated";
        }
        state.Kings!.Tokens.Add(new() { SovereignId = SovereignCatalog.Brand, Location = KingsDismay.Location });
        var samples = state.Dice.Where(d => d.Location == DieLocation.Unrolled).Select(d => d.Special is null ? 0 : 5).ToArray();
        TurnFlow.RollAndBeginActions(state, new SequenceRandom(samples));
        return state;
    }
}
