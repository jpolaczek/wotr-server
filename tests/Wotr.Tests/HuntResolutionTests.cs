using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class HuntResolutionTests
{
    private const string Grey = "base.character.gandalf-grey";
    private const string Strider = "base.character.strider";

    [Fact]
    public void RandomLossIncludesGuideAndOnlyOneCompanionCanAbsorbDamage()
    {
        var state = HuntTests.State();
        HuntTests.Pool(state, HuntTests.Three);
        state = HuntTests.Move(state, new SequenceRandom(5, 0));
        var random = new SequenceRandom(3); // Gimli, level 2.
        state = HuntTests.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.RandomCompanion }, random);
        Assert.Equal(1, random.Calls);
        Assert.Equal("base.character.gimli", Assert.Single(state.Characters).Id);
        Assert.Equal("casualties", state.Characters[0].Location);
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        Assert.Equal(0, state.Fellowship.Corruption); // New Guide precedes remaining damage.
        state = HuntTests.Do(StateCodec.Copy(state), CommandKind.SelectGuide, new() { GuideId = Grey });
        Assert.Equal(1, state.Fellowship.Corruption);
        Assert.Single(state.Events, e => e.Kind == "HuntCasualty");
        Assert.Equal(DieLocation.Hunt, state.Dice[0].Location);

        state = HuntTests.State();
        HuntTests.Pool(state, HuntTests.Three);
        state = HuntTests.Move(state, new SequenceRandom(5, 0));
        state = HuntTests.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.RandomCompanion }, new SequenceRandom(0));
        Assert.Equal(Grey, Assert.Single(state.Characters).Id);
        Assert.Equal(Strider, state.Fellowship.Guide);
        Assert.Equal(0, state.Fellowship.Corruption);
    }

    [Fact]
    public void V10TableReactionThenNewGuideAbilityRunBeforeRemainingCorruptionAndReveal()
    {
        var catalog = GuideCatalog();
        var state = HuntTests.State(5);
        state.Player(Side.FreePeoples).Table.Add("test.shield");
        HuntTests.Pool(state, HuntTests.Eye);
        state = HuntTests.Move(state, new SequenceRandom(5, 5, 5, 5, 5, 0), catalog);
        Assert.Equal("hunt.table", state.Pending!.Window);
        Assert.Equal(5, HuntRuntime.Active(state)!.Damage);
        state = HuntTests.Do(StateCodec.Copy(state), CommandKind.RespondEffect, new() { ReactionId = "test.shield" }, catalog: catalog);
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        Assert.Equal(4, HuntRuntime.Active(state)!.Damage);
        Assert.Empty(state.Player(Side.FreePeoples).Table);
        state = HuntTests.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Guide }, catalog: catalog);
        Assert.Equal(Strider, state.Fellowship.Guide);
        Assert.Equal("hunt.guide", state.Pending!.Window);
        Assert.Equal(1, HuntRuntime.Active(state)!.Damage);
        Assert.Equal(0, state.Fellowship.Corruption);
        state = HuntTests.Do(StateCodec.Copy(state), CommandKind.RespondEffect, new() { ReactionId = "test.guide" }, catalog: catalog);
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Equal(DecisionKind.FellowshipRoute, state.Pending!.Kind);
        Assert.Single(state.Events, e => e.Kind == "HuntCasualty");
        state = HuntTests.Do(state, CommandKind.RevealFellowship, new() { Route = ["hollin"] }, catalog: catalog);
        Assert.Equal(DieLocation.Hunt, state.Dice[0].Location);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void RevealEventCompletesBeforeEveryStrongholdTileAndRevealIconsDoNotLoop()
    {
        var catalog = new EffectCatalog([
            new("test.reveal", [new DrawHuntTileStep()])
        ], [], [new("test.reveal", "fellowship.revealed", "test.reveal", new UnconditionalEvent(), "test.reveal", ReactionCost.DiscardTableCard)]);
        var state = HuntTests.State(1, "moria");
        state.Fellowship.Progress = 2;
        state.Player(Side.Shadow).Table.Add("test.reveal");
        var zero = "base.hunt.standard.0.1.1";
        HuntTests.Pool(state, HuntTests.Eye, HuntTests.Three, zero, "base.hunt.standard.0.1.2");
        state = HuntTests.Move(state, new SequenceRandom(5, 0), catalog);
        state = HuntTests.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }, catalog: catalog);
        Assert.Equal(1, state.Fellowship.Corruption);
        state = HuntTests.Do(state, CommandKind.RevealFellowship,
            new() { Route = ["moria", "dimrill-dale", "north-anduin-vale", "dol-guldur"] }, catalog: catalog);
        Assert.Equal("fellowship.revealed", state.Pending!.Window);
        Assert.Single(state.Events, e => e.Kind == "HuntTileDrawn");
        state = HuntTests.Do(state, CommandKind.RespondEffect, new() { ReactionId = "test.reveal" }, new SequenceRandom(0), catalog);
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        Assert.Equal("test.reveal", HuntRuntime.Active(state)!.SourceId);
        state = HuntTests.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }, new SequenceRandom(0, 0), catalog);
        Assert.Equal(4, state.Fellowship.Corruption);
        Assert.Equal(4, state.Events.Count(e => e.Kind == "HuntTileDrawn"));
        Assert.Single(state.Events, e => e.Kind == "FellowshipRevealed");
        Assert.Equal("dol-guldur", state.Fellowship.RegionId);
        Assert.False(state.Fellowship.Hidden);
        Assert.Equal(DieLocation.Hunt, state.Dice[0].Location);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void CancellingTilePreventsDamageAndRevelationWithoutUndoingMovement()
    {
        var catalog = new EffectCatalog([new("test.cancel", [new AdjustHuntStep(CancelTile: true)])], [],
            [new("test.cancel", "hunt.table", "test.cancel", new UnconditionalEvent(), "test.cancel", ReactionCost.DiscardTableCard)]);
        var state = HuntTests.State();
        state.Player(Side.FreePeoples).Table.Add("test.cancel");
        HuntTests.Pool(state, HuntTests.Eye);
        state = HuntTests.Move(state, new SequenceRandom(5, 0), catalog);
        state = HuntTests.Do(state, CommandKind.RespondEffect, new() { ReactionId = "test.cancel" }, catalog: catalog);
        Assert.True(state.Fellowship.Hidden);
        Assert.Equal(1, state.Fellowship.Progress);
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == HuntTests.Eye).Location); // Empty pool refills at the action boundary.
        Assert.Single(state.Player(Side.FreePeoples).Discard);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void CorruptionTwelveEndsImmediatelyBeforeRevealAndFurtherTiles()
    {
        var state = HuntTests.State(1, "moria");
        state.Fellowship.Corruption = 11;
        HuntTests.Pool(state, HuntTests.Eye);
        state = HuntTests.Move(state, new SequenceRandom(5, 0));
        state = HuntTests.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal(Side.Shadow, state.Winner);
        Assert.Equal(GamePhase.Finished, state.Phase);
        Assert.Null(state.Pending);
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Events, e => e.Kind == "HuntTileDrawn");
        Assert.DoesNotContain(state.Events, e => e.Kind == "FellowshipRevealed");
        Assert.Equal(DieLocation.Hunt, state.Dice[0].Location);
        var request = new GameCommand
        {
            MatchId = state.Id,
            ExpectedRevision = state.Revision,
            CommandId = Guid.NewGuid().ToString("N"),
            Kind = CommandKind.MoveFellowship
        };
        Assert.Equal("GameFinished", HuntTests.Engine().Execute(state, Side.FreePeoples, request, new SequenceRandom()).Receipt.Error);
    }

    [Fact]
    public void CardMovementDoesNotPutTheEventCostInHunt()
    {
        var state = HuntTests.State(0);
        DiceRules.Pay(state, Side.FreePeoples, state.Dice[0].Id, DieFace.Character);
        state.Resolution.Frames.Add(new() { EffectId = "test.move", Effect = new() { Owner = Side.FreePeoples, Return = EffectReturn.FinishAction } });
        state.Pending = null;
        var catalog = new EffectCatalog([new("test.move", [new MoveFellowshipStep()])], []);
        Pump(state, catalog, new SequenceRandom());
        Assert.Equal(1, state.Fellowship.Progress);
        Assert.Equal(DieLocation.Spent, state.Dice[0].Location);
        Assert.Equal("", state.Dice[0].HuntOrigin);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData("base.hunt.special-fp.-2.0.1", 1, 0)]
    [InlineData("base.hunt.special-fp.-1.0.1", 3, 2)]
    public void NegativeTileHealsToZeroWithoutACasualtyDecision(string id, int corruption, int expected)
    {
        var state = HuntTests.State();
        state.Fellowship.MordorStep = 0;
        state.Fellowship.Corruption = corruption;
        HuntTests.Pool(state, id);
        HuntRuntime.BeginTile(state, "test-tile");
        Pump(state, EffectCatalog.Base, new SequenceRandom(0));
        Assert.Equal(expected, state.Fellowship.Corruption);
        Assert.Null(state.Pending);
        Assert.Equal("discard", state.HuntTiles.Single(t => t.Id == id).Location);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void DieTileRollsAfterDrawingAndShowsOnlyThatActualResult(bool movement, bool stopped)
    {
        var state = HuntTests.State();
        state.Fellowship.MordorStep = 0;
        state.Fellowship.RegionId = MordorRules.Track;
        HuntTests.Pool(state, "base.hunt.special-sh.die.0.1");
        HuntRuntime.BeginTile(state, "test-tile");
        HuntRuntime.Active(state)!.Movement = movement;
        var random = new SequenceRandom(0, 4);
        Pump(state, EffectCatalog.Base, random);
        Assert.Equal(2, random.Calls);
        var view = HuntTests.Views().For(state, Side.FreePeoples).Hunt!;
        Assert.Equal(5, view.TileDie);
        Assert.Equal(5, view.Damage);
        Assert.Equal(stopped, view.Stop);
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
    }

    // Infrastructure programs exercise timing hooks, not yet-implemented card/character text.
    internal static EffectCatalog GuideCatalog() => new([
        new("test.shield", [new AdjustHuntStep(1)]),
        new("test.guide", [new AdjustHuntStep(1)])
    ], [], [
        new("test.shield", "hunt.table", "test.shield", new UnconditionalEvent(), "test.shield", ReactionCost.DiscardTableCard),
        new("test.guide", "hunt.guide", "test.guide", new UnconditionalEvent(), RequiredGuideId: Strider)
    ]);

    internal static void Pump(MatchState state, EffectCatalog catalog, IRandomSource random)
    {
        IResolutionContinuation[] continuations = [new EffectCommands(catalog), new HuntCommands(catalog)];
        while (state.Pending is null && state.Winner is null)
        {
            var next = continuations.SingleOrDefault(c => c.CanContinue(state));
            if (next is null)
                return;
            next.Continue(state, random);
        }
    }
}
