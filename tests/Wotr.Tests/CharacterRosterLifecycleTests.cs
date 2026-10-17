using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.CharacterRosterFixture;

namespace Wotr.Tests;

public class CharacterRosterLifecycleTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GandalfReturnsAsWhiteAfterHistoricalMinionDiesWithoutDuplicatingTheFigure(bool keeper)
    {
        var state = Actions(keeper ? 1 : 0, Side.Shadow);
        state.Nations.Single(n => n.Id == "isengard").StepsToWar = 0;
        state = Enter(state, CharacterRules.Saruman, "orthanc");
        CharacterLifecycle.Eliminate(state, state.Characters.Single(c => c.Id == CharacterRules.Saruman));
        var removed = Row(state, Side.Shadow, CharacterRules.Saruman);
        Assert.Equal(CharacterRosterLifecycle.Eliminated, removed.Lifecycle);
        Assert.Empty(removed.FutureVersions);
        Assert.True(state.MinionEverEntered);
        Put(state, keeper ? LordsCharacters.Gandalf : CharacterRules.Grey, "casualties", CharacterRules.Grey);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        var grey = Row(state, Side.FreePeoples, CharacterRules.Grey);
        Assert.Equal(CharacterRosterLifecycle.EliminatedWithReplacement, grey.Lifecycle);
        var next = Assert.Single(grey.FutureVersions);
        Assert.Equal(CharacterEntryMode.ReturnAsNewVersion, next.Mode);
        Assert.True(next.Entry.CanAct);
        Assert.Contains("fangorn", next.Entry.Targets);
        Assert.DoesNotContain("minas-tirith", next.Entry.Targets);
        var diceBefore = state.Dice.Where(d => d.Owner == Side.FreePeoples && d.Location != DieLocation.Removed).Count();
        state = Enter(state, CharacterRules.White, "fangorn");
        var white = Row(state, Side.FreePeoples, CharacterRules.Grey);
        Assert.Equal(CharacterRules.White, white.CurrentDefinitionId);
        Assert.Equal(CharacterRosterLifecycle.OnBoard, white.Lifecycle);
        Assert.Empty(white.FutureVersions);
        Assert.Equal(diceBefore, state.Dice.Where(d => d.Owner == Side.FreePeoples && d.Location != DieLocation.Removed).Count());
        Assert.Equal(1, state.Player(Side.FreePeoples).ExtraDiceNextRecovery);
        CharacterLifecycle.Eliminate(state, state.Characters.Single(c => c.Id == white.FigureId));
        Assert.Equal(CharacterRosterLifecycle.Eliminated, Row(state, Side.FreePeoples, white.FigureId).Lifecycle);
        Assert.Empty(Row(state, Side.FreePeoples, white.FigureId).FutureVersions);
    }

    [Fact]
    public void LivingGandalfKeepsHisGarrisonAndStriderHisPhysicalIdentity()
    {
        var state = Actions();
        state.MinionEverEntered = true;
        var grey = Put(state, CharacterRules.Grey, "lorien");
        grey.Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "lorien");
        Assert.Equal(CharacterEntryMode.Transformation,
            Row(state, Side.FreePeoples, grey.Id).FutureVersions.Single().Mode);
        state = Enter(state, CharacterRules.White, "lorien");
        Assert.True(Row(state, Side.FreePeoples, grey.Id).Garrison);
        Put(state, CharacterRules.Strider, "pelargir");
        TurnFlow.SelectNext(state, Side.FreePeoples);
        state = Enter(state, CharacterRules.Aragorn, "pelargir");
        var aragorn = Row(state, Side.FreePeoples, CharacterRules.Strider);
        Assert.Equal(CharacterRules.Aragorn, aragorn.CurrentDefinitionId);
        Assert.Equal("pelargir", aragorn.RegionId);
        Assert.Empty(aragorn.FutureVersions);
        Assert.DoesNotContain(Roster(state, Side.FreePeoples), r => r.FigureId == CharacterRules.Aragorn);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WitchKingCanReplaceHisVersionOnceButMouthCannot(bool blackCaptainFirst)
    {
        var state = Actions(1, Side.Shadow);
        state.Fellowship.RegionId = "hollin";
        if (blackCaptainFirst)
        {
            state.Nations.Single(n => n.Id == "sauron").StepsToWar = 0;
            state.Nations.Single(n => n.Id == "elves").StepsToWar = 0;
        }
        var first = blackCaptainFirst ? CharacterRules.WitchKing : LordsCharacters.WitchKing;
        var second = blackCaptainFirst ? LordsCharacters.WitchKing : CharacterRules.WitchKing;
        var region = blackCaptainFirst ? "minas-morgul" : "hollin";
        state = Enter(state, first, region);
        var chief = Row(state, Side.Shadow, CharacterRules.WitchKing);
        Assert.Equal(first, chief.CurrentDefinitionId);
        Assert.Equal(blackCaptainFirst, chief.FutureVersions.Single().Entry.ConditionsMet);
        state.Nations.Single(n => n.Id == "sauron").StepsToWar = 0;
        state.Nations.Single(n => n.Id == "elves").StepsToWar = 0;
        TurnFlow.SelectNext(state, Side.Shadow);
        state = Enter(state, second, region);
        Assert.True(state.Lords!.WitchKingReplaced);
        var captain = Row(state, Side.Shadow, CharacterRules.WitchKing);
        Assert.Equal(second, captain.CurrentDefinitionId);
        Assert.Empty(captain.FutureVersions);
        CharacterLifecycle.Eliminate(state, state.Characters.Single(c => c.Id == captain.FigureId));
        Assert.Equal(CharacterRosterLifecycle.Eliminated, Row(state, Side.Shadow, captain.FigureId).Lifecycle);
        Assert.Empty(Row(state, Side.Shadow, captain.FigureId).FutureVersions);
        Put(state, LordsCharacters.Mouth, "barad-dur", CharacterRules.Mouth);
        Assert.Empty(Row(state, Side.Shadow, CharacterRules.Mouth).FutureVersions);
    }

    [Fact]
    public void ReturningHobbitsAreDistinctFromPermanentCasualtiesDuringBothReturnDecisions()
    {
        var state = CouncilHobbitBattleTests.ReturnDecision(both: true);
        var ids = new[] { "base.character.merry", "base.character.pippin" };
        foreach (var id in ids)
            Assert.Equal(CharacterRosterLifecycle.AwaitingReturn, Row(state, Side.FreePeoples, id).Lifecycle);
        var target = new EffectOptions().GetOptions(state, Side.FreePeoples).Single().EligibleIds.First();
        var returning = state.Pending!.SourceId;
        state = LordsFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = [target] });
        Assert.Equal(CharacterRosterLifecycle.OnBoard, Row(state, Side.FreePeoples, returning).Lifecycle);
        Assert.Equal(target, Row(state, Side.FreePeoples, returning).RegionId);
        Assert.Equal(CharacterRosterLifecycle.AwaitingReturn,
            Row(state, Side.FreePeoples, ids.Single(id => id != returning)).Lifecycle);
    }

    [Fact]
    public void TakenAliveAwaitsPlacementButOrdinaryEliminationHasNoReturn()
    {
        var state = Actions();
        HobbitDeparture.EliminateFromFellowship(state, "base.character.merry");
        var merry = Row(state, Side.FreePeoples, "base.character.merry");
        Assert.Equal(CharacterRosterLifecycle.AwaitingPlacement, merry.Lifecycle);
        Assert.Empty(merry.RegionId);
        Assert.Empty(merry.FutureVersions);
        var boromir = Put(state, "base.character.boromir", "pelargir");
        CharacterLifecycle.Eliminate(state, boromir);
        Assert.Equal(CharacterRosterLifecycle.Eliminated, Row(state, Side.FreePeoples, boromir.Id).Lifecycle);
        Assert.Empty(Row(state, Side.FreePeoples, boromir.Id).FutureVersions);
    }

    [Fact]
    public void FateCompanionAndAwakenedSovereignRetainTheRightVersionAndRemovedIdentity()
    {
        var state = Actions(7, council: true);
        Put(state, FateGimli.Alternate, "iron-hills", FateGimli.Figure);
        var gimli = Row(state, Side.FreePeoples, FateGimli.Figure);
        Assert.Equal(FateGimli.Alternate, gimli.CurrentDefinitionId);
        Assert.Equal(2, gimli.MovementLevel);
        Assert.Equal(1, gimli.Level);
        Assert.DoesNotContain(Roster(state, Side.FreePeoples), r => r.FigureId == SovereignCatalog.Dain);
        Assert.DoesNotContain(state.Kings!.Sovereigns, s => s.Id is SovereignCatalog.Dain or SovereignCatalog.Brand);
        var sovereign = KingsFixture.Sovereign(state, SovereignCatalog.Thranduil);
        Assert.DoesNotContain(Roster(state, Side.FreePeoples), r => r.FigureId == sovereign.Id);
        SovereignLifecycle.Awaken(state, sovereign, SovereignCatalog.All[sovereign.Id].Home);
        var awakened = Row(state, Side.FreePeoples, sovereign.Id);
        Assert.Equal(CharacterRosterLifecycle.OnBoard, awakened.Lifecycle);
        Assert.Equal(SovereignCatalog.All[sovereign.Id].AwakenedCard, awakened.CurrentDefinitionId);
        CharacterLifecycle.Eliminate(state, state.Characters.Single(c => c.Id == sovereign.Id));
        Assert.Equal(SovereignStatus.Removed, sovereign.Status);
        Assert.Equal(CharacterRosterLifecycle.Eliminated, Row(state, Side.FreePeoples, sovereign.Id).Lifecycle);
        Assert.Empty(Row(state, Side.FreePeoples, sovereign.Id).FutureVersions);
        Assert.DoesNotContain(Roster(state, Side.Shadow), r => r.FigureId == sovereign.Id);
    }

    private static MatchState Enter(MatchState state, string definition, string region)
    {
        var version = Roster(state, state.Pending!.Owner).SelectMany(r => r.FutureVersions)
            .Single(v => v.DefinitionId == definition);
        Assert.True(version.Entry.CanAct);
        return LordsFixture.Do(state, CommandKind.EnterCharacter,
            new() { CharacterId = definition, RegionId = region, DieId = version.Entry.EligibleDice[0] });
    }
}
