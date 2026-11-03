using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

// TEST-16: each base-game layout scenario of the owner's audit stands in the first tab's Layout stress group without a
// module, builds the same state every time and waits on its side's decision; and the state holds what the owner checks —
// Scouts' withdrawal into Helm's Deep beside its retreat, Dreadful Spells' field force and garrison in one region, the
// dense siege of Helm's Deep, nine characters in one region, the Voice of Saruman paid by its one Muster die and Fateful
// Strike's Minion choice with no Nazgûl in the battle.
public class BaseLayoutScenarioTests
{
    private const string HelmsDeep = "helms-deep";
    private const string Lorien = "lorien";
    private const string MinasTirith = "minas-tirith";

    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);

    public static TheoryData<string> Ids => new(BaseLayoutScenarios.All().Select(s => s.Id));

    [Theory]
    [MemberData(nameof(Ids))]
    public void TheScenarioStandsInTheFirstTabAndRepeats(string id)
    {
        var scenario = TestScenarioCatalog.Find(id)!;
        Assert.Single(TestScenarioCatalog.All, s => s.Id == id);
        Assert.StartsWith("layout-", id);
        Assert.True(scenario.Description.Length > 0 && scenario.Title.Length > 0);
        Assert.Equal(BaseLayoutScenarios.Group, scenario.Group);
        Assert.Empty(scenario.Modules);
        var state = scenario.Create("layout-" + id);
        Assert.Empty(ExpansionModules.Of(state.Configuration));
        Assert.Equal(StateCodec.Write(state), StateCodec.Write(scenario.Create("layout-" + id)));
        Assert.Equal(scenario.StartingSide, state.Pending?.Owner);
        Assert.Contains(Views.For(state, scenario.StartingSide).Options, o => o.Enabled);
    }

    [Fact]
    public void ScoutsWithdrawsIntoHelmsDeepOrRetreatsToTheFords()
    {
        var state = Create("layout-scouts-helms-deep");
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal((HelmsDeep, BattleMode.Field, Side.FreePeoples), (battle.Region, battle.Mode, battle.Defender.Side));
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        const string card = FreePeoplesStrategyCards01.SpiritOfMordor;
        Assert.Contains(card, Option(state, Side.FreePeoples, CommandKind.CommitCombatCard).EligibleIds);
        state = Run(state, Side.FreePeoples, CommandKind.CommitCombatCard, new() { CardId = card });
        Assert.Equal(("RegionOrShelter", Side.FreePeoples), (state.Pending!.Presentation, state.Pending.Owner));
        Assert.Equal(["fords-of-isen", ScoutsStep.Shelter], Option(state, Side.FreePeoples, CommandKind.ChooseEffect).EligibleIds);
        state = Run(state, Side.FreePeoples, CommandKind.ChooseEffect, new() { Ids = [ScoutsStep.Shelter] });
        Assert.All(FreePeoplesUnits(state, HelmsDeep), unit => Assert.True(unit.Garrison));
    }

    [Fact]
    public void DreadfulSpellsCastsFromTheBesiegersAtTheGarrisonOrTheFieldArmyNextDoor()
    {
        var state = Create("layout-dreadful-spells-siege");
        Assert.True(BoardRules.Besieged(state, Lorien, Side.FreePeoples));
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available && d.Face == DieFace.Character);
        Assert.Equal(["lorien:field"], DreadfulSpellsRules.Sources(state).Select(DreadfulSpellsRules.Key));
        state = Run(state, Side.Shadow, CommandKind.PlayEvent,
            new() { CardId = ShadowCharacterCards04.Spells, DieId = die.Id, CardCost = DieFace.Character });
        var targets = Choices(state, Side.Shadow);
        Assert.Equal(new[] { "lorien:garrison", "parth-celebrant:field" }, targets.Select(c => c.Id).Order());
        Assert.Contains(targets, c => c.Id == "lorien:garrison" && c.TargetId == Lorien && c.Title == "Target · Garrison");
        state = Run(state, Side.Shadow, CommandKind.ChooseEffect, new() { Ids = ["lorien:garrison"] });
        Assert.Contains(state.Events, e => e.Kind == "DreadfulSpells" &&
            e.Text.StartsWith("Dreadful Spells in lorien:", StringComparison.Ordinal));
    }

    [Fact]
    public void HelmsDeepIsBesiegedOnTheDenseBoard()
    {
        var state = Create("layout-dense-helms-deep");
        Assert.True(BoardRules.Besieged(state, HelmsDeep, Side.FreePeoples));
        var garrison = FreePeoplesUnits(state, HelmsDeep).ToList();
        Assert.All(garrison, unit => Assert.True(unit.Garrison));
        Assert.Equal((5, 2), (garrison.Count(BoardRules.IsArmyUnit), garrison.Count(u => u.Kind == UnitKind.Leader)));
        Assert.Equal(3, state.Characters.Count(c => c.Location == HelmsDeep && c.Garrison));
        var besiegers = state.Units.Where(u => u.Location == HelmsDeep && BoardRules.Owner(u) == Side.Shadow).ToList();
        Assert.Equal((10, 3), (besiegers.Count(BoardRules.IsArmyUnit), besiegers.Count(u => u.Kind == UnitKind.Nazgul)));
        Assert.Equal(12, state.Units.Count(u => u.Location == "westemnet" && u.Nation == "gondor"));
        var relief = Option(state, Side.FreePeoples, CommandKind.AttackArmy).Armies.Single(a => a.RegionId == "westemnet");
        Assert.Contains(relief.Targets, t => t.RegionId == HelmsDeep && t.BattleMode == BattleMode.Relief);
    }

    [Fact]
    public void NineCharactersHoldMinasTirithAndItsSiege()
    {
        var state = Create("layout-crowded-region");
        var characters = state.Characters.Where(c => c.Location == MinasTirith).ToList();
        Assert.Equal(9, characters.Count);
        Assert.Equal(7, characters.Count(c => c.Side == Side.FreePeoples && c.Garrison));
        Assert.Equal(new[] { CharacterRules.Mouth, CharacterRules.WitchKing }, characters.Where(c => c.Side == Side.Shadow)
            .Select(c => c.DefinitionId).Order());
        Assert.Empty(state.Fellowship.Companions);
        Assert.Equal(CharacterRules.Gollum, state.Fellowship.Guide);
        var siege = Option(state, Side.Shadow, CommandKind.AttackArmy).Armies.Single(a => a.RegionId == MinasTirith && !a.Garrison);
        Assert.Equal(2, siege.CharacterIds.Count);
        Assert.Contains(siege.Targets, t => t.RegionId == MinasTirith && t.BattleMode == BattleMode.Siege);
    }

    [Fact]
    public void TheVoiceOfSarumanIsPaidByTheOneMusterDie()
    {
        var state = Create("layout-saruman-voice-one-muster");
        var muster = state.Dice.Where(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available &&
            DiceRules.CanPay(d, DieFace.Muster)).Select(d => d.Id).ToList();
        Assert.Single(muster);
        var voice = Option(state, Side.Shadow, CommandKind.UseCharacterAbility).CharacterActions
            .Where(a => a.CharacterId == CharacterRules.Saruman).ToList();
        Assert.Equal(new[] { CharacterAbilities.VoiceRecruit, CharacterAbilities.VoiceUpgrade }, voice.Select(a => a.AbilityId));
        Assert.All(voice, action => Assert.Equal(muster, action.Dice));
        var recruit = voice[0];
        state = Run(state, Side.Shadow, CommandKind.UseCharacterAbility, new()
        {
            CharacterId = CharacterRules.Saruman, AbilityId = recruit.AbilityId, DieId = muster[0], Ids = [.. recruit.Targets]
        });
        Assert.DoesNotContain(state.Dice, d => d.Owner == Side.Shadow && d.Location == DieLocation.Available && d.Face == DieFace.Muster);
    }

    [Fact]
    public void FatefulStrikeOffersTheMinionOrTheSkipWithoutNazgul()
    {
        var state = Create("layout-fateful-strike-minion");
        var battle = BattleRuntime.Active(state)!;
        Assert.DoesNotContain(BattleRules.Units(state, battle.Force(Side.Shadow)), u => u.Kind == UnitKind.Nazgul);
        Assert.True(battle.Force(Side.FreePeoples).Round.Dice.Count(d => d.RerollHit) >= 2);
        Assert.Equal([CharacterRules.Saruman, FatefulStrikeStep.Decline], Option(state, Side.FreePeoples, CommandKind.ChooseEffect).EligibleIds);
        var skipped = Run(state, Side.FreePeoples, CommandKind.ChooseEffect, new() { Ids = [FatefulStrikeStep.Decline] });
        Assert.Equal("orthanc", skipped.Characters.Single(c => c.DefinitionId == CharacterRules.Saruman).Location);
        var struck = Run(state, Side.FreePeoples, CommandKind.ChooseEffect, new() { Ids = [CharacterRules.Saruman] });
        Assert.False(CharacterRules.OnBoard(struck.Characters.Single(c => c.DefinitionId == CharacterRules.Saruman)));
    }

    private static IEnumerable<UnitState> FreePeoplesUnits(MatchState state, string region) =>
        state.Units.Where(u => u.Location == region && BoardRules.Owner(u) == Side.FreePeoples);

    private static List<EffectChoiceView> Choices(MatchState state, Side side)
    {
        Assert.Equal((DecisionKind.EffectChoice, side), (state.Pending!.Kind, state.Pending.Owner));
        return Option(state, side, CommandKind.ChooseEffect).EffectChoices;
    }

    private static MatchState Create(string id)
    {
        var scenario = TestScenarioCatalog.Find(id)!;
        var state = scenario.Create("layout-" + id);
        Assert.Equal(scenario.StartingSide, state.Pending?.Owner);
        return state;
    }

    private static ActionOption Option(MatchState state, Side side, CommandKind kind)
    {
        var option = Views.For(state, side).Options.FirstOrDefault(o => o.Kind == kind && o.Enabled);
        Assert.True(option is not null, $"{kind} is not offered to {side}.");
        return option!;
    }

    private static MatchState Run(MatchState state, Side side, CommandKind kind, CommandPayload payload)
    {
        var command = new GameCommand
        {
            MatchId = state.Id,
            CommandId = "layout-" + kind + "-" + state.Revision,
            ExpectedRevision = state.Revision,
            DecisionId = state.Pending!.Id,
            Kind = kind,
            Payload = payload
        };
        var error = Engine.Validate(state, side, command);
        Assert.True(error is null, $"{kind}: {error}");
        var result = Engine.Execute(state, side, command, new SeededRandom(1));
        Assert.True(result.Receipt.Accepted, $"{kind}: {result.Receipt.Error}");
        return result.State;
    }
}
