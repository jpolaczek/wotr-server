using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.LordsCardEndToEndScenarioTests;
using static Wotr.Tests.LordsMechanicScenarioTests;

namespace Wotr.Tests;

// TEST-12: the Council of Rivendell scenarios and the Lords + Kings integration scenarios sit in the Lords tab (Lords
// first, Kings after it where the game plays with Kings), offer their mechanic as a legal option of the first decision,
// and committing it runs through the engine with the rule's result.
public class LordsCouncilScenarioTests
{
    private const string Boromir = "base.character.boromir";

    public static TheoryData<string> Ids => new(LordsCouncilScenarios.All().Concat(LordsKingsScenarios.All()).Select(s => s.Id));

    [Theory]
    [MemberData(nameof(Ids))]
    public void TheScenarioIsInTheLordsTabAndRepeatable(string id)
    {
        var scenario = Scenario(id);
        Assert.Equal(id.StartsWith("lords-kings-", StringComparison.Ordinal) ? [ExpansionModules.Lords, ExpansionModules.Kings]
            : [ExpansionModules.Lords], scenario.Modules);
        var state = Create(id);
        Assert.True(state.Configuration.Council || id.StartsWith("lords-kings-", StringComparison.Ordinal));
        Assert.Equal(StateCodec.Write(state), StateCodec.Write(Create(id)));
    }

    [Fact]
    public void TheCouncilSetupOffersEveryAlternativeCompanion()
    {
        var state = Create("lords-council-setup");
        var versions = Option(state, Side.FreePeoples, CommandKind.ChooseCompanionVersions).CompanionVersions;
        Assert.Equal(7, versions.Count);
        state = Run(state, Side.FreePeoples, CommandKind.ChooseCompanionVersions,
            new() { GuideId = CharacterRules.Strider, Ids = [CharacterRules.Grey, "lords.character.boromir", "lords.character.legolas"] },
            new SeededRandom(1));
        Assert.Equal(DecisionKind.CouncilHomelands, state.Pending!.Kind);
    }

    [Fact]
    public void OneCompanionOutsideLetsTheShadowChooseOneToken()
    {
        var state = Create("lords-council-homelands");
        state = Run(state, Side.FreePeoples, CommandKind.ChooseCouncilHomelands,
            new() { Placements = [new() { UnitId = Boromir, RegionId = "minas-tirith" }] }, new SeededRandom(1));
        Assert.Equal("minas-tirith", state.Characters.Single(c => c.Id == Boromir).Location);
        Assert.Equal((DecisionKind.CouncilToken, Side.Shadow), (state.Pending!.Kind, state.Pending.Owner));
    }

    [Fact]
    public void TheShadowTakesTheTokenItChooses()
    {
        var state = Create("lords-council-token-choice");
        state = Run(state, Side.Shadow, CommandKind.ChooseCouncilToken, new() { Ids = [CouncilTokens.Travel] }, new SeededRandom(1));
        Assert.Equal([CouncilTokens.Travel], state.Lords!.Council!.GrantedTokens);
    }

