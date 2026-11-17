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

    // Figures for two destinations (Swords in Eriador: the North in the Shire, the Dwarves in Ered Luin) are asked
    // destination first, then the unit with the destination as the question's words and the unit alone as the option.
    [Fact]
    public void SwordsInEriadorAsksTheDestinationFirst()
    {
        var state = TestScenarioCatalog.Find("fp-event-play")!.Create("protocol-swords");
        var player = state.Players.Single(p => p.Side == Side.FreePeoples);
        const string swords = "base.fp.strategy.20";
        if (!player.Hand.Contains(swords))
        {
            (player.Decks.Values.FirstOrDefault(d => d.Contains(swords)) ?? player.Discard).Remove(swords);
            player.Hand.Add(swords);
            player.HandVersion++;
        }
        var view = Views.For(state, Side.FreePeoples);
        var die = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Event);
        var played = Engine.Execute(state, Side.FreePeoples, new GameCommand
        {
            MatchId = state.Id, CommandId = "swords", ExpectedRevision = state.Revision, DecisionId = view.Decision!.Id,
            Kind = CommandKind.PlayEvent, Payload = new() { CardId = swords, DieId = die.Id, CardCost = die.Face }
        }, new SeededRandom(7));
        Assert.True(played.Receipt.Accepted, played.Receipt.Error);
        state = played.State;
        view = Views.For(state, Side.FreePeoples);
        var snapshot = state;
        var decision = ProtocolDecision.Start(ProtocolQuestions.For(view, c => Engine.Validate(snapshot, Side.FreePeoples, c) is null)!);
        var first = decision.Question!;
        Assert.Equal(new[] { "ered-luin", "the-shire" }, first.Branches.Select(b => (string?)b.Atom["region"]).Where(r => r is not null).Order().ToArray());
        Assert.True(decision.Answer(first.Branches.ToList().FindIndex(b => (string?)b.Atom["answer"] == "the-shire")));
        var unit = decision.Question!;
        Assert.Equal("Which unit is recruited in the-shire for NORTH ?", (string?)unit.Context!["question"]);
        Assert.All(unit.Branches, b => Assert.Null(b.Atom["region"]));
        Assert.True(decision.Answer(unit.Branches.ToList().FindIndex(b => (string?)b.Atom["answer"] == "REGULAR")));
        while (decision.Question is { } next)
            Assert.True(decision.Answer(0));
        Assert.Null(Engine.Validate(state, Side.FreePeoples, decision.Command!));
    }

    // A forfeit of Nazgûl Leadership is a count of points (AlphaWOTR's Dread and Despair counts); the Witch-king's go last.
    [Fact]
    public void ANazgulLeadershipForfeitIsACount()
    {
        string[] ids = ["witch-king:leadership:1", "witch-king:leadership:2", "n1:leadership:1", "n2:leadership:1"];
        var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 1, Maximum = 4, EligibleIds = ids.ToList() };
        foreach (var id in ids)
            option.EffectChoices.Add(new EffectChoiceView { Id = id, Title = id.StartsWith("witch-king") ? "Witch-king ◆" : "Nazgûl ◆" });
        var view = new PlayerView
        {
            You = Side.Shadow, Options = [option],
            Decision = new DecisionView { Kind = DecisionKind.EffectChoice, Presentation = "NazgulLeadership", SourceId = "base.sh.character.05" }
        };
        var context = new ProtocolContext(view, c => c.Payload.Ids.Count >= 1);
        var paths = ProtocolEffectQuestions.Choice(context).ToList();
        Assert.Equal(new int?[] { 1, 2, 3, 4 }, paths.Select(p => (int?)p.Steps[0].Atom["count"]).ToArray());
        Assert.Equal(new[] { "n1:leadership:1", "n2:leadership:1" }, paths[1].Command!.Payload.Ids);
        Assert.Equal("witch-king:leadership:1", paths[2].Command!.Payload.Ids[2]);
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
