using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards03;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy03Fixture;

namespace Wotr.Tests;

public class ShadowStrategy03KingTests
{
    [Theory]
    [InlineData(CharacterRules.Strider)]
    [InlineData(CharacterRules.Grey)]
    [InlineData(CharacterRules.White)]
    public void KingRevealedRequiresAragornVersionInPlay(string other)
    {
        var state = State(KingRevealed);
        state.Characters.Single(c => c.DefinitionId == CharacterRules.Aragorn).Location = "reserve";
        Put(state, other, "minas-tirith");
        Reject(state, CommandKind.PlayEvent, EventCost(state, KingRevealed));
        Put(state, CharacterRules.Aragorn, "minas-tirith");
        state = Play(state, KingRevealed);
        Assert.Equal(6, state.Events.Count(e => e.Kind == "EventRecruited"));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(2, 0)]
    [InlineData(2, 1)]
    [InlineData(5, 0)]
    [InlineData(5, 1)]
    public void KingRevealedRecruitsEachAvailableKindWithoutWarOrExtraChoice(int regulars, int nazgul)
    {
        var state = State(KingRevealed);
        state.Nations.Single(n => n.Id == "sauron").StepsToWar = 2;
        Reserve(state, "sauron", UnitKind.Regular, regulars);
        Reserve(state, "sauron", UnitKind.Nazgul, nazgul);
        state = Play(state, KingRevealed);
        Assert.Equal(regulars, state.Units.Count(u => u.Location == "minas-morgul" && u.Kind == UnitKind.Regular));
        Assert.Equal(nazgul, state.Units.Count(u => u.Location == "minas-morgul" && u.Kind == UnitKind.Nazgul));
        Assert.Equal(2, state.Nations.Single(n => n.Id == "sauron").StepsToWar);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void CapturedMinasMorgulPerformsNoRecruitmentAndDoesNotRecallBoardNazgul()
    {
        var state = State(KingRevealed);
        var nazgul = BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "orthanc");
        state.Control["minas-morgul"] = Side.FreePeoples;
        state = Play(state, KingRevealed);
        Assert.Equal("orthanc", state.Units.Single(u => u.Id == nazgul.Id).Location);
        Assert.DoesNotContain(state.Events, e => e.Kind == "EventRecruited");
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void KingRevealedRecruitsOnCorrectSiegeSideAndResolvesExcessAfterNazgul(bool inside)
    {
        var state = State(KingRevealed);
        for (var i = 0; i < (inside ? 1 : 6); i++)
            BattleFixture.Put(state, "isengard", UnitKind.Regular, "minas-morgul").Garrison = inside;
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-morgul").Garrison = !inside;
        state.Control["minas-morgul"] = inside ? Side.Shadow : Side.FreePeoples;
        state = Play(state, KingRevealed);
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        var recruits = state.Units.Where(u => u.Nation == "sauron" && u.Location == "minas-morgul").ToArray();
        Assert.Equal(6, recruits.Length);
        Assert.All(recruits, u => Assert.Equal(inside, u.Garrison));
        var old = state.Units.First(u => u.Nation == "isengard" && u.Location == "minas-morgul").Id;
        state = Do(state, CommandKind.RemoveExcess, new() { Ids = [old] });
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Fact]
    public void PalantirDrawFollowsWholeRecruitmentAndExcessChoice()
    {
        var state = State(KingRevealed);
        Put(state, CharacterRules.Saruman, "orthanc");
        Shadow04Fixture.Table(state, ShadowCharacterCards04.Palantir);
        for (var i = 0; i < 6; i++)
            BattleFixture.Put(state, "isengard", UnitKind.Regular, "minas-morgul");
        var cost = EventCost(state, KingRevealed);
        state.Dice.Single(d => d.Id == cost.DieId).Face = DieFace.Event;
        cost.CardCost = DieFace.Event;
        state = Do(state, CommandKind.PlayEvent, cost);
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.DoesNotContain(state.Events, e => e.Kind == "CardDrawn");
        var excess = state.Units.First(u => u.Nation == "isengard" && u.Location == "minas-morgul").Id;
        state = Do(state, CommandKind.RemoveExcess, new() { Ids = [excess] });
        Assert.Equal(ShadowCharacterCards04.Palantir, state.Pending!.SourceId);
        Assert.Equal("Deck", state.Pending.Presentation);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["strategy"] });
        Assert.Empty(state.Resolution.Frames);
    }
}
