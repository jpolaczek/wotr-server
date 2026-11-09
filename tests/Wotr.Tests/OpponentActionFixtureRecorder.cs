using System.Text.Json;
using System.Text.Json.Serialization;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

// UI-303: records the authorized All history feed of real engine games as the client receives it, for the client's
// opponent-presentation fixtures. A baseline page holds the history already shown when the board first loads; each
// update is the page an `after` cursor returns after one more revision. Runs only when WOTR_RECORD_OPPONENT_FIXTURES
// names the output folder; otherwise it records nothing and passes.
public class OpponentActionFixtureRecorder
{
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);
    private static readonly DemoBot Bot = new(Engine, Views);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };
    private const int StepLimit = 120;
    private const string RohanCard = "base.fp.strategy.14";
    private const string IsengardCard = "base.sh.strategy.13";

    [Fact]
    public void RecordOpponentActionFixtures()
    {
        var folder = Environment.GetEnvironmentVariable("WOTR_RECORD_OPPONENT_FIXTURES");
        if (string.IsNullOrEmpty(folder))
            return;
        Directory.CreateDirectory(folder);
        Write(folder, "march", Side.FreePeoples, Marches());
        Write(folder, "card-recruit", Side.FreePeoples, CardRecruitment());
        foreach (var viewer in new[] { Side.FreePeoples, Side.Shadow })
        {
            Write(folder, "attack", viewer, Attack());
            Write(folder, "hunt", viewer, Hunt());
        }
        Write(folder, "reaction", Side.FreePeoples, Crebain());
    }

    // Storyboard A: one Army die, a partial march out of Dol Guldur, then the Moria army into Hollin.
    private static Game Marches()
    {
        var random = new SeededRandom(3);
        var board = ScenarioBoard.Setup("fixture-march", random)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.Army)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
            .AtWar("sauron");
        var game = new Game(board.Act(Side.Shadow));
        var die = game.State.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available && d.Face == DieFace.Army).Id;
        var movers = Units(game.State, "dol-guldur", UnitKind.Regular).Take(3).Concat(Units(game.State, "dol-guldur", UnitKind.Elite).Take(1));
        game.Play(Side.Shadow, CommandKind.MoveArmies, March(die, "dol-guldur", "eastern-brown-lands", movers), random);
        game.Play(Side.Shadow, CommandKind.MoveArmies, March(game.State.Resolution.ActionSequence?.DieId ?? die, "moria", "hollin",
            Units(game.State, "moria", UnitKind.Regular)), random);
        return game;
    }

    // Storyboard B: Orcs Multiplying Again recruits in Dol Guldur and Mount Gundabad.
    private static Game CardRecruitment()
    {
        var random = new SeededRandom(5);
        var game = new Game(TestScenarioCatalog.Find(CardScenarios.EventId("base.sh.strategy.20"))!.Create("fixture-card-recruit"));
        game.PlayEvent(Side.Shadow, "base.sh.strategy.20", random);
        game.Resolve(random);
        return game;
    }

    // Storyboard C: Isengard attacks the Rohan army at the Fords of Isen; both sides commit a Combat card.
    private static Game Attack()
    {
        var random = new SeededRandom(7);
        var board = ScenarioBoard.Setup("fixture-attack", random)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Character, DieFace.Muster, DieFace.Event, DieFace.Army, DieFace.Muster)
            .Hunt(1, 0)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
            .AtWar("isengard", "rohan")
            .Place("rohan", UnitKind.Elite, "fords-of-isen")
            .Hand(Side.FreePeoples, RohanCard)
            .Hand(Side.Shadow, IsengardCard);
        var game = new Game(board.Act(Side.Shadow));
        var die = game.State.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available && d.Face == DieFace.Army).Id;
        var attackers = game.State.Units.Where(u => u.Location == "orthanc" && !u.Garrison && BoardRules.Owner(u) == Side.Shadow).Select(u => u.Id).ToList();
        game.Play(Side.Shadow, CommandKind.AttackArmy, new CommandPayload
        {
            DieId = die,
            MovementCost = DieFace.Army,
            Moves = [new() { From = "orthanc", To = "fords-of-isen", UnitIds = attackers }]
        }, random);
        game.Resolve(random, state => state.Pending?.Kind == DecisionKind.CombatCard ? CombatCard(state) : null);
        return game;
    }

    // Storyboard D: Axe and Bow is played, then the Fellowship moves into an almost certain Hunt.
    private static Game Hunt()
    {
        var random = new SeededRandom(2);
        var game = new Game(TestScenarioCatalog.Find(CardScenarios.EventId("base.fp.character.06"))!.Create("fixture-hunt"));
        game.PlayEvent(Side.FreePeoples, "base.fp.character.06", random);
        game.Resolve(random);
        game.MoveFellowship(random);
        game.Resolve(random);
        return game;
    }

    // The opponent's reaction inside our action: Flocks of Crebain adds +1 to the Hunt of the Fellowship's move.
    private static Game Crebain()
    {
        var random = new SeededRandom(4);
        var game = new Game(TestScenarioCatalog.Find(CardScenarios.EventId("base.sh.character.16"))!.Create("fixture-reaction"));
        game.PlayEvent(Side.Shadow, "base.sh.character.16", random);
        game.Resolve(random);
        game.MoveFellowship(random);
        game.Resolve(random, state => CardReaction(state, Side.Shadow, "base.sh.character.16"));
        return game;
    }

    private static IEnumerable<UnitState> Units(MatchState state, string region, UnitKind kind) =>
        state.Units.Where(u => u.Location == region && u.Kind == kind && !u.Garrison);

    private static CommandPayload March(string die, string from, string to, IEnumerable<UnitState> units) => new()
    {
        Stepwise = true,
        DieId = die,
        MovementCost = DieFace.Army,
        Moves = [new() { From = from, To = to, UnitIds = units.Select(u => u.Id).ToList() }]
    };

    // Each side commits a Combat card the battle allows (the one dealt for it first), or no card when none is allowed.
    private static (Side, CommandKind, CommandPayload)? CombatCard(MatchState state)
    {
        var side = state.Pending!.Owner;
        var option = Views.For(state, side).Options.Single(o => o.Kind == CommandKind.CommitCombatCard);
        var dealt = side == Side.FreePeoples ? RohanCard : IsengardCard;
        var card = option.EligibleIds.Contains(dealt) ? dealt : option.EligibleIds.FirstOrDefault() ?? "";
        return (side, CommandKind.CommitCombatCard, new CommandPayload { CardId = card });
    }

    private static (Side, CommandKind, CommandPayload)? CardReaction(MatchState state, Side side, string card)
    {
        if (state.Pending is not { Kind: DecisionKind.Reaction } pending || pending.Owner != side)
            return null;
        var option = Views.For(state, side).Options.FirstOrDefault(o => o.Kind == CommandKind.RespondEffect && o.Enabled);
        var choice = option?.EffectChoices.FirstOrDefault(c => c.SourceId == card);
        return choice is null ? null : (side, CommandKind.RespondEffect, new CommandPayload { ReactionId = choice.Id });
    }

    private static void Write(string folder, string scenario, Side viewer, Game game)
    {
        var name = scenario + "-" + (viewer == Side.FreePeoples ? "free-peoples" : "shadow") + ".json";
        var recording = game.Recording(scenario, viewer);
        File.WriteAllText(Path.Combine(folder, name), JsonSerializer.Serialize(recording, Json) + "\n");
    }

    // A game played command by command, remembering the revision after each one.
    private sealed class Game
    {
        public MatchState State { get; private set; }
        private readonly int baseline;
        private readonly long baselineRevision;
        private readonly List<(long Revision, int Events)> marks = [];

        public Game(MatchState initial)
        {
            State = initial;
            baseline = initial.Events.Count;
            baselineRevision = initial.Revision;
        }

        public void Play(Side side, CommandKind kind, CommandPayload payload, IRandomSource random)
        {
            var view = Views.For(State, side);
            var command = new GameCommand
            {
                MatchId = State.Id,
                CommandId = "fixture-" + State.Revision,
                ExpectedRevision = State.Revision,
                DecisionId = view.Decision!.Id,
                Kind = kind,
                Payload = payload
            };
            Commit(side, command, random);
        }

        public void PlayEvent(Side side, string card, IRandomSource random)
        {
            var view = Views.For(State, side);
            var play = view.Options.First(o => o.Kind == CommandKind.PlayEvent).Events.First(e => e.CardId == card);
            var costs = play.Costs.Where(c => c.AbilityId.Length == 0).ToList();
            var cost = costs.FirstOrDefault(c => State.Dice.Single(d => d.Id == c.DieId).Face == DieFace.Event) ?? costs[0];
            Play(side, CommandKind.PlayEvent, new CommandPayload
            {
                CardId = card, DieId = cost.DieId, CardCost = cost.Face, NationId = play.NationTargets.FirstOrDefault() ?? ""
            }, random);
        }

        public void MoveFellowship(IRandomSource random)
        {
            for (var step = 0; step < StepLimit && State.Pending is { Kind: DecisionKind.Action, Owner: Side.Shadow }; step++)
                Commit(Side.Shadow, Bot.Next(State, Side.Shadow)!, random);
            var option = Views.For(State, Side.FreePeoples).Options.First(o => o.Kind == CommandKind.MoveFellowship && o.Enabled);
            Play(Side.FreePeoples, CommandKind.MoveFellowship, new CommandPayload { DieId = option.EligibleDice[0] }, random);
        }

        // Answers every decision (scripted where given, otherwise by the demo CPU) until an action is due again.
        public void Resolve(IRandomSource random, Func<MatchState, (Side, CommandKind, CommandPayload)?>? script = null)
        {
            for (var step = 0; step < StepLimit && State.Winner is null && State.Pending is { } pending; step++)
            {
                if (pending.Kind == DecisionKind.Action && State.Resolution.Frames.Count == 0)
                    return;
                if (script?.Invoke(State) is { } scripted)
                    Play(scripted.Item1, scripted.Item2, scripted.Item3, random);
                else
                    Commit(pending.Owner, Bot.Next(State, pending.Owner)!, random);
            }
        }

        private void Commit(Side side, GameCommand command, IRandomSource random)
        {
            var result = Engine.Execute(State, side, command, random);
            Assert.True(result.Receipt.Accepted, $"{command.Kind}: {result.Receipt.Error}");
            State = result.State;
            marks.Add((State.Revision, State.Events.Count));
        }

        public Recording Recording(string scenario, Side viewer)
        {
            var records = Records(viewer);
            var updates = new List<Update>();
            var shown = baseline;
            foreach (var (revision, events) in marks)
            {
                if (events > shown)
                    updates.Add(new(revision, Page(records.Where(r => r.Position > shown && r.Position <= events).ToList(), records, viewer)));
                shown = events;
            }
            var history = records.Where(r => r.Position <= baseline).ToList();
            return new(scenario, viewer, baselineRevision, Page(history, records, viewer), updates);
        }

        // As the store keeps them: a stable id per position, the order within a revision, the viewer's visibility.
        private List<PositionedEvent> Records(Side viewer)
        {
            var order = new Dictionary<long, int>();
            return State.Events.Select((item, index) => new { item, position = index + 1 })
                .Where(e => Visibility.CanRead(viewer, e.item.OnlyFor)).Select(e =>
                {
                    order[e.item.Revision] = order.GetValueOrDefault(e.item.Revision) + 1;
                    return new PositionedEvent(EventIdentity.For(State.Id, e.position, e.item), e.position, order[e.item.Revision], e.item);
                }).ToList();
        }

        private ActionLogPage Page(List<PositionedEvent> selected, List<PositionedEvent> all, Side viewer)
        {
            var ids = all.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
            var actions = selected.Select(r => r.ActionId).Distinct()
                .Select(id => ActionLogProjection.Action(State.Id, all.Where(r => r.ActionId == id), viewer, ActionLogFilter.All)?.Action)
                .Where(summary => summary is not null).Select(summary => summary!).ToList();
            return new ActionLogPage
            {
                MatchId = State.Id,
                Steps = selected.Select(r => ActionLogProjection.Step(r, ids)).ToList(),
                Actions = actions,
                LatestCursor = selected.Count > 0 ? selected[^1].Id : "",
                NextCursor = ""
            };
        }
    }

    private sealed record Update(long ViewRevision, ActionLogPage Page);

    private sealed record Recording(string Scenario, Side Viewer, long BaselineRevision, ActionLogPage Baseline, List<Update> Updates);
}
