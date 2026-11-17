using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.BotProtocol;
using Xunit;

namespace Wotr.Tests;

// BOT-221: a card's recruitment is asked figure by figure; every legal plan stays open to the question, so AlphaWOTR's
// engine's own choice (Many Kings to the Service of Mordor: two Regulars in each of three settlements it picks) can be given.
public sealed class BotProtocolCardTests
{
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);

    [Fact]
    public void ManyKingsLeavesEveryThirdSettlementOpen()
    {
        var state = TestScenarioCatalog.Find("sh-muster-event")!.Create("protocol-many-kings");
        var view = Views.For(state, Side.Shadow);
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available && d.Face == DieFace.Muster);
        var play = new GameCommand
        {
            MatchId = state.Id, CommandId = "many-kings", ExpectedRevision = state.Revision, DecisionId = view.Decision!.Id,
            Kind = CommandKind.PlayEvent,
            Payload = new() { CardId = "base.sh.strategy.17", DieId = die.Id, CardCost = die.Face }
        };
        var played = Engine.Execute(state, Side.Shadow, play, new SeededRandom(7));
        Assert.True(played.Receipt.Accepted, played.Receipt.Error);
        state = played.State;
        view = Views.For(state, Side.Shadow);
        var snapshot = state;
        var root = ProtocolQuestions.For(view, c => Engine.Validate(snapshot, Side.Shadow, c) is null);
        Assert.NotNull(root);
        var decision = ProtocolDecision.Start(root!);

        void Pick(string answer)
        {
            var index = decision.Question!.Branches.ToList().FindIndex(b => (string?)b.Atom["answer"] == answer);
            Assert.True(index >= 0, answer + " not offered: " + string.Join(", ", decision.Question.Branches.Select(b => (string?)b.Atom["answer"])));
            Assert.True(decision.Answer(index));
        }

        foreach (var region in new[] { "near-harad", "near-harad", "north-rhun" })
        {
            Pick(region);
            Pick("REGULAR");
        }
        var open = decision.Question!.Branches.Select(b => (string?)b.Atom["answer"]).ToHashSet();
        Assert.Contains("north-rhun", open);
        Assert.Contains("south-rhun", open);
        Assert.Contains("umbar", open);
        Assert.Contains("far-harad", open);
        Pick("south-rhun");
        Pick("REGULAR");
        Pick("north-rhun");
        Pick("REGULAR");
        Pick("south-rhun");
        Pick("REGULAR");
        Assert.Null(decision.Question);
        Assert.Null(Engine.Validate(state, Side.Shadow, decision.Command!));
    }

    // The Palantír of Orthanc's deck is a draw with its deck (AlphaWOTR's engine decides it as its Palantír draw).
    [Fact]
    public void ThePalantirsDeckIsADraw()
    {
        var view = new PlayerView
        {
            You = Side.Shadow,
            Decision = new DecisionView { Kind = DecisionKind.EffectChoice, Presentation = "Deck", SourceId = "base.sh.character.21" }
        };
        var context = new ProtocolContext(view, _ => true);
        foreach (var deck in new[] { "character", "strategy" })
        {
            var atom = ProtocolEffectQuestions.Atom(context, "base.sh.character.21", deck, null);
            Assert.Equal("draw", (string?)atom["kind"]);
            Assert.Equal(deck, (string?)atom["deck"]);
        }
    }
}
