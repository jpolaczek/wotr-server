using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class HandDiscardTests
{
    [Fact]
    public void PartialRecoveryDiscardKeepsOwnerAndRejectsInvalidSetsAndOldDecisions()
    {
        var state = Recovery();
        var projection = new Projection(new TurnOptions());
        var option = projection.For(state, Side.FreePeoples).Options.Single();
        Assert.Equal(1, option.Minimum);
        Assert.Equal(2, option.Maximum);
        var hand = state.Player(Side.FreePeoples).Hand.ToArray();
        var engine = new GameEngine(new TurnCommands());
        foreach (var ids in new[] { Array.Empty<string>(), new[] { hand[0], hand[0] }, hand.Take(3).ToArray(), new[] { "unknown" } })
        {
            var invalid = BattleFixture.Command(state, CommandKind.DiscardCards, new() { Ids = [.. ids] });
            Assert.False(engine.Execute(state, Side.FreePeoples, invalid, new SequenceRandom()).Receipt.Accepted);
        }
        var command = BattleFixture.Command(state, CommandKind.DiscardCards, new() { Ids = [hand[0]] });
        state = engine.Execute(state, Side.FreePeoples, command, new SequenceRandom()).State;
        Assert.Equal(7, state.Player(Side.FreePeoples).Hand.Count);
        Assert.Equal(1, state.Pending!.RequiredCount);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        Assert.Equal(DiscardContinuation.RecoverDraw, state.AfterDiscard);
        Assert.NotEqual(command.DecisionId, state.Pending.Id);
        Assert.False(engine.Execute(state, Side.FreePeoples, command, new SequenceRandom()).Receipt.Accepted);
        Assert.All(hand, id => Assert.DoesNotContain(id, JsonSerializer.Serialize(projection.For(state, Side.Shadow), BoardCatalog.Json)));
        state = Execute(state, [hand[1]]);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        state = Execute(state, state.Player(Side.Shadow).Hand.Take(2).ToList());
        Assert.Equal(GamePhase.Fellowship, state.Phase);
        Assert.All(state.Players, p => Assert.Equal(6, p.Hand.Count));
    }

    [PostgresFact]
    public async Task RecoveryPersistsAndRetriesEachIndividualDiscard()
    {
        var state = Recovery();
        for (var index = 0; index < 4; index++)
        {
            var owner = state.Pending!.Owner;
            state = await CardPackPersistenceTests.RestartDecision(state, CommandKind.DiscardCards,
                new() { Ids = [state.Player(owner).Hand[0]] }, new SequenceRandom());
            Assert.Equal(index < 2 ? 7 - index : 6, state.Player(Side.FreePeoples).Hand.Count);
        }
        Assert.Equal(GamePhase.Fellowship, state.Phase);
    }

    [PostgresFact]
    public async Task NestedAnarionDrawKeepsItsCostAndEffectUntilBothDiscardsPersist()
    {
        var state = KingsAnarionTests.Ready();
        while (state.Player(Side.FreePeoples).Hand.Count < 6)
            CardFlow.Draw(state, Side.FreePeoples, "strategy");
        var cost = state.Dice.Single(d => d.Owner == Side.FreePeoples && d.Source == SovereignCatalog.RulerSource).Id;
        state = KingsFixture.Do(state, CommandKind.DrawCard, new() { DieId = cost, Deck = "character" });
        state = KingsFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = ["strategy"] });
        Assert.Equal(DiscardContinuation.ResumeEffect, state.AfterDiscard);
        state = await CardPackPersistenceTests.RestartDecision(state, CommandKind.DiscardCards,
            new() { Ids = [state.Pending!.PrivateChoices[0]] }, new SequenceRandom());
        Assert.Equal(1, state.Pending!.RequiredCount);
        Assert.Equal(DieLocation.Resolving, state.Dice.Single(d => d.Id == cost).Location);
        Assert.NotEmpty(state.Resolution.Frames);
        state = await CardPackPersistenceTests.RestartDecision(state, CommandKind.DiscardCards,
            new() { Ids = [state.Pending.PrivateChoices[0]] }, new SequenceRandom());
        Assert.Equal(6, state.Player(Side.FreePeoples).Hand.Count);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == cost).Location);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
    }

    private static MatchState Recovery()
    {
        var state = KernelTests.NewGame();
        foreach (var side in Enum.GetValues<Side>())
            while (state.Player(side).Hand.Count < 6)
                CardFlow.Draw(state, side, "character");
        TurnFlow.Start(state);
        return state;
    }

    private static MatchState Execute(MatchState state, List<string> ids)
    {
        var command = BattleFixture.Command(state, CommandKind.DiscardCards, new() { Ids = ids });
        var result = new GameEngine(new TurnCommands()).Execute(StateCodec.Copy(state), state.Pending!.Owner, command, new SequenceRandom());
        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        return result.State;
    }
}
