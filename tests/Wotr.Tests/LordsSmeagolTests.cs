using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsFixture;

namespace Wotr.Tests;

public class LordsSmeagolTests
{
    internal const string FirstTile = "lords.hunt.smeagol.1";
    internal const string SecondTile = "lords.hunt.smeagol.2";

    internal static MatchState State(bool present = false)
    {
        var state = Setup();
        SmeagolTiles.Initialize(state);
        state.Turn = 2;
        state.Phase = GamePhase.ActionResolution;
        state.Fellowship.RegionId = "hollin";
        foreach (var die in state.Dice.Where(d => d.Source == "standard"))
        {
            die.Location = DieLocation.Available;
            die.Face = DieFace.Character;
        }
        TurnFlow.SelectNext(state, Side.FreePeoples);
        if (present)
            SmeagolRules.Enter(state);
        return state;
    }

    internal static MatchState Move(MatchState state, string tile)
    {
        HuntTests.Pool(state, tile);
        var shadow = state.Dice.First(d => d.Owner == Side.Shadow);
        shadow.Location = DieLocation.Hunt;
        shadow.Face = DieFace.Eye;
        shadow.HuntOrigin = "allocated";
        return Do(state, CommandKind.MoveFellowship, new() { DieId = state.Dice[0].Id }, new SequenceRandom(5, 0));
    }

    [Fact]
    public void SetupHasTwoTilesInPoolAndTwoSetAsideAndBaseHasNone()
    {
        var state = State();
        Assert.Equal(2, state.HuntTiles.Count(t => t.Location == "pool" && t.Id.StartsWith("lords.hunt.smeagol.")));
        Assert.Equal(2, state.HuntTiles.Count(t => t.Location == "set-aside" && t.Id.StartsWith("lords.hunt.smeagol.")));
        Assert.DoesNotContain(KernelTests.NewGame().HuntTiles, t => t.Id.StartsWith("lords."));
    }

