using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class EffectRuntimeTests
{
    // Explicit infrastructure fixture, not an invented playable War of the Ring card.
    private static EffectCatalog NestedCatalog(bool privateResponse = false) => new([
        new("test.root", [new HealCorruptionStep(1), new ReactionWindowStep("test.before-heal"), new HealCorruptionStep(1)]),
        new("test.reply", [new ReactionWindowStep("test.nested"), new DrawCardsStep("character")]),
        new("test.inner", [new DrawCardsStep("strategy")])
    ], [], [
        new("test.reply", "test.before-heal", "test.reply", privateResponse ? new PrivateReplyCondition() : new UnconditionalEvent(), "test.table-cost", ReactionCost.DiscardTableCard),
        new("test.inner", "test.nested", "test.inner", new UnconditionalEvent())
    ]);

    private sealed record PrivateReplyCondition : EventCondition
    {
        public override bool CanPlay(MatchState state, Side actor) => state.Player(actor).Hand.Contains(ShadowCharacterCards03.Crebain);
    }

    private static MatchState Begin(EffectCatalog catalog)
    {
        var state = TurnTests.Actions();
        state.Fellowship.Corruption = 5;
        state.Player(Side.Shadow).Table.Add("test.table-cost");
        DiceRules.Pay(state, Side.FreePeoples, state.Dice[0].Id, DieFace.Character);
        state.Pending = null;
        state.Resolution.Frames.Add(new()
        {
            EffectId = "test.root",
            Effect = new() { Owner = Side.FreePeoples, SourceId = "test.root", Return = EffectReturn.FinishAction }
        });
        new EffectRuntime(catalog).Continue(state, new SequenceRandom());
        return state;
    }

    [Fact]
    public void NestedReactionsRestoreEachCursorAndConsumeCostsOnce()
    {
        var catalog = NestedCatalog();
        var state = Begin(catalog);
        var engine = new GameEngine(new EffectCommands(catalog), new DeclineReactionHandler(), new TurnCommands());
        Assert.Equal(4, state.Fellowship.Corruption);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        var command = KernelTests.Command(state, CommandKind.RespondEffect);
        command.Payload.ReactionId = "test.reply";
        state = EventTests.Execute(StateCodec.Copy(state), command, engine);
        Assert.Equal(2, state.Resolution.Frames.Count);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Empty(state.Player(Side.Shadow).Table);
        Assert.Single(state.Player(Side.Shadow).Discard);
        Assert.Equal(DieLocation.Resolving, state.Dice[0].Location);

        var projection = new Projection(new EffectOptions(catalog));
        Assert.Empty(projection.For(state, Side.FreePeoples).EffectTrail);
        Assert.Equal("test.table-cost", Assert.Single(projection.For(StateCodec.Copy(state), Side.Shadow).EffectTrail).SourceId);

        var inner = KernelTests.Command(state, CommandKind.RespondEffect);
        inner.Payload.ReactionId = "test.inner";
        var direct = EventTests.Execute(state, inner, engine);
        var restored = EventTests.Execute(StateCodec.Copy(state), inner, engine);
        Assert.Equal(StateCodec.Write(direct), StateCodec.Write(restored));
        Assert.Equal(3, restored.Fellowship.Corruption);
        Assert.Single(restored.Player(Side.FreePeoples).Hand);
        Assert.Single(restored.Player(Side.Shadow).Hand);
        Assert.Single(restored.Player(Side.Shadow).Discard);
        Assert.Empty(restored.Resolution.Frames);
        Assert.Equal(DieLocation.Spent, restored.Dice[0].Location);
        Assert.Equal(Side.Shadow, restored.Pending!.Owner);
    }

    [Fact]
    public void EmptyPrivateReactionWindowStillExistsAndCannotRevealAvailableResponses()
    {
        var catalog = NestedCatalog(privateResponse: true);
        var offered = Begin(catalog);
        offered.Player(Side.Shadow).Hand.Add(ShadowCharacterCards03.Crebain);
        var unavailable = StateCodec.Copy(offered);
        // Table cards are public. Vary the hidden hand composition while retaining its public size.
        unavailable.Player(Side.Shadow).Hand[0] = ShadowCharacterCards03.Worn;
        // Recompute the owner's options against current state, never trust saved or client choices.
        var projection = new Projection(new CoreOptions(), new EffectOptions(catalog));
        Assert.Equal(JsonSerializer.Serialize(projection.For(offered, Side.FreePeoples), BoardCatalog.Json),
            JsonSerializer.Serialize(projection.For(unavailable, Side.FreePeoples), BoardCatalog.Json));
        Assert.Empty(projection.For(unavailable, Side.Shadow).Options.Single(o => o.Kind == CommandKind.RespondEffect).EligibleIds);
        var wrong = KernelTests.Command(unavailable, CommandKind.RespondEffect);
        wrong.Payload.ReactionId = "test.reply";
        var engine = new GameEngine(new EffectCommands(catalog), new DeclineReactionHandler());
        Assert.False(engine.Execute(unavailable, Side.Shadow, wrong, new SequenceRandom()).Receipt.Accepted);
        var decline = KernelTests.Command(unavailable, CommandKind.DeclineReaction);
        var completed = EventTests.Execute(unavailable, decline, engine);
        Assert.Equal(3, completed.Fellowship.Corruption);
        Assert.Empty(completed.Resolution.Frames);
    }

    [Fact]
    public void PrivateLastingEffectDoesNotLeakSourceOrExpiryAndBoundariesRespectScope()
    {
        var state = TurnTests.Actions();
        state.ActiveEffects.Add(new()
        {
            Id = "private-effect",
            SourceId = "private-source",
            Owner = Side.FreePeoples,
            OnlyFor = Side.FreePeoples,
            Duration = EffectDuration.EndCombatRound,
            ScopeId = "battle-1:round-2",
            Turn = 1
        });
        state.ActiveEffects.Add(new()
        {
            Id = "turn-effect",
            SourceId = "turn-source",
            Owner = Side.FreePeoples,
            Duration = EffectDuration.EndTurn,
            Turn = 1
        });
        var shadow = JsonSerializer.Serialize(EventTests.Views.For(state, Side.Shadow), BoardCatalog.Json);
        Assert.DoesNotContain("private-effect", shadow);
        Assert.DoesNotContain("private-source", shadow);
        Assert.Equal(2, EventTests.Views.For(state, Side.FreePeoples).Effects.Count);
        ActiveEffects.ExpireBoundary(state, EffectDuration.EndCombatRound, "battle-1:round-1");
        Assert.Equal(2, state.ActiveEffects.Count);
        ActiveEffects.ExpireBoundary(state, EffectDuration.EndTurn);
        Assert.Single(state.ActiveEffects);
        ActiveEffects.ExpireBoundary(state, EffectDuration.EndCombatRound, "battle-1:round-2");
        Assert.Empty(state.ActiveEffects);
        shadow = JsonSerializer.Serialize(EventTests.Views.For(state, Side.Shadow), BoardCatalog.Json);
        Assert.DoesNotContain("private-source", shadow);
    }

    [Fact]
    public void CharacterBoundEffectExpiresWhenItsCharacterLeavesPlay()
    {
        var state = TurnTests.Actions();
        state.Characters.Add(new() { Id = "saruman", Side = Side.Shadow, Location = "orthanc" });
        state.ActiveEffects.Add(new()
        {
            Id = "test-bound",
            SourceId = "test-card",
            Owner = Side.Shadow,
            ExpiresWhen = EffectExpiry.CharacterLeavesPlay,
            TargetId = "saruman"
        });
        state.Player(Side.Shadow).Table.Add("test-card");
        ActiveEffects.CheckConditions(state);
        Assert.Single(state.ActiveEffects);
        state.Characters[0].Location = "casualties";
        ActiveEffects.CheckConditions(state);
        Assert.Empty(state.ActiveEffects);
        Assert.Single(state.Player(Side.Shadow).Discard);
    }
}
