using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsFixture;

namespace Wotr.Tests;

public class LordsFireAndShadowTests
{
    internal static MatchState BeforeRoute(bool declaration, params string[] route)
    {
        var state = LordsSmeagolTests.State();
        state.RandomSamples.Clear();
        LordsCharacterFixture.Put(state, LordsCharacters.Balrog, "moria");
        state.Phase = GamePhase.Fellowship;
        state.Fellowship.RegionId = route[0];
        state.Fellowship.Progress = route.Length - 1;
        TurnFlow.OpenFellowshipPhase(state);
        if (!declaration)
            FellowshipFlow.BeginReveal(state, "test", FellowshipReturn.Decision);
        return state;
    }

    [Theory]
    [InlineData(true, "moria")]
    [InlineData(false, "moria")]
    [InlineData(true, "hollin", "moria")]
    [InlineData(false, "hollin", "moria")]
    [InlineData(true, "moria", "hollin")]
    [InlineData(false, "moria", "hollin")]
    [InlineData(true, "hollin", "moria", "dimrill-dale")]
    [InlineData(false, "hollin", "moria", "dimrill-dale")]
    public void EveryPositionOnTheConfirmedRouteTriggersOneFireHuntBeforeStrongholds(bool declaration, params string[] route)
    {
        var state = BeforeRoute(declaration, route);
        HuntTests.Pool(state, HuntTests.Three, HuntTests.Eye);
        var kind = declaration ? CommandKind.DeclareFellowship : CommandKind.RevealFellowship;
        var command = BattleFixture.Command(state, kind, new() { Route = [.. route] });
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples, command);
        Assert.True(preview.Legal);
        Assert.True(preview.HasUnknownResult);
        Assert.Equal(before, StateCodec.Write(state));
        state = Do(state, kind, command.Payload, new SequenceRandom(1));
        Assert.True(HuntRuntime.Active(state)!.FireAndShadow);
        Assert.Equal(LordsCharacters.Balrog, HuntRuntime.Active(state)!.SourceId);
        Assert.Equal(3, HuntRuntime.Active(state)!.Damage);
        var rng = new SequenceRandom(declaration ? [] : [0]);
        state = Do(StateCodec.Copy(state), CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }, rng);
        Assert.Equal(declaration ? 0 : 1, rng.Calls);
        Assert.Equal(3, state.Fellowship.Corruption);
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
        Assert.Equal(declaration, state.Fellowship.Hidden);
        Assert.NotNull(CharacterRules.InPlay(state, LordsCharacters.Balrog));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NumberedTileCannotRevealADeclarationAndActiveBalrogWorksOutsideMoria(bool active)
    {
        var state = BeforeRoute(true, "hollin");
        var balrog = CharacterRules.InPlay(state, LordsCharacters.Balrog)!;
        balrog.Location = active ? "hollin" : "moria";
        balrog.Level = active ? 2 : 0;
        var tile = state.HuntTiles.First(t => HuntPool.Definitions[t.Id] is { Pool: "standard", Value: "1", Reveal: true }).Id;
        HuntTests.Pool(state, tile);
        state = Do(state, CommandKind.DeclareFellowship, new() { Route = ["hollin"] }, new SequenceRandom(active ? [0] : []));
        if (active)
        {
            Assert.False(HuntRuntime.Active(state)!.Reveal);
            state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        }
        Assert.True(state.Fellowship.Hidden);
        Assert.Equal(active ? 1 : 0, state.Fellowship.Corruption);
    }

    [Fact]
    public void EyeKillsBalrogAndGuideAndResumesAfterGuideSelectionWithoutRedrawing()
    {
        var state = BeforeRoute(true, "moria");
        state.Fellowship.Companions.Remove(CharacterRules.Strider);
        HuntTests.Pool(state, HuntTests.Eye);
        state = Do(state, CommandKind.DeclareFellowship, new() { Route = ["moria"] }, new SequenceRandom(0));
        Assert.Null(CharacterRules.InPlay(state, LordsCharacters.Balrog));
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == CharacterRules.Grey).Location);
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        Assert.Equal(3, state.Pending.PrivateChoices.Count);
        state = Do(StateCodec.Copy(state), CommandKind.SelectGuide, new() { GuideId = "base.character.gimli" });
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
        Assert.Single(state.RandomSamples);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void EyeWithGollumDiscardsWithoutEitherElimination()
    {
        var state = BeforeRoute(true, "moria");
        state.Fellowship.Companions.Clear();
        state.Fellowship.Guide = CharacterRules.Gollum;
        HuntTests.Pool(state, HuntTests.Eye);
        state = Do(state, CommandKind.DeclareFellowship, new() { Route = ["moria"] }, new SequenceRandom(0));
        Assert.NotNull(CharacterRules.InPlay(state, LordsCharacters.Balrog));
        Assert.Equal(CharacterRules.Gollum, state.Fellowship.Guide);
        Assert.DoesNotContain(state.Events, e => e.Kind == "CharacterEliminated");
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void SmeagolTileHasPriorityAndHisLaterEliminationEntersTheEvent()
    {
        var state = BeforeRoute(true, "moria");
        HuntTests.Pool(state, LordsSmeagolTests.FirstTile);
        state = Do(state, CommandKind.DeclareFellowship, new() { Route = ["moria"] }, new SequenceRandom(0));
        Assert.True(SmeagolRules.Present(state));
        Assert.NotNull(CharacterRules.InPlay(state, LordsCharacters.Balrog));
        state.Turn++;
        HuntTests.Pool(state, HuntTests.Eye);
        state = Do(state, CommandKind.DeclareFellowship, new() { Route = ["moria"] }, new SequenceRandom(0));
        Assert.False(SmeagolRules.Present(state));
        Assert.Contains(SmeagolRules.WeShallGetIt, state.Player(Side.Shadow).Table);
        Assert.Null(CharacterRules.InPlay(state, LordsCharacters.Balrog));
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
    }

    [Fact]
    public void NenyaCanReplaceTheEyeBeforeItsEliminationEffect()
    {
        var state = BeforeRoute(true, "moria");
        LordsCharacterFixture.Put(state, LordsCharacters.Galadriel, "lorien");
        HuntTests.Pool(state, HuntTests.Eye);
        state = Do(state, CommandKind.DeclareFellowship, new() { Route = ["moria"] }, new SequenceRandom(0));
        Assert.Equal(DecisionKind.Reaction, state.Pending!.Kind);
        Assert.NotNull(CharacterRules.InPlay(state, LordsCharacters.Balrog));
        state.HuntTiles.Single(t => t.Id == LordsSmeagolTests.FirstTile).Location = "pool";
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = NenyaReaction.Id }, new SequenceRandom(0));
        Assert.NotNull(CharacterRules.InPlay(state, LordsCharacters.Balrog));
        Assert.True(SmeagolRules.Present(state));
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void EliminatedHobbitReturnsThroughSavedGroupPlacement()
    {
        var state = BeforeRoute(true, "moria");
        state.Fellowship.Companions = ["base.character.merry", "base.character.pippin"];
        state.Fellowship.Guide = "base.character.merry";
        HuntTests.Pool(state, HuntTests.Eye);
        state = Do(state, CommandKind.DeclareFellowship, new() { Route = ["moria"] }, new SequenceRandom(0));
        Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);
        Assert.Equal("base.character.pippin", state.Fellowship.Guide);
        state = Do(StateCodec.Copy(state), CommandKind.SeparateCompanions,
            new() { Ids = ["base.character.merry"], Route = ["moria", "hollin"] });
        Assert.Equal("hollin", state.Characters.Single(c => c.Id == "base.character.merry").Location);
        Assert.Empty(state.Resolution.Frames);
    }
}
