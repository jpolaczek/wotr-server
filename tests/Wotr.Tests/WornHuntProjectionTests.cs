using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public sealed class WornHuntProjectionTests
{
    [Theory]
    [InlineData("movement")]
    [InlineData("stronghold")]
    [InlineData("balrog")]
    [InlineData(ShadowCharacterCards01.OrcPatrol)]
    [InlineData(ShadowCharacterCards02.FoulThing)]
    [InlineData(ShadowCharacterCards02.Strike)]
    public void NestedWornChoicePreservesItsHuntAndDoesNotRevealTheOtherHand(string parent)
    {
        var state = At(parent);
        var projection = GameRuntime.Projection(EffectCatalog.All);
        var view = projection.For(state, Side.Shadow);
        Assert.NotNull(view.ActionBoundary);
        Assert.Equal(DecisionKind.EffectChoice, view.Decision!.Kind);
        Assert.Equal(ShadowCharacterCards03.Worn, view.Decision.SourceId);
        Assert.Equal("Guide", view.Hunt!.Stage);
        Assert.True(view.Hunt.CasualtyTaken);
        Assert.Contains(view.Options.Single().EligibleIds, id => id == WornDiscardStep.RandomHand);
        var before = JsonSerializer.Serialize(view);
        var other = state.Player(Side.FreePeoples);
        (other.Hand[0], other.Decks["character"][0]) = (other.Decks["character"][0], other.Hand[0]);
        foreach (var deck in other.Decks.Values)
            deck.Reverse();
        state.HuntTiles.Reverse();
        state.RandomSamples.Add(new() { Value = 987 });
        Assert.Equal(before, JsonSerializer.Serialize(projection.For(state, Side.Shadow)));
        var waiting = projection.For(state, Side.FreePeoples);
        Assert.NotNull(waiting.ActionBoundary);
        Assert.Null(waiting.Decision);
        Assert.Empty(waiting.Options);
    }

    [Theory]
    [InlineData("table")]
    [InlineData("casualty")]
    [InlineData("stage")]
    [InlineData("reaction")]
    [InlineData("cursor")]
    [InlineData("return")]
    public void InconsistentNestedChoicesOmitTheBoundary(string change)
    {
        var state = At("movement");
        switch (change)
        {
            case "table": state.Player(Side.Shadow).Table.Remove(ShadowCharacterCards03.Worn); break;
            case "casualty": HuntRuntime.Active(state)!.CasualtyTaken = false; break;
            case "stage": HuntRuntime.Active(state)!.Stage = HuntStage.Damage; break;
            case "reaction": state.Resolution.Frames[^2].Effect!.ReactionWindow = "hunt.roll"; break;
            case "cursor": state.Resolution.Frames[^1].NextStep = 0; break;
            case "return": state.Resolution.Frames[^1].Effect!.Return = EffectReturn.FinishAction; break;
        }
        Assert.Null(new Projection().For(state, Side.Shadow).ActionBoundary);
    }

    internal static MatchState At(string parent)
    {
        var state = parent switch
        {
            "movement" => FellowshipMoveProjectionTests.Position("damage"),
            "stronghold" => StrongholdHuntProjectionTests.At("first"),
            "balrog" => BalrogHuntProjectionTests.At(BalrogHuntProjectionTests.Movement, "damage"),
            _ => CardHuntProjectionTests.At(parent, parent == ShadowCharacterCards02.FoulThing ? "guide" : "damage")
        };
        state.Id = Guid.NewGuid().ToString("N");
        Shadow03Fixture.Table(state, ShadowCharacterCards03.Worn);
        CardFlow.Draw(state, Side.FreePeoples, "character");
        if (state.Pending!.Kind == DecisionKind.HuntDamage)
            state = CharacterFixture.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.RandomCompanion },
                new SequenceRandom(state.Fellowship.Companions.IndexOf("base.character.gimli")));
        state = Shadow03Fixture.ResolveGuide(state);
        Assert.Equal("hunt.casualty", state.Pending!.Window);
        return CharacterFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = ShadowCharacterCards03.WornReaction });
    }
}
