using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.KingsCards02;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsWestTests
{
    internal static MatchState Ready(int eyes = 3)
    {
        var state = State(Side.Shadow);
        Eyes(state, eyes);
        BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "dale");
        BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "edoras");
        Pool(state, One, HuntTests.Three, KingsTiles.One);
        return state;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void EventDrawLimitUsesUnspentEyesWithoutNeedingUnusedFpDice(int eyes)
    {
        var state = Ready(eyes);
        foreach (var die in state.Dice.Where(d => d.Owner == Side.FreePeoples))
            die.Location = DieLocation.Spent;
        state = Play(state, West);
        if (eyes == 0)
        {
            Assert.Null(CorruptionRuntime.Current(state));
            Assert.Empty(state.RandomSamples);
            Assert.Empty(state.Resolution.Frames);
            return;
        }
        state = Choose(state, SovereignCatalog.Dain, 0);
        Assert.Equal(eyes, CorruptionRules.Eyes(state).Count());
        Assert.Equal(Math.Min(3, eyes), CorruptionRuntime.Current(state)!.DrawLimit);
        Assert.Equal("", CorruptionRuntime.Current(state)!.DieId);
        Assert.Equal(EffectReturn.Parent, CorruptionRuntime.Current(state)!.Return);
        Assert.Single(state.Resolution.PaidDieIds);
    }

    [Fact]
    public void NazgulRangeAndInitialStatusRestrictTargetsButEventMayRepeatThisTurn()
    {
        var state = Ready();
        Sovereign(state, SovereignCatalog.Dain).LastAttemptTurn = state.Turn;
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Brand), "dale");
        state = Play(state, West);
        Assert.Contains(SovereignCatalog.Dain, state.Pending!.PrivateChoices);
        Assert.Contains(SovereignCatalog.Theoden, state.Pending.PrivateChoices);
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Denethor] });
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Brand] });
        state = Choose(state, SovereignCatalog.Dain, 0);
        Assert.Equal(state.Turn, Sovereign(state, SovereignCatalog.Dain).LastAttemptTurn);
        Assert.DoesNotContain(CorruptionRules.Targets(state), s => s.Id == SovereignCatalog.Dain);
    }

    [Fact]
    public void NoNazgulInRangeConsumesOnlyTheEventWithoutRandomness()
    {
        var state = Ready();
        foreach (var unit in state.Units.Where(u => u.Kind == UnitKind.Nazgul))
            BoardRules.ReturnToReserve(unit);
        state = Play(state, West);
        Assert.Empty(state.Resolution.Frames);
        Assert.Empty(state.RandomSamples);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Equal(3, CorruptionRules.Eyes(state).Count());
    }

    [Fact]
    public void WitchKingCountsAsNazgulForTheEventRange()
    {
        var state = Ready(1);
        foreach (var unit in state.Units.Where(u => u.Kind == UnitKind.Nazgul))
            BoardRules.ReturnToReserve(unit);
        var witchKing = LordsFixture.Put(state, CharacterRules.WitchKing, "dale");
        CharacterLifecycle.ApplyVersion(witchKing, Wotr.Domain.Content.CharacterCatalog.All[CharacterRules.WitchKing]);
        state = Play(state, West);
        Assert.Contains(SovereignCatalog.Dain, state.Pending!.PrivateChoices);
        Assert.Contains(SovereignCatalog.Brand, state.Pending.PrivateChoices);
        Assert.DoesNotContain(SovereignCatalog.Theoden, state.Pending.PrivateChoices);
    }

    [Fact]
    public void PrivateTileChoiceResumesEventAndSpendsTheActionDieExactlyOnce()
    {
        var state = Ready(1);
        Pool(state, One);
        state = Choose(Play(state, West), SovereignCatalog.Dain, 0);
        var fp = LordsFixture.Views().For(state, Side.FreePeoples);
        Assert.Null(fp.Decision);
        Assert.Empty(fp.Options);
        Assert.Empty(fp.CorruptionAttempt!.OwnChoices);
        Assert.DoesNotContain(One, Json(state, Side.FreePeoples));
        Assert.Single(LordsFixture.Views().For(state, Side.Shadow).CorruptionAttempt!.OwnChoices);
        LordsFixture.Reject(state, CommandKind.ChooseCorruptionTile, new() { Ids = [HuntTests.Three] });
        LordsFixture.Reject(state, CommandKind.ChooseCorruptionTile, new() { Ids = [One, One] });
        state = Do(StateCodec.Copy(state), CommandKind.ChooseCorruptionTile, new() { Ids = [One] });
        Assert.Equal(new[] { One }, Sovereign(state, SovereignCatalog.Dain).Tiles);
        Assert.DoesNotContain(One, Json(state, Side.FreePeoples));
        Assert.Empty(state.Resolution.Frames);
        Assert.Empty(state.Resolution.PaidDieIds);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Single(CorruptionRules.Eyes(state));
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Single(state.RandomSamples);
    }

    [Theory]
    [InlineData(HuntTests.Eye)]
    [InlineData("base.hunt.special-fp.0.0.1")]
    public void IgnoredTilesDoNotCountAndKingsTileImmediatelyReturnsToParent(string ignored)
    {
        var state = Ready();
        Pool(state, ignored);
        state = Choose(Play(state, West), SovereignCatalog.Dain, 0);
        Assert.Equal(new[] { ignored }, CorruptionRuntime.Current(state)!.Ignored);
        Assert.Empty(CorruptionRuntime.Current(state)!.Drawn);
        state.HuntTiles.Single(t => t.Id == KingsTiles.One).Location = "pool";
        state = Do(StateCodec.Copy(state), CommandKind.DrawCorruptionTile, null, 0);
        Assert.Equal(new[] { KingsTiles.One }, Sovereign(state, SovereignCatalog.Dain).Tiles);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == ignored).Location);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(3, CorruptionRules.Eyes(state).Count());
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }
}
