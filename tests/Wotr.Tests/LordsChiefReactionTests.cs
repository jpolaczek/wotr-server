using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsFixture;

namespace Wotr.Tests;

public class LordsChiefReactionTests
{
    [Theory]
    [InlineData("character", DieFace.Muster, true)]
    [InlineData("strategy", DieFace.Character, false)]
    public void SpearUsesTheDeckEvenWhenTheEventIconBelongsToAnotherCategory(string deck, DieFace icon, bool allowed)
    {
        // Boundary metadata for expansion cards: no unimplemented Event is played by this test.
        var card = new Wotr.Domain.Content.CardDefinition { Side = Side.Shadow, Deck = deck, EventType = icon };
        var state = BeforeEvent();
        Assert.Equal(allowed, ChiefReactions.CanDrawFor(state, Side.Shadow, card));
        CharacterRules.InPlay(state, LordsCharacters.WitchKing)!.Location = "moria";
        Assert.False(ChiefReactions.CanDrawFor(state, Side.Shadow, card));
        Assert.False(ChiefReactions.CanDrawFor(state, Side.FreePeoples, card));
    }

    internal static MatchState BeforeEvent(bool palantir = false, string chiefRegion = "hollin")
    {
        var state = LordsCharacterFixture.State(Side.Shadow);
        state.RandomSamples.Clear();
        state.Fellowship.RegionId = "hollin";
        LordsCharacterFixture.Put(state, LordsCharacters.WitchKing, chiefRegion, CharacterRules.WitchKing);
        if (palantir)
        {
            LordsCharacterFixture.Put(state, CharacterRules.Saruman, "orthanc");
            state.Player(Side.Shadow).Table.Add(ShadowCharacterCards04.Palantir);
        }
        return state;
    }

    internal static MatchState Play(MatchState state, string card = ShadowCharacterCards03.Lidless, DieFace face = DieFace.Character)
    {
        EventTests.GiveCard(state, Side.Shadow, card);
        state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Face = face;
        return Do(state, CommandKind.PlayEvent, EventTests.Play(state, card, face).Payload);
    }

    [Theory]
    [InlineData(DieFace.Character, false)]
    [InlineData(DieFace.Event, false)]
    [InlineData(DieFace.Event, true)]
    public void SpearDrawsPrivatelyBeforeTheEventAndNeverAddsASecondPalantirDraw(DieFace face, bool palantir)
    {
        var state = BeforeEvent(palantir);
        var next = state.Player(Side.Shadow).Decks["character"][0];
        state = Play(state, face: face);
        Assert.Equal("event.played", state.Pending!.Window);
        Assert.Empty(state.Player(Side.Shadow).Hand);
        Assert.Equal(0, state.Resolution.Frames.First(f => f.Effect?.IsEvent == true).NextStep);
        var before = StateCodec.Write(state);
        var command = BattleFixture.Command(state, CommandKind.RespondEffect, new() { ReactionId = ChiefReactions.Spear });
        Assert.True(new PreviewService(Engine()).Preview(state, Side.Shadow, command).HasUnknownResult);
        Assert.Equal(before, StateCodec.Write(state));
        state = Do(StateCodec.Copy(state), command.Kind, command.Payload);
        Assert.Equal(next, Assert.Single(state.Player(Side.Shadow).Hand));
        Assert.DoesNotContain(Views().For(state, Side.FreePeoples).History, e => e.Text.Contains(next, StringComparison.Ordinal));
        Assert.Equal("ActionDie", state.Pending!.Presentation);
        state = Do(StateCodec.Copy(state), CommandKind.ChooseEffect, new());
        Assert.Single(state.Player(Side.Shadow).Hand);
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Empty(state.RandomSamples);
    }

