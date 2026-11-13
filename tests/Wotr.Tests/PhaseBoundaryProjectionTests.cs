using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public sealed class PhaseBoundaryProjectionTests
{
    internal static readonly string[] Windows = ["fellowship", "allocation", "recovery-fp", "recovery-shadow", "recovery-both", "draw-fp", "draw-shadow"];
    public static IEnumerable<object[]> Cases() => Windows.Select(window => new object[] { window });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OrdinaryPhaseAndDiscardFactsAreCopiedAndRespectDecisionOwnership(string window)
    {
        var state = At(window);
        var original = StateCodec.Write(state);
        var projection = GameRuntime.Projection(EffectCatalog.All);
        foreach (var side in Enum.GetValues<Side>())
        {
            var view = projection.For(state, side);
            var facts = Assert.IsType<ActionBoundaryView>(view.ActionBoundary);
            Assert.Null(facts.FellowshipMove);
            Assert.Null(facts.CardHunt);
            Assert.Equal(window.StartsWith("draw-") ? 1 : 0, facts.PaidDieIds.Count);
            Assert.Equal(JsonSerializer.Serialize(view), JsonSerializer.Serialize(projection.For(StateCodec.Copy(state), side)));
            if (side != state.Pending!.Owner)
            {
                Assert.Null(view.Decision);
                Assert.Empty(view.Options);
            }
            facts.Companions.Clear();
            facts.Players.Single(p => p.Side == side).KnownDiscards.Clear();
        }
        Assert.Equal(original, StateCodec.Write(state));
    }

    [Theory]
    [InlineData("resume-effect")]
    [InlineData("missing-payment")]
    [InlineData("wrong-count")]
    [InlineData("wrong-owner")]
    [InlineData("wrong-phase")]
    [InlineData("politics")]
    public void OtherDiscardContinuationsAreNotMistakenForAnOrdinaryHandLimit(string change)
    {
        var state = At("draw-shadow");
        switch (change)
        {
            case "resume-effect": state.AfterDiscard = DiscardContinuation.ResumeEffect; break;
            case "missing-payment": state.Resolution.PaidDieIds.Clear(); break;
            case "wrong-count": state.Pending!.RequiredCount++; break;
            case "wrong-owner": state.Actor = Side.FreePeoples; break;
            case "wrong-phase": state.Phase = GamePhase.Fellowship; break;
            case "politics": state.PoliticalReactions.Add(new(PoliticalTrigger.ArmyAttack, "north", "dale")); break;
        }
        Assert.Null(new Projection().For(state, Side.Shadow).ActionBoundary);
    }

    internal static MatchState At(string window)
    {
        var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), new SeededRandom(137));
        if (window.StartsWith("recovery-"))
            foreach (var side in Enum.GetValues<Side>().Where(side => window == "recovery-both" ||
                side == (window == "recovery-fp" ? Side.FreePeoples : Side.Shadow)))
                for (var i = 0; i < 5; i++)
                    CardFlow.Draw(state, side, "character");
        TurnFlow.Start(state);
        if (window.StartsWith("recovery-") || window == "fellowship")
            return state;
        state = CharacterFixture.Do(state, CommandKind.ContinueFellowship);
        if (window == "allocation")
            return state;
        state = CharacterFixture.Do(state, CommandKind.AllocateHunt, new(), new SeededRandom(137));
        var owner = window == "draw-fp" ? Side.FreePeoples : Side.Shadow;
        for (var i = 0; i < 4; i++)
            CardFlow.Draw(state, owner, "character");
        var die = state.Dice.First(d => d.Owner == owner && d.Location == DieLocation.Available);
        die.Face = DieFace.Event;
        TurnFlow.SelectNext(state, owner);
        return CharacterFixture.Do(state, CommandKind.DrawCard, new() { DieId = die.Id, Deck = "character" });
    }
}
