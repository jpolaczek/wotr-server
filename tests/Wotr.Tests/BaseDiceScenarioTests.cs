using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

// TEST-02: each base-game dice scenario offers its tested action from the first decision, the die with the
// tested result can pay for it, and one example command of that action passes validation and execution
// through the same engine and projection the server uses.
public class BaseDiceScenarioTests
{
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);

    private delegate CommandPayload Example(MatchState state, ActionOption option, DieState? die);

    private sealed record Expectation(CommandKind Kind, DieFace? Face, Example Payload);

    private static readonly IReadOnlyDictionary<string, Expectation> Expected = new Dictionary<string, Expectation>
    {
        ["fp-character-move-fellowship"] = new(CommandKind.MoveFellowship, DieFace.Character, (_, _, die) => new() { DieId = die!.Id }),
        ["fp-character-hide-fellowship"] = new(CommandKind.HideFellowship, DieFace.Character, (_, _, die) => new() { DieId = die!.Id }),
        ["fp-character-separate-companions"] = new(CommandKind.SeparateCompanions, DieFace.Character, Separate),
        ["fp-character-move-companions"] = new(CommandKind.MoveCompanions, DieFace.Character, MoveCharacters),
        ["fp-character-lead-army"] = new(CommandKind.AttackArmy, DieFace.Character, Attack),
        ["fp-character-event"] = new(CommandKind.PlayEvent, DieFace.Character, Play(FreePeoplesDiceScenarios.BilbosSong)),
        ["fp-army-move"] = new(CommandKind.MoveArmies, DieFace.Army, Move),
        ["fp-army-attack"] = new(CommandKind.AttackArmy, DieFace.Army, Attack),
        ["fp-army-event"] = new(CommandKind.PlayEvent, DieFace.Army, Play(FreePeoplesDiceScenarios.FaramirsRangers)),
        ["fp-muster-politics"] = new(CommandKind.AdvancePolitics, DieFace.Muster, Politics),
        ["fp-muster-recruit"] = new(CommandKind.Recruit, DieFace.Muster, Recruit),
        ["fp-muster-event"] = new(CommandKind.PlayEvent, DieFace.Muster, Play(FreePeoplesDiceScenarios.WisdomOfElrond)),
        ["fp-army-muster"] = new(CommandKind.AttackArmy, DieFace.ArmyMuster, Attack),
        ["fp-event-draw"] = new(CommandKind.DrawCard, DieFace.Event, (_, _, die) => new() { DieId = die!.Id, Deck = "character" }),
        ["fp-event-play"] = new(CommandKind.PlayEvent, DieFace.Event, Play(FreePeoplesDiceScenarios.FaramirsRangers)),
        ["fp-will-any-action"] = new(CommandKind.Recruit, DieFace.WillOfTheWest, Recruit),
        ["fp-will-gandalf-white"] = new(CommandKind.EnterCharacter, DieFace.WillOfTheWest, Enter(CharacterRules.White)),
        ["fp-will-aragorn"] = new(CommandKind.EnterCharacter, DieFace.WillOfTheWest, Enter(CharacterRules.Aragorn)),
        ["fp-elven-ring"] = new(CommandKind.UseRing, DieFace.Muster, Ring(DieFace.Army)),
        ["fp-pass"] = new(CommandKind.Pass, null, (_, _, _) => new()),
        ["sh-character-move-minions"] = new(CommandKind.MoveShadowCharacters, DieFace.Character, MoveCharacters),
        ["sh-character-lead-army"] = new(CommandKind.AttackArmy, DieFace.Character, Attack),
        ["sh-character-event"] = new(CommandKind.PlayEvent, DieFace.Character, Play(ShadowDiceScenarios.RingwraithsAbroad)),
        ["sh-army-move"] = new(CommandKind.MoveArmies, DieFace.Army, Move),
        ["sh-army-attack"] = new(CommandKind.AttackArmy, DieFace.Army, Attack),
        ["sh-army-event"] = new(CommandKind.PlayEvent, DieFace.Army, Play(ShadowDiceScenarios.ShadowsGather)),
        ["sh-muster-politics"] = new(CommandKind.AdvancePolitics, DieFace.Muster, Politics),
        ["sh-muster-recruit"] = new(CommandKind.Recruit, DieFace.Muster, Recruit),
        ["sh-muster-saruman"] = new(CommandKind.EnterCharacter, DieFace.Muster, Enter(CharacterRules.Saruman)),
        ["sh-muster-witch-king"] = new(CommandKind.EnterCharacter, DieFace.Muster, Enter(CharacterRules.WitchKing)),
        ["sh-muster-mouth"] = new(CommandKind.EnterCharacter, DieFace.Muster, Enter(CharacterRules.Mouth)),
        ["sh-muster-event"] = new(CommandKind.PlayEvent, DieFace.Muster, Play(ShadowDiceScenarios.OrcsMultiplying)),
        ["sh-army-muster"] = new(CommandKind.AttackArmy, DieFace.ArmyMuster, Attack),
        ["sh-event-draw"] = new(CommandKind.DrawCard, DieFace.Event, (_, _, die) => new() { DieId = die!.Id, Deck = "strategy" }),
        ["sh-event-play"] = new(CommandKind.PlayEvent, DieFace.Event, Play(ShadowDiceScenarios.ShadowsGather)),
        ["sh-eye-hunt-allocation"] = new(CommandKind.AllocateHunt, null, Allocate),
        ["sh-elven-ring"] = new(CommandKind.UseRing, DieFace.Muster, Ring(DieFace.Eye)),
        ["fp-mordor-enter"] = new(CommandKind.EnterMordor, null, (_, _, _) => new()),
        ["fp-mordor-track"] = new(CommandKind.MoveFellowship, DieFace.Character, (_, _, die) => new() { DieId = die!.Id }),
        ["fp-mordor-crack"] = new(CommandKind.MoveFellowship, DieFace.Character, (_, _, die) => new() { DieId = die!.Id })
    };

    private static IEnumerable<string> DiceIds => FreePeoplesDiceScenarios.All().Concat(ShadowDiceScenarios.All())
        .Concat(MordorTrackScenarios.All()).Select(s => s.Id);

    public static TheoryData<string> DiceScenarios => new(DiceIds);

    [Fact]
    public void EveryDiceScenarioHasAnExpectation()
    {
        Assert.Equal(Expected.Keys.Order(), DiceIds.Order());
        foreach (var scenario in TestScenarioCatalog.All)
            Assert.True(scenario.Title.Length > 0 && scenario.Description.Length > 0 && scenario.Group.Length > 0, scenario.Id);
    }

    [Fact]
    public void TheCatalogDocumentListsEveryScenario()
    {
        var document = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../../docs/TEST_SCENARIOS.md"));
        foreach (var scenario in TestScenarioCatalog.All)
            Assert.Contains($"| `{scenario.Id}` |", document);
    }

    [Theory]
    [MemberData(nameof(DiceScenarios))]
    public void TheTestedActionIsOfferedPaidByTheDieAndRuns(string id)
    {
        var scenario = TestScenarioCatalog.Find(id)!;
        var expected = Expected[id];
        var state = scenario.Create("dice-" + id);
        Assert.Equal(scenario.StartingSide, state.Pending?.Owner);
        var view = Views.For(state, scenario.StartingSide);
        var option = view.Options.FirstOrDefault(o => o.Kind == expected.Kind);
        Assert.True(option is { Enabled: true }, $"{id}: {expected.Kind} {(option is null ? "missing" : option.Reason)}");
        var die = expected.Face is { } face
            ? state.Dice.First(d => d.Owner == scenario.StartingSide && d.Location == DieLocation.Available && d.Face == face)
            : null;
        if (die is not null)
            Assert.Contains(die.Id, Payers(option!, expected.Kind));
        var command = new GameCommand
        {
            MatchId = state.Id,
            CommandId = "example-" + id,
            ExpectedRevision = state.Revision,
            DecisionId = view.Decision!.Id,
            Kind = expected.Kind,
            Payload = expected.Payload(state, option!, die)
        };
        var error = Engine.Validate(state, scenario.StartingSide, command);
        Assert.True(error is null, $"{id}: {error} {System.Text.Json.JsonSerializer.Serialize(command.Payload)}");
        var result = Engine.Execute(state, scenario.StartingSide, command, new SeededRandom(7));
        Assert.True(result.Receipt.Accepted, $"{id}: {result.Receipt.Error}");
        Assert.True(result.State.Revision > state.Revision);
    }

    private static IEnumerable<string> Payers(ActionOption option, CommandKind kind) => kind switch
    {
        CommandKind.PlayEvent => option.Events.SelectMany(e => e.Costs).Select(c => c.DieId),
        CommandKind.MoveArmies or CommandKind.AttackArmy => option.MovementCosts.Select(c => c.DieId),
        CommandKind.EnterCharacter => option.EligibleDice.Count > 0 ? option.EligibleDice : option.CharacterActions.SelectMany(a => a.Dice),
        _ => option.EligibleDice
    };

    private static Example Play(string card) => (_, option, die) =>
    {
        var play = option.Events.Single(e => e.CardId == card);
        Assert.Contains(play.Costs, c => c.DieId == die!.Id);
        return new() { CardId = card, DieId = die!.Id, CardCost = die.Face, NationId = play.NationTargets.FirstOrDefault() ?? "" };
    };

    private static Example Enter(string character) => (state, _, die) =>
    {
        var plan = CharacterEntry.Candidates(state, die!.Owner).First(p => p.DefinitionId == character);
        return new() { DieId = die.Id, CharacterId = character, RegionId = plan.Region };
    };

    private static Example Ring(DieFace face) => (state, _, die) =>
        new() { DieId = die!.Id, RingId = state.Rings.First(r => r.Owner == die.Owner).Id, Face = face };

    private static CommandPayload Politics(MatchState state, ActionOption option, DieState? die) =>
        new() { DieId = die!.Id, NationId = option.Targets[0] };

    private static CommandPayload Recruit(MatchState state, ActionOption option, DieState? die)
    {
        var group = option.Recruitment[0];
        return new() { DieId = die!.Id, Placements = [new() { UnitId = group.ReserveIds[0], RegionId = group.Targets[0] }] };
    }

    private static CommandPayload Move(MatchState state, ActionOption option, DieState? die)
    {
        var army = option.Armies[0];
        var target = army.Targets[0];
        var cost = option.MovementCosts.First(c => c.DieId == die!.Id);
        return new()
        {
            DieId = die!.Id,
            MovementCost = cost.Face,
            Moves = [new() { From = army.RegionId, To = target.RegionId, UnitIds = army.UnitIds.Where(target.EligibleIds.Contains).ToList() }]
        };
    }

    // The army able to pay with this die attacks its first target with every unit and character.
    private static CommandPayload Attack(MatchState state, ActionOption option, DieState? die)
    {
        var cost = option.MovementCosts.First(c => c.DieId == die!.Id);
        var army = option.Armies.First(a => cost.Face != DieFace.Character || a.CommanderIds.Count > 0);
        return new()
        {
            DieId = die!.Id,
            MovementCost = cost.Face,
            Moves = [new() { From = army.RegionId, To = army.Targets[0].RegionId, UnitIds = army.UnitIds, CharacterIds = army.CharacterIds }]
        };
    }

    // Routes start in the region left. One companion leaves the Fellowship to a neighbouring region.
    private static CommandPayload Separate(MatchState state, ActionOption option, DieState? die)
    {
        var travel = option.Travel.First(t => Neighbour(t) is not null);
        return new() { DieId = die!.Id, Ids = [travel.Id], Route = [travel.Origin, Neighbour(travel)!] };
    }

    private static CommandPayload MoveCharacters(MatchState state, ActionOption option, DieState? die)
    {
        var travel = option.Travel.First(t => Neighbour(t) is not null);
        return new() { DieId = die!.Id, CompanionMoves = [new() { Ids = [travel.Id], Route = [travel.Origin, Neighbour(travel)!] }] };
    }

    private static CommandPayload Allocate(MatchState state, ActionOption option, DieState? die) =>
        new() { Ids = [state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Unrolled).Id] };

    private static string? Neighbour(TravelOption travel) =>
        BoardRules.Regions.TryGetValue(travel.Origin, out var origin) ? travel.Targets.FirstOrDefault(origin.Neighbors.Contains) : null;
}