    [Fact]
    public void TheTravelTokenMovesANazgulWithoutADie()
    {
        var state = Create("lords-council-token-travel");
        Assert.Equal(CouncilTokens.All.Order(), state.Lords!.Council!.GrantedTokens.Order());
        Option(state, Side.Shadow, CommandKind.UseCouncilToken);
        var nazgul = state.Units.First(u => u.Kind == UnitKind.Nazgul && BoardRules.Regions.ContainsKey(u.Location));
        var dice = state.Dice.Count(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        state = Run(state, Side.Shadow, CommandKind.UseCouncilToken, new()
        {
            AbilityId = CouncilTokens.Travel, CompanionMoves = [new() { Ids = [nazgul.Id], Route = [nazgul.Location, "orthanc"] }]
        }, new SeededRandom(1));
        Assert.Equal("orthanc", state.Units.Single(u => u.Id == nazgul.Id).Location);
        Assert.Equal(dice, state.Dice.Count(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available));
    }

    [Theory]
    [InlineData(Boromir, "messenger-gondor", "gondor")]
    [InlineData("base.character.gimli", "messenger-dwarves", "dwarves")]
    [InlineData("base.character.merry", "messenger-north", "north")]
    [InlineData("base.character.legolas", "prince-of-mirkwood", "elves")]
    public void EachCouncilMessengerAdvancesItsNationWithAnyDie(string figure, string ability, string nation)
    {
        var state = Create("lords-council-messengers");
        var action = Option(state, Side.FreePeoples, CommandKind.UseCharacterAbility).CharacterActions
            .Single(a => a.CharacterId == figure && a.AbilityId == ability);
        var before = state.Nations.Single(n => n.Id == nation).StepsToWar;
        state = Run(state, Side.FreePeoples, CommandKind.UseCharacterAbility,
            new() { DieId = action.Dice[0], CharacterId = figure, AbilityId = ability, Ids = [.. action.Targets] }, new SeededRandom(1));
        var after = state.Nations.Single(n => n.Id == nation);
        Assert.True(after.Active);
        Assert.Equal(before - 1, after.StepsToWar);
    }

    [Fact]
    public void TheAlternativeBoromirIsGondorsChampionInItsRegion()
    {
        var state = Create("lords-council-champion");
        var boromir = state.Characters.Single(c => c.Id == Boromir);
        Assert.Contains(boromir.Id, BattleRuntime.Active(state)!.Defender.Participants);
        Assert.Equal(1, CouncilAbilities.ChampionBonus(state, boromir));
        Run(state, Side.FreePeoples, CommandKind.CommitCombatCard, new() { CardId = "" }, new SeededRandom(1));
    }

    [Fact]
    public void TheAlternativeGuideSeparatesToLessenTheHunt()
    {
        var state = Move(Create("lords-council-guide-separate"));
        Assert.Equal("hunt.guide", state.Pending!.Window);
        var damage = HuntRuntime.Active(state)!.Damage;
        state = Run(state, Side.FreePeoples, CommandKind.RespondEffect, new() { ReactionId = "council.boromir-guide" }, new SeededRandom(1));
        Assert.Equal(damage - 1, HuntRuntime.Active(state)!.Damage);
    }

    [Fact]
    public void TheAlternativeHobbitGuideCancelsTheStandardTile()
    {
        var state = Move(Create("lords-council-hobbit-guide"));
        Assert.Equal(DecisionKind.Reaction, state.Pending!.Kind);
        state = Run(state, Side.FreePeoples, CommandKind.RespondEffect,
            new() { ReactionId = CouncilGuideReactions.Cancel + "." + CouncilCompanions.Merry }, new SeededRandom(1));
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.True(state.Fellowship.Hidden);
    }

    [Fact]
    public void TheHobbitsDisappearFromTheAnnihilatedArmy()
    {
        var state = Create("lords-council-art-of-disappearing");
        var regular = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.FreePeoples)).Single(BoardRules.IsArmyUnit);
        state = Run(state, Side.FreePeoples, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(regular.Id)] }, new SeededRandom(1));
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.NotEmpty(state.Lords!.Council!.BattleReturns);
    }

    [Fact]
    public void TheAlternativeStridersFirstMoveDieGoesToTheFellowshipBox()
    {
        var state = Create("lords-council-strider-guide");
        Assert.Equal(CouncilCompanions.Strider, CharacterRules.GuideDefinition(state));
        var die = Die(state, Side.FreePeoples, DieFace.Character);
        state = Run(state, Side.FreePeoples, CommandKind.MoveFellowship, new() { DieId = die.Id }, new SequenceRandom(0, 0, 0, 0, 0, 0));
        Assert.Equal(DieLocation.FellowshipBox, state.Dice.Single(d => d.Id == die.Id).Location);
    }

    [Fact]
    public void TheAlternativeStriderBecomesAragornOnHisFigure()
    {
        var state = Create("lords-council-strider-aragorn");
        var plan = CharacterEntry.Candidates(state, Side.FreePeoples).First(p => p.DefinitionId == CharacterRules.Aragorn);
        state = Run(state, Side.FreePeoples, CommandKind.EnterCharacter, new()
        {
            DieId = Die(state, Side.FreePeoples, DieFace.WillOfTheWest).Id, CharacterId = CharacterRules.Aragorn, RegionId = plan.Region
        }, new SeededRandom(1));
        Assert.Equal(CharacterRules.Aragorn, state.Characters.Single(c => c.Id == CharacterRules.Strider).DefinitionId);
    }

    [Fact]
    public void ACorruptionAttemptRedrawsSmeagolsTiles()
    {
        var state = Create("lords-kings-smeagol-corruption");
        Assert.Equal(4, state.HuntTiles.Count(t => t.Location == "pool" && Smeagol(t.Id)));
        var option = Option(state, Side.Shadow, CommandKind.AttemptCorruption);
        var sovereign = option.EligibleIds[0];
        // The pool lists the numbered tile first, so drawing the last tile meets every Sméagol tile before it.
        state = Run(state, Side.Shadow, CommandKind.AttemptCorruption, new() { DieId = option.EligibleDice[0], CharacterId = sovereign },
            new HighestFace());
        var attempt = CorruptionRuntime.Current(state)!;
        Assert.Contains(attempt.Ignored, Smeagol);
        Assert.DoesNotContain(attempt.Drawn, Smeagol);
        state = Complete(state);
        Assert.DoesNotContain(state.Characters, c => c.DefinitionId == LordsCharacters.Smeagol);
        Assert.False(Smeagol(Assert.Single(state.Kings!.Sovereigns.Single(s => s.Id == sovereign).Tiles)));
        Assert.DoesNotContain(state.HuntTiles, t => t.Location == "removed" && Smeagol(t.Id));
    }

    [Fact]
    public void TheCaptainsEyeNeitherPaysNorRaisesTheDraws()
    {
        var state = Create("lords-kings-captain-corruption");
        var captain = state.Dice.Single(d => d.HuntOrigin == CaptainOfDespair.HuntOrigin);
        Assert.DoesNotContain(captain.Id, CorruptionRules.Eyes(state).Select(d => d.Id));
        Assert.DoesNotContain(captain.Id, Option(state, Side.Shadow, CommandKind.AttemptCorruption).EligibleDice);
        state = Attempt(state);
        Assert.Equal(2, CorruptionRuntime.Current(state)!.DrawLimit);
    }

    [Fact]
    public void AKeeperEyePaysTheAttemptAndJoinsTheFreePeoplesUsedDice()
    {
        var state = Create("lords-kings-keeper-eye-corruption");
        var option = Option(state, Side.Shadow, CommandKind.AttemptCorruption);
        Assert.Contains(LordsDiceCatalog.Vilya, option.EligibleDice);
        state = Run(state, Side.Shadow, CommandKind.AttemptCorruption,
            new() { DieId = LordsDiceCatalog.Vilya, CharacterId = option.EligibleIds[0] }, new SeededRandom(1));
        state = Complete(state);
        Assert.Equal(Side.FreePeoples, state.Dice.Single(d => d.Id == LordsDiceCatalog.Vilya).SpentWith);
    }

    [Fact]
    public void ACouncilTokenBringsTheBlackSerpentInWithoutHisCondition()
    {
        var state = Create("lords-kings-council-chieftain");
        Assert.False(KingsChieftains.Prerequisite(state, KingsChieftains.Serpent));
        var action = Option(state, Side.Shadow, CommandKind.EnterCharacter).CharacterActions.Single(a => a.CharacterId == KingsChieftains.Serpent);
        Assert.True(action.RequiresCouncilToken);
        state = Run(state, Side.Shadow, CommandKind.EnterCharacter, new()
        {
            CharacterId = KingsChieftains.Serpent, RegionId = action.Targets[0], CouncilTokenId = action.CouncilTokens[0],
            DieId = Die(state, Side.Shadow, DieFace.Muster).Id
        }, new SeededRandom(1));
        Assert.Equal(action.Targets[0], state.Characters.Single(c => c.Id == KingsChieftains.Serpent).Location);
        Assert.Single(state.Lords!.Council!.SpentTokens);
    }

    [Fact]
    public void OnlyACompanionWithTheRightIconRemovesTheCorruptedDenethor()
    {
        var state = Create("lords-kings-iconless-cleanse");
        Assert.Equal("minas-tirith", state.Characters.Single(c => c.Id == Boromir).Location);
        Assert.DoesNotContain(Views(state).Options, o => o.Kind == CommandKind.RemoveCorruptedSovereign && o.Enabled);
        state = Run(state, Side.FreePeoples, CommandKind.MoveCompanions, new()
        {
            DieId = Die(state, Side.FreePeoples, DieFace.Character).Id,
            CompanionMoves = [new() { Ids = [CharacterRules.Grey], Route = ["osgiliath", "minas-tirith"] }]
        }, new SeededRandom(1));
        state = ShadowToken(state);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        state = Run(state, Side.FreePeoples, CommandKind.RemoveCorruptedSovereign,
            new() { DieId = Die(state, Side.FreePeoples, DieFace.Character).Id, CharacterId = SovereignCatalog.Denethor }, new SeededRandom(1));
        Assert.NotEqual(SovereignStatus.Corrupted, state.Kings!.Sovereigns.Single(s => s.Id == SovereignCatalog.Denethor).Status);
    }

    // The Shadow, out of dice, may still spend its Council token while the Free Peoples have dice.
    private static MatchState ShadowToken(MatchState state)
    {
        if (state.Pending?.Owner != Side.Shadow)
            return state;
        var option = Option(state, Side.Shadow, CommandKind.UseCouncilToken);
        return Run(state, Side.Shadow, CommandKind.UseCouncilToken,
            new() { AbilityId = CouncilTokens.Politics, NationId = option.Targets[0] }, new SeededRandom(1));
    }

    private static bool Smeagol(string tile) => HuntPool.Definitions[tile].Pool == SmeagolTiles.Pool;

    private static PlayerView Views(MatchState state) => GameRuntime.Projection(EffectCatalog.All).For(state, Side.FreePeoples);

    private static MatchState Attempt(MatchState state)
    {
        var option = Option(state, Side.Shadow, CommandKind.AttemptCorruption);
        return Run(state, Side.Shadow, CommandKind.AttemptCorruption,
            new() { DieId = option.EligibleDice[0], CharacterId = option.EligibleIds[0] }, new SeededRandom(1));
    }

    // The Shadow draws every tile the attempt allows, then attaches the first one drawn.
    private static MatchState Complete(MatchState state)
    {
        for (var step = 0; step < 6 && CorruptionRuntime.Current(state) is { } attempt; step++)
            state = CorruptionRules.CanDraw(attempt)
                ? Run(state, Side.Shadow, CommandKind.DrawCorruptionTile, new(), new SeededRandom(step))
                : Run(state, Side.Shadow, CommandKind.ChooseCorruptionTile, new() { Ids = [CorruptionRules.Choices(attempt)[0]] }, new SeededRandom(step));
        Assert.Null(CorruptionRuntime.Current(state));
        return state;
    }

    private static MatchState Move(MatchState state) => Run(state, Side.FreePeoples, CommandKind.MoveFellowship,
        new() { DieId = Die(state, Side.FreePeoples, DieFace.Character).Id }, new HighestFace());
}
