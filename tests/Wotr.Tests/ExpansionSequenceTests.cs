using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.ActionSequenceFixture;

namespace Wotr.Tests;

public class ExpansionSequenceTests
{
    [Fact]
    public void MessengerPaysOnceAndItsSecondArmyStepDoesNotRequireTheAbilityAgain()
    {
        var state = Board(Side.Shadow);
        CharacterFixture.Put(state, CharacterRules.Mouth, "barad-dur");
        var first = Put(state, "sauron", "osgiliath").Id;
        var second = Put(state, "sauron", "south-ithilien").Id;
        var payload = Move(state, "osgiliath", "minas-tirith", first);
        state.Dice.Single(d => d.Id == payload.DieId).Face = DieFace.Muster;
        payload.AbilityId = CharacterRules.MouthArmy;
        state = Do(state, CommandKind.MoveArmies, payload);
        Assert.Equal(state.Turn, state.Characters.Single(c => c.Id == CharacterRules.Mouth).LastArmyConversionTurn);
        payload = Move(state, "south-ithilien", "osgiliath", second);
        payload.AbilityId = CharacterRules.MouthArmy;
        state = Do(StateCodec.Copy(state), CommandKind.MoveArmies, payload);
        Assert.Single(state.Events, e => e.Kind == "CharacterAbilityUsed");
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Fact]
    public void GreatSorceryRetainsItsOneArmyLimitAndRequiredMouth()
    {
        var state = LordsCharacterFixture.State(Side.Shadow, emptyBoard: true);
        LordsCharacterFixture.War(state, "sauron");
        var mouth = LordsCharacterFixture.Put(state, LordsCharacters.Mouth, "osgiliath", CharacterRules.Mouth);
        var unit = Put(state, "sauron", "osgiliath").Id;
        var payload = Move(state, "osgiliath", "south-ithilien", unit);
        payload.AbilityId = LordsCharacters.GreatSorcery;
        payload.MovementCost = DieFace.Character;
        state.Dice.Single(d => d.Id == payload.DieId).Face = DieFace.Muster;
        Reject(state, CommandKind.MoveArmies, payload);
        payload.Moves[0].CharacterIds.Add(mouth.Id);
        state = Do(state, CommandKind.MoveArmies, payload);
        Assert.Null(state.Resolution.ActionSequence);
        Assert.Equal(state.Turn, state.Characters.Single(c => c.Id == mouth.Id).LastArmyConversionTurn);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == payload.DieId).Location);
    }

    [Fact]
    public void VilyaWaitsForTheWholeSequenceAndCanKeepItsSinglePaidDie()
    {
        var state = LordsVilyaTests.Ready();
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        var unitId = Put(state, "gondor", "minas-tirith").Id;
        state.Dice[0].Face = DieFace.Army;
        state = Do(state, CommandKind.MoveArmies, Move(state, "minas-tirith", "osgiliath", unitId));
        Assert.Equal(DecisionKind.ActionSequence, state.Pending!.Kind);
        Assert.Null(state.Lords!.ActionCompletion);
        state = Do(StateCodec.Copy(state), CommandKind.FinishActionSequence);
        Assert.Equal(DecisionKind.KeepActionDie, state.Pending!.Kind);
        Assert.Null(state.Resolution.ActionSequence);
        state = Do(StateCodec.Copy(state), CommandKind.UseVilya, new() { DieId = state.Dice[0].Id, RingId = LordsDiceSetup.VilyaRing });
        Assert.Equal(DieLocation.Available, state.Dice[0].Location);
        Assert.Single(state.Events, e => e.Kind == "ArmyMoved");
        Assert.Equal(Side.Shadow, state.Rings.Single(r => r.Id == LordsDiceSetup.VilyaRing).Owner);
    }

    [Fact]
    public void FarSightedAndItsSpentRulerTokenSurviveBetweenCompanionGroups()
    {
        var state = KingsFarSightedTests.Ready();
        var ruler = KingsFixture.Ruler(state, DieFace.Event);
        var merry = CharacterFixture.Put(state, "base.character.merry", "rivendell").Id;
        var pippin = CharacterFixture.Put(state, "base.character.pippin", "rivendell").Id;
        var payload = TravelSequenceTests.Travel(state, merry, "rivendell", "trollshaws", "hollin");
        payload.DieId = ruler.Id;
        payload.AbilityId = KingsFarSighted.Ability;
        payload.RulerTokenId = KingsRulerTokens.Id(SovereignCatalog.Thranduil);
        state = Do(state, CommandKind.MoveCompanions, payload);
        state = StateCodec.Copy(state);
        Assert.Equal("removed", state.Kings!.Tokens.Single().Location);
        Assert.Equal(SovereignCatalog.Thranduil, state.Kings.TokenActionSovereign);
        var option = ActionSequenceOptions.For(state, Side.FreePeoples).Single(o => o.Kind == CommandKind.MoveCompanions);
        Assert.Equal(2, option.Travel.Single(t => t.Id == pippin).Range);
        payload = TravelSequenceTests.Travel(state, pippin, "rivendell", "trollshaws", "hollin");
        var invalid = TravelSequenceTests.Travel(state, pippin, "rivendell", "trollshaws", "hollin");
        invalid.AbilityId = "";
        Reject(state, CommandKind.MoveCompanions, invalid);
        state = Do(state, CommandKind.MoveCompanions, payload);
        state = Do(state, CommandKind.FinishActionSequence); // Thranduil deliberately stays in place.
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == ruler.Id).Location);
        Assert.Single(state.Events, e => e.Kind == "RulerTokenUsed");
        Assert.Empty(state.Kings!.TokenActionSovereign);
    }

    [Fact]
    public void BalrogResultStillMovesOnlyTheBalrogAndEndsAfterHisGroup()
    {
        var state = LordsCharacterFixture.State(Side.Shadow, emptyBoard: true);
        var balrog = LordsCharacterFixture.Put(state, LordsCharacters.Balrog, "moria");
        balrog.Level = 2;
        var gothmog = LordsCharacterFixture.Put(state, LordsCharacters.Gothmog, "moria");
        var die = LordsBalrogActionTests.BalrogDie(state);
        var payload = TravelSequenceTests.Travel(state, balrog.Id, "moria", "dimrill-dale");
        payload.DieId = die.Id;
        payload.AbilityId = LordsArmyCosts.UseBalrog;
        payload.CompanionMoves[0].Ids.Add(gothmog.Id);
        Reject(state, CommandKind.MoveShadowCharacters, payload);
        payload.CompanionMoves[0].Ids.Remove(gothmog.Id);
        state = Do(state, CommandKind.MoveShadowCharacters, payload);
        Assert.Null(state.Resolution.ActionSequence);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == die.Id).Location);
        Assert.Equal("moria", state.Characters.Single(c => c.Id == gothmog.Id).Location);
    }
}
