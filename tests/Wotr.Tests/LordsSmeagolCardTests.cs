using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsFixture;

namespace Wotr.Tests;

public class LordsSmeagolCardTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LureTreatsSmeagolAsACompanionWithDynamicLevelInsteadOfGollum(bool eliminate)
    {
        var state = LordsSmeagolTests.State(true);
        state.Fellowship.Hidden = false;
        state.Fellowship.Companions.Remove(CharacterRules.Grey);
        state.Fellowship.Companions.Remove(CharacterRules.Strider);
        TurnFlow.SelectNext(state, Side.Shadow);
        EventTests.GiveCard(state, Side.Shadow, ShadowCharacterCards03.Lure);
        state = Do(state, CommandKind.PlayEvent, EventTests.Play(state, ShadowCharacterCards03.Lure, DieFace.Character).Payload,
            new SequenceRandom(state.Fellowship.Companions.IndexOf(LordsCharacters.Smeagol)));
        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        Assert.Equal(0, state.Fellowship.Corruption);
        var choices = Views().For(state, Side.FreePeoples).Options.Single(o => o.Kind == CommandKind.ChooseEffect).EffectChoices;
        Assert.Equal("Corruption +2", choices.Single(c => c.Id == LureChoiceStep.Corruption).Title);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [eliminate ? LureChoiceStep.Eliminate : LureChoiceStep.Corruption] });
        Assert.Equal(eliminate ? 0 : 2, state.Fellowship.Corruption);
        Assert.Equal(eliminate, state.Player(Side.Shadow).Table.Contains(SmeagolRules.WeShallGetIt));
        Assert.Equal(!eliminate, SmeagolRules.Present(state));
    }

    [Fact]
    public void BreakingDiscardsSmeagolAndItsPreviewNeverPlacesHimOnTheMap()
    {
        var state = LordsSmeagolTests.State(true);
        state.Fellowship.Hidden = false;
        TurnFlow.SelectNext(state, Side.Shadow);
        var tile = state.HuntTiles.First(t => HuntPool.Definitions[t.Id] is { Pool: "standard", Value: "1" }).Id;
        HuntTests.Pool(state, tile);
        EventTests.GiveCard(state, Side.Shadow, ShadowCharacterCards03.Breaking);
        state = Do(state, CommandKind.PlayEvent, EventTests.Play(state, ShadowCharacterCards03.Breaking, DieFace.Character).Payload,
            new SequenceRandom(0));
        var command = BattleFixture.Command(state, CommandKind.ChooseEffect, new() { Ids = [LordsCharacters.Smeagol] });
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples, command);
        Assert.True(preview.Legal);
        Assert.Equal(new[] { LordsCharacters.Smeagol }, preview.RemovedIds);
        Assert.Empty(preview.Placements);
        state = Do(state, command.Kind, command.Payload);
        Assert.Equal("removed", state.Characters.Single(c => c.Id == LordsCharacters.Smeagol).Location);
        Assert.Contains(SmeagolRules.WeShallGetIt, state.Player(Side.Shadow).Table);
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
    }
}
