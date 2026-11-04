using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public sealed class DeclarationHuntProjectionTests
{
    internal static readonly string[] Windows = ["offer", "damage", "guide", "placement", "draw", "replace", "table", "casualty", "worn", "first", "second"];

    public static IEnumerable<object[]> Cases() => Windows.Select(window => new object[] { window });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OrdinaryViewsRetainTheDeclarationRouteAndRemainingHunts(string window)
    {
        var state = At(window);
        var original = StateCodec.Write(state);
        var projection = GameRuntime.Projection(EffectCatalog.All);
        foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
        {
            var view = projection.For(state, side);
            var boundary = Assert.IsType<ActionBoundaryView>(view.ActionBoundary);
            var declaration = Assert.IsType<FellowshipDeclarationView>(boundary.FellowshipDeclaration);
            Assert.Equal(7, boundary.Version);
            Assert.True(boundary.FellowshipDeclaredThisTurn);
            Assert.Empty(boundary.PaidDieIds);
            Assert.Null(boundary.FellowshipMove);
            Assert.Null(boundary.CardHunt);
            Assert.Equal(state.Resolution.Frames[0].Fellowship!.Route, declaration.Route);
            Assert.Equal(state.Resolution.Frames[0].Fellowship!.AdditionalHuntRegions, declaration.RemainingStrongholds);
            Assert.Equal(JsonSerializer.Serialize(view), JsonSerializer.Serialize(projection.For(StateCodec.Read(original), side)));
            if (state.Pending!.Owner != side)
            {
                Assert.Null(view.Decision);
                Assert.Empty(view.Options);
            }
        }
        Assert.Equal(original, StateCodec.Write(state));
    }

    [Theory]
    [InlineData("offer")]
    [InlineData("damage")]
    [InlineData("first")]
    [InlineData("worn")]
    public void OpposingSecretsAndTheSuspendedDecisionTokenAreAbsent(string window)
    {
        var state = At(window);
        var side = state.Pending!.Owner;
        var other = state.Player(side == Side.Shadow ? Side.FreePeoples : Side.Shadow);
        var projection = GameRuntime.Projection(EffectCatalog.All);
        var expected = JsonSerializer.Serialize(projection.For(state, side));
        (other.Hand[0], other.Decks["character"][0]) = (other.Decks["character"][0], other.Hand[0]);
        foreach (var deck in other.Decks.Values)
            deck.Reverse();
        state.HuntTiles.Reverse();
        state.RandomSamples.Add(new() { Value = 127 });
        state.Resolution.Frames[0].ReturnDecision!.Id = "not-player-information";
        Assert.Equal(expected, JsonSerializer.Serialize(projection.For(state, side)));
    }

    [Theory]
    [InlineData("phase")]
    [InlineData("return")]
    [InlineData("route")]
    [InlineData("destination")]
    [InlineData("queue")]
    [InlineData("declared")]
    [InlineData("paid")]
    [InlineData("program")]
    [InlineData("flags")]
    public void UnsupportedOrInconsistentDeclarationContextsOmitTheBoundary(string change)
    {
        var state = At("damage");
        switch (change)
        {
            case "phase": state.Phase = GamePhase.ActionResolution; break;
            case "return": state.Resolution.Frames[0].ReturnDecision!.Kind = DecisionKind.Action; break;
            case "route": state.Resolution.Frames[0].Fellowship!.Route.Clear(); break;
            case "destination": state.Resolution.Frames[0].Fellowship!.Route = ["moria"]; break;
            case "queue": state.Resolution.Frames[0].Fellowship!.AdditionalHuntRegions = ["moria"]; break;
            case "declared": state.Fellowship.LastDeclarationTurn = -1; break;
            case "paid": state.Resolution.PaidDieIds = [state.Dice[0].Id]; break;
            case "program": state.Resolution.Frames[2].NextStep = 0; break;
            case "flags": HuntRuntime.Active(state)!.DeclarationHunt = false; break;
        }
        Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, state.Pending!.Owner).ActionBoundary);
    }

    internal static MatchState At(string window, string destination = "hollin", bool captured = false)
    {
        var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), new SeededRandom(137));
        state.Turn = 1;
        state.Phase = GamePhase.Fellowship;
        state.Fellowship.RegionId = "moria";
        state.Fellowship.Progress = 3;
        state.Fellowship.Corruption = 1;
        var route = destination == "lorien" ? new[] { "moria", "dimrill-dale", "lorien" } :
            window is "first" or "second" ? new[] { "moria", "dimrill-dale", "north-anduin-vale", "dol-guldur" } :
            new[] { "moria", destination };
        if (captured)
            state.Control[destination] = Side.Shadow;
        Shadow03Fixture.Table(state, ShadowCharacterCards03.Balrog);
        if (window is "draw" or "replace")
            Shadow03Fixture.Table(state, FreePeoplesCharacterCards01.Mithril);
        if (window == "table")
            Shadow03Fixture.Table(state, FreePeoplesCharacterCards01.Axe);
        if (window is "casualty" or "worn")
            Shadow03Fixture.Table(state, ShadowCharacterCards03.Worn);
        CardFlow.Draw(state, Side.FreePeoples, "character");
        CardFlow.Draw(state, Side.Shadow, "character");
        TurnFlow.OpenFellowshipPhase(state);
        state = CharacterFixture.Do(state, CommandKind.DeclareFellowship, new() { Route = [.. route] });
        Assert.Equal("fellowship.declared", state.Pending!.Window);
        if (window == "offer")
            return state;
        var tile = window is "first" or "second" || destination == "lorien" ? "base.hunt.standard.1.1.1" : "base.hunt.standard.3.0.1";
        state = CharacterFixture.Do(state, CommandKind.RespondEffect,
            new() { ReactionId = ShadowCharacterCards03.BalrogReaction + ".declaration" },
            new SequenceRandom(Index(state, tile)));
        if (window == "replace")
            state = CharacterFixture.Do(state, CommandKind.DeclineReaction, random: new SequenceRandom(Index(state, tile)));
        if (window is "guide" or "placement" or "casualty" or "worn")
        {
            var casualty = window == "placement" ? "base.character.merry" : "base.character.gimli";
            state = CharacterFixture.Do(state, CommandKind.ResolveHuntDamage,
                new() { HuntResponse = HuntResponse.RandomCompanion },
                new SequenceRandom(state.Fellowship.Companions.IndexOf(casualty)));
            if (window != "guide")
                state = Shadow03Fixture.ResolveGuide(state);
            if (window == "worn")
                state = CharacterFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = ShadowCharacterCards03.WornReaction });
        }
        if (window is "first" or "second")
            state = CharacterFixture.Do(state, CommandKind.ResolveHuntDamage,
                new() { HuntResponse = HuntResponse.Ring }, new SequenceRandom(Index(state, "base.hunt.standard.3.0.1")));
        if (window == "second")
            state = CharacterFixture.Do(state, CommandKind.ResolveHuntDamage,
                new() { HuntResponse = HuntResponse.Ring }, new SequenceRandom(Index(state, "base.hunt.standard.3.0.2")));
        return state;
    }

    private static int Index(MatchState state, string id) =>
        state.HuntTiles.Where(t => t.Location == "pool").Select(t => t.Id).ToList().IndexOf(id);
}