    [Fact]
    public void FirstAndSecondTilesArePermanentlyRemovedAndCreateOnlyOneMandatoryGuide()
    {
        var state = Move(State(), FirstTile);
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Equal(8, state.Fellowship.Companions.Count);
        Assert.Equal(LordsCharacters.Smeagol, state.Fellowship.Guide);
        Assert.Equal(new[] { LordsCharacters.Smeagol }, CompanionRules.Guides(state));
        Assert.Equal("removed", state.HuntTiles.Single(t => t.Id == FirstTile).Location);
        state.HuntTiles.Single(t => t.Id == SecondTile).Location = "pool";
        HuntPool.Draw(state, new SequenceRandom(0));
        Assert.Single(state.Characters, c => c.Id == LordsCharacters.Smeagol);
        Assert.Equal("removed", state.HuntTiles.Single(t => t.Id == SecondTile).Location);
        state.Phase = GamePhase.Fellowship;
        TurnFlow.OpenFellowshipPhase(state);
        Reject(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider });
        state.Fellowship.RegionId = "morannon";
        MordorRules.Enter(state);
        Assert.Equal("removed", state.HuntTiles.Single(t => t.Id == FirstTile).Location);
        Assert.Equal("removed", state.HuntTiles.Single(t => t.Id == SecondTile).Location);
    }

    [Fact]
    public void MithrilReplacementCannotUndoEntryOrReturnTheSmeagolTile()
    {
        var state = State();
        state.Player(Side.FreePeoples).Table.Add(FreePeoplesCharacterCards01.Mithril);
        state = Move(state, FirstTile);
        Assert.Equal(DecisionKind.Reaction, state.Pending!.Kind);
        Assert.Equal(LordsCharacters.Smeagol, state.Fellowship.Guide);
        state.HuntTiles.Single(t => t.Id == HuntTests.Three).Location = "pool";
        state = Do(StateCodec.Copy(state), CommandKind.RespondEffect,
            new() { ReactionId = FreePeoplesCharacterCards01.ReplaceReaction }, new SequenceRandom(0));
        Assert.Equal(3, HuntRuntime.Active(state)!.Damage);
        Assert.Equal(LordsCharacters.Smeagol, state.Fellowship.Guide);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal("removed", state.HuntTiles.Single(t => t.Id == FirstTile).Location);
    }

    [Fact]
    public void NenyaReplacementResolvesSmeagolBeforeAnyFurtherHuntWindow()
    {
        var state = LordsNenyaTests.Window();
        state.HuntTiles.Add(new() { Id = FirstTile, Location = "pool" });
        state.HuntTiles.Single(t => t.Id == HuntTests.Three).Location = "removed";
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = NenyaReaction.Id }, new SequenceRandom(0));
        Assert.Equal(LordsCharacters.Smeagol, state.Fellowship.Guide);
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Equal("removed", state.HuntTiles.Single(t => t.Id == FirstTile).Location);
        Assert.Equal("removed", state.HuntTiles.Single(t => t.Id == HuntTests.Eye).Location);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LeftSmeagolOrPreviousGollumCausesDiscardAndAnotherDrawAfterRestart(bool gollum)
    {
        var state = State(!gollum);
        if (gollum)
        {
            state.Fellowship.Companions.Clear();
            FellowshipFlow.CompositionChanged(state, FellowshipReturn.Parent);
            Assert.True(state.Lords!.GollumEntered);
        }
        else
            SmeagolRules.Discard(state);
        HuntTests.Pool(state, FirstTile, HuntTests.Three);
        state = StateCodec.Copy(state);
        var rng = new SequenceRandom(1, 0);
        var tile = HuntPool.Draw(state, rng);
        Assert.Equal(HuntTests.Three, tile.Id);
        Assert.Equal(2, rng.Calls);
        Assert.Equal("discard", state.HuntTiles.Single(t => t.Id == FirstTile).Location);
        Assert.False(SmeagolRules.Present(state));
        Assert.Equal(gollum ? 0 : 1, state.Player(Side.Shadow).Table.Count(c => c == SmeagolRules.WeShallGetIt));
    }

    [Fact]
    public void DynamicLevelChangesWithCompositionAndProjectionDoesNotMutateState()
    {
        var state = State(true);
        Assert.Equal(3, SmeagolRules.Level(state));
        state.Fellowship.Companions.Remove(CharacterRules.Grey);
        state.Fellowship.Companions.Remove(CharacterRules.Strider);
        Assert.Equal(2, SmeagolRules.Level(state));
        state.Fellowship.Companions.RemoveAll(id => id is not (LordsCharacters.Smeagol or "base.character.merry" or "base.character.pippin"));
        Assert.Equal(1, SmeagolRules.Level(state));
        DiceRules.Recover(state);
        Assert.Equal(3, DiceRules.HuntMaximum(state));
        var before = StateCodec.Write(state);
        Assert.Equal(1, Views().For(state, Side.FreePeoples).OwnCompanions.Single(c => c.Id == LordsCharacters.Smeagol).Level);
        Assert.Empty(Views().For(state, Side.Shadow).OwnCompanions);
        Assert.Equal(before, StateCodec.Write(state));
    }

    [Fact]
    public void TakingSmeagolAsCasualtyReducesDamageByCurrentLevelAndImmediatelyEntersThePublicEvent()
    {
        var state = Move(State(true), HuntTests.Three);
        state = Do(StateCodec.Copy(state), CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Guide });
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        Assert.Equal(0, HuntRuntime.Active(state)!.Damage);
        Assert.Equal("removed", state.Characters.Single(c => c.Id == LordsCharacters.Smeagol).Location);
        foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
            Assert.Contains(SmeagolRules.WeShallGetIt, Views().For(state, side).Players.Single(p => p.Side == Side.Shadow).TableCards);
        // UI-344: his departure is a public figure change, so the opponent's presentation can show him leave.
        Assert.Contains(ActionLogFamilyTests.Steps(state).SelectMany(s => s.Facts.Figures), f =>
            f.After?.DefinitionId == LordsCharacters.Smeagol && f.From == "fellowship" && f.To == "removed");
        state = Do(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider });
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Empty(state.Resolution.Frames);
        Assert.True(state.Lords!.SmeagolLeft);
    }
}
