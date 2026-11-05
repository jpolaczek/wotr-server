using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

// TEST-04: each expansion scenario offers its special die or action from the first decision, the named
// die pays for it and an example command validates and runs through the server's engine and projection.
// TEST-10 (restored from the expansion-test-scenarios branch): each names the module its game plays with.
public class ExpansionDiceScenarioTests
{
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);

    private delegate DieState? Payer(MatchState state, ActionOption option);
    private delegate CommandPayload Example(MatchState state, ActionOption option, DieState? die);

    private sealed record Expectation(CommandKind Kind, Payer Die, Example Payload, string Token = "");

    private static readonly IReadOnlyDictionary<string, Expectation> Expected = new Dictionary<string, Expectation>
    {
        ["lords-special-dice-choice"] = new(CommandKind.ChooseSpecialDie, Eligible, (_, _, die) => new() { DieId = die!.Id }),
        ["lords-forced-eye"] = new(CommandKind.ChooseSpecialDie, Eligible, (_, _, die) => new() { DieId = die!.Id }),
        ["lords-keeper-eye-choice"] = new(CommandKind.ChooseSpecialDie, OnlyEyes, (_, _, die) => new() { DieId = die!.Id }),
        ["lords-fp-narya-die"] = new(CommandKind.AdvancePolitics, Special(LordsDiceCatalog.Narya, DieFace.Muster),
            (_, option, die) => new() { DieId = die!.Id, NationId = option.Targets[0] }),
        ["lords-fp-nenya-die"] = new(CommandKind.MoveFellowship, Special(LordsDiceCatalog.Nenya, DieFace.Character),
            (_, _, die) => new() { DieId = die!.Id }),
        ["kings-sh-chieftain-entry"] = new(CommandKind.EnterCharacter, Standard(DieFace.Muster), Enter(KingsChieftains.Serpent)),
        ["kings-sh-ruler-die"] = new(CommandKind.EnterCharacter, Special(KingsDice.Shadow, DieFace.Event), Enter(KingsChieftains.Mirkwood)),
        ["fate-gimli-renown"] = new(CommandKind.UseCharacterAbility, Standard(DieFace.Event), Ability(FateGimli.Standard, FateGimli.Renown)),
        ["lords-fp-card-draw"] = new(CommandKind.DrawCard, Special(LordsDiceCatalog.Vilya, DieFace.CardDraw),
            (_, _, die) => new() { DieId = die!.Id, Deck = "character" }),
        ["lords-sh-use-balrog"] = new(CommandKind.UseCharacterAbility, Special(LordsDiceCatalog.Balrog, DieFace.UseBalrog),
            Ability(LordsCharacters.Balrog, LordsCharacterAbilities.ActivateBalrog)),
        ["lords-sh-gothmog-die"] = new(CommandKind.MoveArmies, Special(LordsDiceCatalog.Gothmog, DieFace.Army), Move),
        ["lords-sh-balrog-entry"] = new(CommandKind.EnterCharacter, Standard(DieFace.Muster), Enter(LordsCharacters.Balrog)),
        ["lords-sh-gothmog-muster"] = new(CommandKind.UseCharacterAbility, Standard(DieFace.Muster),
            Ability(LordsCharacters.Gothmog, LordsCharacterAbilities.GothmogRecruit)),
        ["lords-fp-narya"] = new(CommandKind.UseNarya, Eligible,
            (_, option, die) => new() { DieId = die!.Id, RingId = LordsDiceSetup.NaryaRing, NationId = option.Targets[0] }),
        ["lords-fp-vilya"] = new(CommandKind.UseVilya, Eligible, (_, _, die) => new() { DieId = die!.Id, RingId = LordsDiceSetup.VilyaRing }),
        ["lords-council-token"] = new(CommandKind.UseCouncilToken, (_, _) => null,
            (_, option, _) => new() { AbilityId = CouncilTokens.Politics, NationId = option.Targets[0] }),
        ["kings-fp-ruler-ability"] = new(CommandKind.UseCharacterAbility, Special(KingsDice.FreePeoples, DieFace.Muster), Mustering),
        ["kings-fp-ruler-token"] = new(CommandKind.DrawCard, Special(KingsDice.FreePeoples, DieFace.Muster),
            (_, _, die) => new() { DieId = die!.Id, RulerTokenId = "kings.token.denethor", Deck = "character" }, "kings.token.denethor"),
        ["kings-ruler-discard"] = new(CommandKind.DiscardRulerDie, Eligible, (_, _, die) => new() { DieId = die!.Id }),
        ["kings-fp-awaken"] = new(CommandKind.AwakenSovereign, Eligible,
            (_, option, die) => new() { DieId = die!.Id, CharacterId = option.EligibleIds[0], Route = [option.Targets[0]] }),
        ["kings-fp-remove-corrupted"] = new(CommandKind.RemoveCorruptedSovereign, Standard(DieFace.Character),
            (_, option, die) => new() { DieId = die!.Id, CharacterId = option.EligibleIds[0] }),
        ["kings-sh-corruption"] = new(CommandKind.AttemptCorruption, Eligible,
            (_, option, die) => new() { DieId = die!.Id, CharacterId = option.EligibleIds[0] })
    };

    private static IEnumerable<string> Ids => ExpansionDiceScenarios.All().Select(s => s.Id);

    public static TheoryData<string> Scenarios => new(Ids);

    [Fact]
    public void EveryExpansionScenarioHasAnExpectation() => Assert.Equal(Expected.Keys.Order(), Ids.Order());

    // UI-267: once the Shadow keeps a die, the Free Peoples' choice allows only Vilya's Eye, which their client sends.
    [Fact]
    public void TheForcedEyeScenarioLeavesTheFreePeoplesOnlyTheEye()
    {
        var state = TestScenarioCatalog.Find("lords-forced-eye")!.Create("forced-eye");
        var command = new GameCommand
        {
            MatchId = state.Id,
            CommandId = "shadow-keeps",
            ExpectedRevision = state.Revision,
            DecisionId = state.Pending!.Id,
            Kind = CommandKind.ChooseSpecialDie,
            Payload = new() { DieId = LordsDiceCatalog.Balrog }
        };
        var result = Engine.Execute(state, Side.Shadow, command, new SeededRandom(1));
        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        Assert.Equal((DecisionKind.SpecialDieChoice, Side.FreePeoples), (result.State.Pending!.Kind, result.State.Pending.Owner));
        Assert.Equal([LordsDiceCatalog.Vilya], LordsDiceRoll.LegalChoices(result.State, Side.FreePeoples));
        Assert.Equal([LordsDiceCatalog.Vilya], Views.For(result.State, Side.FreePeoples).Options.Single().EligibleDice);
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void TheScenarioNamesTheModuleItsGamePlaysWith(string id)
    {
        var scenario = TestScenarioCatalog.Find(id)!;
        Assert.Equal(ExpansionModules.Of(scenario.Create("modules-" + id).Configuration), scenario.Modules);
        Assert.Equal(scenario.Modules, scenario.View().Modules);
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void TheSpecialDieOrActionIsOfferedAndRuns(string id)
    {
        var scenario = TestScenarioCatalog.Find(id)!;
        var expected = Expected[id];
        var state = scenario.Create("expansion-" + id);
        Assert.Equal(scenario.StartingSide, state.Pending?.Owner);
        Assert.Equal(StateCodec.Write(state), StateCodec.Write(scenario.Create("expansion-" + id)));
        var view = Views.For(state, scenario.StartingSide);
        var option = expected.Token.Length > 0
            ? view.RulerTokenActions.FirstOrDefault(t => t.TokenId == expected.Token && t.Action.Kind == expected.Kind)?.Action
            : view.Options.FirstOrDefault(o => o.Kind == expected.Kind && o.Enabled);
        Assert.True(option is { Enabled: true }, $"{id}: {expected.Kind} is not offered.");
        var die = expected.Die(state, option!);
        if (die is not null)
            Assert.Contains(die.Id, Payers(option!));
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
        var result = Engine.Execute(state, scenario.StartingSide, command, new SeededRandom(11));
        Assert.True(result.Receipt.Accepted, $"{id}: {result.Receipt.Error}");
    }

    private static IEnumerable<string> Payers(ActionOption option) => option.EligibleDice
        .Concat(option.MovementCosts.Select(c => c.DieId))
        .Concat(option.CharacterActions.SelectMany(a => a.Dice));

    private static DieState? Eligible(MatchState state, ActionOption option) =>
        option.EligibleDice.Count > 0 ? state.Dice.Single(d => d.Id == option.EligibleDice[0]) : null;

    // An Eye among the special dice forces the choice of an Eye (LORDS p. 9-13).
    private static DieState? OnlyEyes(MatchState state, ActionOption option)
    {
        Assert.All(option.EligibleDice, id => Assert.Equal(DieFace.Eye, state.Dice.Single(d => d.Id == id).Face));
        return Eligible(state, option);
    }

    private static Payer Special(string dieId, DieFace face) => (state, _) =>
    {
        var die = state.Dice.Single(d => d.Id == dieId);
        Assert.Equal((DieLocation.Available, face), (die.Location, die.Face));
        return die;
    };

    private static Payer Standard(DieFace face) => (state, _) =>
        state.Dice.First(d => d.Location == DieLocation.Available && d.Source == "standard" && d.Face == face && d.Owner == state.Pending!.Owner);

    private static Example Ability(string character, string ability) => (state, _, die) =>
    {
        var figure = state.Characters.Single(c => c.DefinitionId == character);
        return new() { DieId = die!.Id, CharacterId = figure.Id, AbilityId = ability, Ids = [figure.Location] };
    };

    private static Example Enter(string character) => (state, _, die) =>
    {
        var plan = CharacterEntry.Candidates(state, die!.Owner).First(p => p.DefinitionId == character);
        return new() { DieId = die.Id, CharacterId = character, RegionId = plan.Region };
    };

    // Mustering of the Mark: Rohan Regulars in two different free settlements and one in Théoden's region.
    private static CommandPayload Mustering(MatchState state, ActionOption option, DieState? die)
    {
        var action = option.CharacterActions.Single(a => a.CharacterId == SovereignCatalog.Theoden);
        var recruitment = action.RulerRecruitment!;
        var regions = recruitment.Settlements.Take(recruitment.SettlementCount).Append(recruitment.LocalRegion).Take(action.TargetCount);
        var regulars = state.Units.Where(u => u.Location == "reserve" && u.Nation == "rohan" && u.Kind == UnitKind.Regular);
        return new()
        {
            DieId = die!.Id, CharacterId = action.CharacterId, AbilityId = action.AbilityId,
            Placements = regions.Zip(regulars, (region, unit) => new UnitPlacementChoice { UnitId = unit.Id, RegionId = region }).ToList()
        };
    }

    private static CommandPayload Move(MatchState state, ActionOption option, DieState? die)
    {
        var cost = option.MovementCosts.First(c => c.DieId == die!.Id);
        var army = option.Armies[0];
        var target = army.Targets[0];
        return new()
        {
            DieId = die!.Id,
            MovementCost = cost.Face,
            Moves = [new() { From = army.RegionId, To = target.RegionId, UnitIds = army.UnitIds.Where(target.EligibleIds.Contains).ToList() }]
        };
    }
}
