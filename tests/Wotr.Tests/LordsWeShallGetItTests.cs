using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.LordsFixture;

namespace Wotr.Tests;

public class LordsWeShallGetItTests
{
    internal static MatchState State(bool gollum = true)
    {
        var state = LordsSmeagolTests.State(true);
        SmeagolRules.Discard(state);
        state.Fellowship.Guide = CharacterRules.Strider;
        state.Fellowship.Hidden = false;
        if (gollum)
        {
            state.Fellowship.Companions.Clear();
            FellowshipFlow.CompositionChanged(state, FellowshipReturn.Parent);
        }
        return state;
    }

    internal static MatchState Window(string tile = HuntTests.Three, bool mithril = true)
    {
        var state = State();
        if (mithril)
            state.Player(Side.FreePeoples).Table.Add(FreePeoplesCharacterCards01.Mithril);
        return Draw(state, tile);
    }

    internal static MatchState Draw(MatchState state, string tile)
    {
        HuntTests.Pool(state, tile);
        HuntRuntime.BeginTile(state, "lords-test-hunt", eyeDamage: 2);
        var runtime = new HuntRuntime(EffectCatalog.All);
        var rng = new SequenceRandom(0);
        while (state.Pending is null && HuntRuntime.Active(state) is not null)
            runtime.Continue(state, rng);
        return state;
    }

    internal static void AddTile(MatchState state, string tile) => state.HuntTiles.Single(t => t.Id == tile).Location = "pool";

    internal static MatchState Use(MatchState state, params int[] samples) => Do(state, CommandKind.RespondEffect,
        new() { ReactionId = WeShallGetIt.Reaction }, new SequenceRandom(samples));

    [Fact]
    public void AutomaticEntryHasNoDieHandDeckOrCombatAndWaitsForGollum()
    {
        var state = State(false);
        Assert.Single(state.Player(Side.Shadow).Table, c => c == SmeagolRules.WeShallGetIt);
        Assert.DoesNotContain(SmeagolRules.WeShallGetIt, state.Player(Side.Shadow).Hand);
        Assert.DoesNotContain(SmeagolRules.WeShallGetIt, state.Players.SelectMany(p => p.Decks.Values.SelectMany(d => d)));
        Assert.DoesNotContain(SmeagolRules.WeShallGetIt, EffectCatalog.All.Events.Keys);
        Assert.DoesNotContain(SmeagolRules.WeShallGetIt, EffectCatalog.All.Combats.Keys);
        Assert.Empty(state.Resolution.PaidDieIds);
        ActiveEffects.CheckConditions(state);
        Assert.Contains(SmeagolRules.WeShallGetIt, state.Player(Side.Shadow).Table);
        TurnFlow.SelectNext(state, Side.Shadow);
        Reject(state, CommandKind.PlayEvent, LordsShadowCardsFixture.Cost(state, SmeagolRules.WeShallGetIt, DieFace.Character));
        var battle = LordsShadowCardsFixture.Battle(SmeagolRules.WeShallGetIt, false);
        Reject(battle, CommandKind.CommitCombatCard, new() { CardId = SmeagolRules.WeShallGetIt });
        Assert.Equal("special", CardCatalog.Lords[SmeagolRules.WeShallGetIt].Deck);
    }

    [Fact]
    public void LastSmeagolBecomingGollumDoesNotEnterTheSpecialCard()
    {
        var state = LordsSmeagolTests.State(true);
        state.Fellowship.Companions.RemoveAll(id => id != LordsCharacters.Smeagol);
        SmeagolRules.Reconcile(state);
        Assert.Equal(CharacterRules.Gollum, state.Fellowship.Guide);
        Assert.DoesNotContain(SmeagolRules.WeShallGetIt, state.Player(Side.Shadow).Table);
        state = Draw(state, HuntTests.Three);
        Assert.NotEqual(WeShallGetIt.Window, state.Pending?.Window);
    }

