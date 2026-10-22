using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

// UI-300 (owner's decision): a card's event scenario is complete only if the card can really act on its board. Each
// event is played with its event die, the demo CPU resolves the decisions the effect opens, and the board must differ from
// the one where the card was merely discarded and its die spent; a card that rolls dice or draws tiles counts as acting if
// any of a few seeds lets it. A clause the engine may skip ("if", "or", "you may") is checked on the starting board.
public class BaseCardScenarioEffectTests
{
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);
    private static readonly DemoBot Bot = new(Engine, Views);
    private static readonly int[] Seeds = [1, 2, 3, 4, 5, 7];
    private const int StepLimit = 200;
    private const string Gimli = "base.character.gimli";
    private const string Legolas = "base.character.legolas";

    // The clauses of the base events whose standard board would leave them nothing to act on.
    private static readonly Dictionary<string, (string Clause, Func<MatchState, string, bool> Holds)> Conditions = new()
    {
        ["base.fp.character.09"] = ("Corruption to heal while Strider guides", (s, _) => s.Fellowship.Corruption > 0 && s.Fellowship.Guide == CharacterRules.Strider),
        ["base.fp.character.10"] = ("Corruption to heal while Gollum guides", (s, _) => s.Fellowship.Corruption > 0 && s.Fellowship.Guide == CharacterRules.Gollum),
        ["base.fp.character.11"] = ("a Companion to separate and Corruption to heal", (s, _) => s.Fellowship.Companions.Count > 0 && s.Fellowship.Corruption > 0),
        ["base.fp.character.12"] = ("Corruption to heal while Gollum guides", (s, _) => s.Fellowship.Corruption > 0 && s.Fellowship.Guide == CharacterRules.Gollum),
        ["base.fp.character.13"] = ("a second Character result and Corruption to heal in unconquered Lórien", (s, _) =>
            AvailableDice(s, Side.FreePeoples, DieFace.Character) >= 2 && s.Fellowship.Corruption > 0 &&
            s.Fellowship.RegionId == "lorien" && s.Control.GetValueOrDefault("lorien") != Side.Shadow),
        ["base.fp.character.14"] = ("an Eye in every draw of three, and three Eyes possible", (s, _) => PoolTiles(s, eye: false) < 3 && PoolTiles(s, eye: true) >= 3),
        ["base.fp.character.15"] = ("a Companion outside the Fellowship to move and one inside to separate", (s, _) => Outside(s).Any() && s.Fellowship.Companions.Count > 0),
        ["base.fp.character.16"] = ("a Companion outside the Fellowship to move and one inside to separate", (s, _) => Outside(s).Any() && s.Fellowship.Companions.Count > 0),
        ["base.fp.character.17"] = ("Gimli or Legolas in Dale, Erebor or the Woodland Realm", (s, _) =>
            s.Characters.Any(c => c.Id is Gimli or Legolas && c.Location is "dale" or "erebor" or "woodland-realm")),
        ["base.fp.character.19"] = ("another Character Event in hand for the free play", (s, card) => OtherCharacterEvent(s, card)),
        ["base.fp.character.20"] = ("another Character Event in hand for the free play", (s, card) => OtherCharacterEvent(s, card)),
        ["base.fp.character.21"] = ("another Character Event in hand for the free play", (s, card) => OtherCharacterEvent(s, card)),
        ["base.fp.character.22"] = ("a Companion beside Strider and a Shadow army at a destination", (s, _) =>
            Outside(s).Count(c => c.Location == Location(s, CharacterRules.Strider)) >= 2 && new[] { "erech", "lamedon", "pelargir" }.Any(r => Army(s, r, Side.Shadow))),
        ["base.fp.strategy.04"] = ("a Companion outside the Fellowship within a move of Erebor or Ered Luin", (s, _) => Outside(s).Any(c => Near(c.Location, "erebor", "ered-luin"))),
        ["base.fp.strategy.06"] = ("a Shadow army in Osgiliath or Ithilien and a Free Peoples army in Osgiliath", (s, _) =>
            new[] { "osgiliath", "south-ithilien", "north-ithilien" }.Any(r => Army(s, r, Side.Shadow)) && Army(s, "osgiliath", Side.FreePeoples)),
        ["base.fp.strategy.07"] = ("a Companion outside the Fellowship within a move of the Shire or Bree", (s, _) => Outside(s).Any(c => Near(c.Location, "the-shire", "bree"))),
        ["base.fp.strategy.16"] = ("a Companion in a Rohan region", (s, _) => Outside(s).Any(c => BoardRules.Regions[c.Location].Nation == "rohan")),
        ["base.sh.character.09"] = ("a Fellowship region the Nazgûl may enter", (s, _) => BoardRules.Regions[s.Fellowship.RegionId].Settlement != "Stronghold"),
        ["base.sh.character.11"] = ("a Fellowship region the Nazgûl may enter and a Character Event on the Free Peoples table", (s, _) =>
            BoardRules.Regions[s.Fellowship.RegionId].Settlement != "Stronghold" &&
            Player(s, Side.FreePeoples).Table.Any(id => CardCatalog.Base[id].Deck == "character")),
        ["base.sh.character.19"] = ("several Nazgûl to roll for", (s, _) => s.Units.Count(u => u.Kind == UnitKind.Nazgul && u.Location == "minas-morgul") >= 3),
        ["base.sh.strategy.04"] = ("an unused Will of the West result", (s, _) => AvailableDice(s, Side.FreePeoples, DieFace.WillOfTheWest) > 0)
    };

    public static TheoryData<string> ConditionalCards => new(Conditions.Keys.Order(StringComparer.Ordinal));

    [Fact]
    public void EveryEventScenarioLetsItsCardChangeTheBoard()
    {
        var idle = CardCatalog.Base.Keys.Order(StringComparer.Ordinal).Where(id => !Seeds.Any(seed => ChangesTheBoard(id, seed))).ToList();
        Assert.True(idle.Count == 0, "These event scenarios leave their card nothing to act on: " +
            string.Join(", ", idle.Select(id => id + " (" + CardCatalog.Base[id].Title + ")")));
    }

    [Theory]
    [MemberData(nameof(ConditionalCards))]
    public void TheEventScenarioMeetsTheCardsClauses(string cardId)
    {
        var state = TestScenarioCatalog.Find(CardScenarios.EventId(cardId))!.Create("card-clause-" + cardId);
        var (clause, holds) = Conditions[cardId];
        Assert.True(holds(state, cardId), $"{cardId} ({CardCatalog.Base[cardId].Title}) needs {clause}.");
    }

    private static bool ChangesTheBoard(string cardId, int seed)
    {
        var scenario = TestScenarioCatalog.Find(CardScenarios.EventId(cardId))!;
        var state = scenario.Create("card-effect-" + cardId);
        var side = scenario.StartingSide;
        var view = Views.For(state, side);
        var play = view.Options.First(o => o.Kind == CommandKind.PlayEvent).Events.First(e => e.CardId == cardId);
        var cost = play.Costs.First(c => c.AbilityId.Length == 0);
        var before = Board(state, cardId, cost.DieId);
        state = Execute(state, side, new GameCommand
        {
            MatchId = state.Id,
            CommandId = "card-effect-" + cardId,
            ExpectedRevision = state.Revision,
            DecisionId = view.Decision!.Id,
            Kind = CommandKind.PlayEvent,
            Payload = new() { CardId = cardId, DieId = cost.DieId, CardCost = cost.Face, NationId = play.NationTargets.FirstOrDefault() ?? "" }
        }, seed);
        state = Resolve(state, seed);
        return Board(state, cardId, cost.DieId) != before;
    }

    // The demo CPU answers every decision until the effect is over and an action is due again; a journey of Companions
    // outside the Fellowship, which the CPU always ends without a move, moves one of them a region.
    private static MatchState Resolve(MatchState state, int seed)
    {
        for (var step = 0; step < StepLimit && state.Winner is null && state.Pending is { } pending; step++)
        {
            if (EffectRuntime.Current(state) is null && pending.Kind == DecisionKind.Action)
                return state;
            var command = Journey(state, pending) ?? Bot.Next(state, pending.Owner);
            if (command is null)
                return state;
            state = Execute(state, pending.Owner, command, seed + step);
        }
        return state;
    }

    private static GameCommand? Journey(MatchState state, PendingDecision pending)
    {
        if (pending.Kind != DecisionKind.CompanionSeparation)
            return null;
        var view = Views.For(state, pending.Owner);
        var option = view.Options.FirstOrDefault(o => o.Kind == CommandKind.MoveCompanions && o.Enabled);
        var travel = option?.Travel.FirstOrDefault(t => Step(t) is not null);
        if (travel is null)
            return null;
        return new GameCommand
        {
            MatchId = state.Id,
            CommandId = "card-journey-" + state.Revision,
            ExpectedRevision = state.Revision,
            DecisionId = view.Decision!.Id,
            Kind = CommandKind.MoveCompanions,
            Payload = new() { CompanionMoves = [new() { Ids = [travel.Id], Route = [travel.Origin, Step(travel)!] }] }
        };
    }

    private static string? Step(TravelOption travel) =>
        travel.Targets.FirstOrDefault(target => target != travel.Origin && BoardRules.Regions[travel.Origin].Neighbors.Contains(target));

    private static MatchState Execute(MatchState state, Side side, GameCommand command, int seed)
    {
        var result = Engine.Execute(state, side, command, new SeededRandom(seed));
        Assert.True(result.Receipt.Accepted, $"{command.Kind}: {result.Receipt.Error}");
        return result.State;
    }

    // What a card can change: figures, characters, nations, control, the Fellowship, the Hunt, rings, cards and dice. The
    // played card's way to the discard pile and the die that paid for it do not count.
    private static string Board(MatchState state, string cardId, string dieId) => JsonSerializer.Serialize(new
    {
        state.Units,
        state.Characters,
        state.Nations,
        state.Control,
        state.Fellowship,
        state.HuntTiles,
        state.Rings,
        state.MinionEverEntered,
        state.ActiveEffects,
        Players = state.Players.Select(p => new
        {
            p.Side,
            Hand = p.Hand.Where(id => id != cardId).Order(StringComparer.Ordinal),
            p.Decks,
            Discard = p.Discard.Where(id => id != cardId).Order(StringComparer.Ordinal),
            p.Table,
            p.ExtraDiceNextRecovery
        }),
        Dice = state.Dice.Where(d => d.Id != dieId)
    });

    private static IEnumerable<CharacterState> Outside(MatchState state) => state.Characters.Where(c =>
        c.Side == Side.FreePeoples && c.Level > 0 && CharacterRules.OnBoard(c) && !state.Fellowship.Companions.Contains(c.Id));

    private static string Location(MatchState state, string id) => state.Characters.Single(c => c.Id == id).Location;

    private static bool Near(string region, params string[] targets) =>
        targets.Contains(region) || BoardRules.Regions[region].Neighbors.Any(targets.Contains);

    private static bool Army(MatchState state, string region, Side side) =>
        state.Units.Any(u => u.Location == region && BoardRules.Owner(u) == side);

    private static int AvailableDice(MatchState state, Side side, DieFace face) =>
        state.Dice.Count(d => d.Owner == side && d.Location == DieLocation.Available && d.Face == face);

    private static int PoolTiles(MatchState state, bool eye) =>
        state.HuntTiles.Count(t => t.Location == "pool" && (HuntPool.Definitions[t.Id].Value == "eye") == eye);

    private static PlayerState Player(MatchState state, Side side) => state.Players.Single(p => p.Side == side);

    private static bool OtherCharacterEvent(MatchState state, string cardId) =>
        Player(state, Side.FreePeoples).Hand.Any(id => id != cardId && CardCatalog.Base[id].Deck == "character");
}
