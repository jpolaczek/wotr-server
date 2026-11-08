using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public sealed class RevelationHuntProjectionTests
{
    private static readonly Projection Projection = GameRuntime.Projection(EffectCatalog.All);

    public static IEnumerable<object[]> Roots() => BalrogHuntProjectionTests.Parents
        .Select(parent => new object[] { parent, "offer" })
        .Concat(new[] { "single", "none" }.Select(window => new object[] { BalrogHuntProjectionTests.Movement, window }));

    [Theory]
    [MemberData(nameof(Roots))]
    public void TheOfferRetainsOneResolvedHuntAndTheWholeQueueForBothPlayers(string parent, string window)
    {
        var state = BalrogHuntProjectionTests.At(parent, window, offer: true);
        var original = StateCodec.Write(state);
        string? shared = null;
        foreach (var side in Enum.GetValues<Side>())
        {
            var view = Projection.For(state, side);
            var facts = Assert.IsType<ActionBoundaryView>(view.ActionBoundary);
            Assert.Equal(45, facts.Version);
            var resolved = facts.FellowshipMove?.StrongholdHunts?.MovementHunt ?? facts.CardHunt!.StrongholdHunts!.ResolvedCardHunt;
            var remaining = facts.FellowshipMove?.StrongholdHunts?.RemainingStrongholds ?? facts.CardHunt!.StrongholdHunts!.RemainingStrongholds;
            Assert.Equal(parent, resolved.SourceId);
            Assert.Equal("Done", resolved.Stage);
            Assert.Equal(Json(view.Hunt!), Json(resolved));
            Assert.NotSame(view.Hunt, resolved);
            Assert.Equal(window == "none" ? Array.Empty<string>() : window == "single" ? ["moria"] : new[] { "moria", "dol-guldur" }, remaining);
            Assert.Contains(ShadowCharacterCards03.Balrog, view.Players.Single(p => p.Side == Side.Shadow).TableCards);
            Assert.DoesNotContain(ShadowCharacterCards03.Balrog, facts.Players.Single(p => p.Side == Side.Shadow).PublicDiscards);
            Assert.Equal(parent == BalrogHuntProjectionTests.Movement ? Side.FreePeoples : Side.Shadow,
                view.Dice.Single(d => d.Id == Assert.Single(facts.PaidDieIds)).Owner);
            Assert.Equal(Json(view), Json(Projection.For(StateCodec.Copy(state), side)));
            if (shared is not null)
                Assert.Equal(shared, Json(new { facts.FellowshipMove, facts.CardHunt }));
            shared = Json(new { facts.FellowshipMove, facts.CardHunt });
            if (side == Side.Shadow)
            {
                Assert.Equal("fellowship.revealed", view.Decision!.Window);
                Assert.Contains(view.Options, o => o.Kind == CommandKind.RespondEffect);
                Assert.Contains(view.Options, o => o.Kind == CommandKind.DeclineReaction);
            }
            else
            {
                Assert.Null(view.Decision);
                Assert.Empty(view.Options);
            }
            remaining.Clear();
            resolved.TileId = "changed";
        }
        Assert.Equal(original, StateCodec.Write(state));
    }

    [Theory]
    [InlineData("pending-window")]
    [InlineData("pending-owner")]
    [InlineData("pending-source")]
    [InlineData("declared")]
    [InlineData("partial-queue")]
    [InlineData("table")]
    [InlineData("wait")]
    [InlineData("return")]
    [InlineData("cursor")]
    [InlineData("committed")]
    [InlineData("offered")]
    [InlineData("nested")]
    [InlineData("lords")]
    [InlineData("source-stage")]
    public void UnreviewedOrContradictoryOffersOmitTheBoundary(string change)
    {
        var state = BalrogHuntProjectionTests.At(BalrogHuntProjectionTests.Movement, "offer", offer: true);
        var frame = state.Resolution.Frames[^1];
        switch (change)
        {
            case "pending-window": state.Pending!.Window = "hunt.casualty"; break;
            case "pending-owner": state.Pending!.Owner = Side.FreePeoples; break;
            case "pending-source": state.Pending!.SourceId = "other"; break;
            case "declared": frame.Effect!.ReactionWindow = "fellowship.declared"; break;
            case "partial-queue": state.Resolution.Frames[1].Fellowship!.AdditionalHuntRegions.RemoveAt(0); break;
            case "table": state.Player(Side.Shadow).Table.Clear(); break;
            case "wait": frame.Effect!.Wait = EffectWait.None; break;
            case "return": frame.Effect!.Return = EffectReturn.FinishAction; break;
            case "cursor": frame.NextStep = 1; break;
            case "committed": frame.Effect!.Committed.Add(new(Side.Shadow, ShadowCharacterCards03.Balrog)); break;
            case "offered": frame.Effect!.OfferedReactions.Clear(); break;
            case "nested": EffectRuntime.BeginProgram(state, Side.Shadow, "unknown", "unknown"); break;
            case "lords": state.Configuration.Lords = true; break;
            case "source-stage": HuntRuntime.Active(state)!.Stage = HuntStage.Damage; break;
        }
        Assert.Null(Projection.For(state, Side.Shadow).ActionBoundary);
    }

    [Theory]
    [InlineData(BalrogHuntProjectionTests.Movement)]
    [InlineData(ShadowCharacterCards02.Strike)]
    public void OpposingSecretsDoNotChangeTheOfferOrItsPublicContext(string parent)
    {
        var state = BalrogHuntProjectionTests.At(parent, "offer", offer: true);
        CardFlow.Draw(state, Side.FreePeoples, "character");
        var before = Json(Projection.For(state, Side.Shadow));
        var other = state.Player(Side.FreePeoples);
        (other.Hand[0], other.Decks["character"][0]) = (other.Decks["character"][0], other.Hand[0]);
        foreach (var deck in other.Decks.Values)
            deck.Reverse();
        if (parent != BalrogHuntProjectionTests.Movement)
            state.Resolution.Frames[0].Effect!.Committed.Add(new(Side.FreePeoples, "private", "unknown"));
        state.HuntTiles.Reverse();
        state.RandomSamples.Add(new() { Value = 4321 });
        Assert.Equal(before, Json(Projection.For(state, Side.Shadow)));
    }

    private static string Json(object value) => JsonSerializer.Serialize(value);
}