    [Fact]
    public void DecliningSpearStillAllowsTheOnePalantirDrawFromEitherDeck()
    {
        var state = Play(BeforeEvent(true), face: DieFace.Event);
        state = Do(state, CommandKind.DeclineReaction);
        state = Do(state, CommandKind.ChooseEffect, new());
        Assert.Equal("Deck", state.Pending!.Presentation);
        var next = state.Player(Side.Shadow).Decks["strategy"][0];
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["strategy"] });
        Assert.Equal(next, Assert.Single(state.Player(Side.Shadow).Hand));
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData("wrong-region")]
    [InlineData("black-captain")]
    [InlineData("dead")]
    [InlineData("strategy")]
    [InlineData("base")]
    public void OnlyLivingChiefAtTheFellowshipWithLordsAndACharacterDeckEventGetsSpear(string missing)
    {
        var state = BeforeEvent(chiefRegion: missing == "wrong-region" ? "moria" : "hollin");
        var chief = CharacterRules.InPlay(state, LordsCharacters.WitchKing)!;
        if (missing == "black-captain")
            CharacterLifecycle.ApplyVersion(chief, Wotr.Domain.Content.CharacterCatalog.Base[CharacterRules.WitchKing]);
        if (missing == "dead")
            CharacterLifecycle.Eliminate(state, chief);
        if (missing == "base")
            state.Configuration.Lords = false;
        state = Play(state, missing == "strategy" ? ShadowStrategyCards01.Threats : ShadowCharacterCards03.Crebain, DieFace.Event);
        Assert.Empty(state.Resolution.Frames);
        Assert.Empty(state.Player(Side.Shadow).Hand);
        Reject(state, CommandKind.RespondEffect, new() { ReactionId = ChiefReactions.Spear });
    }

    [Theory]
    [InlineData("edoras", Side.FreePeoples, true)]
    [InlineData("lorien", Side.FreePeoples, false)]
    [InlineData("lorien", Side.Shadow, true)]
    [InlineData("hollin", Side.FreePeoples, true)]
    public void DeclarationOffersOneFreeOptionalFlightOnlyToAllowedRegions(string region, Side owner, bool offered)
    {
        var state = BeforeEvent(chiefRegion: "minas-morgul");
        state.Phase = GamePhase.Fellowship;
        state.Fellowship.RegionId = region;
        state.Control[region] = owner;
        TurnFlow.OpenFellowshipPhase(state);
        state = Do(state, CommandKind.DeclareFellowship, new() { Route = [region] });
        if (offered)
        {
            Assert.Equal(Side.Shadow, state.Pending!.Owner);
            Assert.Equal("fellowship.declared", state.Pending.Window);
            var before = StateCodec.Write(state);
            var command = BattleFixture.Command(state, CommandKind.RespondEffect, new() { ReactionId = ChiefReactions.Follow });
            var preview = new PreviewService(Engine()).Preview(state, Side.Shadow, command);
            Assert.Equal(region, Assert.Single(preview.Placements).RegionId);
            Assert.Empty(preview.Costs);
            Assert.Equal(before, StateCodec.Write(state));
            state = Do(StateCodec.Copy(state), command.Kind, command.Payload);
        }
        Assert.Equal(offered ? region : "minas-morgul", CharacterRules.InPlay(state, LordsCharacters.WitchKing)!.Location);
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
        Assert.DoesNotContain(state.Dice, d => d.Location is DieLocation.Spent or DieLocation.Resolving);
        Assert.Empty(state.RandomSamples);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RevealOrDecliningDeclarationDoesNotMoveChief(bool declaration)
    {
        var state = BeforeEvent(chiefRegion: "moria");
        state.Phase = GamePhase.Fellowship;
        TurnFlow.OpenFellowshipPhase(state);
        if (!declaration)
            FellowshipFlow.BeginReveal(state, "test", FellowshipReturn.Decision);
        state = Do(state, declaration ? CommandKind.DeclareFellowship : CommandKind.RevealFellowship, new() { Route = ["hollin"] });
        if (declaration)
            state = Do(StateCodec.Copy(state), CommandKind.DeclineReaction);
        Assert.Equal("moria", CharacterRules.InPlay(state, LordsCharacters.WitchKing)!.Location);
        Assert.Empty(state.Resolution.Frames);
    }
}
