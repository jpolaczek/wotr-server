using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsCorruptionTests
{
    internal const string One = "base.hunt.standard.1.0.1";

    [Theory]
    [InlineData(1, false, 0)]
    [InlineData(2, true, 1)]
    [InlineData(3, true, 2)]
    [InlineData(4, true, 3)]
    [InlineData(6, true, 3)]
    public void AttemptCountsOnlyRemainingEyesAndNeedsAnUnusedFreePeoplesDie(int eyes, bool legal, int drawLimit)
    {
        var state = Actions(Side.Shadow);
        Eyes(state, eyes);
        Pool(state, HuntTests.Three);
        var cost = CorruptionRules.Eyes(state).First().Id;
        var payload = new CommandPayload { CharacterId = SovereignCatalog.Dain, DieId = cost };
        Assert.Equal(legal, CorruptionRules.CanAttempt(state));
        if (legal)
        {
            var next = Do(state, CommandKind.AttemptCorruption, payload, 0);
            Assert.Equal(drawLimit, CorruptionRuntime.Current(next)!.DrawLimit);
            Assert.Equal(DieLocation.Resolving, next.Dice.Single(d => d.Id == cost).Location);
            Assert.Equal(eyes - 1, CorruptionRules.Eyes(next).Count());
            Assert.Equal(drawLimit == 1 ? CorruptionStage.Choosing : CorruptionStage.Drawing, CorruptionRuntime.Current(next)!.Stage);
        }
        else
            LordsFixture.Reject(state, CommandKind.AttemptCorruption, payload);
        foreach (var die in state.Dice.Where(d => d.Owner == Side.FreePeoples))
            die.Location = DieLocation.Spent;
        Assert.False(CorruptionRules.CanAttempt(state));
        LordsFixture.Reject(state, CommandKind.AttemptCorruption, payload);
    }

    [Fact]
    public void ShadowCanAttemptWithNoAvailableActionDiceAndCannotRepeatTheSameTargetThisTurn()
    {
        var state = Actions(Side.Shadow);
        Eyes(state, 4);
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available))
            die.Location = DieLocation.Spent;
        Pool(state, KingsTiles.One);
        TurnFlow.SelectNext(state, Side.Shadow);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        var cost = CorruptionRules.Eyes(state).First().Id;
        state = Do(state, CommandKind.AttemptCorruption, new() { CharacterId = SovereignCatalog.Dain, DieId = cost }, 0);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == cost).Location);
        Assert.DoesNotContain(CorruptionRules.Targets(state), s => s.Id == SovereignCatalog.Dain);
        state.Turn++;
        Assert.Contains(CorruptionRules.Targets(state), s => s.Id == SovereignCatalog.Dain);
    }

    [Fact]
    public void ChoiceIsPrivateAndRejectsUnknownDuplicateAndOpposingSelectionsWithoutEntropy()
    {
        var state = Attempt();
        var fp = LordsFixture.Views().For(state, Side.FreePeoples);
        var sh = LordsFixture.Views().For(state, Side.Shadow);
        Assert.Null(fp.Decision);
        Assert.Empty(fp.Options);
        Assert.Empty(fp.CorruptionAttempt!.OwnChoices);
        Assert.Single(sh.CorruptionAttempt!.OwnChoices);
        Assert.Equal(3, sh.CorruptionAttempt.OwnChoices[0].Value);
        Assert.DoesNotContain(HuntTests.Three, Json(state, Side.FreePeoples));
        LordsFixture.Reject(state, CommandKind.ChooseCorruptionTile, new() { Ids = [HuntTests.Eye] });
        LordsFixture.Reject(state, CommandKind.ChooseCorruptionTile, new() { Ids = [HuntTests.Three, HuntTests.Three] });
        var command = BattleFixture.Command(state, CommandKind.ChooseCorruptionTile, new() { Ids = [HuntTests.Three] });
        Assert.Equal("NotAuthorized", LordsFixture.Engine().Validate(state, Side.FreePeoples, command));
        state = Do(StateCodec.Copy(state), command.Kind, command.Payload);
        Assert.Equal(3, Sovereign(state, SovereignCatalog.Dain).Tiles.Sum(KingsTiles.CorruptionValue));
        var tile = LordsFixture.Views().For(state, Side.FreePeoples).Sovereigns.Single(s => s.Id == SovereignCatalog.Dain).Tiles.Single();
        Assert.Null(tile.Value);
        Assert.Equal("", tile.TileId);
        Assert.DoesNotContain(HuntTests.Three, tile.Id);
        Assert.DoesNotContain(HuntTests.Three, Json(state, Side.FreePeoples));
    }

    [Theory]
    [InlineData(HuntTests.Eye)]
    [InlineData("base.hunt.special-fp.0.0.1")]
    public void IgnoredTileIsRevealedHeldUntilCompletionAndNeverTriggersHuntProtection(string ignored)
    {
        var state = Actions(Side.Shadow);
        Eyes(state, 2);
        Pool(state, ignored);
        state.Player(Side.FreePeoples).Table.Add(FreePeoplesCharacterCards01.Mithril);
        state = Do(state, CommandKind.AttemptCorruption,
            new() { CharacterId = SovereignCatalog.Dain, DieId = CorruptionRules.Eyes(state).First().Id }, 0);
        Assert.Equal(CorruptionStage.Drawing, CorruptionRuntime.Current(state)!.Stage);
        Assert.Equal(new[] { ignored }, CorruptionRuntime.Current(state)!.Ignored);
        Assert.Equal("corruption-draw", state.HuntTiles.Single(t => t.Id == ignored).Location);
        Assert.Contains(ignored, Json(state, Side.FreePeoples));
        Assert.Null(HuntRuntime.Active(state));
        state.HuntTiles.Single(t => t.Id == HuntTests.Three).Location = "pool";
        state = Do(StateCodec.Copy(state), CommandKind.DrawCorruptionTile, null, 0);
        state = Do(state, CommandKind.ChooseCorruptionTile, new() { Ids = [HuntTests.Three] });
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == ignored).Location);
        Assert.Contains(FreePeoplesCharacterCards01.Mithril, state.Player(Side.FreePeoples).Table);
        Assert.Equal(0, state.Fellowship.Corruption);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void KingsTileImmediatelyStopsAtEveryDrawPositionAndCountsAsZero(int beforeKings)
    {
        var state = Actions(Side.Shadow);
        Eyes(state, 4);
        Pool(state, KingsTiles.Two);
        var numeric = state.HuntTiles.Where(t => !KingsTiles.IsKings(t.Id) && KingsTiles.IsNumeric(t.Id)).Take(beforeKings).Select(t => t.Id).ToArray();
        foreach (var id in numeric)
            state.HuntTiles.Single(t => t.Id == id).Location = "pool";
        var cost = CorruptionRules.Eyes(state).First().Id;
        state = Do(state, CommandKind.AttemptCorruption, new() { CharacterId = SovereignCatalog.Dain, DieId = cost }, 0);
        for (var i = 0; i < beforeKings; i++)
            state = Do(StateCodec.Copy(state), CommandKind.DrawCorruptionTile, null, 0);
        Assert.Null(CorruptionRuntime.Current(state));
        Assert.Equal(new[] { KingsTiles.Two }, Sovereign(state, SovereignCatalog.Dain).Tiles);
        Assert.Equal(0, Sovereign(state, SovereignCatalog.Dain).Tiles.Sum(KingsTiles.CorruptionValue));
        Assert.All(numeric, id => Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == id).Location));
        Assert.Equal(beforeKings + 1, state.RandomSamples.Count - KingsFixture.SetupSamples);
        var view = LordsFixture.Views().For(state, Side.FreePeoples).Sovereigns.Single(s => s.Id == SovereignCatalog.Dain).Tiles.Single();
        Assert.True(view.Kings);
        Assert.Equal(KingsTiles.Two, view.TileId);
        Assert.Equal(0, view.Value);
    }
}
