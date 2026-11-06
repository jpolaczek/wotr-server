using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public sealed class ActionBoundaryProjectionTests
{
    private static readonly Projection Projection = GameRuntime.Projection(EffectCatalog.All);

    [PostgresFact]
    public async Task OrdinaryPlayerHttpResponsesRetainTheSameFactsAcrossRestart()
    {
        await using var fixture = await CorrespondenceFixture.Create();
        var state = TurnTests.Actions();
        state.Id = Guid.NewGuid().ToString("N");
        TurnFlow.SelectNext(state, Side.FreePeoples);
        CardFlow.Draw(state, Side.Shadow, "character");
        await fixture.Save(state);
        var before = new Dictionary<Side, string>();
        foreach (var side in Enum.GetValues<Side>())
        {
            var client = side == Side.FreePeoples ? fixture.Fp : fixture.Sh;
            var view = await CorrespondenceFixture.Read<PlayerView>(client, $"/api/matches/{state.Id}");
            Assert.Equal(Json(Projection.For(state, side).ActionBoundary), Json(view.ActionBoundary));
            before.Add(side, Json(view.ActionBoundary));
        }
        await fixture.Restart();
        foreach (var side in Enum.GetValues<Side>())
        {
            var client = side == Side.FreePeoples ? fixture.Fp : fixture.Sh;
            var view = await CorrespondenceFixture.Read<PlayerView>(client, $"/api/matches/{state.Id}");
            Assert.Equal(before[side], Json(view.ActionBoundary));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrdinaryActionsDescribePublicRuleFactsWithoutAStateOrEntropy(bool fate)
    {
        var state = TurnTests.Actions();
        state.Configuration.Fate = fate;
        state.Fellowship.LastDeclarationTurn = state.Turn;
        state.MinionEverEntered = true;
        state.MandatoryHuntAllocation = true;
        state.Player(Side.Shadow).ExtraDiceNextRecovery = 2;
        state.Player(Side.FreePeoples).RingActionPending = true;
        var character = CharacterCatalog.Companion(state, CharacterRules.Strider);
        character.LastArmyConversionTurn = state.Turn;
        state.Characters.Add(character);
        var before = StateCodec.Write(state);

        var facts = Assert.IsType<ActionBoundaryView>(Projection.For(state, Side.Shadow).ActionBoundary);
        Assert.True(facts.FellowshipDeclaredThisTurn);
        Assert.True(facts.MinionEverEntered);
        Assert.True(facts.MandatoryHuntAllocation);
        Assert.True(Assert.Single(facts.Characters).ConvertedArmyThisTurn);
        Assert.Equal(2, facts.Players.Single(p => p.Side == Side.Shadow).ExtraDiceNextRecovery);
        Assert.True(facts.Players.Single(p => p.Side == Side.FreePeoples).RingActionPending);
        Assert.Equal(state.HuntTiles.Count, facts.HuntTiles.Sum(z => z.Tiles.Sum(f => f.Count)));
        facts.Companions.Clear();
        facts.Characters[0].Character.Location = "changed";
        Assert.Equal(before, StateCodec.Write(state));
    }

    [Fact]
    public void HiddenAssignmentsOrdersAndPrivateHistoryDoNotChangeTheOtherSidesFacts()
    {
        var state = TurnTests.Actions();
        CardFlow.Draw(state, Side.Shadow, "character");
        CardFlow.Draw(state, Side.Shadow, "character");
        var player = state.Player(Side.Shadow);
        var discarded = player.Hand[0];
        player.Hand.RemoveAt(0);
        player.Discard.Add(discarded);
        state.Events.Add(new()
        {
            OnlyFor = Side.Shadow,
            Activity = Fact(ActionCardUse.Discard, discarded)
        });
        var first = Json(Projection.For(state, Side.FreePeoples));
        (player.Hand[0], player.Discard[0]) = (player.Discard[0], player.Hand[0]);
        foreach (var deck in player.Decks.Values)
            deck.Reverse();
        state.HuntTiles.Reverse();
        state.Events[^1].Activity!.Facts.Cards[0].Id = player.Discard[0];
        state.RandomSamples.Add(new() { Value = 987654 });
        Assert.Equal(first, Json(Projection.For(state, Side.FreePeoples)));
    }

    [Fact]
    public void RememberedDrawOrInspectionDoesNotRevealAFaceDownDiscard()
    {
        var state = TurnTests.Actions();
        var player = state.Player(Side.Shadow);
        var cards = player.Decks["character"].Take(3).ToArray();
        player.Decks["character"].RemoveRange(0, 3);
        player.Discard.AddRange(cards);
        state.Events.Add(new() { Activity = Fact(ActionCardUse.Event, cards[0]) });
        state.Events.Add(new() { Activity = Fact(ActionCardUse.Draw, cards[1]) });
        state.Events.Add(new()
        {
            OnlyFor = Side.Shadow,
            Activity = Fact(ActionCardUse.Discard, cards[2])
        });
        var visible = Projection.For(state, Side.FreePeoples).ActionBoundary!.Players.Single(p => p.Side == Side.Shadow);
        Assert.Equal([cards[0]], visible.KnownDiscards);
        var own = Projection.For(state, Side.Shadow).ActionBoundary!.Players.Single(p => p.Side == Side.Shadow);
        Assert.Equal(cards.Order(StringComparer.Ordinal), own.KnownDiscards);
        Assert.Equal([cards[0]], own.PublicDiscards);
    }

    [Fact]
    public void IdenticalSpecialTileFacesKeepTheirPublicCardAssociations()
    {
        var state = TurnTests.Actions();
        var second = FreePeoplesCharacterCards01.SpecialTiles[1].Tile;
        HuntPool.PrepareSpecial(state, second);
        var facts = Projection.For(state, Side.Shadow).ActionBoundary!;
        Assert.Equal(32, facts.Version);
        Assert.Equal([second], facts.HuntTiles.Single(z => z.Location == "prepared").SpecialTileIds);
        Assert.Contains(FreePeoplesCharacterCards01.SpecialTiles[0].Tile,
            facts.HuntTiles.Single(z => z.Location == "set-aside").SpecialTileIds);
    }

    [Theory]
    [InlineData("lords")]
    [InlineData("kings")]
    [InlineData("reaction")]
    [InlineData("paid")]
    [InlineData("sequence")]
    [InlineData("discard")]
    public void UnsupportedContinuationsHaveNoActionBoundary(string kind)
    {
        var state = TurnTests.Actions();
        switch (kind)
        {
            case "lords": state.Configuration.Lords = true; break;
            case "kings": state.Configuration.Kings = true; break;
            case "reaction": state.Resolution.Suspend(state, "test", 0); break;
            case "paid": state.Resolution.PaidDieIds.Add("paid"); break;
            case "sequence": state.Resolution.ActionSequence = new() { Kind = CommandKind.MoveArmies }; break;
            case "discard": state.AfterDiscard = DiscardContinuation.ResumeEffect; break;
        }
        // Use the projection seam directly: malformed synthetic continuations are
        // not passed to unrelated option providers as if they were playable saves.
        var projection = new Projection();
        Assert.Null(projection.For(state, Side.FreePeoples).ActionBoundary);
    }

    [Fact]
    public void EffectLifetimeUsesTheExistingSideVisibilityBoundary()
    {
        var state = TurnTests.Actions();
        state.ActiveEffects.Add(new()
        {
            Id = "private-effect", SourceId = "base.fp.character.1", Owner = Side.FreePeoples,
            OnlyFor = Side.FreePeoples, Turn = 1, ScopeId = "scope", RequiredCompanions = [CharacterRules.Grey]
        });
        var own = Assert.Single(InGameEffectProjection.For(state, Side.FreePeoples));
        Assert.True(own.Lifetime!.Private);
        Assert.Equal([CharacterRules.Grey], own.Lifetime.RequiredCompanions);
        Assert.Empty(InGameEffectProjection.For(state, Side.Shadow));
        own.Lifetime.RequiredCompanions.Clear();
        Assert.Single(state.ActiveEffects[0].RequiredCompanions);
    }

    private static string Json(object? value) => JsonSerializer.Serialize(value, BoardCatalog.Json);

    private static ActionEvent Fact(ActionCardUse use, string id) => new()
    {
        Facts = new() { Cards = [new() { Owner = Side.Shadow, Use = use, Id = id }] }
    };
}
