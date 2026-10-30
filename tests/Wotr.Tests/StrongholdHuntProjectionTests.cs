using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public sealed class StrongholdHuntProjectionTests
{
    internal static readonly string[] Route = ["moria", "dimrill-dale", "north-anduin-vale", "dol-guldur"];
    private static readonly Projection Projection = GameRuntime.Projection(EffectCatalog.All);

    [Theory]
    [InlineData("first")]
    [InlineData("second")]
    [InlineData("guide")]
    [InlineData("placement")]
    [InlineData("reaction")]
    public void RevealedRouteAndRemainingStrongholdsArePublicAndSurviveSaveCopy(string window)
    {
        var state = At(window);
        var original = StateCodec.Write(state);
        foreach (var side in Enum.GetValues<Side>())
        {
            var view = Projection.For(state, side);
            var facts = Assert.IsType<FellowshipStrongholdView>(view.ActionBoundary?.FellowshipMove?.StrongholdHunts);
            Assert.Equal(Route, facts.Route);
            Assert.Equal(window == "second" ? ["dol-guldur"] : new[] { "moria", "dol-guldur" }, facts.RemainingStrongholds);
            Assert.Equal("fellowship-move", facts.MovementHunt.SourceId);
            Assert.Equal("Done", facts.MovementHunt.Stage);
            Assert.Equal(HuntTests.Eye, facts.MovementHunt.TileId);
            Assert.Equal(facts.RemainingStrongholds[0], view.Hunt!.SourceId);
            Assert.Equal(0, view.Hunt.EyeDamage);
            Assert.Equal(Json(view), Json(Projection.For(StateCodec.Copy(state), side)));
            if (side != state.Pending!.Owner)
            {
                Assert.Null(view.Decision);
                Assert.Empty(view.Options);
            }
            facts.Route.Clear();
            facts.RemainingStrongholds.Clear();
            facts.MovementHunt.Dice.Clear();
        }
        Assert.Equal(original, StateCodec.Write(state));
    }

    [Theory]
    [InlineData("route")]
    [InlineData("remaining")]
    [InlineData("payment")]
    [InlineData("return")]
    [InlineData("eye")]
    [InlineData("cursor")]
    [InlineData("source")]
    [InlineData("lords")]
    [InlineData("declaration")]
    public void UnreviewedOrInconsistentContextsDoNotClaimAnOrdinaryMovementReturn(string change)
    {
        var state = At("first");
        var reveal = state.Resolution.Frames[1].Fellowship!;
        switch (change)
        {
            case "route": reveal.Route.Clear(); break;
            case "remaining": reveal.AdditionalHuntRegions.Reverse(); break;
            case "payment": state.Resolution.PaidDieIds.Clear(); break;
            case "return": state.Resolution.Frames[0].Hunt!.Return = HuntReturn.Parent; break;
            case "eye": HuntRuntime.Active(state)!.EyeDamage = 1; break;
            case "cursor": state.Resolution.Frames[2].NextStep = 1; break;
            case "source": HuntRuntime.Active(state)!.SourceId = "other-source"; break;
            case "lords": state.Configuration.Lords = true; break;
            case "declaration": reveal.Kind = FellowshipProcedureKind.Declaration; break;
        }
        Assert.Null(Projection.For(state, state.Pending!.Owner).ActionBoundary);
    }

    [Fact]
    public void HiddenHandDeckTileOrderAndEntropyDoNotChangeThePublicRouteContext()
    {
        var state = At("first");
        CardFlow.Draw(state, Side.Shadow, "character");
        var before = Json(Projection.For(state, Side.FreePeoples));
        var opponent = state.Player(Side.Shadow);
        (opponent.Hand[0], opponent.Decks["character"][0]) = (opponent.Decks["character"][0], opponent.Hand[0]);
        opponent.Decks["character"].Reverse();
        state.HuntTiles.Reverse();
        state.RandomSamples.Add(new() { Value = 76543 });
        Assert.Equal(before, Json(Projection.For(state, Side.FreePeoples)));
    }

    internal static MatchState At(string window)
    {
        var state = HuntTests.State(1, "moria");
        state.Id = Guid.NewGuid().ToString("N");
        state.Fellowship.Progress = 2;
        HuntTests.Pool(state, HuntTests.Eye, HuntTests.Three, "base.hunt.standard.3.0.2");
        var eye = state.HuntTiles.Where(t => t.Location == "pool").Select(t => t.Id).ToList().IndexOf(HuntTests.Eye);
        state = HuntTests.Move(state, new SequenceRandom(5, eye), EffectCatalog.All);
        state = HuntTests.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }, catalog: EffectCatalog.All);
        if (window == "reaction")
        {
            var player = state.Player(Side.FreePeoples);
            Assert.True(player.Decks["character"].Remove(FreePeoplesCharacterCards01.Mithril));
            player.Table.Add(FreePeoplesCharacterCards01.Mithril);
        }
        state = HuntTests.Do(state, CommandKind.RevealFellowship, new() { Route = [.. Route] }, new SequenceRandom(0), EffectCatalog.All);
        if (window == "second")
            state = HuntTests.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }, new SequenceRandom(0), EffectCatalog.All);
        if (window is "guide" or "placement")
        {
            var casualty = window == "guide" ? "base.character.gimli" : "base.character.merry";
            state = HuntTests.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.RandomCompanion },
                new SequenceRandom(state.Fellowship.Companions.IndexOf(casualty)), EffectCatalog.All);
            if (window == "placement")
                state = HuntTests.Do(state, CommandKind.SelectGuide, new() { GuideId = CompanionRules.Guides(state)[0] }, catalog: EffectCatalog.All);
        }
        return state;
    }

    private static string Json(object value) => JsonSerializer.Serialize(value);
}