    [Fact]
    public void PublicShadowWindowPrecedesMithrilAndOffersOnlyItsOwnerAChoice()
    {
        var state = Window();
        Assert.Equal(WeShallGetIt.Window, state.Pending!.Window);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        Assert.Equal(new[] { WeShallGetIt.Reaction }, state.Pending.PrivateChoices);
        var fp = Views().For(state, Side.FreePeoples);
        var sh = Views().For(state, Side.Shadow);
        Assert.Empty(fp.Options);
        Assert.True(fp.Hunt!.DamagePending);
        Assert.Equal(HuntTests.Three, fp.Hunt.TileId);
        var choice = sh.Options.Single(o => o.Kind == CommandKind.RespondEffect).EffectChoices.Single();
        Assert.Equal(SmeagolRules.WeShallGetIt, choice.SourceId);
        Assert.Equal("DiscardTableCard", choice.Cost);
        Assert.Equal("We Shall Get It", choice.Title);
        var command = BattleFixture.Command(state, CommandKind.RespondEffect, new() { ReactionId = WeShallGetIt.Reaction });
        Assert.NotNull(Engine().Validate(state, Side.FreePeoples, command));
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow, command);
        Assert.True(preview.Legal);
        Assert.True(preview.HasUnknownResult);
        Assert.Contains(SmeagolRules.WeShallGetIt, preview.Costs);
        Assert.Equal(before, StateCodec.Write(state));
        Reject(state, CommandKind.RespondEffect, new() { ReactionId = FreePeoplesCharacterCards01.ReplaceReaction });
    }

    [Theory]
    [InlineData("guide")]
    [InlineData("table")]
    [InlineData("module")]
    public void MissingPublicConditionDoesNotOpenAShadowWindow(string reason)
    {
        var state = State(reason != "guide");
        if (reason == "table")
            state.Player(Side.Shadow).Table.Clear();
        if (reason == "module")
            state.Configuration.Lords = false;
        state.Player(Side.FreePeoples).Table.Add(FreePeoplesCharacterCards01.Mithril);
        state = Draw(state, HuntTests.Three);
        Assert.Equal("hunt.replace", state.Pending!.Window);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        Reject(state, CommandKind.RespondEffect, new() { ReactionId = WeShallGetIt.Reaction });
    }

    [Fact]
    public void DecliningRetainsTheCardAndAllowsItOnALaterTile()
    {
        var state = Window();
        state = Do(StateCodec.Copy(state), CommandKind.DeclineReaction);
        Assert.Equal("hunt.replace", state.Pending!.Window);
        Assert.Contains(SmeagolRules.WeShallGetIt, state.Player(Side.Shadow).Table);
        Assert.Equal(HuntTests.Three, HuntRuntime.Active(state)!.TileId);
        AddTile(state, HuntTests.Eye);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = FreePeoplesCharacterCards01.ReplaceReaction }, new SequenceRandom(0));
        Assert.Equal(WeShallGetIt.Window, state.Pending!.Window);
        Assert.Equal(HuntTests.Eye, HuntRuntime.Active(state)!.TileId);
        Assert.Equal("return-after-action", state.HuntTiles.Single(t => t.Id == HuntTests.Three).Location);
    }

    [Fact]
    public void ReplacementIsDrawnOnceBeforeProtectionAndOriginalReturnsAfterResolution()
    {
        var state = Window();
        AddTile(state, HuntTests.Eye);
        state = Use(StateCodec.Copy(state), 0);
        Assert.Equal("hunt.replace", state.Pending!.Window);
        Assert.Equal(HuntTests.Eye, HuntRuntime.Active(state)!.TileId);
        Assert.Equal("return-after-action", state.HuntTiles.Single(t => t.Id == HuntTests.Three).Location);
        Assert.DoesNotContain(SmeagolRules.WeShallGetIt, state.Player(Side.Shadow).Table);
        Assert.Single(state.Player(Side.Shadow).Discard, c => c == SmeagolRules.WeShallGetIt);
        Assert.True(Views().For(state, Side.FreePeoples).Hunt!.DamagePending);
        state = Do(state, CommandKind.DeclineReaction);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(2, state.Fellowship.Corruption);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == HuntTests.Three).Location);
    }

    [Fact]
    public void DepletedPoolRefillsDiscardedStandardsWithoutReturningTheFirstTileEarly()
    {
        var state = Window();
        state.HuntTiles.Single(t => t.Id == HuntTests.Eye).Location = "discard";
        state = Use(state, 0);
        Assert.Equal(HuntTests.Eye, HuntRuntime.Active(state)!.TileId);
        Assert.Equal("return-after-action", state.HuntTiles.Single(t => t.Id == HuntTests.Three).Location);
    }

    [Fact]
    public void ReplacementSkipsSmeagolWhoAlreadyLeftAndNeverReentersOrCostsTwice()
    {
        var state = Window();
        AddTile(state, LordsSmeagolTests.FirstTile);
        AddTile(state, HuntTests.Eye);
        state = Use(state, 1, 0);
        Assert.Equal(HuntTests.Eye, HuntRuntime.Active(state)!.TileId);
        Assert.False(SmeagolRules.Present(state));
        Assert.Equal("discard", state.HuntTiles.Single(t => t.Id == LordsSmeagolTests.FirstTile).Location);
        Assert.Single(state.Player(Side.Shadow).Discard, c => c == SmeagolRules.WeShallGetIt);
    }
}
