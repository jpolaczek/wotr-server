using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

// TEST-15: each layout stress scenario stands in the tab of its first module (Kings first when it plays with Kings),
// names exactly the modules its game uses, builds the same state every time and waits on its side's decision; and the
// state holds what makes it a stress case — every character version, the Chieftains' chips, a response at each Hunt
// window, the Council token's many figures, the White Rider, every pool resource, a full Effects panel and the Nazgûl
// Strike's choices (board objects in the client since UI-430).
public class LayoutStressScenarioTests
{
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);

    public static TheoryData<string> Ids => new(LayoutStressScenarios.All().Select(s => s.Id));

    [Theory]
    [MemberData(nameof(Ids))]
    public void TheScenarioStandsInItsModuleTabAndRepeats(string id)
    {
        var scenario = TestScenarioCatalog.Find(id)!;
        Assert.Single(TestScenarioCatalog.All, s => s.Id == id);
        Assert.StartsWith("layout-", id);
        Assert.True(scenario.Description.Length > 0 && scenario.Title.Length > 0);
        var kings = scenario.Modules.Contains(ExpansionModules.Kings);
        Assert.Equal(kings ? ExpansionModules.Kings : ExpansionModules.Lords, scenario.Modules[0]);
        Assert.Equal(kings ? LayoutStressScenarios.KingsGroup : LayoutStressScenarios.LordsGroup, scenario.Group);
        Assert.Equal(scenario.Modules, scenario.View().Modules);
        var state = scenario.Create("layout-" + id);
        Assert.Equal(ExpansionModules.Of(state.Configuration).Order(), scenario.Modules.Order());
        Assert.Equal(StateCodec.Write(state), StateCodec.Write(scenario.Create("layout-" + id)));
        Assert.Equal(scenario.StartingSide, state.Pending?.Owner);
        Assert.Contains(Views.For(state, scenario.StartingSide).Options, o => o.Enabled);
    }

    [Fact]
    public void TheFullRosterHasEveryAlternativeVersionWithFateLordsAndKings()
    {
        var state = Create("layout-full-roster");
        Assert.True(state.Configuration is { Fate: true, Lords: true, Council: true, Kings: true });
        Assert.Equal(LordsCharacters.Gandalf, CharacterRules.GuideDefinition(state));
        Assert.Equal([CouncilCompanions.Strider, CouncilCompanions.Merry, CouncilCompanions.Pippin],
            state.Fellowship.Companions.Where(id => id != state.Fellowship.Guide).Select(id => Definition(state, id)));
        AssertAt(state, "lords.character.boromir", "minas-tirith");
        AssertAt(state, FateGimli.Alternate, "iron-hills");
        AssertAt(state, "lords.character.legolas", "woodland-realm");
        AssertShadowRoster(state);
        Assert.Equal([SovereignStatus.Awakened, SovereignStatus.Corrupted, SovereignStatus.Initial],
            Statuses(state, SovereignCatalog.Denethor, SovereignCatalog.Theoden, SovereignCatalog.Thranduil));
        Assert.Equal(3, state.Kings!.Sovereigns.Count);
        Assert.Equal(2, Sovereign(state, SovereignCatalog.Thranduil).Tiles.Count);
        var view = Views.For(state, Side.FreePeoples);
        Assert.Contains(view.RulerTokenActions, t => t.TokenId == KingsRulerTokens.Id(SovereignCatalog.Denethor));
        Assert.True(view.OwnCharacterRoster.Count(r => r.Lifecycle == CharacterRosterLifecycle.OnBoard) >= 6);
        Assert.Equal(CouncilTokens.All.Order(), state.Lords!.Council!.GrantedTokens.Order());
    }

    [Fact]
    public void TheFiveKingsRosterShowsEveryKingsState()
    {
        var state = Create("layout-full-roster-five-kings");
        Assert.False(state.Configuration.Fate);
        Assert.Equal(
            [SovereignStatus.Awakened, SovereignStatus.Initial, SovereignStatus.Awakened, SovereignStatus.Corrupted, SovereignStatus.Initial],
            Statuses(state, SovereignCatalog.Brand, SovereignCatalog.Dain, SovereignCatalog.Denethor, SovereignCatalog.Theoden,
                SovereignCatalog.Thranduil));
        Assert.Equal(2, Sovereign(state, SovereignCatalog.Dain).Tiles.Count);
        AssertAt(state, "lords.character.gimli", "erebor");
        AssertShadowRoster(state);
        var tokens = Views.For(state, Side.FreePeoples).RulerTokenActions.Select(t => t.TokenId).Distinct().Order();
        Assert.Equal(new[] { SovereignCatalog.Brand, SovereignCatalog.Denethor }.Select(KingsRulerTokens.Id).Order(), tokens);
    }

    [Fact]
    public void ThreeChieftainsOfferTheirReplacementsAndBrandFollowsTheReveal()
    {
        var state = Create("layout-chieftains-battle");
        Assert.Equal(("CombatReplacement", Side.Shadow), (state.Pending!.Presentation, state.Pending.Owner));
        Assert.Equal([.. KingsChieftains.Ids, "keep"], Option(state, Side.Shadow, CommandKind.ChooseEffect).EligibleIds);
        var battle = BattleRuntime.Active(state)!;
        Assert.Contains(SovereignCatalog.Brand, BattleRules.Characters(state, battle.Force(Side.FreePeoples)).Select(c => c.Id));
        state = Run(state, Side.Shadow, CommandKind.ChooseEffect, new() { Ids = [KingsChieftains.Mirkwood] });
        Assert.Equal(("CombatReplacement", Side.FreePeoples), (state.Pending!.Presentation, state.Pending.Owner));
    }

    [Fact]
    public void TheHuntMeetsAResponseAtEveryWindow()
    {
        var state = Create("layout-hunt-responses");
        Assert.Equal(5, state.Fellowship.Companions.Count);
        Assert.Equal(CouncilCompanions.Merry, CharacterRules.GuideDefinition(state));
        Assert.Equal(3, state.Player(Side.FreePeoples).Table.Count);
        AssertResponses(state, "hunt.replace", FreePeoplesCharacterCards01.ReplaceReaction, NenyaReaction.Id);
        state = Run(state, Side.FreePeoples, CommandKind.DeclineReaction, new());
        AssertResponses(state, "hunt.table", FreePeoplesCharacterCards01.ProtectReaction, FreePeoplesCharacterCards02.HornReaction);
        state = Run(state, Side.FreePeoples, CommandKind.DeclineReaction, new());
        AssertResponses(state, "hunt.guide", CouncilGuideReactions.Cancel + "." + CouncilCompanions.Merry);
        state = Run(state, Side.FreePeoples, CommandKind.DeclineReaction, new());
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        Assert.Equal(3, Option(state, Side.FreePeoples, CommandKind.ResolveHuntDamage).HuntResponses.Count);
    }

    [Fact]
    public void TheCouncilTokenMovesEveryNazgulAndMinion()
    {
        var state = Create("layout-council-travel");
        var travel = Views.For(state, Side.Shadow).Options
            .Single(o => o.Kind == CommandKind.UseCouncilToken && o.EligibleIds.Contains(CouncilTokens.Travel)).Travel;
        Assert.Equal(8, travel.Count(t => state.Units.Any(u => u.Id == t.Id && u.Kind == UnitKind.Nazgul)));
        Assert.Equal(5, state.Units.Where(u => u.Kind == UnitKind.Nazgul).Select(u => u.Location).Distinct().Count());
        string[] minions = [CharacterRules.WitchKing, LordsCharacters.Mouth, LordsCharacters.Gothmog, LordsCharacters.Balrog];
        Assert.Equal(minions.Order(), travel.Select(t => state.Characters.Find(c => c.Id == t.Id)?.DefinitionId).OfType<string>().Order());
        var king = state.Characters.Single(CharacterRules.IsWitchKing);
        state = Run(state, Side.Shadow, CommandKind.UseCouncilToken, new()
        {
            AbilityId = CouncilTokens.Travel, CompanionMoves = [new() { Ids = [king.Id], Route = [king.Location, "dol-guldur"] }]
        });
        Assert.Equal("dol-guldur", state.Characters.Single(CharacterRules.IsWitchKing).Location);
        Assert.Contains(CouncilTokens.Travel, state.Lords!.Council!.SpentTokens);
    }

    [Fact]
    public void TheWhiteRiderWaitsBeforeAnyCombatCard()
    {
        var state = Create("layout-white-rider");
        AssertResponses(state, "battle.start", "character.white-rider");
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(new[] { CharacterRules.White, "kings.character.theoden.awakened" }.Order(),
            BattleRules.Characters(state, battle.Force(Side.FreePeoples)).Select(c => c.DefinitionId).Order());
        Assert.Equal(new[] { CharacterRules.WitchKing, KingsChieftains.Ugluk }.Order(),
            BattleRules.Characters(state, battle.Force(Side.Shadow)).Select(c => c.DefinitionId).Order());
        Assert.Equal(2, BattleRules.Units(state, battle.Force(Side.Shadow)).Count(u => u.Kind == UnitKind.Nazgul));
        state = Run(state, Side.FreePeoples, CommandKind.RespondEffect, new() { ReactionId = "character.white-rider" });
        Assert.True(BattleRuntime.Active(state)!.WhiteRiderUsed);
    }

    [Fact]
    public void EveryPoolResourceIsInPlayAtOnce()
    {
        var state = Create("layout-all-resources");
        var view = Views.For(state, Side.FreePeoples);
        foreach (var die in new[] { LordsDiceCatalog.Narya, LordsDiceCatalog.Gothmog, KingsDice.FreePeoples, KingsDice.Shadow })
            Assert.Equal(DieLocation.Available, view.Dice.Single(d => d.Id == die).Location);
        Assert.Equal(2, view.Dice.Count(d => d.Location == DieLocation.Discarded));
        Assert.Equal(3, view.RulerTokens.Count(t => t.Location == "free-peoples"));
        Assert.Equal(3, view.RulerTokenActions.Select(t => t.TokenId).Distinct().Count());
        Assert.Equal(2, view.RulerTokens.Count(t => t.Location == KingsDismay.Location));
        Assert.Equal(CouncilTokens.All.Order(), view.CouncilTokens.Where(t => !t.Spent).Select(t => t.Id).Order());
        Assert.Equal([Side.FreePeoples, Side.Shadow, Side.FreePeoples],
            new[] { LordsDiceSetup.NaryaRing, LordsDiceSetup.NenyaRing, LordsDiceSetup.VilyaRing }
                .Select(ring => view.Rings.Single(r => r.Id == ring).Owner));
    }

    [Fact]
    public void TheEffectsPanelListsEveryTableCardAndCorruptedKing()
    {
        var state = Create("layout-effects-sources");
        Assert.Equal((6, 8), (state.Player(Side.FreePeoples).Table.Count, state.Player(Side.Shadow).Table.Count));
        var effects = Views.For(state, Side.FreePeoples).Effects;
        Assert.True(effects.Count >= 20, $"{effects.Count} effects");
        Assert.All(state.Players.SelectMany(p => p.Table), card => Assert.Contains(effects, e => e.SourceId == card));
        Assert.Contains(effects, e => e.SourceId == FreePeoplesCharacterCards01.Mithril &&
            !e.Id.StartsWith("table:", StringComparison.Ordinal));
        Assert.Equal(4, effects.Count(e => e.SourceKind == "Ability" && e.Owner == Side.Shadow));
    }

    [Fact]
    public void TheNazgulStrikeOffersItsChoicesAfterThePlayedCard()
    {
        var state = Create("layout-console-options");
        Assert.Equal("NazgulStrike", state.Pending!.Presentation);
        var table = state.Player(Side.FreePeoples).Table;
        Assert.Equal(4, table.Count);
        Assert.Equal([.. table, NazgulStrikeStep.Hunt, NazgulStrikeStep.Decline],
            Option(state, Side.Shadow, CommandKind.ChooseEffect).EligibleIds);
        Assert.Equal([ShadowCharacterCards02.Strike], Views.For(state, Side.Shadow).EffectTrail.Select(t => t.SourceId));
        Assert.Equal(3, state.Units.Count(u => u.Kind == UnitKind.Nazgul && u.Location == "hollin"));
        Assert.Equal("hollin", state.Characters.Single(CharacterRules.IsWitchKing).Location);
    }

    // The Chief of the Ringwraiths, the Black Númenórean, Gothmog, the Balrog, Saruman and the three Chieftains in play.
    private static void AssertShadowRoster(MatchState state)
    {
        string[] expected = [LordsCharacters.WitchKing, LordsCharacters.Mouth, LordsCharacters.Gothmog, LordsCharacters.Balrog,
            CharacterRules.Saruman, .. KingsChieftains.Ids];
        var shadow = state.Characters.Where(c => c.Side == Side.Shadow && CharacterRules.OnBoard(c)).Select(c => c.DefinitionId);
        Assert.Equal(expected.Order(), shadow.Order());
        foreach (var keeper in new[] { LordsCharacters.Elrond, LordsCharacters.Galadriel })
            Assert.True(CharacterRules.InPlay(state, keeper) is not null, keeper);
    }

    private static void AssertAt(MatchState state, string definition, string region) =>
        Assert.Equal(region, state.Characters.Single(c => c.DefinitionId == definition).Location);

    // The Free Peoples' reaction window and the responses it offers at once.
    private static void AssertResponses(MatchState state, string window, params string[] responses)
    {
        var pending = state.Pending!;
        Assert.Equal((DecisionKind.Reaction, Side.FreePeoples, window), (pending.Kind, pending.Owner, pending.Window));
        Assert.Equal(responses, Option(state, Side.FreePeoples, CommandKind.RespondEffect).EligibleIds);
    }

    private static string Definition(MatchState state, string figure) => CharacterCatalog.Companion(state, figure).DefinitionId;

    private static SovereignState Sovereign(MatchState state, string id) => state.Kings!.Sovereigns.Single(s => s.Id == id);

    private static IEnumerable<SovereignStatus> Statuses(MatchState state, params string[] sovereigns) =>
        sovereigns.Select(id => Sovereign(state, id).Status);

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
