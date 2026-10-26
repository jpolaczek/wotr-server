using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

// UI-266: the six Fate cards with their own choices stand in the Fate tab. Each scenario is a Fate game whose owner holds
// the card with dice that pay for it; playing it runs through the engine and waits on the card's own choice.
public class FateCardScenarioTests
{
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);

    private static readonly string[] Cards =
        [FateCards.ThereAndBack, FateCards.Book, FateCards.Bardings, FateCards.Dwarves, FateCards.Orcs, FateCards.Monsters];

    public static TheoryData<string> Scenarios => new(Cards);

    [Fact]
    public void TheSixFateCardsStandInTheFateTab()
    {
        // UI-349: the group also holds one Fate Combat card (FateGimliScenarioTests).
        var scenarios = TestScenarioCatalog.All.Where(s => s.Group == FateCardScenarios.Group && s.Title.EndsWith(" · Event")).ToList();
        Assert.Equal(Cards.Select(Id), scenarios.Select(s => s.Id));
        foreach (var scenario in scenarios)
        {
            Assert.Equal([ExpansionModules.Fate], scenario.Modules);
            Assert.EndsWith(" · Event", scenario.Title);
        }
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void PlayingTheCardWaitsOnItsOwnChoice(string cardId)
    {
        var card = CardCatalog.All[cardId];
        var scenario = TestScenarioCatalog.Find(Id(cardId))!;
        var state = scenario.Create("fate-card-" + cardId);
        Assert.True(state.Configuration.Fate);
        Assert.Equal(card.Side, state.Pending?.Owner);
        var view = Views.For(state, card.Side);
        var play = view.Options.Single(o => o.Kind == CommandKind.PlayEvent).Events.Single(e => e.CardId == cardId);
        var cost = play.Costs.First(c => c.AbilityId.Length == 0);
        var command = new GameCommand
        {
            MatchId = state.Id,
            CommandId = "fate-card-" + cardId,
            ExpectedRevision = state.Revision,
            DecisionId = view.Decision!.Id,
            Kind = CommandKind.PlayEvent,
            Payload = new() { CardId = cardId, DieId = cost.DieId, CardCost = cost.Face }
        };
        var result = Engine.Execute(state, card.Side, command, new SeededRandom(7));
        Assert.True(result.Receipt.Accepted, $"{cardId}: {result.Receipt.Error}");
        Assert.Equal(card.Side, result.State.Pending?.Owner);
        Assert.NotEqual(DecisionKind.Action, result.State.Pending!.Kind);
        Assert.NotNull(Views.For(result.State, card.Side).Decision);
    }

    private static string Id(string cardId) => FateCardScenarios.Id(CardCatalog.All[cardId]);
}
