using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards04;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Strategy01Fixture;

namespace Wotr.Tests;

public class Strategy04RecruitmentTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void BrandRecruitsOnlyAvailableRegularsThenDraws(int regulars)
    {
        var state = BattleFixture.State();
        foreach (var unit in state.Units.Where(u => u.Nation == "north" && u.Kind == UnitKind.Regular).Skip(regulars))
            unit.Location = "casualties";
        state.Nations.Single(n => n.Id == "north").StepsToWar = 3;
        var drawn = state.Player(Side.FreePeoples).Decks["strategy"][0];
        state = Play(state, KingBrandsMen);
        Assert.Equal(regulars, state.Units.Count(u => u.Location == "dale"));
        Assert.All(state.Units.Where(u => u.Location == "dale"), u => Assert.Equal(UnitKind.Regular, u.Kind));
        Assert.Contains(drawn, state.Player(Side.FreePeoples).Hand);
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Theory]
    [InlineData(UnitKind.Regular, UnitKind.Regular)]
    [InlineData(UnitKind.Regular, UnitKind.Elite)]
    [InlineData(UnitKind.Elite, UnitKind.Regular)]
    [InlineData(UnitKind.Elite, UnitKind.Elite)]
    public void SwordsCommitsIndependentNationalRecruitsTogether(UnitKind north, UnitKind dwarves)
    {
        var state = Play(BattleFixture.State(), SwordsInEriador);
        var option = new EffectOptions().GetOptions(state, Side.FreePeoples).Single();
        Assert.Equal(2, option.Minimum);
        Assert.Equal(4, option.EligibleIds.Count);
        Assert.Equal(new[] { "Ered Luin", "The Shire" }, option.EffectChoices.Select(c => c.SelectionGroup).Distinct().Order());
        var ids = new[] { Pick(state, "north", north), Pick(state, "dwarves", dwarves) };
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [ids[0]] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [Pick(state, "north", UnitKind.Regular), Pick(state, "north", UnitKind.Elite)] });
        var command = BattleFixture.Command(state, CommandKind.ChooseEffect, new() { Ids = [.. ids] });
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples, command);
        Assert.True(preview.Legal);
        Assert.Equal(new[] { "ered-luin", "the-shire" }, preview.Placements.Select(p => p.RegionId).Order());
        Assert.Equal(before, StateCodec.Write(state));
        var revision = state.Revision;
        var drawn = state.Player(Side.FreePeoples).Decks["strategy"][0];
        state = Do(state, CommandKind.ChooseEffect, command.Payload);
        Assert.Equal(revision + 1, state.Revision);
        Assert.Equal("the-shire", state.Units.Single(u => u.Id == ids[0]).Location);
        Assert.Equal("ered-luin", state.Units.Single(u => u.Id == ids[1]).Location);
        Assert.Contains(drawn, state.Player(Side.FreePeoples).Hand);
    }

    [Theory]
    [InlineData("north", false)]
    [InlineData("dwarves", false)]
    [InlineData("north", true)]
    [InlineData("dwarves", true)]
    public void SwordsStillRecruitsOtherNationWhenOneDestinationOrReserveIsUnavailable(string absent, bool captured)
    {
        var state = BattleFixture.State();
        if (captured)
            state.Control[absent == "north" ? "the-shire" : "ered-luin"] = Side.Shadow;
        else
            foreach (var unit in state.Units.Where(u => u.Nation == absent && BoardRules.IsArmyUnit(u)))
                unit.Location = "casualties";
        state = Play(state, SwordsInEriador);
        Assert.Equal(1, state.Pending!.RequiredCount);
        var nation = absent == "north" ? "dwarves" : "north";
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [Pick(state, nation, UnitKind.Elite)] });
        Assert.Single(state.Events, e => e.Kind == "EventRecruited");
        Assert.Single(state.Events, e => e.Kind == "CardDrawn");
    }

    [Theory]
    [InlineData(KindredOfGlorfindel, "elves", "rivendell", UnitKind.Regular)]
    [InlineData(KindredOfGlorfindel, "elves", "rivendell", UnitKind.Elite)]
    [InlineData(DainIronfoot, "dwarves", "erebor", UnitKind.Regular)]
    [InlineData(DainIronfoot, "dwarves", "erebor", UnitKind.Elite)]
    [InlineData(ThranduilsArchers, "elves", "woodland-realm", UnitKind.Regular)]
    [InlineData(ThranduilsArchers, "elves", "woodland-realm", UnitKind.Elite)]
    public void FixedRecruitmentsUsePrintedNationAndRegionBeforeWar(string card, string nation, string region, UnitKind kind)
    {
        var state = BattleFixture.State();
        state.Nations.Single(n => n.Id == nation).StepsToWar = 3;
        state = Play(state, card);
        var id = Pick(state, nation, kind);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [id] });
        Assert.Equal(region, state.Units.Single(u => u.Id == id).Location);
        Assert.Equal(card == DainIronfoot ? 2 : 1, state.Units.Count(u => u.Location == region && u.Nation == nation));
        Assert.Equal(card == DainIronfoot ? 0 : 1, state.Events.Count(e => e.Kind == "CardDrawn"));
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Theory]
    [InlineData("friendly", true)]
    [InlineData("captured", false)]
    [InlineData("fp-besieged", false)]
    [InlineData("fp-besieging", true)]
    public void EomerRequiresFreeSettlementAndCannotReinforceBesiegedGarrison(string situation, bool allowed)
    {
        var state = BattleFixture.State();
        if (situation is "captured" or "fp-besieging")
            state.Control["helms-deep"] = Side.Shadow;
        if (situation is "fp-besieged" or "fp-besieging")
        {
            BattleFixture.Put(state, "rohan", UnitKind.Regular, "helms-deep").Garrison = situation == "fp-besieged";
            BattleFixture.Put(state, "isengard", UnitKind.Regular, "helms-deep").Garrison = situation == "fp-besieging";
        }
        state.Nations.Single(n => n.Id == "rohan").StepsToWar = 3;
        state = Play(state, Eomer);
        Assert.Equal(allowed, state.Pending!.PrivateChoices.Contains("helms-deep"));
        Assert.DoesNotContain("fords-of-isen", state.Pending.PrivateChoices);
        Assert.DoesNotContain("eastemnet", state.Pending.PrivateChoices);
        if (!allowed)
            Reject(state, CommandKind.ChooseEffect, new() { Ids = ["helms-deep"] });
        var target = allowed ? "helms-deep" : "folde";
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [target] });
        var id = Pick(state, "rohan", UnitKind.Elite);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [id] });
        Assert.Equal(target, state.Units.Single(u => u.Id == id).Location);
        Assert.False(state.Units.Single(u => u.Id == id).Garrison);
        Assert.Single(state.Units, u => u.Location == target && u.Kind == UnitKind.Leader && u.Nation == "rohan");
    }

    [Fact]
    public void EomerStillResolvesWhenEverySettlementIsOccupied()
    {
        var state = BattleFixture.State();
        foreach (var region in BoardRules.Regions.Values.Where(r => r.Nation == "rohan" && BoardRules.IsSettlement(r)))
            state.Control[region.Id] = Side.Shadow;
        state = Play(state, Eomer);
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.Events, e => e.Kind == "EventRecruited");
    }

    [Theory]
    [InlineData(KingBrandsMen, "dale")]
    [InlineData(SwordsInEriador, "the-shire")]
    [InlineData(KindredOfGlorfindel, "rivendell")]
    [InlineData(ThranduilsArchers, "woodland-realm")]
    public void DrawStillHappensWhenAllDestinationsAreCapturedAndEmptyDeckDoesNotReshuffle(string card, string region)
    {
        var state = BattleFixture.State();
        state.Control[region] = Side.Shadow;
        state.Control["ered-luin"] = Side.Shadow;
        var drawn = state.Player(Side.FreePeoples).Decks["strategy"][0];
        var empty = StateCodec.Copy(state);
        empty.Player(Side.FreePeoples).Decks["strategy"].Clear();
        Assert.Contains(drawn, Play(state, card).Player(Side.FreePeoples).Hand);
        empty = Play(empty, card);
        Assert.DoesNotContain(empty.Events, e => e.Kind == "CardDrawn");
        Assert.Empty(empty.Player(Side.FreePeoples).Hand);
        Assert.Empty(empty.Resolution.Frames);
    }

    internal static string Pick(MatchState state, string nation, UnitKind kind) => state.Pending!.PrivateChoices
        .Single(id => state.Units.Any(u => u.Id == id && u.Nation == nation && u.Kind == kind));
}
