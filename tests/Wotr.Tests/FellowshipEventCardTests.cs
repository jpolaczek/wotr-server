using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Domain.FreePeoplesCharacterCards02;

namespace Wotr.Tests;

public class FellowshipEventCardTests
{
    internal static MatchState Play(MatchState state, string card, params int[] rolls)
    {
        EventTests.GiveCard(state, Side.FreePeoples, card);
        var payload = Cost(state, DieFace.Character);
        payload.CardId = card;
        payload.CardCost = DieFace.Character;
        return Do(state, CommandKind.PlayEvent, payload, new SequenceRandom(rolls));
    }

    [Theory]
    [InlineData(Horn)]
    [InlineData(Staff)]
    [InlineData(Athelas)]
    [InlineData(AnotherWay)]
    [InlineData(Alone)]
    [InlineData(Bilbo)]
    public void EveryEventRejectsMissingCardWrongFaceAndSpentCost(string card)
    {
        var state = TurnTests.Actions();
        EventTests.GiveCard(state, Side.FreePeoples, card);
        var payload = EventTests.Play(state, card, DieFace.Character).Payload;
        payload.CardCost = DieFace.Army;
        Reject(state, CommandKind.PlayEvent, payload);
        payload.CardCost = DieFace.Character;
        state.Dice[0].Location = DieLocation.Spent;
        Reject(state, CommandKind.PlayEvent, payload);
        state.Dice[0].Location = DieLocation.Available;
        HandRules.Remove(state.Player(Side.FreePeoples), card);
        Reject(state, CommandKind.PlayEvent, payload);
    }

