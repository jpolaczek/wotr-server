using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public sealed class CorruptionBoundaryProjectionTests
{
    internal static readonly string[] Windows = ["draw", "ignored", "mixed", "choice", "sequential", "sequential-later", "sequential-ignored"];
    public static IEnumerable<object[]> Cases() => Enumerable.Range(0, 6).SelectMany(c => Windows.SelectMany(w =>
        new[] { false, true }.Select(v => new object[] { w, c, v })));

    [Theory]
    [MemberData(nameof(Cases))]
    public void OnlyShadowReceivesItsEarlierDrawsWhileBothSeatsRetainPublicCosts(string window, int configuration, bool visible)
    {
        var state = At(window, configuration, visible);
        var before = StateCodec.Write(state);
        var attempt = CorruptionRuntime.Current(state)!;
        foreach (var side in Enum.GetValues<Side>())
        {
            var view = GameRuntime.Projection(EffectCatalog.All).For(state, side);
            Assert.Equal(49, view.ActionBoundary!.Version);
            Assert.Empty(view.ActionBoundary.PaidDieIds);
            Assert.Equal(attempt.DieId, view.CorruptionAttempt!.DieId);
            Assert.Equal(attempt.Drawn.Count, view.CorruptionAttempt.DrawnCount);
            Assert.Equal(attempt.Ignored, view.CorruptionAttempt.RevealedTiles);
            if (side == Side.Shadow)
            {
                Assert.Equal(attempt.Drawn, view.CorruptionAttempt.OwnDrawnTiles.Select(t => t.TileId));
                Assert.All(view.CorruptionAttempt.OwnDrawnTiles, t => Assert.NotNull(t.Value));
                if (state.Configuration.SequentialCorruption)
                    Assert.Equal(CorruptionRules.Choices(attempt), view.CorruptionAttempt.OwnChoices.Select(t => t.TileId));
            }
            else
            {
                Assert.Empty(view.CorruptionAttempt.OwnDrawnTiles);
                Assert.Empty(view.CorruptionAttempt.OwnChoices);
                Assert.Null(view.Decision);
                Assert.Empty(view.Options);
                Assert.Equal(attempt.Drawn.Count, view.ActionBoundary.HuntTiles.Single(z => z.Location == "corruption-draw").HiddenCount);
            }
            view.CorruptionAttempt.OwnDrawnTiles.Clear();
            view.CorruptionAttempt.RevealedTiles.Clear();
        }
        Assert.Equal(before, StateCodec.Write(state));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void PrivateDrawAndPoolPermutationsCannotChangeFreePeoplesInformation(bool sequential, bool visible)
    {
        var state = At(sequential ? "sequential-later" : "draw", 5, visible);
        var projection = GameRuntime.Projection(EffectCatalog.All);
        var first = JsonSerializer.Serialize(projection.For(state, Side.FreePeoples));
        var attempt = CorruptionRuntime.Current(state)!;
        var tile = state.HuntTiles.First(t => t.Location == "pool" && KingsTiles.IsNumeric(t.Id) &&
            !KingsTiles.IsKings(t.Id) && KingsTiles.CorruptionValue(t.Id) == 3);
        state.HuntTiles.Single(t => t.Id == attempt.Drawn[0]).Location = "pool";
        attempt.Drawn[0] = tile.Id;
        tile.Location = "corruption-draw";
        state.Pending!.PrivateChoices = [.. CorruptionRules.Choices(attempt)];
        state.HuntTiles.Reverse();
        state.Player(Side.Shadow).Decks["character"].Reverse();
        state.RandomSamples.Add(new() { Value = 123456 });
        Assert.Equal(first, JsonSerializer.Serialize(projection.For(state, Side.FreePeoples)));
    }

    [Theory]
    [InlineData("parent")]
    [InlineData("nested")]
    [InlineData("event")]
    [InlineData("effect")]
    [InlineData("payment")]
    [InlineData("phase")]
    public void OtherReturnsAndUnreviewedContextsDoNotMasqueradeAsAnOrdinaryAttempt(string change)
    {
        var state = At("draw");
        switch (change)
        {
            case "parent": CorruptionRuntime.Current(state)!.Return = EffectReturn.Parent; break;
            case "nested": state.Resolution.Frames.Insert(0, new() { EffectId = "parent" }); break;
            case "event": CorruptionRuntime.Current(state)!.DieId = ""; break;
            case "effect": state.Resolution.Frames[0].EffectId = "program"; break;
            case "payment": state.Resolution.PaidDieIds.Add(CorruptionRuntime.Current(state)!.DieId); break;
            case "phase": state.Phase = GamePhase.Fellowship; break;
        }
        Assert.Null(new Projection().For(state, Side.Shadow).ActionBoundary);
    }

    [Theory]
    [InlineData(LordsDiceCatalog.Vilya)]
    [InlineData(LordsDiceCatalog.Nenya)]
    [InlineData(LordsDiceCatalog.Narya)]
    public void AKeeperEyeOfTheFreePeoplesPaysAnOrdinaryAttempt(string die)
    {
        var state = At("draw", configuration: 1, keeperEye: die);
        var paid = state.Dice.Single(item => item.Id == die);
        Assert.Equal(Side.FreePeoples, paid.Owner);
        Assert.Equal(DieLocation.Resolving, paid.Location);
        Assert.Equal(DecisionKind.CorruptionTile, state.Pending!.Kind);

        var view = GameRuntime.Projection(EffectCatalog.All).For(state, Side.Shadow);
        Assert.Equal(49, view.ActionBoundary?.Version);
        Assert.Equal(die, view.CorruptionAttempt!.DieId);

        paid.HuntOrigin = "";
        Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, Side.Shadow).ActionBoundary);
    }

    internal static MatchState At(string window, int configuration = 0, bool visible = false, string keeperEye = "")
    {
        var state = KingsBoundaryProjectionTests.At("action", configuration, visible);
        state.Configuration.SequentialCorruption = window.StartsWith("sequential", StringComparison.Ordinal);
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow && d.Source == "standard").Take(4))
        {
            die.Face = DieFace.Eye;
            die.Location = DieLocation.Hunt;
            die.HuntOrigin = "rolled-eye";
        }
        if (keeperEye.Length > 0)
            KingsIntegrationFixture.SpecialEye(state, keeperEye);
        TurnFlow.SelectNext(state, Side.Shadow);
        var payload = new CommandPayload
        {
            CharacterId = SovereignCatalog.Denethor,
            DieId = keeperEye.Length > 0 ? keeperEye : CorruptionRules.Eyes(state).First().Id
        };
        state = Draw(state, CommandKind.AttemptCorruption, window == "ignored" ? "eye" : "0", payload);
        if (window is "mixed" or "sequential-ignored")
            state = Draw(state, CommandKind.DrawCorruptionTile, "eye");
        if (window is "choice" or "sequential-later")
            state = Draw(state, CommandKind.DrawCorruptionTile, "1");
        if (window == "choice")
            state = Draw(state, CommandKind.DrawCorruptionTile, "2");
        return state;
    }

    private static MatchState Draw(MatchState state, CommandKind kind, string value, CommandPayload? payload = null)
    {
        var pool = state.HuntTiles.Where(t => t.Location == "pool").ToList();
        var index = pool.FindIndex(t => HuntPool.Definitions[t.Id] is { Pool: "standard" } definition &&
            definition.Value == value && !KingsTiles.IsKings(t.Id));
        Assert.True(index >= 0);
        var result = GameRuntime.Engine(EffectCatalog.All).Execute(state, Side.Shadow,
            BattleFixture.Command(state, kind, payload), new SequenceRandom(index));
        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        return result.State;
    }
}
