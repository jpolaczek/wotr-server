using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsFixture;

namespace Wotr.Tests;

public class LordsNenyaTests
{
    private const string OtherEye = "base.hunt.standard.eye.1.2";

    [Fact]
    public void NenyaTransfersItsRingRemovesTheEyePermanentlyAndDrawsOnceAfterRestart()
    {
        var state = Window();
        Assert.Equal(DecisionKind.Reaction, state.Pending!.Kind);
        Assert.Contains(NenyaReaction.Id, state.Pending.PrivateChoices);
        var choice = Views().For(state, Side.FreePeoples).Options.Single(o => o.Kind == CommandKind.RespondEffect)
            .EffectChoices.Single(c => c.Id == NenyaReaction.Id);
        Assert.Equal("TransferNenya", choice.Cost);
        Assert.NotEmpty(choice.Title);
        var command = BattleFixture.Command(state, CommandKind.RespondEffect, new() { ReactionId = NenyaReaction.Id });
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples, command);
        Assert.True(preview.Legal);
        Assert.True(preview.HasUnknownResult);
        Assert.Equal(new[] { LordsDiceSetup.NenyaRing }, preview.Costs);
        Assert.Equal(before, StateCodec.Write(state));
        var rng = new SequenceRandom(0);
        state = Do(StateCodec.Copy(state), command.Kind, command.Payload, rng);
        Assert.Equal(1, rng.Calls);
        Assert.Equal("removed", state.HuntTiles.Single(t => t.Id == HuntTests.Eye).Location);
        Assert.Equal(HuntTests.Three, HuntRuntime.Active(state)!.TileId);
        Assert.Equal(3, HuntRuntime.Active(state)!.Damage);
        Assert.Equal(Side.Shadow, state.Rings.Single(r => r.Id == LordsDiceSetup.NenyaRing).Owner);
        Assert.False(state.Player(Side.FreePeoples).RingUsedThisTurn);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        HuntPool.CompleteAction(state);
        state.Fellowship.RegionId = "morannon";
        MordorRules.Enter(state);
        Assert.Equal("removed", state.HuntTiles.Single(t => t.Id == HuntTests.Eye).Location);
        Assert.Equal(3, state.Fellowship.Corruption);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlayerCanChooseEitherOrderWhenNenyaAndMithrilBothApply(bool nenyaFirst)
    {
        var state = Window(mithril: true);
        Assert.Contains(NenyaReaction.Id, state.Pending!.PrivateChoices);
        Assert.Contains(FreePeoplesCharacterCards01.ReplaceReaction, state.Pending.PrivateChoices);
        state.HuntTiles.Single(t => t.Id == HuntTests.Three).Location = "removed";
        state.HuntTiles.Single(t => t.Id == OtherEye).Location = "pool";
        state = Do(state, CommandKind.RespondEffect, new()
        {
            ReactionId = nenyaFirst ? NenyaReaction.Id : FreePeoplesCharacterCards01.ReplaceReaction
        }, new SequenceRandom(0));
        Assert.Equal(DecisionKind.Reaction, state.Pending!.Kind);
        Assert.Equal(OtherEye, HuntRuntime.Active(state)!.TileId);
        Assert.Equal(new[] { nenyaFirst ? FreePeoplesCharacterCards01.ReplaceReaction : NenyaReaction.Id }, state.Pending.PrivateChoices);
        state.HuntTiles.Single(t => t.Id == HuntTests.Three).Location = "pool";
        state = Do(StateCodec.Copy(state), CommandKind.RespondEffect, new()
        {
            ReactionId = nenyaFirst ? FreePeoplesCharacterCards01.ReplaceReaction : NenyaReaction.Id
        }, new SequenceRandom(0));
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        Assert.Equal(HuntTests.Three, HuntRuntime.Active(state)!.TileId);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal("removed", state.HuntTiles.Single(t => t.Id == (nenyaFirst ? HuntTests.Eye : OtherEye)).Location);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == (nenyaFirst ? OtherEye : HuntTests.Eye)).Location);
        Assert.Contains(FreePeoplesCharacterCards01.Mithril, state.Player(Side.FreePeoples).Discard);
    }

    [Theory]
    [InlineData("number")]
    [InlineData("special-eye")]
    [InlineData("dead-keeper")]
    [InlineData("ring-with-shadow")]
    [InlineData("base")]
    public void NenyaIsNotOfferedWithoutItsExactPublicConditions(string missing)
    {
        var state = Actions();
        Put(state, KeeperRings.Galadriel, missing == "dead-keeper" ? "casualties" : "lorien");
        var tileId = missing switch
        {
            "number" => HuntTests.Three,
            "special-eye" => HuntPool.Definitions.Values.First(t => t.Pool != "standard" && t.Value == "eye").Id,
            _ => HuntTests.Eye
        };
        if (missing == "ring-with-shadow")
            state.Rings.Single(r => r.Id == LordsDiceSetup.NenyaRing).Owner = Side.Shadow;
        if (missing == "base")
            state.Configuration.Lords = false;
        HuntRuntime.BeginTile(state, "test-draw");
        HuntRuntime.Active(state)!.TileId = tileId;
        HuntRuntime.Active(state)!.Stage = HuntStage.DetermineDamage;
        Assert.False(new EffectRuntime(EffectCatalog.Base).BeginPublicWindow(state, Side.FreePeoples, tileId, "hunt.replace"));
        Assert.Null(state.Pending);
    }

    [Fact]
    public void DecliningKeepsRingAndResolvesTheOriginalEyeWithoutDrawingAgain()
    {
        var state = Window();
        var samples = state.RandomSamples.Count;
        state = Do(state, CommandKind.DeclineReaction);
        Assert.Equal(HuntTests.Eye, HuntRuntime.Active(state)!.TileId);
        Assert.Equal(1, HuntRuntime.Active(state)!.Damage);
        Assert.Equal(samples, state.RandomSamples.Count);
        Assert.Equal(Side.FreePeoples, state.Rings.Single(r => r.Id == LordsDiceSetup.NenyaRing).Owner);
    }

    [Fact]
    public void RingVictoryDuringHuntFinishesImmediatelyWithoutOfferingVilya()
    {
        var state = LordsVilyaTests.Ready();
        state.Fellowship.Corruption = 11;
        var shadow = state.Dice.First(d => d.Owner == Side.Shadow);
        shadow.Location = DieLocation.Hunt;
        HuntTests.Pool(state, HuntTests.Three);
        state = Do(state, CommandKind.MoveFellowship, new() { DieId = state.Dice[0].Id }, new SequenceRandom(5, 0));
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal(VictoryCondition.RingCorrupted, state.Victory);
        Assert.Null(state.Pending);
        Assert.Null(state.Lords!.ActionCompletion);
        Assert.Empty(state.Resolution.PaidDieIds);
    }

    internal static MatchState Window(bool mithril = false)
    {
        var state = Actions();
        Put(state, KeeperRings.Galadriel, "lorien");
        var shadow = state.Dice.First(d => d.Owner == Side.Shadow);
        shadow.Location = DieLocation.Hunt;
        shadow.Face = DieFace.Eye;
        shadow.HuntOrigin = "allocated";
        if (mithril)
        {
            foreach (var deck in state.Player(Side.FreePeoples).Decks.Values)
                deck.Remove(FreePeoplesCharacterCards01.Mithril);
            state.Player(Side.FreePeoples).Table.Add(FreePeoplesCharacterCards01.Mithril);
        }
        HuntTests.Pool(state, HuntTests.Eye);
        state = Do(state, CommandKind.MoveFellowship, new() { DieId = state.Dice[0].Id }, new SequenceRandom(5, 0));
        state.HuntTiles.Single(t => t.Id == HuntTests.Three).Location = "pool";
        return state;
    }
}
