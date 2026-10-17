using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards01;
using static Wotr.Domain.FreePeoplesCharacterCards01;
using static Wotr.Domain.FreePeoplesCharacterCards02;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;

namespace Wotr.Tests;

public class Shadow01HuntTests
{
    [Theory]
    [InlineData(ProtectReaction)]
    [InlineData(HornReaction)]
    public void PatrolAllowsExactlyOneTableProtectionThenOneCasualtyAndNewGuide(string reaction)
    {
        var state = BeforeEvent(OrcPatrol);
        state.Player(Side.FreePeoples).Table.AddRange([Axe, Horn]);
        state.Fellowship.Guide = "base.character.legolas";
        Pool(state, HuntTests.Three);
        state = Play(state, OrcPatrol, 0);
        Assert.Equal("hunt.table", state.Pending!.Window);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = reaction });
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        Assert.Equal(2, HuntRuntime.Active(state)!.Damage);
        Reject(state, CommandKind.RespondEffect, new() { ReactionId = reaction == HornReaction ? ProtectReaction : HornReaction });
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Guide });
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        state = Do(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider });
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.DoesNotContain("base.character.legolas", state.Fellowship.Companions);
        Assert.Single(state.Player(Side.FreePeoples).Table);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(CharacterRules.Grey)]
    [InlineData("base.character.merry")]
    [InlineData("base.character.pippin")]
    [InlineData(CharacterRules.Gollum)]
    public void BaneAppliesDirectCorruptionWithoutAnyDamageReductionWindow(string guide)
    {
        var state = BeforeEvent(IsildursBane);
        state.Fellowship.Guide = guide;
        if (guide == CharacterRules.Gollum)
            state.Fellowship.Companions.Clear();
        var count = state.Fellowship.Companions.Count;
        state.Player(Side.FreePeoples).Table.AddRange([Axe, Horn]);
        Pool(state, HuntTests.Three);
        state = Play(state, IsildursBane, 0);
        Assert.Equal(3, state.Fellowship.Corruption);
        Assert.Equal(count, state.Fellowship.Companions.Count);
        Assert.True(state.Fellowship.Hidden);
        Assert.Equal(2, state.Player(Side.FreePeoples).Table.Count);
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.Events, e => e.Kind is "HuntCasualty" or "CharacterAbilityUsed");
        Reject(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Guide });
    }

    [Fact]
    public void MithrilRemainsAvailableForBaneBeforeShelobRollAndReplacementStillUsesTheEventFilter()
    {
        var state = BeforeEvent(IsildursBane, mordor: true);
        state.Player(Side.FreePeoples).Table.AddRange([Mithril, Axe, Horn]);
        EventTests.GiveCard(state, Side.FreePeoples, "base.fp.character.24");
        Pool(state, ShadowCharacterCards01.SpecialTiles[0].Tile, HuntTests.Eye);
        state = Play(state, IsildursBane, 0);
        Assert.Equal("hunt.replace", state.Pending!.Window);
        var view = HuntTests.Views().For(state, Side.FreePeoples);
        Assert.True(view.Hunt!.CorruptionOnly);
        Assert.True(view.Hunt.DamagePending);
        Assert.Null(view.Hunt.TileDie);
        Assert.False(view.Hunt.GuideReturns);
        var other = JsonSerializer.Serialize(HuntTests.Views().For(state, Side.Shadow), BoardCatalog.Json);
        state.HuntTiles.Reverse();
        HandRules.Remove(state.Player(Side.FreePeoples), "base.fp.character.24");
        HandRules.Add(state.Player(Side.FreePeoples), "base.fp.character.23");
        Assert.Equal(other, JsonSerializer.Serialize(HuntTests.Views().For(state, Side.Shadow), BoardCatalog.Json));
        Assert.Null(HuntTests.Views().For(state, Side.Shadow).Decision);
        state = Do(StateCodec.Copy(state), CommandKind.RespondEffect, new() { ReactionId = ReplaceReaction }, new SequenceRandom(0));
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Equal(2, state.RandomSamples.Count);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == ShadowCharacterCards01.SpecialTiles[0].Tile).Location);
        Assert.Contains(Axe, state.Player(Side.FreePeoples).Table);
        Assert.Contains(Horn, state.Player(Side.FreePeoples).Table);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(OrcPatrol)]
    [InlineData(IsildursBane)]
    public void GollumIgnoresStandardNumberedRevealButNotShadowSpecialReveal(string card)
    {
        foreach (var special in new[] { false, true })
        {
            var state = BeforeEvent(card, mordor: true);
            state.Fellowship.Companions.Clear();
            state.Fellowship.Guide = CharacterRules.Gollum;
            Pool(state, special ? ShadowCharacterCards01.SpecialTiles[3].Tile : RevealOne);
            state = Play(state, card, 0);
            if (!special && card == OrcPatrol)
            {
                Assert.Equal("hunt.guide", state.Pending!.Window);
                state = Do(state, CommandKind.DeclineReaction);
            }
            Assert.Equal(!special, state.Fellowship.Hidden);
            Assert.Equal(1, state.Fellowship.Corruption);
            Assert.Equal(2, state.Fellowship.MordorStep);
        }
    }

    [Fact]
    public void BaneRevelationStartsAnIndependentStrongholdHuntThatCanBeReduced()
    {
        var state = BeforeEvent(IsildursBane, "moria");
        state.Fellowship.Progress = 1;
        state.Player(Side.FreePeoples).Table.Add(Axe);
        Pool(state, RevealOne, HuntTests.Three);
        state = Play(state, IsildursBane, 0);
        Assert.Equal(DecisionKind.FellowshipRoute, state.Pending!.Kind);
        Assert.Equal(1, state.Fellowship.Corruption);
        state = Do(StateCodec.Copy(state), CommandKind.RevealFellowship, new() { Route = ["moria", "dimrill-dale"] }, new SequenceRandom(0));
        Assert.Equal("hunt.table", state.Pending!.Window);
        Assert.Equal(HuntDamageRule.Normal, HuntRuntime.Active(state)!.DamageRule);
        Assert.False(HuntOptions.View(state)!.CorruptionOnly);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = ProtectReaction });
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal(3, state.Fellowship.Corruption);
        Assert.Equal("dimrill-dale", state.Fellowship.RegionId);
        Assert.False(state.Fellowship.Hidden);
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Fact]
    public void BaneEndsTheGameAtTwelveBeforeAnyRevelationChoice()
    {
        var state = BeforeEvent(IsildursBane);
        state.Fellowship.Corruption = 11;
        Pool(state, RevealOne);
        state = Play(state, IsildursBane, 0);
        Assert.Equal(12, state.Fellowship.Corruption);
        Assert.Equal(Side.Shadow, state.Winner);
        Assert.Equal(VictoryCondition.RingCorrupted, state.Victory);
        Assert.Null(state.Pending);
    }
}
