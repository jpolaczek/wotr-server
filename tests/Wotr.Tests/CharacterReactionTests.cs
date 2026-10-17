using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

public class CharacterReactionTests
{
    internal const string Bilbo = "base.fp.character.12";

    [Theory]
    [InlineData(DieFace.Event, true)]
    [InlineData(DieFace.Character, false)]
    public void GreyDrawsMatchingDeckOnlyAfterEventDieActionAndOnlyIfPlayerAccepts(DieFace cost, bool offered)
    {
        var state = TurnTests.Actions();
        HandRules.Add(state.Player(Side.FreePeoples), Bilbo);
        state.Fellowship.Corruption = 3;
        var p = Cost(state, cost);
        p.CardCost = cost;
        p.CardId = Bilbo;
        var top = state.Player(Side.FreePeoples).Decks["character"][0];
        var count = state.Player(Side.FreePeoples).Hand.Count;
        state = Do(state, CommandKind.PlayEvent, p);
        Assert.Equal(2, state.Fellowship.Corruption);
        Assert.Equal(count - 1, state.Player(Side.FreePeoples).Hand.Count);
        if (offered)
        {
            Assert.Equal("event.completed", state.Pending!.Window);
            var declined = Do(StateCodec.Copy(state), CommandKind.DeclineReaction);
            Assert.Equal(count - 1, declined.Player(Side.FreePeoples).Hand.Count);
            state = Do(StateCodec.Copy(state), CommandKind.RespondEffect, new() { ReactionId = "character.grey-draw" });
            Assert.Contains(top, state.Player(Side.FreePeoples).Hand);
        }
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == p.DieId).Location);
    }

    [Fact]
    public void GandalfBecomingGuideDuringEventCanDrawAndLeavingDuringEventCannot()
    {
        foreach (var leaving in new[] { false, true })
        {
            var state = TurnTests.Actions();
            state.Fellowship.Companions = [CharacterRules.Grey, CharacterRules.Strider];
            state.Fellowship.Guide = leaving ? CharacterRules.Grey : CharacterRules.Strider;
            var removed = leaving ? CharacterRules.Grey : CharacterRules.Strider;
            var catalog = new EffectCatalog([new(Bilbo, [new LeaveCompanionStep(removed)]), .. CharacterReactions.Programs],
                [new(Bilbo, Bilbo, new UnconditionalEvent())], CharacterReactions.Reactions);
            HandRules.Add(state.Player(Side.FreePeoples), Bilbo);
            var p = Cost(state, DieFace.Event);
            p.CardId = Bilbo;
            state = Do(state, CommandKind.PlayEvent, p, catalog: catalog);
            Assert.Equal(leaving ? CharacterRules.Strider : CharacterRules.Grey, state.Fellowship.Guide);
            Assert.Equal(!leaving, state.Pending!.Kind == DecisionKind.Reaction);
        }
    }

    [Fact]
    public void GreyDrawAfterCelebornPreservesHandLimitAndPaidContinuation()
    {
        var state = TurnTests.Actions();
        const string card = "base.fp.strategy.15";
        HandRules.Add(state.Player(Side.FreePeoples), card);
        foreach (var id in state.Player(Side.FreePeoples).Decks["character"].Take(5).ToArray())
        {
            state.Player(Side.FreePeoples).Decks["character"].Remove(id);
            HandRules.Add(state.Player(Side.FreePeoples), id);
        }
        var p = Cost(state, DieFace.Event);
        p.CardId = card;
        state = Do(state, CommandKind.PlayEvent, p);
        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [state.Pending.PrivateChoices[0]] });
        Assert.Equal("event.completed", state.Pending!.Window);
        Assert.Equal(6, state.Player(Side.FreePeoples).Hand.Count);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.grey-draw" });
        Assert.Equal(DecisionKind.DiscardCards, state.Pending!.Kind);
        Assert.Equal(7, state.Player(Side.FreePeoples).Hand.Count);
        Assert.Equal(DieLocation.Resolving, state.Dice.Single(d => d.Id == p.DieId).Location);
        state = Do(StateCodec.Copy(state), CommandKind.DiscardCards, new() { Ids = [state.Player(Side.FreePeoples).Hand[0]] });
        Assert.Equal(6, state.Player(Side.FreePeoples).Hand.Count);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Empty(state.Resolution.Frames);
    }

    private sealed record LeaveCompanionStep(string Id) : EffectStep
    {
        public override void Apply(EffectContext context)
        {
            var plan = CompanionRules.Separate(context.State, [Id], [context.State.Fellowship.RegionId])!;
            CompanionRules.Apply(context.State, plan, true);
            FellowshipFlow.CompositionChanged(context.State, FellowshipReturn.Parent);
        }
    }
}
