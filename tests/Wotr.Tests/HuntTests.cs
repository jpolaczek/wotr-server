using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class HuntTests
{
    internal const string Eye = "base.hunt.standard.eye.1.1";
    internal const string Three = "base.hunt.standard.3.0.1";
    internal static GameEngine Engine(EffectCatalog? catalog = null) => new(new TurnCommands(), new FellowshipCommands(),
        new HuntCommands(catalog), new EffectCommands(catalog), new DeclineReactionHandler());
    internal static Projection Views(EffectCatalog? catalog = null) => new(new TurnOptions(), new FellowshipOptions(),
        new HuntOptions(), new EffectOptions(catalog), new CoreOptions());

    internal static MatchState State(int level = 1, string region = "hollin")
    {
        var state = TurnTests.Actions();
        state.Fellowship.RegionId = region;
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow).Take(level))
        {
            die.Location = DieLocation.Hunt;
            die.HuntOrigin = "allocated";
        }
        return state;
    }

    // Small deterministic pool fixture. The production inventory and setup remain unchanged.
    internal static void Pool(MatchState state, params string[] ids)
    {
        foreach (var tile in state.HuntTiles)
            tile.Location = ids.Contains(tile.Id) ? "pool" : "removed";
    }

    internal static MatchState Do(MatchState state, CommandKind kind, CommandPayload? payload = null,
        IRandomSource? random = null, EffectCatalog? catalog = null)
    {
        var command = FellowshipTests.Command(state, kind, payload ?? new());
        var result = Engine(catalog).Execute(state, state.Pending!.Owner, command, random ?? new SequenceRandom());
        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        return result.State;
    }

    internal static MatchState Move(MatchState state, IRandomSource random, EffectCatalog? catalog = null) =>
        Do(state, CommandKind.MoveFellowship, new() { DieId = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Id }, random, catalog);

    [Fact]
    public void MovementWithoutHuntDiceNeedsNoRandomnessAndAddsOnlyTheCompletedCost()
    {
        var state = State(0);
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples,
            FellowshipTests.Command(state, CommandKind.MoveFellowship, new() { DieId = state.Dice[0].Id }));
        Assert.False(preview.HasUnknownResult);
        state = Move(state, new SequenceRandom());
        Assert.Equal(1, state.Fellowship.Progress);
        Assert.Equal("hollin", state.Fellowship.RegionId);
        Assert.True(state.Fellowship.Hidden);
        Assert.Equal(DiceRules.FellowshipMove, state.Dice[0].HuntOrigin);
        Assert.Equal(DieLocation.Hunt, state.Dice[0].Location);
        Assert.DoesNotContain(state.Events, e => e.Kind == "HuntTileDrawn");
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void MaximumHuntRollIsFiveEvenWithSevenShadowDice()
    {
        var random = new SequenceRandom(0, 0, 0, 0, 0);
        var state = Move(State(7), random);
        Assert.Equal(5, random.Calls);
        Assert.DoesNotContain(state.Events, e => e.Kind == "HuntTileDrawn");
        Assert.Equal(DieLocation.Hunt, state.Dice[0].Location);
    }

    [Theory]
    [InlineData(1, 0, false)]
    [InlineData(1, 10, false)]
    [InlineData(2, 4, true)]
    [InlineData(5, 0, false)]
    [InlineData(5, 1, true)]
    [InlineData(6, 0, true)]
    public void NaturalOneFailsAndEarlierMovementBonusChangesThreshold(int result, int bonus, bool success) =>
        Assert.Equal(success, HuntRoll.Success(result, bonus));

    [Fact]
    public void CurrentCostIsNotIncludedInBonusAndOtherFpEyeOriginsDoNotCount()
    {
        var state = State();
        Pool(state, Three);
        state.Dice[1].Location = DieLocation.Hunt;
        state.Dice[1].HuntOrigin = DiceRules.FellowshipMove;
        state.Dice[2].Location = DieLocation.Hunt;
        state.Dice[2].HuntOrigin = "keeper-eye";
        state = Move(state, new SequenceRandom(4, 0));
        var hunt = HuntRuntime.Active(state)!;
        Assert.Equal(1, hunt.Bonus);
        Assert.Equal(3, hunt.Damage);
        Assert.Equal(DieLocation.Resolving, state.Dice[0].Location);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal(3, state.Fellowship.Corruption);
        Assert.Equal(DieLocation.Hunt, state.Dice[0].Location);
    }

    [Fact]
    public void EachRerollCauseCountsOnceAndOnlyChosenFailedDiceAreRolled()
    {
        var state = State(4, "moria");
        Pool(state, Eye);
        foreach (var nazgul in state.Units.Where(u => u.Kind == UnitKind.Nazgul).Take(2))
            nazgul.Location = "moria";
        state = Move(state, new SequenceRandom(0, 5, 1, 2));
        Assert.Equal(DecisionKind.HuntReroll, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        var hunt = HuntRuntime.Active(state)!;
        Assert.Equal(new[] { "stronghold", "army", "nazgul" }, hunt.RerollSources);
        var options = Assert.Single(Views().For(state, Side.Shadow).Options);
        Assert.Equal(3, options.Maximum);
        Assert.DoesNotContain("hunt-die-1", options.EligibleIds);
        Reject(state, CommandKind.RerollHunt, new() { Ids = ["hunt-die-1"] });
        Reject(state, CommandKind.RerollHunt, new() { Ids = ["hunt-die-0", "hunt-die-0"] });
        var random = new SequenceRandom(5, 3, 0);
        state = Do(StateCodec.Copy(state), CommandKind.RerollHunt, new() { Ids = ["hunt-die-0", "hunt-die-2"] }, random);
        Assert.Equal(3, random.Calls); // Two rerolls, then one tile.
        hunt = HuntRuntime.Active(state)!;
        Assert.Equal(2, hunt.Damage);
        Assert.Equal(2, hunt.Dice.Count(d => d.Rerolled));
        Assert.Equal(1, hunt.Dice[0].First);
        Assert.Equal(6, hunt.Dice[0].Result);
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
    }

    [Fact]
    public void ShadowCanDeclineRerollsEvenAfterASuccess()
    {
        var state = State(2, "moria");
        Pool(state, Eye);
        state = Move(state, new SequenceRandom(5, 0));
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow,
            FellowshipTests.Command(state, CommandKind.RerollHunt, new()));
        Assert.True(preview.HasUnknownResult); // Declining still draws an unknown tile for the existing success.
        var random = new SequenceRandom(0);
        state = Do(state, CommandKind.RerollHunt, new(), random);
        Assert.Equal(1, HuntRuntime.Active(state)!.Damage);
        Assert.Equal(1, random.Calls);
        Assert.All(HuntRuntime.Active(state)!.Dice, d => Assert.False(d.Rerolled));
    }

    [Theory]
    [InlineData("revealed")]
    [InlineData("phase")]
    [InlineData("muster")]
    [InlineData("shadow")]
    public void IllegalMovementCannotAdvancePayOrRoll(string reason)
    {
        var state = State();
        if (reason == "revealed")
            state.Fellowship.Hidden = false;
        if (reason == "phase")
            state.Phase = GamePhase.Fellowship;
        if (reason == "muster")
            state.Dice[0].Face = DieFace.Muster;
        if (reason == "shadow")
            TurnFlow.SelectNext(state, Side.Shadow);
        Reject(state, CommandKind.MoveFellowship, new() { DieId = state.Dice[0].Id });
    }

    [Fact]
    public void ExhaustedPoolReturnsOnlyDiscardedStandardTiles()
    {
        var state = State();
        Pool(state);
        var standard = state.HuntTiles.Single(t => t.Id == Three);
        standard.Location = "discard";
        var special = state.HuntTiles.First(t => HuntPool.Definitions[t.Id].Pool != "standard");
        special.Location = "discard";
        var prepared = state.HuntTiles.Last();
        prepared.Location = "prepared";
        Assert.Equal(Three, HuntPool.Draw(state, new SequenceRandom(0)).Id);
        Assert.Equal("discard", special.Location);
        Assert.Equal("prepared", prepared.Location);
        Assert.Equal("removed", state.HuntTiles.Single(t => t.Id == Eye).Location);
        HuntPool.Discard(state, Three);
        Assert.Equal("spent", standard.Location);
        HuntPool.CompleteAction(state);
        Assert.Equal(Three, HuntPool.Draw(state, new SequenceRandom(0)).Id);
        HuntPool.Remove(state, Three);
        HuntPool.Discard(state, Three);
        Assert.Equal("removed", standard.Location);
    }

    [Fact]
    public void SpecialTilesArePreparedUntilMordorAndNeverAddedToRegionalPoolEarly()
    {
        var state = State();
        var id = "base.hunt.special-fp.-2.0.1";
        HuntPool.PrepareSpecial(state, id);
        Assert.Equal("prepared", state.HuntTiles.Single(t => t.Id == id).Location);
        state.Fellowship.MordorStep = 0;
        var other = "base.hunt.special-fp.-1.0.1";
        HuntPool.PrepareSpecial(state, other);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == other).Location);
    }

    [Fact]
    public void RandomCasualtyCannotBeChosenByIdAndPreviewDoesNotConsumeEntropyOrRevealPoolOrder()
    {
        var state = State();
        Pool(state, Three);
        state = Move(state, new SequenceRandom(5, 0));
        Reject(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.RandomCompanion, Ids = ["base.character.merry"] });
        var before = StateCodec.Write(state);
        var request = FellowshipTests.Command(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.RandomCompanion });
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples, request);
        Assert.True(preview.HasUnknownResult);
        Assert.Empty(preview.RemovedIds);
        Assert.Equal(before, StateCodec.Write(state));
        var first = JsonSerializer.Serialize(Views().For(state, Side.Shadow), BoardCatalog.Json);
        state.HuntTiles.Reverse();
        Assert.Equal(first, JsonSerializer.Serialize(Views().For(state, Side.Shadow), BoardCatalog.Json));
        Assert.DoesNotContain("hunt.standard.eye", first);
        Assert.Null(Views().For(state, Side.Shadow).Decision);
    }

    internal static void Reject(MatchState state, CommandKind kind, CommandPayload payload)
    {
        var before = StateCodec.Write(state);
        var random = new SequenceRandom();
        var command = FellowshipTests.Command(state, kind, payload);
        Assert.False(Engine().Execute(state, state.Pending!.Owner, command, random).Receipt.Accepted);
        Assert.Equal(0, random.Calls);
        Assert.Equal(before, StateCodec.Write(state));
    }
}
