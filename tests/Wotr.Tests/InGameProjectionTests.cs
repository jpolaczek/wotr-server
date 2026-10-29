using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class InGameProjectionTests
{
    private static readonly Projection Views = LordsFixture.Views();

    internal static Projection ProjectionViews() => Views;

    internal static ActionAvailabilityView Availability(MatchState state, Side viewer, string subject, string action) =>
        Views.For(state, viewer).Availability.Single(a => a.SubjectId == subject && a.ActionId == action);

    [Fact]
    public void OwnRingConditionsRemainReadableOutsideTheActionWindowWithoutGrantingPermission()
    {
        var state = KingsFixture.Actions();
        var ring = state.Rings.First(r => r.Owner == Side.FreePeoples).Id;
        var die = KingsFixture.Die(state);
        var payload = new CommandPayload { RingId = ring, DieId = die.Id, Face = DieFace.Army };
        Assert.True(Availability(state, Side.FreePeoples, ring, "change-die").CanAct);
        Assert.Null(LordsFixture.Engine().Validate(state, Side.FreePeoples, BattleFixture.Command(state, CommandKind.UseRing, payload)));
        TurnFlow.SelectNext(state, Side.Shadow);
        var description = Availability(state, Side.FreePeoples, ring, "change-die");
        Assert.True(description.ConditionsMet);
        Assert.False(description.WindowOpen);
        Assert.False(description.CanAct);
        Assert.Contains(die.Id, description.EligibleDice);
        Assert.DoesNotContain(Views.For(state, Side.Shadow).Availability, a => a.SubjectId == ring);
        Assert.NotNull(LordsFixture.Engine().Validate(state, Side.FreePeoples, BattleFixture.Command(state, CommandKind.UseRing, payload)));
    }

    [Theory]
    [InlineData("used-ring", "ring-turn")]
    [InlineData("used-token", "ruler-token")]
    [InlineData("no-dice", "changeable-die")]
    public void RingDiagnosticsAgreeWithRejectedCostAndLimit(string stateChange, string condition)
    {
        var state = KingsFixture.Actions();
        var ring = state.Rings.First(r => r.Owner == Side.FreePeoples).Id;
        var die = KingsFixture.Die(state);
        if (stateChange == "used-ring")
            state.Player(Side.FreePeoples).RingUsedThisTurn = true;
        if (stateChange == "used-token")
            state.Kings!.RulerTokenUsedThisTurn = true;
        if (stateChange == "no-dice")
            foreach (var own in state.Dice.Where(d => d.Owner == Side.FreePeoples))
                own.Location = DieLocation.Spent;
        var description = Availability(state, Side.FreePeoples, ring, "change-die");
        Assert.False(description.CanAct);
        Assert.False(description.Conditions.Single(c => c.Code == condition).Met);
        Assert.NotNull(RingRules.Validate(state, Side.FreePeoples, new() { RingId = ring, DieId = die.Id, Face = DieFace.Army }));
    }

    // UI-414 (owner): a Corrupted sovereign's effects are its passive ones, titled by ability and naming their place; the
    // optional uses (Shadows in the North, the Fate abilities) are not effects.
    [Fact]
    public void CorruptedSovereignsProjectOnlyTheirPassiveEffectsTitledByAbility()
    {
        var state = KingsFixture.Actions();
        foreach (var sovereign in state.Kings!.Sovereigns)
            sovereign.Status = SovereignStatus.Corrupted;
        var effects = InGameEffectProjection.For(state, Side.FreePeoples).Where(e => e.SourceKind == "Ability").ToList();
        Assert.Equal(6, effects.Count);
        Assert.Equal(5, effects.Count(e => e.Title.StartsWith("Corrupted ruler · ", StringComparison.Ordinal)));
        var dain = effects.Single(e => e.Title == "Corrupted ruler · Dáin");
        Assert.Contains("in Erebor", dain.Description);
        Assert.Equal("erebor", dain.Targets.Single().Id);
        Assert.Contains("in the Woodland Realm", effects.Single(e => e.Title == "Corrupted ruler · Thranduil").Description);
        var mark = effects.Single(e => e.Title == "Mark of the White Hand · Théoden");
        Assert.Equal("helms-deep", mark.Targets.Single().Id);
        Assert.DoesNotContain(effects, e => e.Description.Contains("Shadow may", StringComparison.Ordinal));
    }

    [Fact]
    public void AwakeningDiagnosticsFollowNationCostRouteAndTiming()
    {
        var state = KingsFixture.Actions();
        var sovereign = SovereignCatalog.Dain;
        Assert.False(Availability(state, Side.FreePeoples, sovereign, "awaken").ConditionsMet);
        state.Nations.Single(n => n.Id == "dwarves").Active = true;
        var description = Availability(state, Side.FreePeoples, sovereign, "awaken");
        Assert.True(description.CanAct);
        Assert.Contains("erebor", description.Targets);
        var payload = new CommandPayload { CharacterId = sovereign, DieId = description.EligibleDice[0], Route = ["erebor"] };
        Assert.True(SovereignAwakening.Valid(state, payload));
        TurnFlow.SelectNext(state, Side.Shadow);
        Assert.True(Availability(state, Side.FreePeoples, sovereign, "awaken").ConditionsMet);
        Assert.False(Availability(state, Side.FreePeoples, sovereign, "awaken").CanAct);
        state.Control["erebor"] = Side.Shadow;
        Assert.False(Availability(state, Side.FreePeoples, sovereign, "awaken").ConditionsMet);
        Assert.False(SovereignAwakening.Valid(state, payload));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(4, true)]
    public void CorruptionDiagnosticsShareEyeEligibilityAndOncePerTurnLimit(int eyes, bool available)
    {
        var state = KingsFixture.Actions(Side.Shadow);
        KingsFixture.Eyes(state, eyes);
        var description = Availability(state, Side.Shadow, SovereignCatalog.Dain, "attempt-corruption");
        Assert.Equal(available, description.CanAct);
        Assert.Equal(CorruptionRules.CanAttempt(state), description.ConditionsMet);
        Assert.Equal(eyes, description.EligibleDice.Count);
        KingsFixture.Sovereign(state, SovereignCatalog.Dain).LastAttemptTurn = state.Turn;
        Assert.False(Availability(state, Side.Shadow, SovereignCatalog.Dain, "attempt-corruption").CanAct);
        Assert.DoesNotContain(CorruptionRules.Targets(state), s => s.Id == SovereignCatalog.Dain);
    }

    [Fact]
    public void CaptainDiceDoNotMakeCorruptionAvailableOrBecomeEligibleCosts()
    {
        var state = KingsFixture.Actions(Side.Shadow);
        KingsFixture.Eyes(state, 2);
        var excluded = CorruptionRules.Eyes(state).First();
        excluded.HuntOrigin = "captain-of-despair";
        var description = Availability(state, Side.Shadow, SovereignCatalog.Dain, "attempt-corruption");
        Assert.False(description.ConditionsMet);
        Assert.DoesNotContain(excluded.Id, description.EligibleDice);
        Assert.False(CorruptionRules.CanAttempt(state));
    }

    [Fact]
    public void RulerInspectionFindsActualAbilityTargetsOutsideItsWindowAndRejectsPeace()
    {
        var (state, payload) = KingsRulerTokenTests.Recruitment();
        var subject = payload.RulerTokenId;
        Assert.True(Availability(state, Side.FreePeoples, subject, "ruler-token").CanAct);
        Assert.Null(LordsFixture.Engine().Validate(state, Side.FreePeoples, BattleFixture.Command(state, CommandKind.UseCharacterAbility, payload)));
        TurnFlow.SelectNext(state, Side.Shadow);
        var before = StateCodec.Write(state);
        Assert.True(Availability(state, Side.FreePeoples, subject, "ruler-token").ConditionsMet);
        Assert.False(Availability(state, Side.FreePeoples, subject, "ruler-token").CanAct);
        Assert.Empty(Views.For(state, Side.FreePeoples).RulerTokenActions);
        Assert.Equal(before, StateCodec.Write(state));
        state.Nations.Single(n => n.Id == "north").StepsToWar = 1;
        var blocked = Availability(state, Side.FreePeoples, subject, "ruler-token");
        Assert.False(blocked.ConditionsMet);
        Assert.False(blocked.Conditions.Single(c => c.Code == "ruler-action").Met);
        state.Player(Side.FreePeoples).RingUsedThisTurn = true;
        var ringLimit = Availability(state, Side.FreePeoples, subject, "ruler-token");
        Assert.False(ringLimit.Conditions.Single(c => c.Code == "ring-turn").Met);
        Assert.True(ringLimit.Conditions.Single(c => c.Code == "ruler-source").Met);
    }

    [Theory]
    [InlineData("ruler-enabled")]
    [InlineData("ring-turn")]
    [InlineData("ruler-die")]
    public void RulerUnavailablePrerequisiteDoesNotPretendThatUninspectedTargetsAreMissing(string blockedCondition)
    {
        var (state, payload) = KingsRulerTokenTests.Recruitment();
        var ready = Availability(state, Side.FreePeoples, payload.RulerTokenId, "ruler-token");
        Assert.True(ready.Conditions.Single(c => c.Code == "ruler-action").Met);
        if (blockedCondition == "ruler-enabled")
            state.Configuration.RemovingRulerTokens = true;
        else if (blockedCondition == "ring-turn")
            state.Player(Side.FreePeoples).RingUsedThisTurn = true;
        else
            KingsDice.Ruler(state, Side.FreePeoples)!.Location = DieLocation.Spent;
        var before = StateCodec.Write(state);
        var blocked = Availability(state, Side.FreePeoples, payload.RulerTokenId, "ruler-token");
        Assert.Equal(blockedCondition, Assert.Single(blocked.Conditions, c => !c.Met).Code);
        Assert.DoesNotContain(blocked.Conditions, c => c.Code == "ruler-action");
        Assert.False(blocked.ConditionsMet);
        Assert.False(blocked.CanAct);
        Assert.NotNull(LordsFixture.Engine().Validate(state, Side.FreePeoples,
            BattleFixture.Command(state, CommandKind.UseCharacterAbility, payload)));
        Assert.Equal(before, StateCodec.Write(state));
    }

    [Fact]
    public void RemovingRulerTokensVariantNeverGrantsAwakenedTokenAvailability()
    {
        var state = KingsIntegrationFixture.Actions(new() { Lords = true, Kings = true, RemovingRulerTokens = true });
        SovereignLifecycle.Awaken(state, KingsFixture.Sovereign(state, SovereignCatalog.Brand), "dale");
        KingsFixture.Ruler(state, DieFace.Muster);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        var view = Views.For(state, Side.FreePeoples);
        Assert.Empty(view.RulerTokens);
        Assert.Empty(view.RulerTokenActions);
        Assert.DoesNotContain(view.Availability, a => a.ActionId == "ruler-token");
    }

    [Fact]
    public void EffectsKeepSeparateExpiryAndIncludeTableOnlySourcesWithoutDuplication()
    {
        var state = KernelTests.NewGame();
        const string card = FreePeoplesCharacterCards01.Mithril;
        state.Player(Side.FreePeoples).Table.Add(card);
        state.ActiveEffects.Add(new() { Id = "turn", SourceId = card, TargetId = "rivendell", Duration = EffectDuration.EndTurn });
        state.ActiveEffects.Add(new() { Id = "round", SourceId = card, TargetId = "lorien", Duration = EffectDuration.EndCombatRound });
        state.Player(Side.Shadow).Table.Add(ShadowStrategyCards01.Threats);
        state.ActiveEffects.Add(new() { Id = "private", SourceId = "private-effect", OnlyFor = Side.FreePeoples });
        var effects = Views.For(state, Side.Shadow).Effects;
        Assert.Equal(2, effects.Count(e => e.SourceId == card));
        Assert.Equal(new[] { "At the end of this turn.", "At the end of this combat round." },
            effects.Where(e => e.SourceId == card).Select(e => e.ExpiryDescription));
        Assert.All(effects.Where(e => e.SourceId == card), e => Assert.Equal("Region", Assert.Single(e.Targets).Kind));
        var table = Assert.Single(effects, e => e.SourceId == ShadowStrategyCards01.Threats);
        Assert.Equal("Card", table.SourceKind);
        Assert.NotEmpty(table.Description);
        Assert.NotEmpty(table.SourceReference);
        Assert.DoesNotContain(effects, e => e.Id == "private");
    }

    [Fact]
    public void PlayedWormtongueScopesRohanWhileSarumanRemainsItsExpirySubject()
    {
        var state = Shadow04WormtongueTests.Passive();
        var effect = Assert.Single(state.ActiveEffects, e => e.SourceId == ShadowCharacterCards04.Wormtongue);
        var saruman = CharacterRules.InPlay(state, CharacterRules.Saruman)!;
        Assert.Equal(saruman.Id, effect.TargetId);
        foreach (var viewer in Enum.GetValues<Side>())
        {
            var projected = Assert.Single(Views.For(state, viewer).Effects, e => e.Id == effect.Id);
            var target = Assert.Single(projected.Targets);
            Assert.Equal("Nation", target.Kind);
            Assert.Equal("rohan", target.Id);
            Assert.Contains("source character leaves play or Rohan activates", projected.ExpiryDescription);
        }
        CharacterLifecycle.Eliminate(state, saruman);
        ActiveEffects.CheckConditions(state);
        Assert.DoesNotContain(Views.For(state, Side.FreePeoples).Effects, e => e.Id == effect.Id);
    }

    [Fact]
    public void PlayedDenethorsFollyScopesMinasTirithWithoutAStoredTargetId()
    {
        const string card = ShadowStrategyCards01.Denethor;
        var state = ShadowStrategy01Fixture.Play(ShadowStrategy01Fixture.State(card), card);
        var effect = Assert.Single(state.ActiveEffects, e => e.SourceId == card);
        Assert.Empty(effect.TargetId);
        foreach (var viewer in Enum.GetValues<Side>())
        {
            var projected = Assert.Single(Views.For(state, viewer).Effects, e => e.Id == effect.Id);
            var target = Assert.Single(projected.Targets);
            Assert.Equal("Region", target.Kind);
            Assert.Equal("minas-tirith", target.Id);
            Assert.Contains("siege of Minas Tirith ends", projected.ExpiryDescription);
        }
    }

    [Fact]
    public void PlayedPalantirDoesNotTreatItsExpirySubjectAsAnAffectedCharacter()
    {
        var state = Shadow04Fixture.Played(ShadowCharacterCards04.Palantir);
        var effect = Assert.Single(state.ActiveEffects, e => e.SourceId == ShadowCharacterCards04.Palantir);
        Assert.Equal(CharacterRules.InPlay(state, CharacterRules.Saruman)!.Id, effect.TargetId);
        foreach (var viewer in Enum.GetValues<Side>())
        {
            var projected = Assert.Single(Views.For(state, viewer).Effects, e => e.Id == effect.Id);
            Assert.Equal("Global", Assert.Single(projected.Targets).Kind);
            Assert.Contains("source character leaves play", projected.ExpiryDescription);
        }
    }

    [Fact]
    public void CorruptedSovereignAbilitiesUseTheirPublicVersionAndKeepTheirDifferentScopes()
    {
        var state = KingsFixture.Actions();
        KingsFixture.Sovereign(state, SovereignCatalog.Theoden).Status = SovereignStatus.Corrupted;
        var effects = Views.For(state, Side.FreePeoples).Effects;
        Assert.Equal(2, effects.Count);
        Assert.All(effects, e => Assert.Equal("kings.character.theoden.corrupted", e.SourceId));
        Assert.All(effects, e => Assert.Equal("Ability", e.SourceKind));
        Assert.Contains(effects, e => e.Targets.Any(t => t.Kind == "NationRegions" && t.Id == "rohan"));
        Assert.Contains(effects, e => e.Targets.Any(t => t.Kind == "Region" && t.Id == "helms-deep"));
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void VictoryBreakdownUsesScenarioOwnershipAndDoesNotDeclareAnEarlyWinner(bool lords, bool kings, bool fate)
    {
        var state = MatchFactory.Create("victory-data", new SequenceRandom(new int[200]), new() { Lords = lords, Kings = kings, Fate = fate });
        if (kings)
            KingsSetup.Initialize(state);
        Assert.All(Views.For(state, Side.FreePeoples).MilitaryVictory, score => Assert.Equal(0, score.Points));
        state.Control["erebor"] = fate ? Side.FreePeoples : Side.Shadow;
        state.Control["helms-deep"] = Side.Shadow;
        if (kings)
            KingsFixture.Sovereign(state, SovereignCatalog.Theoden).Status = SovereignStatus.Corrupted;
        foreach (var viewer in Enum.GetValues<Side>())
        {
            var view = Views.For(state, viewer);
            foreach (var score in view.MilitaryVictory)
            {
                Assert.Equal(score.Points, score.Regions.Sum(r => r.Points));
                Assert.Equal(MilitaryVictory.Points(state, score.Side), score.Points);
                Assert.Equal(view.Players.Single(p => p.Side == score.Side).VictoryPoints, score.Points);
            }
            Assert.Equal(2, view.MilitaryVictory.Single(s => s.Side == Side.Shadow).Regions.Single(r => r.RegionId == "helms-deep").Points);
            Assert.Equal(2, view.MilitaryVictory.Single(s => s.Side == (fate ? Side.FreePeoples : Side.Shadow)).Regions.Single(r => r.RegionId == "erebor").Points);
            Assert.Null(view.Winner);
        }
        foreach (var region in BoardRules.SetupRegions(state.Configuration).Where(r => r.OriginalOwner == Side.FreePeoples))
            state.Control[region.Id] = Side.Shadow;
        Assert.True(Views.For(state, Side.FreePeoples).MilitaryVictory.Single(s => s.Side == Side.Shadow).ThresholdReached);
        Assert.Null(state.Winner);
    }

    [Fact]
    public void PrivateHandAndVisibleCorruptionDrawCannotChangeTheOtherSidesDiagnostics()
    {
        var state = KingsFixture.Attempt(3, tiles: [HuntTests.Three]);
        state.Configuration.VisibleCorruption = true;
        var changed = StateCodec.Copy(state);
        changed.Player(Side.Shadow).Hand = ["private-card"];
        state.Player(Side.Shadow).Hand = ["different-private-card"];
        CorruptionRuntime.Current(changed)!.Drawn[0] = "base.hunt.standard.1.0.1";
        Assert.Equal(Json(Views.For(state, Side.FreePeoples)), Json(Views.For(changed, Side.FreePeoples)));
        Assert.Empty(Views.For(changed, Side.FreePeoples).CorruptionAttempt!.OwnChoices);
    }

    private static string Json(object value) => JsonSerializer.Serialize(value, BoardCatalog.Json);
}