    [Theory]
    [InlineData(Horn, Boromir)]
    [InlineData(Staff, CharacterRules.Grey)]
    public void TableCardRequiresItsCompanionAndExpiresBeforeTheNextGuideChoice(string card, string companion)
    {
        var state = Play(TurnTests.Actions(), card);
        Assert.Contains(card, state.Player(Side.FreePeoples).Table);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        var payload = Cost(state, DieFace.Character);
        payload.Ids = [companion];
        payload.Route = [state.Fellowship.RegionId];
        state = Do(state, CommandKind.SeparateCompanions, payload);
        Assert.DoesNotContain(card, state.Player(Side.FreePeoples).Table);
        Assert.Contains(card, state.Player(Side.FreePeoples).Discard);
        Assert.Empty(state.ActiveEffects);
        if (state.Pending!.Kind == DecisionKind.Guide)
            state = Do(state, CommandKind.SelectGuide, new() { GuideId = state.Pending.PrivateChoices[0] });
        TurnFlow.SelectNext(state, Side.FreePeoples);
        HandRules.Add(state.Player(Side.FreePeoples), card);
        payload = Cost(state, DieFace.Character);
        payload.CardId = card;
        payload.CardCost = DieFace.Character;
        Reject(state, CommandKind.PlayEvent, payload);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    public void StaffPreventsUnknownDrawAndMordorStillAdvances(bool mordor, int samples)
    {
        var state = StaffWindow(mordor);
        Assert.Equal("hunt.draw", state.Pending!.Window);
        Assert.Equal("", HuntOptions.View(state)!.TileId);
        Assert.True(HuntOptions.View(state)!.DamagePending);
        Assert.Equal(samples, state.RandomSamples.Count);
        Assert.Null(HuntTests.Views().For(state, Side.Shadow).Decision);
        state = Do(StateCodec.Copy(state), CommandKind.RespondEffect, new() { ReactionId = StaffReaction });
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Equal(mordor ? 1 : null, state.Fellowship.MordorStep);
        Assert.Contains(Staff, state.Player(Side.FreePeoples).Discard);
        Assert.Equal(DieLocation.Hunt, state.Dice[0].Location);
        Assert.DoesNotContain(state.Events, e => e.Kind == "HuntTileDrawn");
        Assert.Equal(samples, state.RandomSamples.Count);
    }

    internal static MatchState StaffWindow(bool mordor = true)
    {
        var state = HuntTests.State();
        state.RandomSamples.Clear();
        state.Player(Side.FreePeoples).Table.Add(Staff);
        if (mordor)
        {
            state.Fellowship.MordorStep = 0;
            state.Fellowship.RegionId = MordorRules.Track;
        }
        HuntTests.Pool(state, HuntTests.Three);
        return HuntTests.Move(state, new SequenceRandom(5));
    }

    [Fact]
    public void StaffIsNotOfferedForFailedHuntFreePeoplesDrawOrMithrilReplacement()
    {
        var state = HuntTests.State();
        state.Player(Side.FreePeoples).Table.Add(Staff);
        state = HuntTests.Move(state, new SequenceRandom(0));
        Assert.Empty(state.Resolution.Frames);
        Assert.Contains(Staff, state.Player(Side.FreePeoples).Table);
        HuntTests.Pool(state, HuntTests.Three);
        HuntRuntime.BeginTile(state, "fp-draw");
        HuntRuntime.Active(state)!.DrawnBy = Side.FreePeoples;
        HuntResolutionTests.Pump(state, EffectCatalog.Base, new SequenceRandom(0));
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        state = StaffWindow();
        state.Player(Side.FreePeoples).Table.Add(FreePeoplesCharacterCards01.Mithril);
        state.HuntTiles.Single(t => t.Id == HuntTests.Eye).Location = "pool";
        state = Do(state, CommandKind.DeclineReaction, random: new SequenceRandom(1));
        Assert.Equal("hunt.replace", state.Pending!.Window);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = FreePeoplesCharacterCards01.ReplaceReaction }, new SequenceRandom(0));
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        Assert.Contains(Staff, state.Player(Side.FreePeoples).Table);
    }

    [Theory]
    [InlineData(HuntTests.Three, 2)]
    [InlineData("base.hunt.standard.0.1.1", 0)]
    public void HornReducesDamageAndUsesTheSameSingleProtectionWindowAsAxe(string tile, int damage)
    {
        var state = HuntTests.State();
        HuntTests.Pool(state, tile);
        state.Player(Side.FreePeoples).Table.AddRange([Horn, FreePeoplesCharacterCards01.Axe]);
        state = HuntTests.Move(state, new SequenceRandom(5, 0));
        Assert.Equal("hunt.table", state.Pending!.Window);
        Assert.Equal(2, state.Pending.PrivateChoices.Count);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = HornReaction });
        Assert.Equal(damage, HuntRuntime.Active(state)!.Damage);
        Assert.DoesNotContain(Horn, state.Player(Side.FreePeoples).Table);
        Assert.Contains(FreePeoplesCharacterCards01.Axe, state.Player(Side.FreePeoples).Table);
        Assert.NotEqual(DecisionKind.Reaction, state.Pending!.Kind);
    }

    [Fact]
    public void StaffMayBeKeptForTheAdditionalStrongholdTileInTheSameAction()
    {
        var state = HuntTests.State(region: "moria");
        state.RandomSamples.Clear();
        state.Player(Side.FreePeoples).Table.Add(Staff);
        HuntTests.Pool(state, HuntTests.Eye, HuntTests.Three);
        state = HuntTests.Move(state, new SequenceRandom(5));
        state = Do(state, CommandKind.DeclineReaction, random: new SequenceRandom(0));
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        state = Do(state, CommandKind.RevealFellowship, new() { Route = ["moria", "dimrill-dale"] });
        Assert.Equal("hunt.draw", state.Pending!.Window);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = StaffReaction });
        Assert.Single(state.Events, e => e.Kind == "HuntTileDrawn");
        Assert.Equal(2, state.RandomSamples.Count);
        Assert.Equal(1, state.Fellowship.Corruption);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(false, 4, 3)]
    [InlineData(true, 4, 2)]
    [InlineData(true, 0, 0)]
    public void AthelasRollsThreeDiceAtTheGuideThresholdAndNeverHealsBelowZero(bool strider, int before, int after)
    {
        var state = TurnTests.Actions();
        state.RandomSamples.Clear();
        state.Fellowship.Guide = strider ? CharacterRules.Strider : CharacterRules.Grey;
        state.Fellowship.Corruption = before;
        state = Play(state, Athelas, 0, 2, 4);
        Assert.Equal(after, state.Fellowship.Corruption);
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Single(state.Events, e => e.Kind == "HealingRoll");
        Assert.Empty(state.Resolution.Frames);
    }
}
