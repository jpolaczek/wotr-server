using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

// TEST-09: every base-game card has an event and a combat scenario in its side's tab. The event scenario offers the
// card as a legal event paid by a die of the turn and its play runs through the engine; the combat scenario waits on the
// card owner's Combat card choice, which offers the card, and committing it runs through the engine.
public class BaseCardScenarioTests
{
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);

    public static TheoryData<string> Cards => new(CardCatalog.Base.Keys.Order(StringComparer.Ordinal));

    [Fact]
    public void EveryBaseCardHasAnEventAndACombatScenarioInItsSidesTab()
    {
        var scenarios = TestScenarioCatalog.All.Where(s => s.Id.StartsWith("card-", StringComparison.Ordinal)).ToList();
        Assert.Equal(192, scenarios.Count);
        Assert.Equal(scenarios.Count, scenarios.Select(s => s.Id).Distinct().Count());
        Assert.Equal(
            ["Free Peoples cards · Character", "Free Peoples cards · Strategy", "Shadow cards · Character", "Shadow cards · Strategy"],
            scenarios.Select(s => s.Group).Distinct().Order(StringComparer.Ordinal));
        foreach (var card in CardCatalog.Base.Values)
        {
            var play = TestScenarioCatalog.Find(CardScenarios.EventId(card.Id))!;
            var combat = TestScenarioCatalog.Find(CardScenarios.CombatId(card.Id))!;
            Assert.Equal(card.Side, play.StartingSide);
            Assert.Equal(card.Side, combat.StartingSide);
            Assert.Equal(card.Title + " · Event", play.Title);
            Assert.StartsWith(card.Title + " · Combat: ", combat.Title);
            Assert.True(CardTextCatalog.CombatTitle(card.Id).Length > 0, card.Id);
            Assert.Contains(card.Title, play.Description);
            Assert.Contains(card.Title, combat.Description);
        }
    }

    [Theory]
    [MemberData(nameof(Cards))]
    public void TheEventScenarioOffersTheCardAndItsPlayRuns(string cardId)
    {
        var scenario = TestScenarioCatalog.Find(CardScenarios.EventId(cardId))!;
        var state = scenario.Create("card-event-" + cardId);
        Assert.Equal(scenario.StartingSide, state.Pending?.Owner);
        var view = Views.For(state, scenario.StartingSide);
        var option = view.Options.FirstOrDefault(o => o.Kind == CommandKind.PlayEvent);
        var play = option?.Events.FirstOrDefault(e => e.CardId == cardId);
        Assert.True(option is { Enabled: true } && play is not null,
            $"{cardId}: no legal event play ({option?.Reason ?? "no PlayEvent option"}; offered {string.Join(", ", option?.EligibleIds ?? [])})");
        var cost = play!.Costs.First(c => c.AbilityId.Length == 0);
        var command = new GameCommand
        {
            MatchId = state.Id,
            CommandId = "card-event-" + cardId,
            ExpectedRevision = state.Revision,
            DecisionId = view.Decision!.Id,
            Kind = CommandKind.PlayEvent,
            Payload = new() { CardId = cardId, DieId = cost.DieId, CardCost = cost.Face, NationId = play.NationTargets.FirstOrDefault() ?? "" }
        };
        Accepted(state, scenario.StartingSide, command);
    }

    [Theory]
    [MemberData(nameof(Cards))]
    public void TheCombatScenarioOffersTheCardAtItsOwnersCombatCardChoice(string cardId)
    {
        var scenario = TestScenarioCatalog.Find(CardScenarios.CombatId(cardId))!;
        var state = scenario.Create("card-combat-" + cardId);
        Assert.Equal(DecisionKind.CombatCard, state.Pending?.Kind);
        Assert.Equal(scenario.StartingSide, state.Pending!.Owner);
        var view = Views.For(state, scenario.StartingSide);
        var option = view.Options.Single(o => o.Kind == CommandKind.CommitCombatCard);
        Assert.Contains(cardId, option.EligibleIds);
        var command = new GameCommand
        {
            MatchId = state.Id,
            CommandId = "card-combat-" + cardId,
            ExpectedRevision = state.Revision,
            DecisionId = view.Decision!.Id,
            Kind = CommandKind.CommitCombatCard,
            Payload = new() { CardId = cardId }
        };
        Accepted(state, scenario.StartingSide, command);
    }

    private static void Accepted(MatchState state, Side side, GameCommand command)
    {
        var error = Engine.Validate(state, side, command);
        Assert.True(error is null, $"{command.Payload.CardId}: {error}");
        var result = Engine.Execute(state, side, command, new SeededRandom(7));
        Assert.True(result.Receipt.Accepted, $"{command.Payload.CardId}: {result.Receipt.Error}");
        Assert.True(result.State.Revision > state.Revision);
    }
}
