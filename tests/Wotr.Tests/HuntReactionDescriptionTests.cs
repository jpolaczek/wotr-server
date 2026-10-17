using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class HuntReactionDescriptionTests
{
    [Theory]
    [InlineData(FreePeoplesCharacterCards01.Mithril, FreePeoplesCharacterCards01.ReplaceReaction, "hunt.replace", "replacement")]
    [InlineData(FreePeoplesCharacterCards01.Axe, FreePeoplesCharacterCards01.ProtectReaction, "hunt.table", "1")]
    [InlineData(FreePeoplesCharacterCards02.Horn, FreePeoplesCharacterCards02.HornReaction, "hunt.table", "1")]
    [InlineData(FreePeoplesCharacterCards02.Staff, FreePeoplesCharacterCards02.StaffReaction, "hunt.draw", "Do not draw")]
    public void TableReactionDescribesItsCurrentEffectSourceAndDiscardCost(string card, string reaction, string window, string detail)
    {
        var state = HuntTests.State();
        Shadow03Fixture.Table(state, card);
        HuntTests.Pool(state, HuntTests.Three);
        state = HuntTests.Move(state, new SequenceRandom(5, 0));
        Assert.Equal(window, state.Pending!.Window);
        var before = StateCodec.Write(state);
        var provider = new EffectOptions();
        var option = provider.GetOptions(state, Side.FreePeoples).Single(o => o.Kind == CommandKind.RespondEffect);
        var choice = Assert.Single(option.EffectChoices, c => c.Id == reaction);
        Assert.Equal(card, choice.SourceId);
        Assert.Equal("DiscardTableCard", choice.Cost);
        Assert.NotEmpty(choice.Title);
        Assert.Contains(detail, choice.Detail);
        Assert.Empty(provider.GetOptions(state, Side.Shadow));
        Assert.Equal(before, StateCodec.Write(state));
        var restored = provider.GetOptions(StateCodec.Copy(state), Side.FreePeoples).Single(o => o.Kind == CommandKind.RespondEffect);
        Assert.Equal(JsonSerializer.Serialize(option), JsonSerializer.Serialize(restored));
    }

    [Fact]
    public void CrebainBeforeRollAndWornAfterCasualtyKeepTheirDifferentCosts()
    {
        var crebain = HuntTests.State();
        Shadow03Fixture.Table(crebain, ShadowCharacterCards03.Crebain);
        crebain = HuntTests.Move(crebain, new SequenceRandom());
        var beforeRoll = new EffectOptions().GetOptions(crebain, Side.Shadow)
            .Single(o => o.Kind == CommandKind.RespondEffect).EffectChoices.Single();
        Assert.Equal("DiscardTableCard", beforeRoll.Cost);
        Assert.Contains("does not grant a reroll", beforeRoll.Detail);
        Assert.Empty(HuntRuntime.Active(crebain)!.Dice);

        var casualty = Shadow03HuntTests.WornChoice();
        var afterCasualty = new EffectOptions().GetOptions(casualty, Side.Shadow)
            .Single(o => o.Kind == CommandKind.RespondEffect).EffectChoices.Single(c => c.Id == ShadowCharacterCards03.WornReaction);
        Assert.Equal("None", afterCasualty.Cost);
        Assert.Equal(ShadowCharacterCards03.Worn, afterCasualty.SourceId);
        Assert.Contains("random", afterCasualty.Detail);
        Assert.Contains("if any", afterCasualty.Detail);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BalrogDescribesItsAdditionalTileAtRevelationAndDeclaration(bool declaration)
    {
        var state = declaration ? Shadow03Fixture.Declaration("moria") : HuntTests.State(region: "moria");
        if (declaration)
            state = CharacterFixture.Do(state, CommandKind.DeclareFellowship, new() { Route = ["moria"] });
        else
        {
            Shadow03Fixture.Table(state, ShadowCharacterCards03.Balrog);
            HuntTests.Pool(state, Shadow01Fixture.RevealOne);
            state = HuntTests.Move(state, new SequenceRandom(5, 0));
            state = HuntTests.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
            state = HuntTests.Do(state, CommandKind.RevealFellowship, new() { Route = ["moria"] });
            Assert.NotNull(HuntRuntime.Active(state));
        }
        Assert.Equal(declaration ? "fellowship.declared" : "fellowship.revealed", state.Pending!.Window);
        var reaction = ShadowCharacterCards03.BalrogReaction + (declaration ? ".declaration" : "");
        var before = StateCodec.Write(state);
        var provider = new EffectOptions();
        var choice = provider.GetOptions(state, Side.Shadow).Single(o => o.Kind == CommandKind.RespondEffect)
            .EffectChoices.Single(c => c.Id == reaction);
        Assert.Equal(ShadowCharacterCards03.Balrog, choice.SourceId);
        Assert.Equal("DiscardTableCard", choice.Cost);
        Assert.Equal("Extra Hunt tile", choice.Title);
        Assert.Contains("ignore Eye tiles", choice.Detail);
        Assert.Equal(declaration, choice.Detail.Contains("Also ignore Reveal", StringComparison.Ordinal));
        Assert.Empty(provider.GetOptions(state, Side.FreePeoples));
        Assert.Equal(before, StateCodec.Write(state));
    }
}
