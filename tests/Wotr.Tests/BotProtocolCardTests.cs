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

    // The Free Peoples' own card separating Companions (I Will Go Alone) asks CARD_CHOICE: each Companion with its character
    // key, then the destination as a region (AlphaWOTR's adapter reads its engine's choice back by these fields).
    [Fact]
    public void IWillGoAloneAsksCardChoices()
    {
        const string alone = "base.fp.character.11";
        var state = TestScenarioCatalog.Find("card-" + alone + "-event")!.Create("protocol-alone");
        var view = Views.For(state, Side.FreePeoples);
        var play = view.Options.Single(o => o.Kind == CommandKind.PlayEvent).Events.Single(e => e.CardId == alone);
        var cost = play.Costs.First();
        var die = state.Dice.Single(d => d.Id == cost.DieId);
        var played = Engine.Execute(state, Side.FreePeoples, new GameCommand
        {
            MatchId = state.Id, CommandId = "alone", ExpectedRevision = state.Revision, DecisionId = view.Decision!.Id,
            Kind = CommandKind.PlayEvent, Payload = new() { CardId = alone, DieId = die.Id, CardCost = die.Face }
        }, new SeededRandom(7));
        Assert.True(played.Receipt.Accepted, played.Receipt.Error);
        state = played.State;
        view = Views.For(state, Side.FreePeoples);
        Assert.Equal(DecisionKind.CompanionSeparation, view.Decision!.Kind);
        var snapshot = state;
        var decision = ProtocolDecision.Start(ProtocolQuestions.For(view, c => Engine.Validate(snapshot, Side.FreePeoples, c) is null)!);
        var first = decision.Question!;
        Assert.Equal("CARD_CHOICE", first.Question);
        Assert.All(first.Branches, b => Assert.NotNull(b.Atom["character"]));
        Assert.True(decision.Answer(0));
        var next = decision.Question!;
        var to = next.Branches.ToList().FindIndex(b => b.Atom["region"] is not null);
        Assert.True(to >= 0);
        Assert.True(decision.Answer(to));
        Assert.Null(decision.Question);
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

    // Worn with Sorrow and Toil answers in the words AlphaWOTR's adapter has its rule for.
    [Fact]
    public void WornWithSorrowAnswersHandOrSkip()
    {
        var context = new ProtocolContext(new PlayerView { You = Side.Shadow, Decision = new DecisionView { Kind = DecisionKind.EffectChoice } }, _ => true);
        Assert.Equal("hand", (string?)ProtocolEffectQuestions.Atom(context, "base.sh.character.15", "worn.random-character", null)["answer"]);
        var skip = ProtocolEffectQuestions.Atom(context, "base.sh.character.15", "worn.decline", null);
        Assert.Equal("skip", (string?)skip["answer"]);
        Assert.True((bool?)skip["skip"]);
    }

    // The Lidless Eye is one question of face multisets (AlphaWOTR's dice_count and dice_faces); dice of one face are alike.
    [Fact]
    public void TheLidlessEyeOffersFaceMultisets()
    {
        var dice = new[] { ("d1", DieFace.Army), ("d2", DieFace.Army), ("d3", DieFace.Muster), ("d4", DieFace.Character) };
        var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 0, Maximum = 3, EligibleIds = dice.Select(d => d.Item1).ToList() };
        var view = new PlayerView
        {
            You = Side.Shadow, Options = [option],
            Dice = dice.Select(d => new DieView { Id = d.Item1, Owner = Side.Shadow, Face = d.Item2, Location = DieLocation.Available }).ToList(),
            Decision = new DecisionView { Kind = DecisionKind.EffectChoice, Presentation = "ActionDie", SourceId = "base.sh.character.18" }
        };
        var paths = ProtocolEffectQuestions.Choice(new ProtocolContext(view, _ => true)).ToList();
        var answers = paths.Select(p => (string?)p.Steps[0].Atom["answer"]).ToList();
        Assert.Equal(answers.Count, answers.Distinct().Count());
        Assert.Contains("none", answers);
        Assert.Contains("ARMY:ARMY", answers);
        Assert.Contains("ARMY:ARMY:MUSTER", answers);
        var pair = paths.Single(p => (string?)p.Steps[0].Atom["answer"] == "ARMY:ARMY");
        Assert.Equal(2, (int?)pair.Steps[0].Atom["count"]);
        Assert.Equal(new[] { "d1", "d2" }, pair.Command!.Payload.Ids);
        // 0 dice, 3 single faces, 4 pairs (AA, AC, AM, CM), 4 triples (AAC, AAM, ACM... ) at most.
        Assert.True(paths.Count <= 1 + 3 + 4 + 4);
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
