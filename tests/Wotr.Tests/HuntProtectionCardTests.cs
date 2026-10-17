using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Domain.FreePeoplesCharacterCards01;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

public class HuntProtectionCardTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void SpecialTileEventsPrepareExactlyTheirOwnTileAndCannotDuplicateIt(int index, bool mordor)
    {
        var (card, tile) = SpecialTiles[index];
        var state = TurnTests.Actions();
        if (mordor)
            state.Fellowship.MordorStep = 1;
        EventTests.GiveCard(state, Side.FreePeoples, card);
        var command = EventTests.Play(state, card, DieFace.Character);
        state = EventTests.Execute(state, command);
        Assert.Equal(mordor ? "pool" : "prepared", state.HuntTiles.Single(t => t.Id == tile).Location);
        Assert.Equal(3, state.HuntTiles.Count(t => HuntPool.Definitions[t.Id].Pool == "special-fp" && t.Location == "set-aside"));
        Assert.Contains(card, state.Player(Side.FreePeoples).Discard);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == command.Payload.DieId).Location);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        HandRules.Add(state.Player(Side.FreePeoples), card); // A forged second copy cannot create a tile.
        Reject(state, CommandKind.PlayEvent, EventTests.Play(state, card, DieFace.Character).Payload);
    }

    [Theory]
    [InlineData("base.fp.character.01")]
    [InlineData("base.fp.character.02")]
    [InlineData("base.fp.character.03")]
    [InlineData("base.fp.character.04")]
    [InlineData(Mithril)]
    [InlineData(Axe)]
    public void EachEventRequiresAnOwnedCardAndCorrectAvailableCost(string card)
    {
        var state = TurnTests.Actions();
        EventTests.GiveCard(state, Side.FreePeoples, card);
        var payload = EventTests.Play(state, card, DieFace.Character).Payload;
        payload.CardCost = DieFace.Army;
        Reject(state, CommandKind.PlayEvent, payload);
        payload.CardCost = DieFace.Character;
        state.Dice.Single(d => d.Id == payload.DieId).Location = DieLocation.Spent;
        Reject(state, CommandKind.PlayEvent, payload);
        state.Dice.Single(d => d.Id == payload.DieId).Location = DieLocation.Available;
        HandRules.Remove(state.Player(Side.FreePeoples), card);
        Reject(state, CommandKind.PlayEvent, payload);
    }

    [Fact]
    public void TableCardsStayAfterPlayingAndAxeExpiresAsSoonAsBothCompanionsLeave()
    {
        var state = TurnTests.Actions();
        foreach (var card in new[] { Mithril, Axe })
        {
            TurnFlow.SelectNext(state, Side.FreePeoples);
            EventTests.GiveCard(state, Side.FreePeoples, card);
            state = EventTests.Execute(state, EventTests.Play(state, card, DieFace.Character));
            Assert.Contains(card, state.Player(Side.FreePeoples).Table);
        }
        foreach (var id in AxeCompanions)
        {
            TurnFlow.SelectNext(state, Side.FreePeoples);
            var cost = Cost(state, DieFace.Character);
            cost.Ids = [id];
            cost.Route = [state.Fellowship.RegionId];
            state = Do(state, CommandKind.SeparateCompanions, cost);
            Assert.Equal(id == AxeCompanions[0], state.Player(Side.FreePeoples).Table.Contains(Axe));
        }
        Assert.Contains(Axe, state.Player(Side.FreePeoples).Discard);
        Assert.Contains(Mithril, state.Player(Side.FreePeoples).Table);
        Assert.Single(state.Events, e => e.Kind == "EffectExpired" && e.Text.Contains(Axe));
        state = TurnTests.Actions();
        state.Fellowship.Companions.RemoveAll(AxeCompanions.Contains);
        HandRules.Add(state.Player(Side.FreePeoples), Axe);
        var payload = Cost(state, DieFace.Character);
        payload.CardId = Axe;
        payload.CardCost = DieFace.Character;
        Reject(state, CommandKind.PlayEvent, payload);
    }

    internal static MatchState ShelobReplacement()
    {
        var state = HuntTests.State();
        state.RandomSamples.Clear();
        state.Fellowship.MordorStep = 0;
        state.Fellowship.RegionId = MordorRules.Track;
        state.Player(Side.FreePeoples).Table.AddRange([Mithril, Axe]);
        HuntTests.Pool(state, "base.hunt.special-sh.die.0.1", HuntTests.Three);
        return HuntTests.Move(state, new SequenceRandom(1));
    }

    [Fact]
    public void MithrilPrecedesShelobRollAndAxeCanProtectTheReplacementWithOneCostEach()
    {
        var state = ShelobReplacement();
        Assert.Equal("hunt.replace", state.Pending!.Window);
        var hunt = HuntTests.Views().For(state, Side.FreePeoples).Hunt!;
        Assert.True(hunt.DamagePending);
        Assert.Null(hunt.TileDie);
        Assert.Single(state.RandomSamples);
        var enemy = HuntTests.Views().For(state, Side.Shadow);
        Assert.Null(enemy.Decision);
        Assert.Empty(enemy.Options);
        state = Do(StateCodec.Copy(state), CommandKind.RespondEffect, new() { ReactionId = ReplaceReaction }, new SequenceRandom(0));
        Assert.Equal("hunt.table", state.Pending!.Window);
        Assert.Equal(HuntTests.Three, HuntRuntime.Active(state)!.TileId);
        Assert.Null(HuntRuntime.Active(state)!.TileDie);
        Assert.Equal("return-after-action", state.HuntTiles.Single(t => t.Id.Contains("special-sh.die")).Location);
        Reject(state, CommandKind.RespondEffect, new() { ReactionId = ReplaceReaction });
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = ProtectReaction });
        Assert.Equal(2, HuntRuntime.Active(state)!.Damage);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal(2, state.Fellowship.Corruption);
        Assert.Equal(1, state.Fellowship.MordorStep);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id.Contains("special-sh.die")).Location);
        Assert.Equal(2, state.Player(Side.FreePeoples).Discard.Count);
        Assert.Equal(DieLocation.Hunt, state.Dice[0].Location);
        Assert.Equal(2, state.RandomSamples.Count);
    }

    [Fact]
    public void DecliningMithrilRollsShelobOnceBeforeTheProtectionDecision()
    {
        var state = Do(ShelobReplacement(), CommandKind.DeclineReaction, random: new SequenceRandom(4));
        Assert.Equal("hunt.table", state.Pending!.Window);
        Assert.Equal(5, HuntRuntime.Active(state)!.TileDie);
        Assert.False(HuntOptions.View(state)!.DamagePending);
        Assert.Contains(Mithril, state.Player(Side.FreePeoples).Table);
        state = Do(StateCodec.Copy(state), CommandKind.DeclineReaction);
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        Assert.Equal(2, state.RandomSamples.Count);
    }

    [Fact]
    public void ReplacementWaitsForAdditionalStrongholdHuntBeforeReturningOriginalTile()
    {
        var state = HuntTests.State(region: "moria");
        const string zero = "base.hunt.standard.0.1.1";
        HuntTests.Pool(state, HuntTests.Three, HuntTests.Eye, zero);
        state.Player(Side.FreePeoples).Table.Add(Mithril);
        state = HuntTests.Move(state, new SequenceRandom(5, 1));
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = ReplaceReaction }, new SequenceRandom(0));
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal("return-after-action", state.HuntTiles.Single(t => t.Id == HuntTests.Three).Location);
        Assert.Equal("spent", state.HuntTiles.Single(t => t.Id == HuntTests.Eye).Location);
        state = Do(StateCodec.Copy(state), CommandKind.RevealFellowship, new() { Route = ["moria", "dimrill-dale"] }, new SequenceRandom(0));
        Assert.Equal(1, state.Fellowship.Corruption);
        Assert.Contains(zero, state.Events.Last(e => e.Kind == "HuntTileDrawn").Text);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == HuntTests.Three).Location);
        Assert.Equal("discard", state.HuntTiles.Single(t => t.Id == HuntTests.Eye).Location);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void EmptyPoolRefillsOnlyOldDiscardsAndAxeCannotMakeNegativeDamage()
    {
        var state = HuntTests.State();
        HuntTests.Pool(state, HuntTests.Three);
        const string zero = "base.hunt.standard.0.1.1";
        state.HuntTiles.Single(t => t.Id == zero).Location = "discard";
        state.Player(Side.FreePeoples).Table.AddRange([Mithril, Axe]);
        state = HuntTests.Move(state, new SequenceRandom(5, 0));
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = ReplaceReaction }, new SequenceRandom(0));
        Assert.Equal(zero, HuntRuntime.Active(state)!.TileId);
        Assert.Equal(0, HuntRuntime.Active(state)!.Damage);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = ProtectReaction });
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Equal(DecisionKind.FellowshipRoute, state.Pending!.Kind);
        state = Do(state, CommandKind.RevealFellowship, new() { Route = ["hollin"] });
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == HuntTests.Three).Location);
        Assert.Equal("discard", state.HuntTiles.Single(t => t.Id == zero).Location);
    }

    [Fact]
    public void AxeExpiresAfterTheLastRequiredCompanionDiesBeforeTheNewGuideResponds()
    {
        var state = HuntTests.State();
        state.Fellowship.Companions = ["base.character.gimli"];
        state.Fellowship.Guide = "base.character.gimli";
        EventTests.GiveCard(state, Side.FreePeoples, Axe);
        state = EventTests.Execute(state, EventTests.Play(state, Axe, DieFace.Character));
        TurnFlow.SelectNext(state, Side.FreePeoples);
        HuntTests.Pool(state, HuntTests.Three);
        state = HuntTests.Move(state, new SequenceRandom(5, 0));
        state = Do(state, CommandKind.DeclineReaction);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Guide });
        Assert.Equal("hunt.guide", state.Pending!.Window);
        Assert.Equal(CharacterRules.Gollum, state.Fellowship.Guide);
        Assert.DoesNotContain(Axe, state.Player(Side.FreePeoples).Table);
        Assert.Contains(Axe, state.Player(Side.FreePeoples).Discard);
        Assert.Empty(state.ActiveEffects);
    }

    [Fact]
    public void FreePeoplesDrawDoesNotOfferShadowDrawCardsAndHiddenHandCannotCreateAWindow()
    {
        var state = HuntTests.State();
        HuntTests.Pool(state, HuntTests.Three);
        state.Player(Side.FreePeoples).Table.AddRange([Mithril, Axe]);
        HuntRuntime.BeginTile(state, "test-fp-draw");
        HuntRuntime.Active(state)!.DrawnBy = Side.FreePeoples;
        HuntResolutionTests.Pump(state, EffectCatalog.Base, new SequenceRandom(0));
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        state = HuntTests.State();
        HuntTests.Pool(state, HuntTests.Three);
        HandRules.Add(state.Player(Side.FreePeoples), Mithril);
        state = HuntTests.Move(state, new SequenceRandom(5, 0));
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        Assert.DoesNotContain(Mithril, JsonSerializer.Serialize(HuntTests.Views().For(state, Side.Shadow), BoardCatalog.Json));
    }
}
