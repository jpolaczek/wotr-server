using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public sealed class DirectEventChoiceFamilyTests
{
    private const string CirdansShips = "base.fp.strategy.13";
    private const string HalfOrcs = "base.sh.strategy.13";
    private const string Stewards = "base.fp.character.23";

    private static readonly string[] Cards =
    [
        FreePeoplesStrategyCards03.CelebornsGaladhrim,
        FreePeoplesStrategyCards03.GuardsOfCitadel,
        FreePeoplesStrategyCards03.Grimbeorn,
        FreePeoplesStrategyCards04.SwordsInEriador,
        FreePeoplesStrategyCards04.DainIronfoot,
        FreePeoplesStrategyCards01.SpiritOfMordor,
        FreePeoplesStrategyCards01.FaramirsRangers,
        FreePeoplesStrategyCards02.RedArrow,
        CirdansShips,
        KingsCards02.Villages,
        KingsCards04.Enemy,
        FateCards.Dwarves,
        FateCards.Orcs,
        ShadowStrategyCards03.HillTrolls,
        ShadowStrategyCards03.ManyKings,
        ShadowStrategyCards03.NewPower,
        HalfOrcs,
        ShadowStrategyCards04.MistyMountains,
        ShadowStrategyCards04.Horde,
        ShadowCharacterCards02.Weather,
        ShadowCharacterCards04.Captain
    ];

    public static TheoryData<string> EachCard()
    {
        var data = new TheoryData<string>();
        foreach (var card in Cards)
            data.Add(card);
        return data;
    }

    public static TheoryData<string, string> EachCardAndAlteration()
    {
        var data = new TheoryData<string, string>();
        foreach (var card in Cards)
            foreach (var change in new[] { "cursor", "payment", "public-play", "selection" })
                data.Add(card, change);
        return data;
    }

    [Fact]
    public void ReviewedEventsAreExactlyThese()
    {
        var reviewed = EffectCatalog.All.Events
            .Where(pair => EffectCatalog.All.Programs.TryGetValue(pair.Value.ProgramId, out var program) &&
                Enumerable.Range(1, program.Steps.Count).Any(step => DirectEventChoices.IsReviewed(pair.Key, step, program)))
            .Select(pair => pair.Key).OrderBy(id => id, StringComparer.Ordinal);

        Assert.Equal(
        [
            "base.fp.character.23",
            "base.fp.strategy.05", "base.fp.strategy.06", "base.fp.strategy.09", "base.fp.strategy.13",
            "base.fp.strategy.14", "base.fp.strategy.15", "base.fp.strategy.16", "base.fp.strategy.17",
            "base.fp.strategy.18", "base.fp.strategy.19", "base.fp.strategy.20", "base.fp.strategy.21",
            "base.fp.strategy.22", "base.fp.strategy.23", "base.fp.strategy.24",
            "base.sh.character.10", "base.sh.character.11", "base.sh.character.18", "base.sh.character.24",
            "base.sh.strategy.01", "base.sh.strategy.11", "base.sh.strategy.12", "base.sh.strategy.13",
            "base.sh.strategy.14", "base.sh.strategy.15", "base.sh.strategy.16", "base.sh.strategy.17",
            "base.sh.strategy.18", "base.sh.strategy.19", "base.sh.strategy.20", "base.sh.strategy.21",
            "base.sh.strategy.22", "base.sh.strategy.23", "base.sh.strategy.24",
            "fate.fp.strategy.19", "fate.fp.strategy.22", "fate.sh.strategy.20",
            "kings.fp.strategy.08", "kings.fp.strategy.25", "kings.fp.strategy.26",
            "kings.sh.strategy.01", "kings.sh.strategy.18", "kings.sh.strategy.26",
            "lords.fp.character.27", "lords.fp.strategy.25", "lords.fp.strategy.26", "lords.sh.character.17",
            "lords.sh.strategy.26"
        ], reviewed);
    }

    [Theory]
    [MemberData(nameof(EachCard))]
    public void MarksTheFirstChoiceWithOnlyThePublicCardAndPayment(string card)
    {
        var state = Played(card);

        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        Assert.Equal(card, state.Pending.SourceId);
        AssertMarked(state, card);
    }

    [Theory]
    [InlineData(CirdansShips)]
    [InlineData(HalfOrcs)]
    public void MarksTheRecruitmentInTheChosenRegion(string card)
    {
        var state = Played(card);
        var region = state.Pending!.PrivateChoices[0];

        state = KingsFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = [region] });

        Assert.Equal("ReserveFigures", state.Pending!.Presentation);
        Assert.Equal(region, state.Resolution.Frames[0].Effect!.RegionId);
        AssertMarked(state, card);
    }

    [Fact]
    public void MarksRangersRecruitmentAfterTheRoll()
    {
        var state = Played(FreePeoplesStrategyCards01.FaramirsRangers);

        state = KingsFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = [state.Pending!.PrivateChoices[0]] }, 0, 0, 0);

        Assert.Equal("ReserveFigures", state.Pending!.Presentation);
        Assert.Equal(3, state.Resolution.Frames[0].NextStep);
        Assert.Equal("osgiliath", state.Resolution.Frames[0].Effect!.RegionId);
        AssertMarked(state, FreePeoplesStrategyCards01.FaramirsRangers);
    }

    [Fact]
    public void MarksARecruitmentAfterAnAutomaticBoromirRegion()
    {
        var state = Played(Stewards);

        Assert.Equal(2, state.Resolution.Frames[0].NextStep);
        Assert.Equal(StewardsCondition.Boromir(state)!.Location, state.Resolution.Frames[0].Effect!.RegionId);
        AssertMarked(state, Stewards);
    }

    [Theory]
    [InlineData("missing-region")]
    [InlineData("unknown-region")]
    [InlineData("target")]
    [InlineData("nation")]
    [InlineData("foreign-selection")]
    public void TheChosenRegionMustBeAPublicRegion(string change)
    {
        var state = Played(CirdansShips);
        state = KingsFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = [state.Pending!.PrivateChoices[0]] });
        var execution = state.Resolution.Frames[0].Effect!;
        switch (change)
        {
            case "missing-region": execution.RegionId = ""; break;
            case "unknown-region": execution.RegionId = "nowhere"; break;
            case "target": execution.TargetIds = ["altered"]; break;
            case "nation": execution.NationId = "elves"; break;
            case "foreign-selection": execution.Committed.Add(new(Side.Shadow, CirdansShips)); break;
        }

        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary?.DirectEventChoice));
    }

    [Theory]
    [MemberData(nameof(EachCardAndAlteration))]
    public void RefusesAlteredEvidence(string card, string change)
    {
        var state = Played(card);
        var execution = state.Resolution.Frames[0].Effect!;
        switch (change)
        {
            case "cursor": state.Resolution.Frames[0].NextStep = 0; break;
            case "payment":
                state.Dice.Single(die => die.Id == state.Resolution.PaidDieIds[0]).Location = DieLocation.Spent;
                break;
            case "public-play": state.Events.RemoveAll(item => item.Kind == "EventPlayed"); break;
            case "selection": execution.TargetIds = ["altered"]; break;
        }

        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary?.DirectEventChoice));
    }

    [Theory]
    [InlineData(DieFace.Character, false)]
    [InlineData(DieFace.Event, false)]
    [InlineData(DieFace.Event, true)]
    public void MarksTheSpearWindowBeforeTheFirstStep(DieFace face, bool palantir)
    {
        var state = LordsChiefReactionTests.Play(LordsChiefReactionTests.BeforeEvent(palantir), face: face);

        Assert.Equal("event.played", state.Pending!.Window);
        Assert.Equal(palantir, state.Resolution.Frames[0].Effect!.PalantirDrawPending);
        AssertMarked(state, ShadowCharacterCards03.Lidless, face);
    }

    [Theory]
    [InlineData(DieFace.Character, false)]
    [InlineData(DieFace.Event, true)]
    public void MarksTheFirstChoiceAfterSpearWasTaken(DieFace face, bool palantir)
    {
        var state = LordsChiefReactionTests.BeforeEvent(palantir);
        state.Fellowship.Progress = 1;
        state = LordsChiefReactionTests.Play(state, ShadowCharacterCards02.Weather, face);
        state = LordsFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = ChiefReactions.Spear });

        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        Assert.False(state.Resolution.Frames[0].Effect!.PalantirDrawPending);
        Assert.True(DirectEventChoices.IsSpearOfTerror(state.Resolution.Frames[0].Effect!.Committed[1]));
        AssertMarked(state, ShadowCharacterCards02.Weather, face);
    }

    [Fact]
    public void TheFirstChoiceExpectsThePalantirDraw()
    {
        var state = LordsChiefReactionTests.BeforeEvent(palantir: true, chiefRegion: "moria");
        state.Fellowship.Progress = 1;
        state = LordsChiefReactionTests.Play(state, ShadowCharacterCards02.Weather, DieFace.Event);

        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        Assert.True(state.Resolution.Frames[0].Effect!.PalantirDrawPending);
        AssertMarked(state, ShadowCharacterCards02.Weather, DieFace.Event);

        state.Resolution.Frames[0].Effect!.PalantirDrawPending = false;
        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary?.DirectEventChoice));
    }

    [Fact]
    public void MarksTheNazgulTravelAfterTheCaptainsOrderToFly()
    {
        var state = Played(ShadowCharacterCards04.Captain);
        state = KingsFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = [BlackCaptainStep.Fly] });

        Assert.Equal(DecisionKind.CharacterEvent, state.Pending!.Kind);
        Assert.Equal(EffectWait.NazgulTravel, state.Resolution.Frames[0].Effect!.Wait);
        AssertMarked(state, ShadowCharacterCards04.Captain);
    }

    [Theory]
    [InlineData("base.sh.character.09")]
    [InlineData("base.sh.character.11")]
    [InlineData("base.sh.character.23")]
    public void MarksTheNazgulTravelThatOpensAnEvent(string card)
    {
        var state = BattleFixture.State(Side.Shadow);
        CharacterFixture.Put(state, CharacterRules.WitchKing, "angmar");
        state.Fellowship.Progress = 1;
        EventTests.GiveCard(state, Side.Shadow, card);
        var die = state.Dice.First(item => item.Owner == Side.Shadow && item.Location == DieLocation.Available);
        die.Face = DieFace.Character;
        state = KingsFixture.Do(state, CommandKind.PlayEvent, new() { CardId = card, DieId = die.Id, CardCost = DieFace.Character });

        Assert.Equal(DecisionKind.CharacterEvent, state.Pending!.Kind);
        AssertMarked(state, card);
    }

    [Theory]
    [InlineData("cursor")]
    [InlineData("foreign-trail")]
    [InlineData("region")]
    [InlineData("wait")]
    public void TheNazgulTravelRequiresItsExactFrame(string change)
    {
        var state = Played(ShadowCharacterCards04.Captain);
        state = KingsFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = [BlackCaptainStep.Fly] });
        var frame = state.Resolution.Frames[0];
        switch (change)
        {
            case "cursor": frame.NextStep = 2; break;
            case "foreign-trail": frame.Effect!.Committed.Add(new(Side.Shadow, "altered")); break;
            case "region": frame.Effect!.RegionId = "angmar"; break;
            case "wait": frame.Effect!.Wait = EffectWait.None; break;
        }

        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary?.DirectEventChoice));
    }

    [Theory]
    [InlineData("base.fp.strategy.02")]
    [InlineData("base.fp.strategy.14")]
    public void MarksTheGreyDrawWindowAfterTheLastStep(string card)
    {
        var state = GreyWindow(card);

        Assert.Equal("event.completed", state.Pending!.Window);
        AssertMarked(state, card, DieFace.Event);
    }

    [Theory]
    [InlineData("window-cursor")]
    [InlineData("window-owner")]
    [InlineData("window-source")]
    [InlineData("window-trail")]
    [InlineData("event-cursor")]
    [InlineData("completion-flag")]
    [InlineData("palantir")]
    [InlineData("event-frame")]
    [InlineData("payment")]
    public void TheCompletedWindowRequiresItsExactFrames(string change)
    {
        var state = GreyWindow("base.fp.strategy.14");
        var parent = state.Resolution.Frames[0];
        var window = state.Resolution.Frames[1];
        switch (change)
        {
            case "window-cursor": window.NextStep = 1; break;
            case "window-owner": window.Effect!.Owner = Side.Shadow; break;
            case "window-source": window.Effect!.SourceId = "base.fp.strategy.15"; break;
            case "window-trail": window.Effect!.Committed.Add(new(Side.FreePeoples, "altered")); break;
            case "event-cursor": parent.NextStep = 1; break;
            case "completion-flag": parent.Effect!.CompletionWindowOffered = false; break;
            case "palantir": parent.Effect!.PalantirDrawPending = true; break;
            case "event-frame": parent.EffectId = "base.fp.strategy.15"; break;
            case "payment":
                state.Dice.Single(die => die.Id == state.Resolution.PaidDieIds[0]).Location = DieLocation.Spent;
                break;
        }

        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary?.DirectEventChoice));
    }

    [Theory]
    [InlineData(FreePeoplesStrategyCards01.SpiritOfMordor, Side.Shadow)]
    [InlineData(FreePeoplesStrategyCards01.FaramirsRangers, Side.Shadow)]
    public void MarksTheLossesOfAnArmyEventForTheirOwner(string card, Side lossOwner)
    {
        var state = LossesOf(card);

        Assert.Equal(lossOwner, state.Pending!.Owner);
        Assert.Equal(DecisionKind.Losses, state.Pending.Kind);
        AssertMarked(state, card);
        Assert.Equal(card, GameRuntime.Projection(EffectCatalog.All).For(state, lossOwner).EventLoss!.SourceId);
    }

    [Theory]
    [InlineData("source")]
    [InlineData("cause")]
    [InlineData("cursor")]
    [InlineData("event-wait")]
    [InlineData("extra-frame")]
    [InlineData("payment")]
    public void TheLossesRequireTheirExactFrames(string change)
    {
        var state = LossesOf(FreePeoplesStrategyCards01.SpiritOfMordor);
        var loss = state.Resolution.Frames[1].EventLoss!;
        switch (change)
        {
            case "source": state.Resolution.Frames[1].EventLoss = loss with { SourceId = "base.fp.strategy.06" }; break;
            case "cause": state.Resolution.Frames[1].EventLoss = loss with { Cause = CasualtyCause.DestructiveEvent }; break;
            case "cursor": state.Resolution.Frames[0].NextStep = 0; break;
            case "event-wait": state.Resolution.Frames[0].Effect!.Wait = EffectWait.Choice; break;
            case "extra-frame": state.Resolution.Frames.Insert(0, new()); break;
            case "payment":
                state.Dice.Single(die => die.Id == state.Resolution.PaidDieIds[0]).Location = DieLocation.Spent;
                break;
        }

        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary?.DirectEventChoice));
    }

    [Theory]
    [InlineData(FreePeoplesCharacterCards02.Alone)]
    [InlineData(FreePeoplesCharacterCards03.ThereAndBack)]
    [InlineData(FateCards.ThereAndBack)]
    public void MarksTheSeparationThatOpensAnEvent(string card)
    {
        var state = Separating(card);

        Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);
        AssertMarked(state, card);
    }

    // Gandalf and Strider leave together, so three Companions of level 2 can guide.
    private static MatchState GuideAfterSeparation() => KingsFixture.Do(Separating(FreePeoplesCharacterCards03.ThereAndBack),
        CommandKind.SeparateCompanions, new() { Ids = [CharacterRules.Grey, CharacterRules.Strider], Route = ["rivendell", "fords-of-bruinen"] });

    [Theory]
    [InlineData("")]
    [InlineData("bonus")]
    [InlineData("source")]
    [InlineData("choices")]
    public void MarksTheGuideChoiceAfterAnEventSeparation(string change)
    {
        var state = GuideAfterSeparation();
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        var procedure = state.Resolution.Frames[1].Fellowship!;
        switch (change)
        {
            case "bonus": procedure.SeparationBonus = 9; break;
            case "source": procedure.SourceId = "base.fp.character.11"; break;
            case "choices": state.Pending.PrivateChoices.RemoveAt(0); break;
        }

        if (change.Length == 0)
            AssertMarked(state, FreePeoplesCharacterCards03.ThereAndBack);
        else
            Assert.All(Enum.GetValues<Side>(), side =>
                Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary?.DirectEventChoice));
    }

    // Separating in Rivendell itself brings Gandalf to an Elven Stronghold, a political
    // consequence that waits for the action to complete.
    [Fact]
    public void MarksTheGuideChoiceWithAPendingPoliticalConsequence()
    {
        var state = KingsFixture.Do(Separating(FreePeoplesCharacterCards03.ThereAndBack),
            CommandKind.SeparateCompanions, new() { Ids = [CharacterRules.Grey, CharacterRules.Strider], Route = ["rivendell"] });

        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        Assert.NotEmpty(state.PoliticalReactions);
        AssertMarked(state, FreePeoplesCharacterCards03.ThereAndBack);
    }

    [Theory]
    [InlineData("erebor")]
    [InlineData("mount-gundabad")]
    public void MarksFateOrcsRecruitmentWithItsSecondRegion(string region)
    {
        var state = Played(FateCards.Orcs);
        Assert.Equal("Region", state.Pending!.Presentation);
        // Four Sauron Regulars wait in reserve, so the two regions must share them by choice.
        foreach (var unit in state.Units.Where(unit => unit.Nation == "sauron" && unit.Kind == UnitKind.Regular &&
                     unit.Location == "reserve").Skip(4).ToArray())
            unit.Location = "barad-dur";
        Assert.Equal(4, state.Units.Count(unit => unit.Nation == "sauron" && unit.Kind == UnitKind.Regular &&
            unit.Location == "reserve"));
        state = KingsFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = [region] });

        Assert.Equal("RecruitmentPlan", state.Pending!.Presentation);
        Assert.All(Enum.GetValues<Side>(), side =>
        {
            var choice = GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary?.DirectEventChoice;
            Assert.Equal(FateCards.Orcs, choice?.CardId);
            Assert.Equal(region, choice!.RegionId);
        });
    }

    [Theory]
    [InlineData("journey")]
    [InlineData("bonus")]
    [InlineData("origin")]
    [InlineData("source")]
    [InlineData("cursor")]
    public void TheSeparationRequiresItsExactFrames(string change)
    {
        var state = Separating(FreePeoplesCharacterCards03.ThereAndBack);
        var procedure = state.Resolution.Frames[1].Fellowship!;
        switch (change)
        {
            case "journey": procedure.Journey = new(); break;
            case "bonus": procedure.SeparationBonus = 9; break;
            case "origin": procedure.SeparationOrigin = "rivendell"; break;
            case "source": procedure.SourceId = "base.fp.character.11"; break;
            case "cursor": state.Resolution.Frames[0].NextStep = 0; break;
        }

        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary?.DirectEventChoice));
    }

    [Fact]
    public void MarksAidAsACompanionJourney()
    {
        var state = KingsCardsFixture.State();
        KingsCardsFixture.Companion(state, KingsCardsFixture.Boromir, "minas-tirith");
        EventTests.GiveCard(state, Side.FreePeoples, KingsFreePeoplesCards01.Aid);
        var die = state.Dice.First(item => item.Owner == Side.FreePeoples && item.Location == DieLocation.Available);
        die.Face = DieFace.Event;
        state = KingsFixture.Do(state, CommandKind.PlayEvent,
            new() { CardId = KingsFreePeoplesCards01.Aid, DieId = die.Id, CardCost = DieFace.Event });

        Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);
        Assert.All(Enum.GetValues<Side>(), side =>
        {
            var boundary = GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary!;
            Assert.Equal(KingsFreePeoplesCards01.Aid, boundary.CompanionJourney!.CardId);
            Assert.Null(boundary.DirectEventChoice);
        });
    }

    [Theory]
    [InlineData(KingsCards02.Dain)]
    [InlineData(KingsCards02.Thranduil)]
    [InlineData(KingsFreePeoplesCards01.Riders)]
    [InlineData(KingsFreePeoplesCards01.Brand)]
    public void MarksTheBranchAndRecruitmentOfAKingsNationalCard(string card)
    {
        var state = NationalCardBranch(card);

        Assert.Equal("SovereignCardBranch", state.Pending!.Presentation);
        Assert.Equal(2, state.Resolution.Frames.Count);
        AssertMarked(state, card, DieFace.Event);

        state = KingsFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = ["recruit"] });
        if (state.Pending?.Kind != DecisionKind.EffectChoice)
        {
            // A lone recruit is placed by the engine without a choice; Thranduil always offers two.
            Assert.NotEqual(KingsCards02.Thranduil, card);
            return;
        }
        Assert.Equal(3, state.Resolution.Frames.Count);
        AssertMarked(state, card, DieFace.Event);
    }

    [Theory]
    [InlineData("branch-trail")]
    [InlineData("nested-owner")]
    [InlineData("nested-program")]
    [InlineData("nested-cursor")]
    [InlineData("nested-region")]
    public void TheNationalCardBranchRequiresItsExactFrames(string change)
    {
        var state = NationalCardBranch(KingsCards02.Thranduil);
        var nested = state.Resolution.Frames[1];
        switch (change)
        {
            case "branch-trail": state.Resolution.Frames[0].Effect!.Committed.Add(new(Side.FreePeoples, KingsCards02.Thranduil, "recruit")); break;
            case "nested-owner": nested.Effect!.Owner = Side.Shadow; break;
            case "nested-program": nested.EffectId = KingsCards02.Dain + ".otherwise"; break;
            case "nested-cursor": nested.NextStep = 2; break;
            case "nested-region": nested.Effect!.RegionId = "dale"; break;
        }

        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary?.DirectEventChoice));
    }

    [Fact]
    public void MarksTheArmyMovementOfAnEvent()
    {
        var state = ArmyMovement();

        Assert.Equal(DecisionKind.ArmyEvent, state.Pending!.Kind);
        AssertMarked(state, ShadowStrategyCards02.Moving);
    }

    [Theory]
    [InlineData("order")]
    [InlineData("cursor")]
    [InlineData("presentation")]
    [InlineData("commands")]
    [InlineData("foreign-trail")]
    public void TheArmyMovementRequiresItsExactFrame(string change)
    {
        var state = ArmyMovement();
        var execution = state.Resolution.Frames[0].Effect!;
        switch (change)
        {
            case "order": execution.ArmyOrder = ArmyEventOrder.ReliefAttack; break;
            case "cursor": state.Resolution.Frames[0].NextStep = 0; break;
            case "presentation": state.Pending!.Presentation = "Altered"; break;
            case "commands": state.Pending!.Commands = [CommandKind.MoveArmies]; break;
            case "foreign-trail": execution.Committed.Add(new(Side.Shadow, "altered")); break;
        }

        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary?.DirectEventChoice));
    }

    [Fact]
    public void MarksTheLidlessEyeChoiceOfUnusedShadowDice()
    {
        var state = Played(ShadowCharacterCards03.Lidless);

        Assert.Equal("ActionDie", state.Pending!.Presentation);
        AssertMarked(state, ShadowCharacterCards03.Lidless);
    }

    [Fact]
    public void MarksTheDunlendingMoveIntoTheRecruitmentRegion()
    {
        var state = DunlendingMove();

        Assert.Equal(3, state.Resolution.Frames[0].NextStep);
        AssertMarked(state, ShadowStrategyCards02.Rage);
    }

    [Theory]
    [InlineData("region")]
    [InlineData("foreign-trail")]
    [InlineData("target")]
    public void TheDunlendingMoveRequiresItsPublicRegion(string change)
    {
        var state = DunlendingMove();
        var execution = state.Resolution.Frames[0].Effect!;
        switch (change)
        {
            case "region": execution.RegionId = ""; break;
            case "foreign-trail": execution.Committed.Add(new(Side.Shadow, "altered")); break;
            case "target": execution.TargetIds = ["altered"]; break;
        }

        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary?.DirectEventChoice));
    }

    [Theory]
    [InlineData("")]
    [InlineData(BalrogEventStep.Alone)]
    [InlineData(BalrogEventStep.Army)]
    public void MarksTheBalrogOrderAndTheTravelOrArmyItOpens(string choice)
    {
        var state = LordsShadowManeuverTests.BalrogChoice();
        Assert.Equal("BalrogOrder", state.Pending!.Presentation);
        Assert.Equal([BalrogEventStep.Decline, BalrogEventStep.Alone, BalrogEventStep.Army], state.Pending.PrivateChoices);
        if (choice.Length > 0)
            state = LordsCardsFixture.Choose(state, choice);

        Assert.Equal(choice switch
        {
            BalrogEventStep.Alone => DecisionKind.CharacterEvent,
            BalrogEventStep.Army => DecisionKind.ArmyEvent,
            _ => DecisionKind.EffectChoice
        }, state.Pending!.Kind);
        AssertMarked(state, LordsShadowCards.Balrog, DieFace.Event);
    }

    [Theory]
    [InlineData(BalrogEventStep.Alone, "swapped")]
    [InlineData(BalrogEventStep.Alone, "extra")]
    [InlineData(BalrogEventStep.Alone, "cursor")]
    [InlineData(BalrogEventStep.Army, "swapped")]
    [InlineData(BalrogEventStep.Army, "extra")]
    [InlineData(BalrogEventStep.Army, "cursor")]
    [InlineData(BalrogEventStep.Army, "order")]
    public void TheBalrogTravelOrArmyRequiresItsOwnersChoice(string choice, string change)
    {
        var state = LordsCardsFixture.Choose(LordsShadowManeuverTests.BalrogChoice(), choice);
        var frame = state.Resolution.Frames[0];
        var execution = frame.Effect!;
        var other = choice == BalrogEventStep.Alone ? BalrogEventStep.Army : BalrogEventStep.Alone;
        switch (change)
        {
            case "swapped": execution.Committed[^1] = execution.Committed[^1] with { ChoiceId = other }; break;
            case "extra": execution.Committed.Insert(1, new(Side.Shadow, LordsShadowCards.Balrog, BalrogEventStep.Decline)); break;
            case "cursor": frame.NextStep = 0; break;
            case "order": execution.ArmyOrder = ArmyEventOrder.DolGuldurManeuver; break;
        }

        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary?.DirectEventChoice));
    }

    [Fact]
    public void AStepTypeOutsideTheReviewRemainsUnmarked()
    {
        var program = EffectCatalog.All.Programs["base.fp.strategy.04"];
        Assert.DoesNotContain(Enumerable.Range(0, program.Steps.Count + 2), step =>
            DirectEventChoices.IsReviewed("base.fp.strategy.04", step, program));
    }

    private static void AssertMarked(MatchState state, string card, DieFace? cost = null)
    {
        Assert.All(Enum.GetValues<Side>(), side =>
        {
            var view = GameRuntime.Projection(EffectCatalog.All).For(state, side);
            Assert.Equal(49, view.ActionBoundary?.Version);
            var context = Assert.IsType<DirectEventChoiceView>(view.ActionBoundary?.DirectEventChoice);
            Assert.Equal(card, context.CardId);
            Assert.Equal(cost ?? CardCatalog.All[card].EventType, context.CardCost);
        });
    }

    // Rage of the Dunlendings chooses a region, recruits there, then offers the Dunland units.
    private static MatchState DunlendingMove()
    {
        var state = Played(ShadowStrategyCards02.Rage);
        while (state.Pending is { Kind: DecisionKind.EffectChoice } pending && pending.Presentation != "ArmyFigures")
            state = KingsFixture.Do(state, CommandKind.ChooseEffect,
                new() { Ids = [.. pending.PrivateChoices.Take(Math.Max(1, pending.RequiredCount))] });
        Assert.Equal("ArmyFigures", state.Pending!.Presentation);
        return state;
    }

    private static MatchState ArmyMovement() =>
        ShadowStrategy02Fixture.Play(ShadowStrategy02Fixture.State(ShadowStrategyCards02.Moving), ShadowStrategyCards02.Moving);

    private static MatchState NationalCardBranch(string card)
    {
        var state = KingsCardsFixture.State();
        EventTests.GiveCard(state, Side.FreePeoples, card);
        var die = state.Dice.First(item => item.Owner == Side.FreePeoples && item.Location == DieLocation.Available);
        die.Face = DieFace.Event;
        return KingsFixture.Do(state, CommandKind.PlayEvent, new() { CardId = card, DieId = die.Id, CardCost = DieFace.Event });
    }

    private static MatchState LossesOf(string card)
    {
        var state = Played(card);
        return KingsFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = [state.Pending!.PrivateChoices[0]] }, 5, 5, 5, 5, 5);
    }

    private static MatchState Separating(string card)
    {
        var state = card == FateCards.ThereAndBack ? FateCardsFixture.State(card) : BattleFixture.State();
        EventTests.GiveCard(state, Side.FreePeoples, card);
        var die = state.Dice.First(item => item.Owner == Side.FreePeoples && item.Location == DieLocation.Available);
        die.Face = DieFace.Character;
        return KingsFixture.Do(state, CommandKind.PlayEvent, new() { CardId = card, DieId = die.Id, CardCost = DieFace.Character });
    }

    private static MatchState GreyWindow(string card)
    {
        var state = BattleFixture.State();
        state.Fellowship.Guide = CharacterRules.Grey;
        EventTests.GiveCard(state, Side.FreePeoples, card);
        var die = state.Dice.First(item => item.Owner == Side.FreePeoples && item.Location == DieLocation.Available);
        die.Face = DieFace.Event;
        state = KingsFixture.Do(state, CommandKind.PlayEvent, new() { CardId = card, DieId = die.Id, CardCost = DieFace.Event });
        while (state.Pending!.Kind == DecisionKind.EffectChoice)
            state = KingsFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = [state.Pending.PrivateChoices[0]] });
        return state;
    }

    private static MatchState Played(string card)
    {
        var state = Prepared(card);
        var side = CardCatalog.All[card].Side;
        EventTests.GiveCard(state, side, card);
        var die = state.Dice.First(item => item.Owner == side && item.Location == DieLocation.Available);
        var cost = CardCatalog.All[card].EventType;
        die.Face = cost;
        return KingsFixture.Do(state, CommandKind.PlayEvent, new() { CardId = card, DieId = die.Id, CardCost = cost });
    }

    private static MatchState Prepared(string card)
    {
        switch (card)
        {
            case KingsCards02.Villages: return KingsCardsFixture.State();
            case KingsCards04.Enemy: return KingsCardsFixture.State(Side.Shadow);
            case FateCards.Dwarves or FateCards.Orcs: return FateCardsFixture.State(card);
        }

        var state = BattleFixture.State(CardCatalog.All[card].Side);
        switch (card)
        {
            case FreePeoplesStrategyCards01.SpiritOfMordor:
                foreach (var region in new[] { "orthanc", "dol-guldur" })
                {
                    BattleFixture.Put(state, "isengard", UnitKind.Regular, region);
                    BattleFixture.Put(state, "sauron", UnitKind.Regular, region);
                }
                break;
            case FreePeoplesStrategyCards01.FaramirsRangers:
                BattleFixture.Put(state, "sauron", UnitKind.Regular, "north-ithilien");
                BattleFixture.Put(state, "sauron", UnitKind.Regular, "south-ithilien");
                BattleFixture.Put(state, "gondor", UnitKind.Regular, "osgiliath");
                break;
            case FreePeoplesStrategyCards02.RedArrow:
                state.Nations.Single(nation => nation.Id == "gondor").Active = true;
                break;
            case CirdansShips:
                BattleFixture.Put(state, "elves", UnitKind.Regular, "grey-havens");
                BattleFixture.Put(state, "gondor", UnitKind.Regular, "dol-amroth");
                break;
            case Stewards:
                CharacterFixture.Put(state, KingsCardsFixture.Boromir, "minas-tirith");
                break;
            case ShadowStrategyCards02.Rage:
                var isengard = state.Nations.Single(nation => nation.Id == "isengard");
                isengard.Active = true;
                isengard.StepsToWar = 0;
                BattleFixture.Put(state, "isengard", UnitKind.Regular, "north-dunland");
                BattleFixture.Put(state, "isengard", UnitKind.Elite, "south-dunland");
                break;
            case ShadowStrategyCards03.HillTrolls:
                state.Nations.Single(nation => nation.Id == "sauron").StepsToWar = 0;
                for (var count = 0; count < 3; count++)
                    BattleFixture.Put(state, "sauron", UnitKind.Regular, "barad-dur");
                break;
            case ShadowStrategyCards03.NewPower:
                CharacterFixture.Put(state, CharacterRules.Saruman, "orthanc");
                break;
            case HalfOrcs:
                BattleFixture.Put(state, "isengard", UnitKind.Regular, "orthanc");
                BattleFixture.Put(state, "isengard", UnitKind.Regular, "north-dunland");
                state.Nations.Single(nation => nation.Id == "isengard").StepsToWar = 0;
                break;
            case ShadowStrategyCards04.Horde:
                state.Nations.Single(nation => nation.Id == "southrons").StepsToWar = 0;
                break;
            case ShadowCharacterCards02.Weather:
                state.Fellowship.Progress = 1;
                break;
            case ShadowCharacterCards04.Captain:
                CharacterFixture.Put(state, CharacterRules.WitchKing, "angmar");
                break;
        }
        return state;
    }
}
