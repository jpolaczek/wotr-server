using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

// TEST-13: every Kings mechanic scenario stands in the Kings tab, offers its mechanic as a legal option from its first
// decision, and committing that option runs through the engine and visibly acts: the king awakens or is corrupted, the
// tile returns, the recruits arrive, the token is spent, the substitution is made. The steps after the first one are
// played to their result where the scenario promises it.
public class KingsMechanicScenarioTests
{
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);
    private static readonly DemoBot Bot = new(Engine, Views);

    private delegate CommandPayload Payload(MatchState state, PlayerView view);

    private sealed record Expectation(CommandKind Kind, Payload Payload, string Effect, Func<MatchState, MatchState, bool> Acted);

    private static readonly IReadOnlyDictionary<string, Expectation> Expected = new Dictionary<string, Expectation>
    {
        ["kings-fp-awaken-ruler-die"] = new(CommandKind.AwakenSovereign, Awakening(SovereignCatalog.Theoden),
            "Théoden awakens in Edoras and his tiles return",
            (_, s) => Status(s, SovereignCatalog.Theoden) == SovereignStatus.Awakened && Location(s, SovereignCatalog.Theoden) == "edoras" &&
                Sovereign(s, SovereignCatalog.Theoden).Tiles.Count == 0),
        ["kings-politics-cleanse"] = new(CommandKind.AdvancePolitics,
            (s, _) => new() { DieId = Die(s, Side.FreePeoples, DieFace.Muster), NationId = "gondor" },
            "the Free Peoples are offered one of Denethor's tiles back",
            (_, s) => s.Pending is { Presentation: "CorruptionRemoval", Owner: Side.FreePeoples }),
        ["kings-brand-call-to-arms"] = new(CommandKind.UseCharacterAbility, Ability(SovereignCatalog.Brand, false),
            "three North recruits arrive", (b, s) => Recruited(b, s, "north") == 3),
        ["kings-dain-iron-guard"] = new(CommandKind.UseCharacterAbility, Ability(SovereignCatalog.Dain, false),
            "Dwarven recruits arrive in Erebor and another settlement", (b, s) => Recruited(b, s, "dwarves") == 2),
        ["kings-sovereign-upgrades"] = new(CommandKind.UseCharacterAbility, Upgrade(SovereignCatalog.Dain),
            "an Elite replaces a Regular in Erebor",
            (b, s) => Gained(b, s, "erebor", UnitKind.Elite) == 1 && Gained(b, s, "erebor", UnitKind.Regular) == -1),
        ["kings-theoden-forth-eorlingas"] = new(CommandKind.CommitCombatCard, (_, _) => new() { CardId = "" },
            "Théoden leads with 3 in the first round", (b, _) => TheodenLeadership(b) == 3),
        ["kings-far-sighted-hunt"] = new(CommandKind.MoveFellowship,
            (s, _) => new() { DieId = Ruler(s), AbilityId = KingsFarSighted.Ability },
            "the Hunt rolls two dice for the Box's three", (_, s) => HuntDice(s) == 2),
        ["kings-corruption-three-tiles"] = new(CommandKind.AttemptCorruption, Attempt(SovereignCatalog.Dain),
            "the attempt draws up to three tiles", (_, s) => CorruptionRuntime.Current(s)?.DrawLimit == 3),
        ["kings-corruption-kings-tile"] = new(CommandKind.AttemptCorruption, Attempt(SovereignCatalog.Dain),
            "a Kings tile ends the draw on Dáin",
            (_, s) => CorruptionRuntime.Current(s) is null && Sovereign(s, SovereignCatalog.Dain).Tiles.Any(KingsTiles.IsKings)),
        ["kings-corruption-reaches-resistance"] = new(CommandKind.AttemptCorruption, Attempt(SovereignCatalog.Brand),
            "one tile is drawn for Brand", (_, s) => CorruptionRuntime.Current(s)?.DrawLimit == 1),
        ["kings-corrupted-recruitment"] = new(CommandKind.Recruit, Recruit("north", UnitKind.Regular),
            "a North Regular still comes", (b, s) => Recruited(b, s, "north") == 1),
        ["kings-theoden-white-hand"] = new(CommandKind.Recruit, Recruit("isengard", UnitKind.Regular, KingsBoard.HelmsDeep),
            "an Isengard Regular is recruited in Helm's Deep", (b, s) => Gained(b, s, KingsBoard.HelmsDeep, UnitKind.Regular) == 1),
        ["kings-siege-lonely-mountain"] = Siege(),
        ["kings-siege-white-tower"] = Siege(),
        ["kings-siege-mirkwood"] = Siege(),
        ["kings-ruler-token-brand"] = Token(SovereignCatalog.Brand),
        ["kings-ruler-token-dain"] = Token(SovereignCatalog.Dain),
        ["kings-ruler-token-theoden"] = Token(SovereignCatalog.Theoden),
        ["kings-ruler-token-thranduil"] = new(CommandKind.MoveFellowship, (s, _) => new()
            {
                DieId = Ruler(s), RulerTokenId = KingsRulerTokens.Id(SovereignCatalog.Thranduil), AbilityId = KingsFarSighted.Ability
            },
            "the token is spent and the Hunt rolls two dice for the Box's three",
            (_, s) => TokenAt(s, SovereignCatalog.Thranduil) == "removed" && HuntDice(s) == 2),
        ["kings-ruler-token-or-ring"] = new(CommandKind.UseRing, (s, _) => new()
            {
                DieId = Die(s, Side.FreePeoples, DieFace.Character), Face = DieFace.Event,
                RingId = s.Rings.First(r => r.Owner == Side.FreePeoples).Id
            },
            "a Ring used blocks the token this turn",
            (_, s) => KingsRulerTokenOptions.Actions(StateFor(s, Side.FreePeoples), Side.FreePeoples).Count == 0),
        ["kings-ruler-die-recovery"] = new(CommandKind.DrawCard, (s, _) => new() { DieId = Ruler(s), Deck = "strategy" },
            "the fallen king's Ruler die still pays this turn",
            (_, s) => KingsDice.Ruler(s, Side.FreePeoples)!.Location == DieLocation.Spent),
        ["kings-dismay-from-token"] = new(CommandKind.DrawCard, (s, _) => new()
            {
                DieId = Ruler(s), Deck = "strategy", RulerTokenId = KingsRulerTokens.Id(SovereignCatalog.Denethor)
            },
            "the spent token becomes the Shadow's Dismay token",
            (_, s) => KingsDismay.Tokens(s).Any(t => t.SovereignId == SovereignCatalog.Denethor)),
        ["kings-ugluk-entry"] = new(CommandKind.EnterCharacter, Entry(KingsChieftains.Ugluk, "hollin"),
            "Uglúk enters in the Fellowship's region", (_, s) => CharacterRules.InPlay(s, KingsChieftains.Ugluk)?.Location == "hollin"),
        ["kings-mirkwood-lord-of-the-bats"] = new(CommandKind.ChooseEffect, Choose(KingsChieftains.Mirkwood),
            "Lord of the Bats replaces the card at initiatives 0 and 4",
            (_, s) => Replacement(s) == KingsChieftains.Mirkwood && ShadowWork(s).Select(w => w.Initiative).SequenceEqual([0, 4])),
        ["kings-brand-vs-swarm"] = new(CommandKind.ChooseEffect, Choose(KingsChieftains.Mirkwood),
            "after the reveal Brand's replacement waits for the Free Peoples",
            (_, s) => s.Pending is { Presentation: "CombatReplacement", Owner: Side.FreePeoples }),
        ["kings-serpent-relentless-assault"] = new(CommandKind.ChooseEffect, Choose(KingsChieftains.Serpent),
            "Red Wrath first takes one hit on the Shadow's army",
            (_, s) => Replacement(s) == KingsChieftains.Serpent && s.Pending is { Kind: DecisionKind.Losses, Owner: Side.Shadow }),
        ["kings-ugluk-i-command"] = new(CommandKind.ChooseEffect, Choose(KingsChieftains.Ugluk),
            "I Command gives both armies +1", (_, s) => ShadowWork(s).Any(w => w.ProgramId == KingsChieftainCombat.CommandBoth)),
        ["kings-ugluk-desperate-battle"] = new(CommandKind.ChooseEffect, Choose(KingsChieftains.Ugluk),
            "with Desperate Battle only the Shadow gains +1",
            (_, s) => ShadowWork(s).Any(w => w.ProgramId == KingsChieftainCombat.CommandShadow)),
        ["kings-ugluk-hunt"] = new(CommandKind.MoveFellowship, (s, _) => new() { DieId = Die(s, Side.FreePeoples, DieFace.Character) },
            "the Hunt's re-rolls add Uglúk's +1", (b, s) => KingsChieftainHunt.RerollBonus(b) == 1 &&
                s.Resolution.Frames.Where(f => f.Hunt is not null).All(f => f.Hunt!.RerollBonus == 1)),
        ["kings-council-chieftain-entry"] = new(CommandKind.EnterCharacter, Council(KingsChieftains.Serpent),
            "the Black Serpent enters with the token", (_, s) => CharacterRules.InPlay(s, KingsChieftains.Serpent) is not null &&
                s.Lords!.Council!.SpentTokens.Contains(CouncilTokens.Politics)),
        ["kings-variant-visible-corruption"] = new(CommandKind.AttemptCorruption, Attempt(SovereignCatalog.Dain),
            "the attempt runs with Dáin's values public",
            (b, s) => PublicCorruption(b, SovereignCatalog.Dain) == 2 && CorruptionRuntime.Current(s) is not null),
        ["kings-variant-sequential-corruption"] = new(CommandKind.AttemptCorruption, Attempt(SovereignCatalog.Dain),
            "only the last tile drawn can be kept", (_, s) => CorruptionRuntime.Current(s) is { } attempt &&
                CorruptionRules.Choices(attempt).Count <= 1 && attempt.Stage != CorruptionStage.Choosing),
        ["kings-variant-removing-ruler-tokens"] = new(CommandKind.UseDismay, (s, _) => new()
            {
                DieId = KingsDice.Ruler(s, Side.FreePeoples)!.Id,
                Ids = [KingsRulerTokens.Id(KingsDismay.Tokens(s).First().SovereignId)]
            },
            "the Dismay token from the fallen king is spent",
            (b, s) => !b.Kings!.Tokens.Any(t => t.Location == "free-peoples") && !KingsDismay.Tokens(s).Any())
    };

    // The owner's Kings decisions (EXP-15) and the scenarios that let each be tried with its condition met.
    public static TheoryData<string, string> PlaytestDecisions => new()
    {
        { "D1", "kings-fp-awaken" }, { "D1", "kings-sh-corruption" }, { "D1", "kings-fp-remove-corrupted" },
        { "D2", "kings-sh-corruption" }, { "D3", "kings-fp-ruler-token" }, { "D3", "kings-dismay-from-token" },
        { "D4", "kings-dismay" }, { "D5", "kings-corruption-reaches-resistance" }, { "D7", "kings-card-wisdom" },
        { "D15", "kings-card-wisdom" }, { "D15", "kings-card-recruit-or-cleanse" }, { "D16", "kings-card-hope" },
        { "D16", "kings-card-power-waxes" }, { "D17", "kings-brand-heir" }, { "D17", "kings-brand-shadows" },
        { "D17", "kings-chieftain-substitution" }, { "D17", "kings-brand-vs-swarm" }, { "D18", "kings-far-sighted" },
        { "D19", "kings-fp-ruler-token" }, { "D20", "kings-dismay" }, { "Palantír", "kings-card-palantir" }
    };

    private static IEnumerable<TestScenario> Mechanics => KingsSovereignScenarios.All().Concat(KingsCorruptionScenarios.All())
        .Concat(KingsRulerScenarios.All()).Concat(KingsChieftainScenarios.All()).Concat(KingsVariantScenarios.All());

    public static TheoryData<string> Scenarios => new(Mechanics.Select(s => s.Id));

    public static TheoryData<string> KingsScenarios =>
        new(TestScenarioCatalog.All.Where(s => s.Id.StartsWith("kings-", StringComparison.Ordinal)).Select(s => s.Id));

    [Fact]
    public void EveryMechanicScenarioHasAnExpectation() => Assert.Equal(Expected.Keys.Order(), Mechanics.Select(s => s.Id).Order());

    [Fact]
    public void EveryKingsScenarioHasAUniqueIdAndNoCardPrefix()
    {
        var ids = TestScenarioCatalog.All.Select(s => s.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.All(KingsCardScenarios.All().Concat(Mechanics), s => Assert.StartsWith("kings-", s.Id));
    }

    [Theory]
    [MemberData(nameof(KingsScenarios))]
    public void TheScenarioStandsInTheKingsTabWithTheModulesItsGameUses(string id)
    {
        var scenario = TestScenarioCatalog.Find(id)!;
        Assert.Equal(ExpansionModules.Kings, scenario.Modules[0]);
        Assert.StartsWith("Kings · ", scenario.Group);
        var state = scenario.Create("kings-tab-" + id);
        Assert.Equal(ExpansionModules.Of(state.Configuration).Order(), scenario.Modules.Order());
        Assert.Equal(scenario.StartingSide, state.Pending?.Owner);
        Assert.Contains(Views.For(state, scenario.StartingSide).Options, o => o.Enabled);
    }

    [Theory]
    [MemberData(nameof(PlaytestDecisions))]
    public void EveryKingsPlaytestDecisionHasAScenario(string decision, string id)
    {
        var scenario = TestScenarioCatalog.Find(id);
        Assert.True(scenario is not null, $"{decision}: {id} is missing");
        Assert.Equal(ExpansionModules.Kings, scenario!.Modules[0]);
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void TheMechanicIsOfferedAndActs(string id)
    {
        var scenario = TestScenarioCatalog.Find(id)!;
        var expected = Expected[id];
        var state = scenario.Create("kings-mechanic-" + id);
        Assert.Equal(StateCodec.Write(state), StateCodec.Write(scenario.Create("kings-mechanic-" + id)));
        var view = Views.For(state, scenario.StartingSide);
        var payload = expected.Payload(state, view);
        var offered = payload.RulerTokenId.Length > 0
            ? view.RulerTokenActions.Any(t => t.TokenId == payload.RulerTokenId && t.Action.Kind == expected.Kind)
            : view.Options.Any(o => o.Kind == expected.Kind && o.Enabled);
        Assert.True(offered, $"{id}: {expected.Kind} is not offered.");
        var after = Run(state, scenario.StartingSide, Command(state, view, expected.Kind, payload), 11);
        Assert.True(expected.Acted(state, after), $"{id}: expected {expected.Effect}.");
    }

    [Fact]
    public void PoliticsReturnsOneOfDenethorsTiles()
    {
        var state = Play("kings-politics-cleanse");
        state = Choose(state, Side.FreePeoples, "remove");
        Assert.Single(Sovereign(state, SovereignCatalog.Denethor).Tiles);
    }

    [Fact]
    public void TheAttemptOnBrandReachesHisResistance()
    {
        var state = FinishAttempt(Play("kings-corruption-reaches-resistance"));
        var brand = Sovereign(state, SovereignCatalog.Brand);
        Assert.Equal(SovereignStatus.Corrupted, brand.Status);
        Assert.Equal("dale", brand.CorruptedRegion);
        Assert.Empty(brand.Tiles);
        Assert.DoesNotContain(state.Units, u => u.Id == SovereignCatalog.Brand);
    }

    [Fact]
    public void TheShadowChoosesOneOfThreeTiles()
    {
        var state = Draws(Play("kings-corruption-three-tiles"));
        Assert.Equal(3, Views.For(state, Side.Shadow).Options.Single(o => o.Kind == CommandKind.ChooseCorruptionTile).EligibleIds.Count);
        state = FinishAttempt(state);
        Assert.Single(Sovereign(state, SovereignCatalog.Dain).Tiles);
    }

    [Theory]
    [InlineData("north", UnitKind.Leader, "dale")]
    [InlineData("north", UnitKind.Elite, "bree")]
    [InlineData("rohan", UnitKind.Elite, "edoras")]
    [InlineData("rohan", UnitKind.Leader, "westemnet")]
    [InlineData("dwarves", UnitKind.Elite, "erebor")]
    [InlineData("gondor", UnitKind.Leader, "minas-tirith")]
    [InlineData("elves", UnitKind.Elite, "woodland-realm")]
    public void CorruptedKingsForbidTheirRecruits(string nation, UnitKind kind, string region)
    {
        var state = TestScenarioCatalog.Find("kings-corrupted-recruitment")!.Create("kings-bans");
        var recruit = Views.For(state, Side.FreePeoples).Options.Single(o => o.Kind == CommandKind.Recruit);
        Assert.DoesNotContain(region, recruit.Recruitment.Where(g => g.Nation == nation && g.Kind == kind).SelectMany(g => g.Targets));
        Assert.Contains(region, recruit.Recruitment.Where(g => g.Nation == nation && g.Kind == UnitKind.Regular).SelectMany(g => g.Targets));
    }

    [Fact]
    public void BrandsReplacementComesTooLateAgainstTheDefendingSwarm()
    {
        var state = Play("kings-brand-vs-swarm");
        state = Choose(state, Side.FreePeoples, "replace");
        var battle = BattleRuntime.Active(state)!;
        Assert.True(battle.Force(Side.FreePeoples).Round.CardCancelled);
        Assert.False(battle.Force(Side.Shadow).Round.CardCancelled);
    }

    [Fact]
    public void RedWrathWithRelentlessAssaultAddsTwo()
    {
        var state = Play("kings-serpent-relentless-assault");
        var regular = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.Shadow)).First(u => u.Kind == UnitKind.Regular);
        state = Run(state, Side.Shadow, Command(state, Views.For(state, Side.Shadow), CommandKind.TakeBattleLosses,
            new() { Losses = [new() { UnitId = regular.Id, Kind = BattleLossKind.Remove }] }));
        Assert.Contains(ShadowWork(state), w => w.ProgramId == KingsChieftainCombat.RelentlessWrath && w.Initiative == 3);
    }

    [Fact]
    public void ATokenSpentFirstBlocksTheRings()
    {
        var state = TestScenarioCatalog.Find("kings-ruler-token-or-ring")!.Create("kings-token-first");
        var view = Views.For(state, Side.FreePeoples);
        state = Run(state, Side.FreePeoples, Command(state, view, CommandKind.DrawCard,
            new() { DieId = Ruler(state), Deck = "strategy", RulerTokenId = KingsRulerTokens.Id(SovereignCatalog.Denethor) }));
        state = Resolve(state);
        Assert.False(KingsRulerTokens.RingAllowed(state, Side.FreePeoples));
    }

    // Both Free Peoples dice spent end the turn; the next Recover takes their Ruler die and keeps the Shadow's.
    [Fact]
    public void TheRulerDieLeavesAtTheNextRecover()
    {
        var state = Play("kings-ruler-die-recovery");
        state = Resolve(state);
        state = Run(state, Side.FreePeoples, Command(state, Views.For(state, Side.FreePeoples), CommandKind.DrawCard,
            new() { DieId = Die(state, Side.FreePeoples, DieFace.Event), Deck = "character" }));
        state = NextRoll(state);
        Assert.Equal(2, state.Turn);
        Assert.Equal(DieLocation.Removed, KingsDice.Ruler(state, Side.FreePeoples)!.Location);
        Assert.NotEqual(DieLocation.Removed, KingsDice.Ruler(state, Side.Shadow)!.Location);
    }

    private static Expectation Siege() => new(CommandKind.ExtendSiege, (s, view) => new()
        {
            Ids = [view.Options.Single(o => o.Kind == CommandKind.ExtendSiege).EligibleIds
                .First(id => s.Units.Single(u => u.Id == id).Kind == UnitKind.Regular)]
        },
        "a Sauron Regular is removed and the siege goes on", (_, s) => BattleRuntime.Active(s) is { Round: 2, Kings.SiegeWeaknessUsed: true });

    private static Expectation Token(string sovereign) => new(CommandKind.UseCharacterAbility, Ability(sovereign, true),
        "the token pays the recruits and is spent", (b, s) => TokenAt(s, sovereign) == "removed" &&
            Recruited(b, s, SovereignCatalog.All[sovereign].Nation) > 0);

    private static Payload Awakening(string sovereign) => (s, view) =>
    {
        // The journey runs from his region to the next one, his home.
        var option = view.Options.Single(o => o.Kind == CommandKind.AwakenSovereign && o.EligibleIds.Contains(sovereign));
        return new() { DieId = Ruler(s), CharacterId = sovereign, Route = [option.Travel[0].Origin, option.Targets[0]] };
    };

    // A recruiting ability's plan: the local slot with an Elite where allowed, then the settlements' slots with Regulars.
    private static Payload Ability(string sovereign, bool token) => (s, view) =>
    {
        var action = token
            ? view.RulerTokenActions.Single(t => t.TokenId == KingsRulerTokens.Id(sovereign)).Action.CharacterActions.Single()
            : Abilities(view).Single(a => a.CharacterId == sovereign && a.RulerRecruitment is not null);
        var plan = action.RulerRecruitment!;
        var regions = plan.Settlements.Where(r => !plan.LocalMustDiffer || r != plan.LocalRegion).Take(plan.SettlementCount)
            .Prepend(plan.LocalRegion).Take(action.TargetCount).ToList();
        var nation = SovereignCatalog.All[sovereign].Nation;
        var reserve = s.Units.Where(u => u.Location == "reserve" && u.Nation == nation).ToList();
        var placements = regions.Select((region, index) =>
        {
            var kind = index == 0 && plan.LocalAllowsElite ? UnitKind.Elite : UnitKind.Regular;
            var unit = reserve.Where(u => u.Kind == kind).ElementAt(kind == UnitKind.Elite ? 0 : index);
            return new UnitPlacementChoice { UnitId = unit.Id, RegionId = region };
        }).ToList();
        return new()
        {
            DieId = Ruler(s), CharacterId = sovereign, AbilityId = action.AbilityId, Placements = placements,
            RulerTokenId = token ? KingsRulerTokens.Id(sovereign) : ""
        };
    };

    private static Payload Upgrade(string sovereign) => (s, view) =>
    {
        var action = Abilities(view).Single(a => a.CharacterId == sovereign && a.RulerRecruitment is null);
        Assert.Equal(3, Abilities(view).Count(a => a.RulerRecruitment is null));
        return new() { DieId = action.Dice[0], CharacterId = sovereign, AbilityId = action.AbilityId, Ids = [.. action.Targets] };
    };

    private static Payload Attempt(string sovereign) => (s, _) => new() { DieId = CorruptionRules.Eyes(s).First().Id, CharacterId = sovereign };

    private static Payload Recruit(string nation, UnitKind kind, string region = "") => (s, view) =>
    {
        var group = view.Options.Single(o => o.Kind == CommandKind.Recruit).Recruitment.First(g => g.Nation == nation && g.Kind == kind &&
            (region.Length == 0 || g.Targets.Contains(region)));
        var side = s.Pending!.Owner;
        return new()
        {
            DieId = Die(s, side, DieFace.Muster),
            Placements = [new() { UnitId = group.ReserveIds[0], RegionId = region.Length > 0 ? region : group.Targets[0] }]
        };
    };

    private static Payload Entry(string chieftain, string region) => (s, _) =>
    {
        var plan = CharacterEntry.Candidates(s, Side.Shadow).First(p => p.DefinitionId == chieftain && p.Region == region);
        return new() { DieId = Die(s, Side.Shadow, DieFace.Muster), CharacterId = chieftain, RegionId = plan.Region };
    };

    private static Payload Council(string chieftain) => (s, view) =>
    {
        var action = view.Options.Single(o => o.Kind == CommandKind.EnterCharacter).CharacterActions.Single(a => a.CharacterId == chieftain);
        Assert.True(action.RequiresCouncilToken);
        return new()
        {
            DieId = Die(s, Side.Shadow, DieFace.Muster), CharacterId = chieftain, RegionId = action.Targets[0],
            CouncilTokenId = action.CouncilTokens[0]
        };
    };

    private static Payload Choose(string id) => (_, _) => new() { Ids = [id] };

    private static MatchState Play(string id)
    {
        var scenario = TestScenarioCatalog.Find(id)!;
        var state = scenario.Create("kings-play-" + id);
        var view = Views.For(state, scenario.StartingSide);
        return Run(state, scenario.StartingSide, Command(state, view, Expected[id].Kind, Expected[id].Payload(state, view)), 11);
    }

    // The Shadow draws until it may choose among the tiles drawn.
    private static MatchState Draws(MatchState state)
    {
        for (var step = 0; step < 12 && state.Pending?.Kind == DecisionKind.CorruptionTile &&
             Views.For(state, Side.Shadow).Options.All(o => o.Kind != CommandKind.ChooseCorruptionTile); step++)
            state = Run(state, Side.Shadow, Command(state, Views.For(state, Side.Shadow), CommandKind.DrawCorruptionTile, new()), step);
        return state;
    }

    private static MatchState FinishAttempt(MatchState state)
    {
        state = Draws(state);
        if (state.Pending?.Kind != DecisionKind.CorruptionTile)
            return state;
        var choice = Views.For(state, Side.Shadow).Options.Single(o => o.Kind == CommandKind.ChooseCorruptionTile).EligibleIds[0];
        return Run(state, Side.Shadow, Command(state, Views.For(state, Side.Shadow), CommandKind.ChooseCorruptionTile, new() { Ids = [choice] }));
    }

    // The demo CPU answers what the action left open until an action is due again.
    private static MatchState Resolve(MatchState state)
    {
        for (var step = 0; step < 40 && state.Pending is { } pending && !ActionDue(state); step++)
            state = Run(state, pending.Owner, Bot.Next(state, pending.Owner)!, step);
        return state;
    }

    private static bool ActionDue(MatchState state) => state.Pending?.Kind == DecisionKind.Action && EffectRuntime.Current(state) is null;

    private static MatchState Choose(MatchState state, Side side, string id) =>
        Run(state, side, Command(state, Views.For(state, side), CommandKind.ChooseEffect, new() { Ids = [id] }));

    private static IEnumerable<CharacterActionOption> Abilities(PlayerView view) =>
        view.Options.Single(o => o.Kind == CommandKind.UseCharacterAbility).CharacterActions;

    // The demo CPU plays the end of the turn and the next turn's start until its dice are rolled and kept.
    private static MatchState NextRoll(MatchState state)
    {
        for (var step = 0; step < 40 && !Rolled(state); step++)
            state = Run(state, state.Pending!.Owner, Bot.Next(state, state.Pending.Owner)!, step);
        return state;
    }

    private static bool Rolled(MatchState state) => state.Turn > 1 &&
        (state.Phase == GamePhase.ActionResolution || state.Pending?.Kind == DecisionKind.RulerDiscard);

    private static MatchState StateFor(MatchState state, Side side)
    {
        var copy = StateCodec.Copy(state);
        if (copy.Pending?.Owner != side)
            TurnFlow.SelectNext(copy, side);
        return copy;
    }

    private static GameCommand Command(MatchState state, PlayerView view, CommandKind kind, CommandPayload payload) => new()
    {
        MatchId = state.Id,
        CommandId = "kings-mechanic-" + kind + "-" + state.Revision,
        ExpectedRevision = state.Revision,
        DecisionId = view.Decision!.Id,
        Kind = kind,
        Payload = payload
    };

    private static MatchState Run(MatchState state, Side side, GameCommand command, int seed = 7)
    {
        var error = Engine.Validate(state, side, command);
        Assert.True(error is null, $"{command.Kind}: {error} {System.Text.Json.JsonSerializer.Serialize(command.Payload)}");
        var result = Engine.Execute(state, side, command, new SeededRandom(seed));
        Assert.True(result.Receipt.Accepted, $"{command.Kind}: {result.Receipt.Error}");
        return result.State;
    }

    private static SovereignState Sovereign(MatchState state, string id) => state.Kings!.Sovereigns.Single(s => s.Id == id);

    private static SovereignStatus Status(MatchState state, string id) => Sovereign(state, id).Status;

    private static string Location(MatchState state, string id) => state.Characters.Single(c => c.Id == id).Location;

    private static string Die(MatchState state, Side side, DieFace face) =>
        state.Dice.First(d => d.Owner == side && d.Source == "standard" && d.Location == DieLocation.Available && d.Face == face).Id;

    private static string Ruler(MatchState state) => KingsDice.Ruler(state, Side.FreePeoples)!.Id;

    private static string TokenAt(MatchState state, string sovereign) => state.Kings!.Tokens.Single(t => t.SovereignId == sovereign).Location;

    private static int Recruited(MatchState before, MatchState after, string nation) => OnBoard(after, nation) - OnBoard(before, nation);

    private static int OnBoard(MatchState state, string nation) => state.Units.Count(u => u.Nation == nation && u.Location != "reserve");

    private static int Gained(MatchState before, MatchState after, string region, UnitKind kind) =>
        Count(after, region, kind) - Count(before, region, kind);

    private static int Count(MatchState state, string region, UnitKind kind) => state.Units.Count(u => u.Location == region && u.Kind == kind);

    private static int? PublicCorruption(MatchState state, string sovereign) =>
        Views.For(state, Side.FreePeoples).Sovereigns.Single(k => k.Id == sovereign).Corruption;

    private static int TheodenLeadership(MatchState state)
    {
        var force = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        return KingsBattle.Leadership(state, force, state.Characters.Single(c => c.Id == SovereignCatalog.Theoden));
    }

    private static string? Replacement(MatchState state) => BattleRuntime.Active(state)!.Force(Side.Shadow).Round.ReplacementSource;

    private static IEnumerable<CombatWork> ShadowWork(MatchState state) => BattleRuntime.Active(state)!.Work.Where(w => w.Owner == Side.Shadow);

    // The dice of the Hunt this move rolled, read from its record ("Hunt: 3, 5; bonus ...").
    private static int HuntDice(MatchState state)
    {
        var text = state.Events.Last(e => e.Kind == "HuntRolled").Text;
        return text["Hunt: ".Length..text.IndexOf(';')].Split(", ").Length;
    }
}
