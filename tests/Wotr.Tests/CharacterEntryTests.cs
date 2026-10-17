using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

public class CharacterEntryTests
{
    [Theory]
    [InlineData(CharacterRules.Saruman, "orthanc", "isengard")]
    [InlineData(CharacterRules.WitchKing, "minas-morgul", "sauron")]
    [InlineData(CharacterRules.Mouth, "barad-dur", "sauron")]
    public void MinionsRequireTheirOwnEntryConditionsAndCannotEnterAgainAfterDeath(string id, string target, string nation)
    {
        var state = TurnTests.Actions(actor: Side.Shadow);
        var p = Cost(state, DieFace.Muster);
        p.CharacterId = id;
        p.RegionId = target;
        Reject(state, CommandKind.EnterCharacter, p);
        if (id == CharacterRules.Mouth)
            state.Fellowship.MordorStep = 0;
        else
            state.Nations.Single(n => n.Id == nation).StepsToWar = 0;
        if (id == CharacterRules.WitchKing)
            state.Nations.Single(n => n.Id == "elves").StepsToWar = 0;
        var before = state.Dice.Count(d => d.Owner == Side.Shadow && d.Location != DieLocation.Removed);
        state = Do(state, CommandKind.EnterCharacter, p);
        var character = Assert.Single(state.Characters);
        Assert.Equal(target, character.Location);
        Assert.Equal(1, state.Player(Side.Shadow).ExtraDiceNextRecovery);
        Assert.Equal(before, state.Dice.Count(d => d.Owner == Side.Shadow && d.Location != DieLocation.Removed));
        Assert.True(state.MinionEverEntered);
        if (id == CharacterRules.WitchKing)
            Assert.All(state.Nations.Where(n => BoardRules.Nations[n.Id].Side == Side.FreePeoples), n => Assert.True(n.Active));
        if (id == CharacterRules.Mouth)
            Assert.NotEqual(0, state.Nations.Single(n => n.Id == "sauron").StepsToWar);
        DiceRules.Recover(state);
        Assert.Equal(8, state.Dice.Count(d => d.Owner == Side.Shadow && d.Location == DieLocation.Unrolled));
        CharacterLifecycle.Eliminate(state, character);
        Assert.Equal(8, state.Dice.Count(d => d.Owner == Side.Shadow && d.Location == DieLocation.Unrolled));
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.Shadow), c => c.DefinitionId == id);
        DiceRules.Recover(state);
        Assert.Equal(7, state.Dice.Count(d => d.Owner == Side.Shadow && d.Location == DieLocation.Unrolled));
    }

    [Fact]
    public void WitchKingNeedsASauronArmyUnitAndMouthCanUseAllFreeNationsAtWar()
    {
        var state = BattleFixture.State(Side.Shadow);
        BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "orthanc");
        BattleFixture.Put(state, "isengard", UnitKind.Elite, "orthanc");
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.Shadow), c => c.DefinitionId == CharacterRules.WitchKing);
        Assert.Contains(CharacterEntry.Candidates(state, Side.Shadow), c => c.DefinitionId == CharacterRules.Mouth && c.Region == "barad-dur");
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.Shadow), c => c.DefinitionId == CharacterRules.Mouth && c.Region == "orthanc");
        var saur = BattleFixture.Put(state, "sauron", UnitKind.Regular, "orthanc");
        saur.Garrison = true;
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "orthanc");
        var entry = Assert.Single(CharacterEntry.Candidates(state, Side.Shadow), c => c.DefinitionId == CharacterRules.WitchKing);
        Assert.True(entry.Garrison);
    }

    [Theory]
    [InlineData("minas-tirith")]
    [InlineData("dol-amroth")]
    [InlineData("pelargir")]
    public void AragornPreservesStridersFigureAndNeedsWillButNotWar(string region)
    {
        var state = TurnTests.Actions();
        var strider = Put(state, CharacterRules.Strider, region);
        var p = Cost(state, DieFace.Character);
        p.CharacterId = CharacterRules.Aragorn;
        p.RegionId = region;
        Reject(state, CommandKind.EnterCharacter, p);
        state.Dice.Single(d => d.Id == p.DieId).Face = DieFace.WillOfTheWest;
        state.Control[region] = Side.Shadow;
        Reject(state, CommandKind.EnterCharacter, p);
        state.Control[region] = Side.FreePeoples;
        state = Do(state, CommandKind.EnterCharacter, p);
        var aragorn = Assert.Single(state.Characters);
        Assert.Equal(strider.Id, aragorn.Id);
        Assert.Equal(CharacterRules.Aragorn, aragorn.DefinitionId);
        Assert.Equal(2, aragorn.Leadership);
        Assert.Equal(1, state.Player(Side.FreePeoples).ExtraDiceNextRecovery);
        Assert.True(state.Nations.Single(n => n.Id == "gondor").Active);
        Assert.NotEqual(0, state.Nations.Single(n => n.Id == "gondor").StepsToWar);
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.FreePeoples), c => c.DefinitionId == CharacterRules.Aragorn);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WhiteGandalfRequiresHistoricalMinionEntryAndReturnsOrReplacesTheSameFigure(bool dead)
    {
        var state = TurnTests.Actions();
        var grey = Put(state, CharacterRules.Grey, dead ? "casualties" : "fangorn");
        Assert.Empty(CharacterEntry.Candidates(state, Side.FreePeoples));
        state.MinionEverEntered = true;
        var p = Cost(state, DieFace.WillOfTheWest);
        p.CharacterId = CharacterRules.White;
        p.RegionId = "fangorn";
        state = Do(state, CommandKind.EnterCharacter, p);
        var white = Assert.Single(state.Characters);
        Assert.Equal(grey.Id, white.Id);
        Assert.Equal(CharacterRules.White, white.DefinitionId);
        Assert.Equal(1, state.Player(Side.FreePeoples).ExtraDiceNextRecovery);
        Assert.Equal(4, state.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location != DieLocation.Removed));
        DiceRules.Recover(state);
        Assert.Equal(5, state.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Unrolled));
        CharacterLifecycle.Eliminate(state, white);
        Assert.Empty(CharacterEntry.Candidates(state, Side.FreePeoples));
        Assert.True(state.MinionEverEntered);
    }

    [Fact]
    public void GandalfCannotTransformInsideFellowshipAndCanReturnInsideUnconqueredBesiegedElfStronghold()
    {
        var state = TurnTests.Actions();
        state.MinionEverEntered = true;
        Assert.Empty(CharacterEntry.Candidates(state, Side.FreePeoples));
        Put(state, CharacterRules.Grey, "casualties");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "lorien");
        Assert.Contains(CharacterEntry.Candidates(state, Side.FreePeoples), p => p.Region == "lorien" && p.Garrison);
        state.Control["lorien"] = Side.Shadow;
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.FreePeoples), p => p.Region == "lorien");
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.FreePeoples), p => p.Region == "minas-tirith");
    }

    [Fact]
    public void SevenCompanionsHaveReviewedStatisticsAndRingbearersAddGollumWithoutACompanion()
    {
        var state = TurnTests.Actions();
        Assert.Equal(7, CompanionRules.Inside(state).Count);
        foreach (var c in CompanionRules.Inside(state))
        {
            var definition = CharacterCatalog.Base[c.DefinitionId];
            Assert.Equal(definition.Level, c.Level);
            Assert.Equal(definition.Leadership, c.Leadership);
        }
        state.Fellowship.Companions.Clear();
        FellowshipFlow.CompositionChanged(state, FellowshipReturn.Decision);
        Assert.Equal(CharacterRules.Gollum, state.Fellowship.Guide);
        Assert.Empty(state.Fellowship.Companions);
        DiceRules.Recover(state);
        Assert.Equal(1, DiceRules.HuntMaximum(state));
        PoliticalRules.FellowshipDeclared(state, "minas-tirith");
        Assert.True(state.Nations.Single(n => n.Id == "gondor").Active);
    }
}
