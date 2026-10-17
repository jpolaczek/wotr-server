using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards03;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Strategy01Fixture;
using static Wotr.Tests.Strategy03Fixture;

namespace Wotr.Tests;

public class Strategy03RecruitmentTests
{
    [Theory]
    [InlineData(GuardsOfCitadel, "gondor", "minas-tirith", UnitKind.Regular)]
    [InlineData(GuardsOfCitadel, "gondor", "minas-tirith", UnitKind.Elite)]
    [InlineData(CelebornsGaladhrim, "elves", "lorien", UnitKind.Regular)]
    [InlineData(CelebornsGaladhrim, "elves", "lorien", UnitKind.Elite)]
    [InlineData(RidersOfTheoden, "rohan", "edoras", UnitKind.Regular)]
    [InlineData(RidersOfTheoden, "rohan", "edoras", UnitKind.Elite)]
    [InlineData(Grimbeorn, "north", "carrock", UnitKind.Regular)]
    [InlineData(Grimbeorn, "north", "carrock", UnitKind.Elite)]
    [InlineData(Imrahil, "gondor", "dol-amroth", UnitKind.Regular)]
    [InlineData(Imrahil, "gondor", "dol-amroth", UnitKind.Elite)]
    public void RecruitmentIgnoresPoliticsAndUsesPrintedNationAndRegion(string card, string nation, string region, UnitKind kind)
    {
        var state = BattleFixture.State();
        state.Nations.Single(n => n.Id == nation).StepsToWar = 3;
        state.Nations.Single(n => n.Id == nation).Active = false;
        var drawn = state.Player(Side.FreePeoples).Decks["strategy"][0];
        state = Play(state, card);
        Assert.Equal("ReserveFigures", state.Pending!.Presentation);
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [state.Units.First(u => u.Nation == "sauron").Id] });
        var recruit = Recruits(state, kind).Single();
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [recruit] });
        Assert.Equal(region, state.Units.Single(u => u.Id == recruit).Location);
        Assert.Equal(card == CelebornsGaladhrim ? 1 : 2, state.Units.Count(u => u.Location == region));
        Assert.All(state.Units.Where(u => u.Location == region), u => Assert.Equal(nation, u.Nation));
        Assert.Equal(3, state.Nations.Single(n => n.Id == nation).StepsToWar);
        Assert.False(state.Nations.Single(n => n.Id == nation).Active);
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        if (card == CelebornsGaladhrim)
            Assert.Contains(drawn, state.Player(Side.FreePeoples).Hand);
    }

    [Theory]
    [InlineData(UnitKind.Regular, UnitKind.Regular)]
    [InlineData(UnitKind.Regular, UnitKind.Elite)]
    [InlineData(UnitKind.Elite, UnitKind.Elite)]
    public void CirdanRecruitsBothFiguresTogetherAndRejectsPartialOrDuplicateChoice(UnitKind first, UnitKind second)
    {
        var state = Play(Coasts(), CirdansShips);
        Assert.Equal(new[] { "dol-amroth", "grey-havens" }, state.Pending!.PrivateChoices.Order());
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["lorien"] });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["grey-havens"] });
        var ids = Recruits(state, first, second);
        Assert.Equal(4, state.Pending!.PrivateChoices.Count);
        Assert.Equal(2, state.Pending.RequiredCount);
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [ids[0]] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [ids[0], ids[0]] });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ids });
        Assert.All(ids, id => Assert.Equal("grey-havens", state.Units.Single(u => u.Id == id).Location));
        Assert.Equal(2, state.Events.Count(e => e.Kind == "EventRecruited"));
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void CirdanAutomaticallyRecruitsEveryRemainingFigure(int remaining)
    {
        var state = Coasts();
        foreach (var unit in state.Units.Where(u => u.Nation == "elves" && BoardRules.IsArmyUnit(u)).Skip(remaining))
            unit.Location = "casualties";
        state = Play(state, CirdansShips);
        if (remaining > 0)
            state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["dol-amroth"] });
        Assert.Equal(remaining, state.Events.Count(e => e.Kind == "EventRecruited"));
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
    }

    [Fact]
    public void CirdanNeedsWarButMayBePlayedWithoutARecruitableArmy()
    {
        var state = BattleFixture.State();
        EventTests.GiveCard(state, Side.FreePeoples, CirdansShips);
        var payload = Cost(state, DieFace.Muster);
        payload.CardId = CirdansShips;
        payload.CardCost = DieFace.Muster;
        state.Nations.Single(n => n.Id == "elves").StepsToWar = 1;
        Reject(state, CommandKind.PlayEvent, payload);
        state.Nations.Single(n => n.Id == "elves").StepsToWar = 0;
        state = Do(state, CommandKind.PlayEvent, payload);
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.Events, e => e.Kind == "EventRecruited");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CirdanRecruitsOnEitherSideOfSiegeAndReturnsMixedNationExcess(bool fpInside)
    {
        var state = BattleFixture.State();
        for (var i = 0; i < (fpInside ? 5 : 10); i++)
            BattleFixture.Put(state, "gondor", UnitKind.Regular, "dol-amroth").Garrison = fpInside;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "dol-amroth").Garrison = !fpInside;
        state.Control["dol-amroth"] = fpInside ? Side.FreePeoples : Side.Shadow;
        state = Play(state, CirdansShips);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = Recruits(state, UnitKind.Elite, UnitKind.Elite) });
        Assert.All(state.Units.Where(u => u.Location == "dol-amroth" && u.Nation == "elves"), u => Assert.Equal(fpInside, u.Garrison));
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        var returned = state.Units.Where(u => u.Location == "dol-amroth" && u.Nation == "gondor").Take(2).Select(u => u.Id).ToList();
        state = Do(state, CommandKind.RemoveExcess, new() { Ids = returned });
        Assert.All(returned, id => Assert.Equal("reserve", state.Units.Single(u => u.Id == id).Location));
        Assert.Equal(2, state.Units.Count(u => u.Location == "dol-amroth" && u.Nation == "elves"));
    }

    [Fact]
    public void RidersOffersOnlyEdorasOrFreeRohanRegionsWithCompanions()
    {
        var state = BattleFixture.State();
        Put(state, "base.character.gimli", "eastemnet");
        Put(state, "base.character.legolas", "westemnet");
        Put(state, "base.character.boromir", "helms-deep");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "westemnet");
        state.Control["helms-deep"] = Side.Shadow;
        Put(state, CharacterRules.Grey, "minas-tirith");
        state = Play(state, RidersOfTheoden);
        Assert.Equal(new[] { "eastemnet", "edoras" }, state.Pending!.PrivateChoices.Order());
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["helms-deep"] });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["eastemnet"] });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = Recruits(state, UnitKind.Elite) });
        Assert.Equal(2, state.Units.Count(u => u.Location == "eastemnet" && u.Nation == "rohan"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RidersRequiresCompanionToRecruitInsideHelmsDeep(bool companion)
    {
        var state = Strategy02Fixture.Passage();
        if (!companion)
            state.Characters.Clear();
        state = Play(state, RidersOfTheoden);
        if (!companion)
        {
            Assert.Equal("ReserveFigures", state.Pending!.Presentation);
            Assert.Equal("edoras", new EffectOptions().GetOptions(state, Side.FreePeoples).Single().EffectChoices[0].TargetId);
            return;
        }
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["helms-deep"] });
        var id = Recruits(state, UnitKind.Regular).Single();
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [id] });
        Assert.True(state.Units.Single(u => u.Id == id).Garrison);
        Assert.Equal(2, state.Units.Count(u => u.Location == "helms-deep" && u.Nation == "rohan" && u.Kind == UnitKind.Leader && u.Garrison));
    }

    [Theory]
    [InlineData(GuardsOfCitadel, "minas-tirith")]
    [InlineData(Grimbeorn, "carrock")]
    [InlineData(Imrahil, "dol-amroth")]
    public void CapturedSettlementDoesNotRecruitButCardStillResolves(string card, string region)
    {
        var state = BattleFixture.State();
        state.Control[region] = Side.Shadow;
        state = Play(state, card);
        Assert.DoesNotContain(state.Events, e => e.Kind == "EventRecruited");
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(GuardsOfCitadel, "gondor", "minas-tirith")]
    [InlineData(RidersOfTheoden, "rohan", "edoras")]
    [InlineData(Grimbeorn, "north", "carrock")]
    [InlineData(Imrahil, "gondor", "dol-amroth")]
    public void MissingArmyReservesStillRecruitLeaderAlongsideExistingForeignArmy(string card, string nation, string region)
    {
        var state = BattleFixture.State();
        foreach (var unit in state.Units.Where(u => u.Nation == nation && BoardRules.IsArmyUnit(u)))
            unit.Location = "casualties";
        BattleFixture.Put(state, "elves", UnitKind.Regular, region);
        state = Play(state, card);
        Assert.Single(state.Units, u => u.Location == region && u.Kind == UnitKind.Leader && u.Nation == nation);
        Assert.Single(state.Events, e => e.Kind == "EventRecruited");
        Assert.Empty(state.Resolution.Frames);
    }
}
