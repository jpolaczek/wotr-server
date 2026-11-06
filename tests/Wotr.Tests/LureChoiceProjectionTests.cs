using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow03Fixture;

namespace Wotr.Tests;

public sealed class LureChoiceProjectionTests
{
    private static readonly Projection Projector = GameRuntime.Projection(EffectCatalog.All);

    [Fact]
    public void PaidLureChoiceHasSharedCardAndPaymentFactsButTargetOnlyInChooserOptions()
    {
        var state = At();
        var before = StateCodec.Write(state);
        var freePeoples = Projector.For(state, Side.FreePeoples);
        var shadow = Projector.For(state, Side.Shadow);
        var facts = Assert.IsType<ActionBoundaryView>(freePeoples.ActionBoundary);
        var choices = Assert.Single(freePeoples.Options).EffectChoices;
        Assert.Equal(2, choices.Count);
        var target = Assert.Single(choices.Select(choice => choice.SourceId).Distinct(StringComparer.Ordinal));

        Assert.Equal(19, facts.Version);
        Assert.Equal(ShadowCharacterCards03.Lure, facts.LureChoice!.CardId);
        Assert.Equal(DieFace.Character, facts.LureChoice.CardCost);
        Assert.Null(facts.CardHunt);
        Assert.Empty(shadow.Options);
        Assert.Equal(JsonSerializer.Serialize(facts.LureChoice),
            JsonSerializer.Serialize(shadow.ActionBoundary!.LureChoice));
        Assert.DoesNotContain(target, JsonSerializer.Serialize(facts.LureChoice), StringComparison.Ordinal);
        Assert.Equal(before, StateCodec.Write(state));
    }

    [Fact]
    public void RandomTargetPermutationDoesNotChangeSharedLureBoundaryFacts()
    {
        var first = At();
        var second = StateCodec.Copy(first);
        var target = first.Resolution.Frames.Single().Effect!.TargetIds.Single();
        var other = second.Fellowship.Companions.First(id => id != target);
        second.Resolution.Frames.Single().Effect!.TargetIds = [other];

        Assert.Equal(JsonSerializer.Serialize(Projector.For(first, Side.Shadow).ActionBoundary),
            JsonSerializer.Serialize(Projector.For(second, Side.Shadow).ActionBoundary));
        Assert.NotEqual(
            Assert.Single(Projector.For(first, Side.FreePeoples).Options).EffectChoices[0].SourceId,
            Assert.Single(Projector.For(second, Side.FreePeoples).Options).EffectChoices[0].SourceId);
    }

    [Theory]
    [InlineData("cursor")]
    [InlineData("card")]
    [InlineData("target")]
    [InlineData("choice-source")]
    [InlineData("choice-owner")]
    [InlineData("required-count")]
    [InlineData("palantir")]
    [InlineData("gollum")]
    [InlineData("nested")]
    public void UnsupportedOrInconsistentLureContinuationsOmitTheActionBoundary(string change)
    {
        var state = At();
        switch (change)
        {
            case "cursor": state.Resolution.Frames[0].NextStep = 1; break;
            case "card": state.Resolution.Frames[0].Effect!.SourceId = ShadowCharacterCards03.Breaking; break;
            case "target": state.Resolution.Frames[0].Effect!.TargetIds.Clear(); break;
            case "choice-source": state.Pending!.SourceId = ShadowCharacterCards03.Breaking; break;
            case "choice-owner": state.Pending!.Owner = Side.Shadow; break;
            case "required-count": state.Pending!.RequiredCount = 2; break;
            case "palantir": state.Resolution.Frames[0].Effect!.PalantirDrawPending = true; break;
            case "gollum": state.Fellowship.Guide = CharacterRules.Gollum; break;
            case "nested": EffectRuntime.BeginProgram(state, Side.FreePeoples, "unknown", "unknown"); break;
        }

        Assert.Null(Projector.For(state, Side.FreePeoples).ActionBoundary);
        Assert.Null(Projector.For(state, Side.Shadow).ActionBoundary);
    }

    private static MatchState At()
    {
        var state = RevealedEvent(ShadowCharacterCards03.Lure);
        var target = "base.character.gimli";
        return Play(state, ShadowCharacterCards03.Lure, state.Fellowship.Companions.IndexOf(target));
    }
}
