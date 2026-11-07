using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public sealed class BalrogHuntProjectionTests
{
    internal const string Movement = "fellowship-move";
    internal static readonly string[] Parents = [Movement, ShadowCharacterCards01.OrcPatrol,
        ShadowCharacterCards01.IsildursBane, ShadowCharacterCards02.FoulThing, ShadowCharacterCards02.Strike];
    private static readonly Projection Projection = GameRuntime.Projection(EffectCatalog.All);

    public static IEnumerable<object[]> Roots() => Parents.SelectMany(parent =>
        new[] { "damage", "replace", "guide" }.Select(window => new object[] { parent, window }))
        .Concat(new[] { "placement", "draw", "table", "single", "none" }.Select(window => new object[] { Movement, window }));

    [Theory]
    [MemberData(nameof(Roots))]
    public void ActiveBalrogRetainsThePublicSourceRouteQueueAndOriginalPayment(string parent, string window)
    {
        var state = At(parent, window);
        var original = StateCodec.Write(state);
        string? shared = null;
        foreach (var side in Enum.GetValues<Side>())
        {
            var view = Projection.For(state, side);
            var facts = Assert.IsType<ActionBoundaryView>(view.ActionBoundary);
            Assert.Equal(36, facts.Version);
            Assert.Equal(ShadowCharacterCards03.Balrog, view.Hunt!.SourceId);
            var resolved = facts.FellowshipMove?.StrongholdHunts?.MovementHunt ?? facts.CardHunt?.StrongholdHunts?.ResolvedCardHunt;
            Assert.Equal(parent, resolved!.SourceId);
            Assert.Equal("Done", resolved.Stage);
            var remaining = facts.FellowshipMove?.StrongholdHunts?.RemainingStrongholds ?? facts.CardHunt!.StrongholdHunts!.RemainingStrongholds;
            Assert.Equal(window == "none" ? Array.Empty<string>() : window == "single" ? ["moria"] : new[] { "moria", "dol-guldur" }, remaining);
            Assert.Contains(ShadowCharacterCards03.Balrog, facts.Players.Single(p => p.Side == Side.Shadow).PublicDiscards);
            Assert.Equal(parent == Movement ? Side.FreePeoples : Side.Shadow,
                view.Dice.Single(d => d.Id == Assert.Single(facts.PaidDieIds)).Owner);
            Assert.Equal(Json(view), Json(Projection.For(StateCodec.Copy(state), side)));
            if (shared is not null)
                Assert.Equal(shared, Json(new { facts.FellowshipMove, facts.CardHunt }));
            shared = Json(new { facts.FellowshipMove, facts.CardHunt });
            if (side != state.Pending!.Owner)
            {
                Assert.Null(view.Decision);
                Assert.Empty(view.Options);
            }
            remaining.Clear();
            resolved.SourceId = "changed";
        }
        Assert.Equal(original, StateCodec.Write(state));
    }

    [Theory]
    [InlineData("declaration")]
    [InlineData("ignore-reveal")]
    [InlineData("tile-rule")]
    [InlineData("return")]
    [InlineData("rerolled")]
    [InlineData("damage-rule")]
    [InlineData("parent-return")]
    [InlineData("parent-window")]
    [InlineData("program-cursor")]
    [InlineData("program-source")]
    [InlineData("program-draw")]
    [InlineData("partial-queue")]
    [InlineData("discard")]
    [InlineData("table")]
    [InlineData("lords")]
    public void UnreviewedBalrogContinuationsOmitTheBoundary(string change)
    {
        var state = At(Movement, "damage");
        var hunt = HuntRuntime.Active(state)!;
        switch (change)
        {
            case "declaration": hunt.DeclarationHunt = true; break;
            case "ignore-reveal": hunt.IgnoreRevealIcon = true; break;
            case "tile-rule": hunt.TileRule = HuntTileRule.All; break;
            case "return": hunt.Return = HuntReturn.Stronghold; break;
            case "rerolled": hunt.Rolled = true; break;
            case "damage-rule": hunt.DamageRule = HuntDamageRule.CorruptionOnly; break;
            case "parent-return": state.Resolution.Frames[2].Effect!.Return = EffectReturn.FinishAction; break;
            case "parent-window": state.Resolution.Frames[2].Effect!.ReactionWindow = "fellowship.declared"; break;
            case "program-cursor": state.Resolution.Frames[3].NextStep = 0; break;
            case "program-source": state.Resolution.Frames[3].Effect!.SourceId = "unknown"; break;
            case "program-draw": state.Resolution.Frames[3].Effect!.PalantirDrawPending = true; break;
            case "partial-queue": state.Resolution.Frames[1].Fellowship!.AdditionalHuntRegions.RemoveAt(0); break;
            case "discard": state.Player(Side.Shadow).Discard.Remove(ShadowCharacterCards03.Balrog); break;
            case "table": state.Player(Side.Shadow).Table.Add(ShadowCharacterCards03.Balrog); break;
            case "lords": state.Configuration.Lords = true; break;
        }
        Assert.Null(Projection.For(state, state.Pending!.Owner).ActionBoundary);
    }

    [Fact]
    public void OpposingCardsTrailAndEntropyDoNotChangeThePublicBalrogContext()
    {
        var state = At(Movement, "damage");
        CardFlow.Draw(state, Side.Shadow, "character");
        var before = Json(Projection.For(state, Side.FreePeoples));
        var opponent = state.Player(Side.Shadow);
        (opponent.Hand[0], opponent.Decks["character"][0]) = (opponent.Decks["character"][0], opponent.Hand[0]);
        opponent.Decks["character"].Reverse();
        state.HuntTiles.Reverse();
        state.RandomSamples.Add(new() { Value = 9876 });
        state.Resolution.Frames[2].Effect!.Committed.Add(new(Side.Shadow, "private-history", "unknown"));
        Assert.Equal(before, Json(Projection.For(state, Side.FreePeoples)));
    }

    internal static MatchState At(string parent, string window, bool offer = false)
    {
        var state = parent == Movement ? MovementReveal(window == "none") : CardHuntProjectionTests.At(parent, "reveal");
        if (state.Pending?.Kind == DecisionKind.Guide)
            state = Shadow03Fixture.ResolveGuide(state);
        foreach (var id in new[] { "base.hunt.standard.2.0.1", "base.hunt.standard.3.0.1", "base.hunt.standard.3.0.3" })
            state.HuntTiles.Single(t => t.Id == id).Location = "pool";
        Shadow03Fixture.Table(state, ShadowCharacterCards03.Balrog);
        var table = window switch
        {
            "replace" => FreePeoplesCharacterCards01.Mithril,
            "draw" => FreePeoplesCharacterCards02.Staff,
            "table" => FreePeoplesCharacterCards01.Axe,
            _ => ""
        };
        if (table != "")
            Shadow03Fixture.Table(state, table);
        string[] route = window is "single" or "none" ? ["moria", "dimrill-dale"] : [.. StrongholdHuntProjectionTests.Route];
        state = CharacterFixture.Do(state, CommandKind.RevealFellowship, new() { Route = [.. route] });
        Assert.Equal("fellowship.revealed", state.Pending!.Window);
        if (offer)
            return state;
        state = CharacterFixture.Do(state, CommandKind.RespondEffect,
            new() { ReactionId = ShadowCharacterCards03.BalrogReaction },
            new SequenceRandom(state.HuntTiles.Where(t => t.Location == "pool").Select(t => t.Id).ToList().IndexOf(HuntTests.Three)));
        if (window is "guide" or "placement")
        {
            var id = window == "placement" ? "base.character.merry" : "base.character.boromir";
            state = CharacterFixture.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.RandomCompanion },
                new SequenceRandom(state.Fellowship.Companions.IndexOf(id)));
            if (window == "placement")
                state = Shadow03Fixture.ResolveGuide(state);
        }
        return state;
    }

    private static MatchState MovementReveal(bool noStronghold)
    {
        var state = HuntTests.State(1, "moria");
        state.Id = Guid.NewGuid().ToString("N");
        state.Fellowship.Progress = 2;
        if (noStronghold)
        {
            state.Control["moria"] = Side.FreePeoples;
            foreach (var unit in state.Units.Where(u => u.Location == "moria" && BoardRules.Owner(u) == Side.Shadow))
                unit.Location = "dimrill-dale";
        }
        HuntTests.Pool(state, HuntTests.Eye, HuntTests.Three, "base.hunt.standard.3.0.2", "base.hunt.standard.3.0.3");
        var eye = state.HuntTiles.Where(t => t.Location == "pool").Select(t => t.Id).ToList().IndexOf(HuntTests.Eye);
        state = HuntTests.Move(state, new SequenceRandom(5, eye), EffectCatalog.All);
        return HuntTests.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }, catalog: EffectCatalog.All);
    }

    private static string Json(object value) => JsonSerializer.Serialize(value);
}
