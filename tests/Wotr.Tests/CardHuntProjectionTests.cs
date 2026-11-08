using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public sealed class CardHuntProjectionTests
{
    private static readonly Projection Projection = GameRuntime.Projection(EffectCatalog.All);

    public static IEnumerable<object[]> Roots()
    {
        foreach (var window in new[] { "damage", "replace", "guide", "placement", "reveal", "first", "second", "palantir" })
            yield return [ShadowCharacterCards01.OrcPatrol, window];
        foreach (var window in new[] { "replace", "reveal", "first", "second" })
            yield return [ShadowCharacterCards01.IsildursBane, window];
        foreach (var window in new[] { "replace", "guide", "placement" })
            yield return [ShadowCharacterCards02.FoulThing, window];
        foreach (var window in new[] { "roll", "reroll", "after-roll", "draw", "damage", "replace", "guide", "placement", "reveal", "first", "second", "palantir" })
            yield return [ShadowCharacterCards02.Strike, window];
    }

    [Theory]
    [MemberData(nameof(Roots))]
    public void CardAndPaymentObligationsArePublicCopiedAndPersisted(string card, string window)
    {
        var state = At(card, window);
        var original = StateCodec.Write(state);
        string? shared = null;
        foreach (var side in Enum.GetValues<Side>())
        {
            var view = Projection.For(state, side);
            var facts = Assert.IsType<CardHuntView>(view.ActionBoundary?.CardHunt);
            Assert.Null(view.ActionBoundary!.FellowshipMove);
            Assert.Equal(43, view.ActionBoundary.Version);
            Assert.Equal(card, facts.CardId);
            Assert.Equal(window == "palantir" ? DieFace.Event : DieFace.Character, facts.CardCost);
            Assert.Equal(window == "palantir", facts.PalantirDrawPending);
            Assert.Contains(view.Dice, d => d.Owner == Side.Shadow && d.Location == DieLocation.Resolving &&
                d.Id == Assert.Single(view.ActionBoundary.PaidDieIds));
            Assert.Equal(Json(view), Json(Projection.For(StateCodec.Copy(state), side)));
            if (shared is not null)
                Assert.Equal(shared, Json(facts));
            shared = Json(facts);
            if (side != state.Pending!.Owner)
            {
                Assert.Null(view.Decision);
                Assert.Empty(view.Options);
            }
            if (window is "first" or "second")
            {
                var extra = Assert.IsType<CardHuntStrongholdView>(facts.StrongholdHunts);
                Assert.Equal(card, extra.ResolvedCardHunt.SourceId);
                Assert.Equal("Done", extra.ResolvedCardHunt.Stage);
                Assert.Equal(StrongholdHuntProjectionTests.Route, extra.Route);
                Assert.Equal(window == "first" ? new[] { "moria", "dol-guldur" } : ["dol-guldur"], extra.RemainingStrongholds);
                extra.Route.Clear();
                extra.RemainingStrongholds.Clear();
                extra.ResolvedCardHunt.SourceId = "changed";
            }
            facts.GuideWindowsOffered.Clear();
            facts.DepartingCompanions.Clear();
        }
        Assert.Equal(original, StateCodec.Write(state));
    }

    [Theory]
    [InlineData("cursor")]
    [InlineData("card")]
    [InlineData("discard")]
    [InlineData("cost")]
    [InlineData("owner")]
    [InlineData("return")]
    [InlineData("palantir")]
    [InlineData("free-event")]
    [InlineData("movement")]
    [InlineData("damage-rule")]
    [InlineData("tile-rule")]
    [InlineData("nested")]
    [InlineData("reveal-route")]
    [InlineData("lords")]
    public void UnknownOrInconsistentParentsDoNotClaimAReviewedCardContext(string change)
    {
        var state = At(ShadowCharacterCards01.OrcPatrol, change == "reveal-route" ? "first" : "damage");
        var parent = state.Resolution.Frames[0];
        var hunt = state.Resolution.Frames[1].Hunt!;
        switch (change)
        {
            case "cursor": parent.NextStep = 0; break;
            case "card": parent.Effect!.SourceId = ShadowCharacterCards02.Strike; break;
            case "discard": state.Player(Side.Shadow).Discard.Clear(); break;
            case "cost": parent.Effect!.PaidFace = DieFace.Army; break;
            case "owner": parent.Effect!.Owner = Side.FreePeoples; break;
            case "return": hunt.Return = HuntReturn.FinishMove; break;
            case "palantir": parent.Effect!.PalantirDrawPending = true; break;
            case "free-event": parent.Effect!.Return = EffectReturn.Parent; break;
            case "movement": hunt.Movement = true; break;
            case "damage-rule": hunt.DamageRule = HuntDamageRule.CorruptionOnly; break;
            case "tile-rule": hunt.TileRule = HuntTileRule.All; break;
            case "nested": EffectRuntime.BeginProgram(state, Side.Shadow, "unknown", "unknown"); break;
            case "reveal-route": state.Resolution.Frames[2].Fellowship!.Route.Clear(); break;
            case "lords": state.Configuration.Lords = true; break;
        }
        Assert.Null(Projection.For(state, Side.FreePeoples).ActionBoundary);
    }

    [Theory]
    [InlineData(ShadowCharacterCards01.OrcPatrol)]
    [InlineData(ShadowCharacterCards02.Strike)]
    public void OpposingHiddenCardsAndEntropyDoNotChangePublicCardHuntFacts(string card)
    {
        var state = At(card, "palantir");
        CardFlow.Draw(state, Side.Shadow, "character");
        var before = Json(Projection.For(state, Side.FreePeoples));
        var opponent = state.Player(Side.Shadow);
        (opponent.Hand[0], opponent.Decks["character"][0]) = (opponent.Decks["character"][0], opponent.Hand[0]);
        opponent.Decks["character"].Reverse();
        state.HuntTiles.Reverse();
        state.RandomSamples.Add(new() { Value = 4321 });
        state.Resolution.Frames[0].Effect!.Committed.Add(new(Side.Shadow, "private-history", "secret"));
        Assert.Equal(before, Json(Projection.For(state, Side.FreePeoples)));
    }

    internal static MatchState At(string card, string window)
    {
        var state = Shadow01Fixture.BeforeEvent(card, window is "reveal" or "first" or "second" ? "moria" : "hollin");
        state.Id = Guid.NewGuid().ToString("N");
        state.Fellowship.Progress = 3;
        var reveal = window is "reveal" or "first" or "second";
        Shadow01Fixture.Pool(state, reveal ? Shadow01Fixture.RevealOne : HuntTests.Three,
            "base.hunt.standard.3.0.2", "base.hunt.standard.3.0.3");
        if (window == "replace")
            Shadow03Fixture.Table(state, FreePeoplesCharacterCards01.Mithril);
        if (window == "palantir")
            Shadow03Fixture.Table(state, ShadowCharacterCards04.Palantir);
        if (window is "roll" or "after-roll")
            Shadow03Fixture.Table(state, ShadowCharacterCards03.Crebain);
        if (window == "draw")
            Shadow03Fixture.Table(state, FreePeoplesCharacterCards02.Staff);
        if (card == ShadowCharacterCards02.Strike)
        {
            var die = state.Dice.Last(d => d.Owner == Side.Shadow);
            die.Location = DieLocation.Hunt;
            die.HuntOrigin = "allocated";
        }
        var cost = Shadow01Fixture.EventCost(state, card);
        if (window == "palantir")
        {
            cost.CardCost = DieFace.Event;
            state.Dice.Single(d => d.Id == cost.DieId).Face = DieFace.Event;
        }
        var casualty = window == "placement" ? "base.character.merry" : "base.character.gimli";
        state = CharacterFixture.Do(state, CommandKind.PlayEvent, cost,
            new SequenceRandom(0, state.Fellowship.Companions.IndexOf(casualty)));
        if (card == ShadowCharacterCards02.Strike)
        {
            var nazgul = NazgulEventTravel.Option(state).EligibleIds[0];
            state = CharacterFixture.Do(state, CommandKind.MoveShadowCharacters,
                Shadow02TravelTests.Flight(state, state.Fellowship.RegionId, nazgul));
            state = CharacterFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = [NazgulStrikeStep.Hunt] },
                new SequenceRandom(window == "reroll" ? 0 : 5, 0));
            if (window == "after-roll")
                state = CharacterFixture.Do(state, CommandKind.RespondEffect,
                    new() { ReactionId = ShadowCharacterCards03.CrebainReaction }, new SequenceRandom(4, 0));
        }
        if (card is ShadowCharacterCards01.OrcPatrol or ShadowCharacterCards02.Strike && window is "guide" or "placement")
            state = CharacterFixture.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.RandomCompanion },
                new SequenceRandom(state.Fellowship.Companions.IndexOf(casualty)));
        if (window == "placement")
            state = Shadow03Fixture.ResolveGuide(state);
        if (reveal && state.Pending?.Kind == DecisionKind.HuntDamage)
            state = CharacterFixture.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        if (window is "first" or "second")
            state = CharacterFixture.Do(state, CommandKind.RevealFellowship,
                new() { Route = [.. StrongholdHuntProjectionTests.Route] }, new SequenceRandom(0));
        if (window == "second")
            state = CharacterFixture.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }, new SequenceRandom(0));
        return state;
    }

    private static string Json(object value) => JsonSerializer.Serialize(value);
}
