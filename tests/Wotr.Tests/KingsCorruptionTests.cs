using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsCorruptionTests
{
    internal const string One = "base.hunt.standard.1.0.1";
    internal const string Two = "base.hunt.standard.2.0.1";

    // UI-442: the command that spends the Eye draws every tile of a standard attempt (KINGS p. 12, step 2).
    [Theory]
    [InlineData(1, false, 0)]
    [InlineData(2, true, 1)]
    [InlineData(3, true, 2)]
    [InlineData(4, true, 3)]
    [InlineData(6, true, 3)]
    public void AttemptCountsOnlyRemainingEyesAndNeedsAnUnusedFreePeoplesDie(int eyes, bool legal, int drawLimit)
    {
        string[] tiles = [HuntTests.Three, One, Two];
        var state = Ready(eyes, tiles);
        var cost = CorruptionRules.Eyes(state).First().Id;
        var payload = new CommandPayload { CharacterId = SovereignCatalog.Dain, DieId = cost };
        Assert.Equal(legal, CorruptionRules.CanAttempt(state));
        if (legal)
        {
            string[] drawn = [.. tiles.Take(drawLimit)];
            var next = Do(state, CommandKind.AttemptCorruption, payload, DrawOrder(state, drawn));
            Assert.Equal(drawLimit, next.RandomSamples.Count - SetupSamples);
            Assert.Equal(drawLimit, next.Events.Count(e => e.Kind == "OwnCorruptionTile"));
            Assert.Equal(eyes - 1, CorruptionRules.Eyes(next).Count());
            if (drawLimit == 1)
                AssertPlacedWithoutDecision(next, cost, HuntTests.Three);
            else
                AssertChoiceOpen(next, cost, drawn);
        }
        else
            LordsFixture.Reject(state, CommandKind.AttemptCorruption, payload);
        foreach (var die in state.Dice.Where(d => d.Owner == Side.FreePeoples))
            die.Location = DieLocation.Spent;
        Assert.False(CorruptionRules.CanAttempt(state));
        LordsFixture.Reject(state, CommandKind.AttemptCorruption, payload);
    }

    // EXP-15 D8: a single drawn tile is the only possible choice, so the server places it without a decision.
    private static void AssertPlacedWithoutDecision(MatchState state, string cost, string tile)
    {
        Assert.Null(CorruptionRuntime.Current(state));
        Assert.Equal(new[] { tile }, Sovereign(state, SovereignCatalog.Dain).Tiles);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == cost).Location);
        Assert.Equal((DecisionKind.Action, Side.FreePeoples), (state.Pending!.Kind, state.Pending.Owner));
        Assert.Single(state.Events, e => e.Kind == "CorruptionAdded");
    }

    private static void AssertChoiceOpen(MatchState state, string cost, string[] drawn)
    {
        var attempt = CorruptionRuntime.Current(state)!;
        Assert.Equal(drawn.Length, attempt.DrawLimit);
        Assert.Equal(CorruptionStage.Choosing, attempt.Stage);
        Assert.Equal(drawn, attempt.Drawn);
        Assert.Equal(DieLocation.Resolving, state.Dice.Single(d => d.Id == cost).Location);
        Assert.Equal(DecisionKind.CorruptionTile, state.Pending!.Kind);
        Assert.Equal("CorruptionChoice", state.Pending.Presentation);
        Assert.Equal(new[] { CommandKind.ChooseCorruptionTile }, state.Pending.Commands);
        Assert.Equal(drawn, state.Pending.PrivateChoices);
        Assert.Empty(Sovereign(state, SovereignCatalog.Dain).Tiles);
        Assert.DoesNotContain(state.Events, e => e.Kind == "CorruptionAdded");
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
        Assert.Equal(2, fp.CorruptionAttempt.DrawnCount);
        Assert.Equal(new int?[] { 3, 1 }, sh.CorruptionAttempt!.OwnChoices.Select(c => c.Value));
        Assert.DoesNotContain(HuntTests.Three, Json(state, Side.FreePeoples));
        Assert.DoesNotContain(One, Json(state, Side.FreePeoples));
        LordsFixture.Reject(state, CommandKind.ChooseCorruptionTile, new() { Ids = [HuntTests.Eye] });
        LordsFixture.Reject(state, CommandKind.ChooseCorruptionTile, new() { Ids = [HuntTests.Three, HuntTests.Three] });
        LordsFixture.Reject(state, CommandKind.ChooseCorruptionTile, new() { Ids = [HuntTests.Three, One] });
        var command = BattleFixture.Command(state, CommandKind.ChooseCorruptionTile, new() { Ids = [HuntTests.Three] });
        Assert.Equal("NotAuthorized", LordsFixture.Engine().Validate(state, Side.FreePeoples, command));
        state = Do(StateCodec.Copy(state), command.Kind, command.Payload);
        Assert.Equal(3, Sovereign(state, SovereignCatalog.Dain).Tiles.Sum(KingsTiles.CorruptionValue));
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == One).Location);
        Assert.Contains(state.Events, e => e.Kind == "CorruptionTileRevealed" && e.Text.Contains(One));
        var tile = LordsFixture.Views().For(state, Side.FreePeoples).Sovereigns.Single(s => s.Id == SovereignCatalog.Dain).Tiles.Single();
        Assert.Null(tile.Value);
        Assert.Equal("", tile.TileId);
        Assert.DoesNotContain(HuntTests.Three, tile.Id);
        Assert.DoesNotContain(HuntTests.Three, Json(state, Side.FreePeoples));
    }

    // UI-442: the draws stay private, one Shadow-only history entry per numeric tile, and no draw step is offered.
    [Fact]
    public void AStandardAttemptOffersNoDrawStepAndRecordsEachDrawnTileOnlyForTheShadow()
    {
        var state = Attempt(4, tiles: [HuntTests.Three, One, Two]);
        var own = state.Events.Where(e => e.Kind == "OwnCorruptionTile").ToList();
        Assert.Equal(3, own.Count);
        Assert.All(own, e => Assert.Equal(Side.Shadow, e.OnlyFor));
        Assert.Equal(new[] { HuntTests.Three, One, Two }, own.Select(e => e.Activity!.Facts.Targets.Single()));
        Assert.Single(state.Events, e => e.Kind == "CorruptionAttempted" && e.OnlyFor is null);
        Assert.DoesNotContain(LordsFixture.Views().For(state, Side.Shadow).Options, o => o.Kind == CommandKind.DrawCorruptionTile);
        LordsFixture.Reject(state, CommandKind.DrawCorruptionTile, new());
        var freePeoples = Json(state, Side.FreePeoples);
        foreach (var tile in new[] { HuntTests.Three, One, Two })
            Assert.DoesNotContain(tile, freePeoples);
    }

    [Theory]
    [InlineData(HuntTests.Eye)]
    [InlineData("base.hunt.special-fp.0.0.1")]
    public void IgnoredTileIsRevealedHeldUntilCompletionAndNeverTriggersHuntProtection(string ignored)
    {
        var state = Ready(3, ignored, HuntTests.Three, One);
        state.Player(Side.FreePeoples).Table.Add(FreePeoplesCharacterCards01.Mithril);
        state = Do(state, CommandKind.AttemptCorruption, AttemptPayload(state), DrawOrder(state, ignored, HuntTests.Three, One));
        Assert.Equal(3, state.RandomSamples.Count - SetupSamples);
        Assert.Equal(CorruptionStage.Choosing, CorruptionRuntime.Current(state)!.Stage);
        Assert.Equal(new[] { ignored }, CorruptionRuntime.Current(state)!.Ignored);
        Assert.Equal(new[] { HuntTests.Three, One }, CorruptionRuntime.Current(state)!.Drawn);
        Assert.Equal("corruption-draw", state.HuntTiles.Single(t => t.Id == ignored).Location);
        Assert.Contains(ignored, Json(state, Side.FreePeoples));
        Assert.Null(HuntRuntime.Active(state));
        state = Do(StateCodec.Copy(state), CommandKind.ChooseCorruptionTile, new() { Ids = [HuntTests.Three] });
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
        var numeric = state.HuntTiles.Where(t => !KingsTiles.IsKings(t.Id) && KingsTiles.IsNumeric(t.Id)).Take(beforeKings).Select(t => t.Id).ToArray();
        string[] order = [.. numeric, KingsTiles.Two];
        Pool(state, order);
        state = Do(state, CommandKind.AttemptCorruption, AttemptPayload(state), DrawOrder(state, order));
        Assert.Null(CorruptionRuntime.Current(state));
        Assert.Equal(new[] { KingsTiles.Two }, Sovereign(state, SovereignCatalog.Dain).Tiles);
        Assert.Equal(0, Sovereign(state, SovereignCatalog.Dain).Tiles.Sum(KingsTiles.CorruptionValue));
        Assert.All(numeric, id => Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == id).Location));
        Assert.Equal(beforeKings + 1, state.RandomSamples.Count - KingsFixture.SetupSamples);
        Assert.Equal((DecisionKind.Action, Side.FreePeoples), (state.Pending!.Kind, state.Pending.Owner));
        var view = LordsFixture.Views().For(state, Side.FreePeoples).Sovereigns.Single(s => s.Id == SovereignCatalog.Dain).Tiles.Single();
        Assert.True(view.Kings);
        Assert.Equal(KingsTiles.Two, view.TileId);
        Assert.Equal(0, view.Value);
    }

    // A pool that runs out ends the draw: the one numeric tile drawn is placed (EXP-15 D8); without one nothing is
    // placed. An empty pool first refills from the discarded standard tiles, as for any Hunt draw.
    [Theory]
    [InlineData(new[] { HuntTests.Three }, new string[0], HuntTests.Three)]
    [InlineData(new[] { HuntTests.Eye, HuntTests.Three }, new string[0], HuntTests.Three)]
    [InlineData(new[] { HuntTests.Eye }, new[] { HuntTests.Three }, HuntTests.Three)]
    [InlineData(new[] { HuntTests.Eye }, new[] { KingsTiles.One }, KingsTiles.One)]
    [InlineData(new[] { HuntTests.Eye }, new string[0], "")]
    public void APoolThatRunsOutEndsTheDrawWithTheTilesAlreadyDrawn(string[] pool, string[] discarded, string placed)
    {
        var state = Ready(4, pool);
        foreach (var id in discarded)
            state.HuntTiles.Single(t => t.Id == id).Location = "discard";
        var cost = CorruptionRules.Eyes(state).First().Id;
        var draws = pool.Length + discarded.Length;
        state = Do(state, CommandKind.AttemptCorruption, AttemptPayload(state), new int[draws]);
        Assert.Equal(draws, state.RandomSamples.Count - SetupSamples);
        Assert.Null(CorruptionRuntime.Current(state));
        string[] expected = placed.Length > 0 ? [placed] : [];
        Assert.Equal(expected, Sovereign(state, SovereignCatalog.Dain).Tiles);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == cost).Location);
        Assert.Equal((DecisionKind.Action, Side.FreePeoples), (state.Pending!.Kind, state.Pending.Owner));
        foreach (var id in pool.Concat(discarded).Where(id => id != placed))
            Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == id).Location);
    }

    // A standard attempt saved at a draw step by an earlier build resumes: its draw finishes the remaining tiles at once.
    [Fact]
    public void ASavedStandardDrawStepDrawsTheRemainingTilesAtOnce()
    {
        var state = Attempt(4, tiles: [HuntTests.Three, One, Two]);
        var attempt = CorruptionRuntime.Current(state)!;
        attempt.Stage = CorruptionStage.Drawing;
        attempt.Drawn = [HuntTests.Three];
        state.HuntTiles.Single(t => t.Id == One).Location = "pool";
        state.HuntTiles.Single(t => t.Id == Two).Location = "pool";
        state.Open(DecisionKind.CorruptionTile, Side.Shadow, [CommandKind.DrawCorruptionTile]);
        state.Pending!.SourceId = SovereignCatalog.Dain;
        state.Pending.Presentation = "CorruptionDraw";
        state = Do(StateCodec.Copy(state), CommandKind.DrawCorruptionTile, new(), DrawOrder(state, One, Two));
        Assert.Equal(new[] { HuntTests.Three, One, Two }, CorruptionRuntime.Current(state)!.Drawn);
        Assert.Equal(CorruptionStage.Choosing, CorruptionRuntime.Current(state)!.Stage);
        Assert.Equal("CorruptionChoice", state.Pending!.Presentation);
    }

    [Theory]
    [InlineData(2, false, "Spend this Eye, draw a Corruption tile and place it on Dáin Ironfoot.")]
    [InlineData(3, false, "Spend this Eye, draw 2 Corruption tiles at once and choose one to place on Dáin Ironfoot.")]
    [InlineData(4, true, "Spend this Eye and draw up to 3 Corruption tiles for Dáin Ironfoot, one at a time.")]
    public void ThePreviewSaysHowTheAttemptDraws(int eyes, bool sequential, string summary)
    {
        var state = Ready(eyes, HuntTests.Three);
        state.Configuration.SequentialCorruption = sequential;
        var command = BattleFixture.Command(state, CommandKind.AttemptCorruption, AttemptPayload(state));
        var before = StateCodec.Write(state);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.Shadow, command);
        Assert.True(preview.Legal);
        Assert.True(preview.HasUnknownResult);
        Assert.Equal(summary, preview.Summary);
        Assert.Equal(before, StateCodec.Write(state));
    }
}
