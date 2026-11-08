using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public sealed class FellowshipMoveProjectionTests
{
    private static readonly Projection Projection = GameRuntime.Projection(EffectCatalog.All);

    [Theory]
    [InlineData("reroll")]
    [InlineData("damage")]
    [InlineData("guide")]
    [InlineData("reveal")]
    [InlineData("reaction")]
    [InlineData("placement")]
    [InlineData("mordor")]
    public void OrdinaryMovementExposesPublicProgressAndSurvivesSaveCopying(string window)
    {
        var state = Position(window);
        var before = StateCodec.Write(state);
        foreach (var side in Enum.GetValues<Side>())
        {
            var view = Projection.For(state, side);
            var boundary = Assert.IsType<ActionBoundaryView>(view.ActionBoundary);
            var progress = Assert.IsType<FellowshipMoveView>(boundary.FellowshipMove);
            var hunt = HuntRuntime.Active(state)!;
            Assert.Equal(53, boundary.Version);
            Assert.Equal(hunt.OfferedGuides, progress.GuideWindowsOffered);
            Assert.Equal(hunt.CasualtyEffectsPending, progress.CasualtyConsequencesPending);
            Assert.Equal(hunt.DepartingCompanions, progress.DepartingCompanions);
            Assert.Equal(hunt.DepartureOrigin, progress.DepartureRegionId);
            Assert.Equal(hunt.DeparturePlaced, progress.PlacementStarted);
            Assert.Equal(Json(view), Json(Projection.For(StateCodec.Copy(state), side)));
            if (state.Pending!.Owner != side)
            {
                Assert.Null(view.Decision);
                Assert.Empty(view.Options);
            }
            progress.GuideWindowsOffered.Clear();
            progress.DepartingCompanions.Clear();
            boundary.PaidDieIds.Clear();
        }
        Assert.Equal(before, StateCodec.Write(state));
    }

    [Fact]
    public void HiddenHandsPoolOrderingAndEntropyNeverChangeTheOtherSidesProgress()
    {
        var state = Position("reaction");
        CardFlow.Draw(state, Side.Shadow, "character");
        var visible = Json(Projection.For(state, Side.FreePeoples));
        var other = state.Player(Side.Shadow);
        (other.Hand[0], other.Decks["character"][0]) = (other.Decks["character"][0], other.Hand[0]);
        foreach (var deck in other.Decks.Values)
            deck.Reverse();
        state.HuntTiles.Reverse();
        state.RandomSamples.Add(new() { Value = 987654 });
        Assert.Equal(visible, Json(Projection.For(state, Side.FreePeoples)));
    }

    [Fact]
    public void ConfiguredMovementPublishesTheExactThranduilAndUglukHuntModifiers()
    {
        var state = KingsFarSightedTests.Ready();
        state.Fellowship.RegionId = "moria";
        KingsDiceTests.Chieftain(state, KingsChieftains.Ugluk, "dimrill-dale");
        KingsFixture.Eyes(state, 2);
        KingsFixture.Pool(state, HuntTests.Three);
        var cost = state.Dice.Single(d => d.Owner == Side.FreePeoples && d.Source == SovereignCatalog.RulerSource);

        state = KingsFixture.Do(state, CommandKind.MoveFellowship,
            new() { DieId = cost.Id, AbilityId = KingsFarSighted.Ability }, 4);

        Assert.Equal(DecisionKind.HuntReroll, state.Pending!.Kind);
        var hunt = Assert.IsType<HuntProcedure>(HuntRuntime.Active(state));
        Assert.Single(hunt.Dice);
        Assert.Equal(1, hunt.DiceReduction);
        Assert.Equal(1, hunt.RerollBonus);
        foreach (var side in Enum.GetValues<Side>())
        {
            var view = Projection.For(state, side);
            var facts = Assert.IsType<ActionBoundaryView>(view.ActionBoundary);
            Assert.Equal(53, facts.Version);
            Assert.Equal(1, view.Hunt!.DiceReduction);
            Assert.Equal(1, view.Hunt.RerollBonus);
            Assert.NotNull(facts.FellowshipMove);
        }
    }

    [Theory]
    [InlineData("card")]
    [InlineData("nested")]
    [InlineData("unknown-effect")]
    [InlineData("lords")]
    [InlineData("kings")]
    [InlineData("wrong-payment")]
    public void OtherContinuationContextsDoNotPretendToBeThePaidMove(string kind)
    {
        var state = Position("damage");
        switch (kind)
        {
            case "card": HuntRuntime.Active(state)!.Return = HuntReturn.Parent; break;
            case "nested": state.Resolution.Frames.Insert(0, new() { EffectId = "unreviewed" }); break;
            case "unknown-effect":
                state.Resolution.Frames.Add(new() { EffectId = "unreviewed", Effect = new() });
                break;
            case "lords": state.Configuration.Lords = true; break;
            case "kings": state.Configuration.Kings = true; break;
            case "wrong-payment": state.Resolution.PaidDieIds.Clear(); break;
        }
        Assert.Null(new Projection().For(state, Side.FreePeoples).ActionBoundary);
    }

    internal static MatchState Position(string window)
    {
        var state = HuntTests.State(1, window == "reroll" ? "moria" : "hollin");
        var tile = window == "reveal" ? HuntTests.Eye : HuntTests.Three;
        HuntTests.Pool(state, tile);
        if (window == "mordor")
            state.Fellowship.MordorStep = 0;
        if (window == "reaction")
        {
            Assert.True(state.Player(Side.FreePeoples).Decks["character"].Remove(FreePeoplesCharacterCards01.Mithril));
            state.Player(Side.FreePeoples).Table.Add(FreePeoplesCharacterCards01.Mithril);
        }
        var random = window == "mordor" ? new SequenceRandom(0) :
            window == "reroll" ? new SequenceRandom(0) : new SequenceRandom(5, 0);
        state = HuntTests.Move(state, random, EffectCatalog.All);
        if (window is "guide" or "placement")
        {
            var casualty = window == "placement" ? "base.character.merry" : "base.character.gimli";
            state = HuntTests.Do(state, CommandKind.ResolveHuntDamage,
                new() { HuntResponse = HuntResponse.RandomCompanion },
                new SequenceRandom(state.Fellowship.Companions.IndexOf(casualty)), EffectCatalog.All);
            Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
            if (window == "placement")
                state = HuntTests.Do(state, CommandKind.SelectGuide,
                    new() { GuideId = CompanionRules.Guides(state)[0] }, catalog: EffectCatalog.All);
        }
        else if (window == "reveal")
            state = HuntTests.Do(state, CommandKind.ResolveHuntDamage,
                new() { HuntResponse = HuntResponse.Ring }, catalog: EffectCatalog.All);
        return state;
    }

    private static string Json(object value) => JsonSerializer.Serialize(value);
}
