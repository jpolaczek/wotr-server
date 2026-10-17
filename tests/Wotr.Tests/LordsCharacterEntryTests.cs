using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.LordsCharacterFixture;

namespace Wotr.Tests;

public class LordsCharacterEntryTests
{
    [Theory]
    [InlineData(LordsCharacters.Elrond, "rivendell", "sauron")]
    [InlineData(LordsCharacters.Galadriel, "lorien", "elves")]
    public void KeepersRequireWarAndUnconqueredHomeButCanEnterAfterRecaptureAndDuringSiege(string id, string home, string nation)
    {
        var state = State(emptyBoard: true);
        var payload = Entry(state, id, home);
        LordsFixture.Reject(state, CommandKind.EnterCharacter, payload);
        War(state, nation);
        state.Control[home] = Side.Shadow;
        LordsFixture.Reject(state, CommandKind.EnterCharacter, payload);
        state.Control[home] = Side.FreePeoples;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, home);
        state = LordsFixture.Do(state, CommandKind.EnterCharacter, payload);
        var keeper = Assert.Single(state.Characters);
        Assert.True(keeper.Garrison);
        Assert.Equal(0, keeper.Level);
        Assert.Equal(3, CharacterRules.CombatLevel(keeper));
        Assert.Equal(2, keeper.Leadership);
        Assert.False(CharacterRules.Mobile(keeper));
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.FreePeoples), e => e.DefinitionId == id);
    }

    [Theory]
    [InlineData(LordsCharacters.Elrond, "rivendell", LordsDiceCatalog.Vilya)]
    [InlineData(LordsCharacters.Galadriel, "lorien", LordsDiceCatalog.Nenya)]
    public void CaptureEliminatesKeeperButAvailableSpecialDieSurvivesUntilRecovery(string id, string home, string dieId)
    {
        var state = State(emptyBoard: true);
        Put(state, id, home);
        var die = LordsFixture.Die(state, dieId);
        die.Location = DieLocation.Available;
        die.Face = DieFace.Muster;
        TerritoryRules.Capture(state, Side.Shadow, home);
        Assert.Equal("casualties", Assert.Single(state.Characters).Location);
        Assert.Equal(DieLocation.Available, die.Location);
        state.Control[home] = Side.FreePeoples;
        War(state, "elves");
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.FreePeoples), e => e.DefinitionId == id);
        DiceRules.Recover(state);
        Assert.Equal(DieLocation.Removed, die.Location);
        Assert.True(die.Special!.PermanentlyRemoved);
    }

    [Fact]
    public void ChiefEntersWithoutSauronAtWarAndOneReplacementKeepsFigureAndLocation()
    {
        var state = State(Side.Shadow);
        var payload = Entry(state, LordsCharacters.WitchKing, state.Fellowship.RegionId);
        LordsFixture.Reject(state, CommandKind.EnterCharacter, payload);
        state.Fellowship.RegionId = "old-forest";
        payload.RegionId = "old-forest";
        state = LordsFixture.Do(state, CommandKind.EnterCharacter, payload);
        var chief = Assert.Single(state.Characters);
        Assert.Equal(CharacterRules.WitchKing, chief.Id);
        Assert.Equal(1, chief.Leadership);
        Assert.False(state.MinionEverEntered);
        Assert.Equal(1, state.Player(Side.Shadow).ExtraDiceNextRecovery);
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.Shadow), e => e.DefinitionId == CharacterRules.WitchKing);
        War(state, "sauron", "elves");
        TurnFlow.SelectNext(state, Side.Shadow);
        payload = Entry(state, CharacterRules.WitchKing, chief.Location);
        state = LordsFixture.Do(state, CommandKind.EnterCharacter, payload);
        var captain = Assert.Single(state.Characters);
        Assert.Equal(chief.Id, captain.Id);
        Assert.Equal(chief.Location, captain.Location);
        Assert.Equal(2, captain.Leadership);
        Assert.True(state.Lords!.WitchKingReplaced);
        Assert.True(state.MinionEverEntered);
        state = StateCodec.Read(StateCodec.Write(state));
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.Shadow), e => e.DefinitionId == LordsCharacters.WitchKing);
    }

    [Fact]
    public void BlackCaptainCanBecomeChiefOnceButNeitherVersionCanReturnAfterDeath()
    {
        var state = State(Side.Shadow);
        Put(state, CharacterRules.WitchKing, "minas-morgul");
        state.Fellowship.RegionId = "rivendell";
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.Shadow), e => e.DefinitionId == LordsCharacters.WitchKing);
        state.Control["rivendell"] = Side.Shadow;
        var payload = Entry(state, LordsCharacters.WitchKing, "minas-morgul");
        state = LordsFixture.Do(state, CommandKind.EnterCharacter, payload);
        var chief = Assert.Single(state.Characters);
        Assert.Equal("minas-morgul", chief.Location);
        Assert.Equal(CharacterRules.WitchKing, chief.Id);
        CharacterLifecycle.Eliminate(state, chief);
        War(state, "sauron", "elves");
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.Shadow), e => e.DefinitionId is CharacterRules.WitchKing or LordsCharacters.WitchKing);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BlackNumenoreanRequiresVictoryPointOrMordorAndBlocksBothMouthVersions(bool mordor)
    {
        var state = State(Side.Shadow);
        var payload = Entry(state, LordsCharacters.Mouth, "barad-dur");
        LordsFixture.Reject(state, CommandKind.EnterCharacter, payload);
        if (mordor)
            state.Fellowship.MordorStep = 0;
        else
            state.Control["dol-guldur"] = Side.FreePeoples;
        state.Control["barad-dur"] = Side.FreePeoples;
        LordsFixture.Reject(state, CommandKind.EnterCharacter, payload);
        state.Control["barad-dur"] = Side.Shadow;
        state = LordsFixture.Do(state, CommandKind.EnterCharacter, payload);
        var mouth = Assert.Single(state.Characters);
        Assert.Equal(CharacterRules.Mouth, mouth.Id);
        Assert.True(state.MinionEverEntered);
        Assert.Equal(1, state.Player(Side.Shadow).ExtraDiceNextRecovery);
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.Shadow), e => e.DefinitionId is CharacterRules.Mouth or LordsCharacters.Mouth);
        CharacterLifecycle.Eliminate(state, mouth);
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.Shadow), e => e.DefinitionId is CharacterRules.Mouth or LordsCharacters.Mouth);
    }

    [Theory]
    [InlineData(LordsCharacters.Balrog, false)]
    [InlineData(LordsCharacters.Gothmog, false)]
    [InlineData(LordsCharacters.WitchKing, false)]
    [InlineData(LordsCharacters.Mouth, true)]
    public void WhiteUsesQualifyingMinionHistoryAndKeepsKeeperGandalfsFigure(string minion, bool qualifies)
    {
        var state = State();
        var gandalf = Put(state, LordsCharacters.Gandalf, "fangorn", CharacterRules.Grey);
        var enemy = Put(state, minion, "moria");
        Assert.Equal(qualifies, CharacterEntry.Candidates(state, Side.FreePeoples).Any(e => e.DefinitionId == CharacterRules.White));
        CharacterLifecycle.Eliminate(state, enemy);
        Assert.Equal(qualifies, CharacterEntry.Candidates(state, Side.FreePeoples).Any(e => e.DefinitionId == CharacterRules.White));
        if (!qualifies)
            return;
        var payload = Entry(state, CharacterRules.White, "fangorn");
        state.Dice.Single(d => d.Id == payload.DieId).Face = DieFace.WillOfTheWest;
        state = LordsFixture.Do(state, CommandKind.EnterCharacter, payload);
        Assert.Equal(CharacterRules.White, state.Characters.Single(c => c.Id == gandalf.Id).DefinitionId);
        Assert.Equal(2, state.Characters.Count);
    }

    [Fact]
    public void GothmogRequiresWarAndHomeControlAndOnlyBlackCaptainPreventsEntry()
    {
        var state = State(Side.Shadow);
        var payload = Entry(state, LordsCharacters.Gothmog, "minas-morgul");
        LordsFixture.Reject(state, CommandKind.EnterCharacter, payload);
        War(state, "sauron");
        state.Control["minas-morgul"] = Side.FreePeoples;
        LordsFixture.Reject(state, CommandKind.EnterCharacter, payload);
        state.Control["minas-morgul"] = Side.Shadow;
        var king = Put(state, CharacterRules.WitchKing, "barad-dur");
        LordsFixture.Reject(state, CommandKind.EnterCharacter, payload);
        CharacterLifecycle.ApplyVersion(king, CharacterCatalog.All[LordsCharacters.WitchKing]);
        state = LordsFixture.Do(state, CommandKind.EnterCharacter, payload);
        Assert.Equal(3, CharacterRules.InPlay(state, LordsCharacters.Gothmog)!.Level);
        Assert.False(state.MinionEverEntered);
        CharacterLifecycle.Eliminate(state, CharacterRules.InPlay(state, LordsCharacters.Gothmog)!);
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.Shadow), e => e.DefinitionId == LordsCharacters.Gothmog);
    }

    [Fact]
    public void BaseConfigurationOffersNoLordsEntriesOrAlternativeDefinitions()
    {
        var state = State(Side.Shadow);
        state.Configuration.Lords = false;
        War(state, "sauron", "elves");
        state.Fellowship.RegionId = "old-forest";
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.Shadow), e => e.DefinitionId.StartsWith("lords."));
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.FreePeoples), e => e.DefinitionId.StartsWith("lords."));
        Assert.Equal(14, CharacterCatalog.Base.Count);
        Assert.Equal(14, CharacterCatalog.Lords.Count);
    }
}
