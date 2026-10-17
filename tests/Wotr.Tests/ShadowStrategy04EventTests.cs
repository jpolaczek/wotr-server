using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards04;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy04Fixture;

namespace Wotr.Tests;

public class ShadowStrategy04EventTests
{
    public static IEnumerable<object[]> EveryCard() => Cards.Select(card => new object[] { card });

    [Theory]
    [MemberData(nameof(EveryCard))]
    public void EveryCardRequiresOwnershipWindowAndMusterOrEventCost(string card)
    {
        var state = State(card);
        foreach (var face in new[] { DieFace.Muster, DieFace.Event })
        {
            var payload = EventCost(state, card, face);
            var command = BattleFixture.Command(state, CommandKind.PlayEvent, payload);
            Assert.Null(Engine().Validate(state, Side.Shadow, command));
            Assert.NotNull(Engine().Validate(state, Side.FreePeoples, command));
        }
        Reject(state, CommandKind.PlayEvent, EventCost(state, card, DieFace.Army));
        var cost = EventCost(state, card);
        HandRules.Remove(state.Player(Side.Shadow), card);
        Reject(state, CommandKind.PlayEvent, cost);
        HandRules.Add(state.Player(Side.Shadow), card);
        state.Pending!.Kind = DecisionKind.Losses;
        Reject(state, CommandKind.PlayEvent, cost);
    }

    [Theory]
    [InlineData(Horde, "southrons")]
    [InlineData(Pits, "sauron")]
    [InlineData(Musterings, "southrons")]
    [InlineData(Musterings, "sauron")]
    [InlineData(Musterings, "isengard")]
    public void PrintedWarConditionsAreMandatory(string card, string nation)
    {
        var state = State(card);
        state.Nations.Single(n => n.Id == nation).StepsToWar = 1;
        Reject(state, CommandKind.PlayEvent, EventCost(state, card));
    }

    [Theory]
    [InlineData(MistyMountains)]
    [InlineData(OrcsMultiplying)]
    [InlineData(Monsters)]
    public void UnconditionalRecruitmentDoesNotActivateOrAdvanceNations(string card)
    {
        var state = State(card);
        foreach (var nation in state.Nations)
        {
            nation.Active = false;
            nation.StepsToWar = 2;
        }
        state = Play(state, card);
        if (card == MistyMountains)
            state = Do(state, CommandKind.ChooseEffect, Plan(state, ("sauron", UnitKind.Elite, "moria", 2), ("sauron", UnitKind.Nazgul, "moria", 1)));
        Assert.All(state.Nations, nation => { Assert.False(nation.Active); Assert.Equal(2, nation.StepsToWar); });
        Assert.Equal(card == OrcsMultiplying ? 6 : card == Monsters ? 4 : 3, state.Events.Count(e => e.Kind == "EventRecruited"));
    }

    [Theory]
    [InlineData("moria", UnitKind.Regular)]
    [InlineData("moria", UnitKind.Elite)]
    [InlineData("mount-gram", UnitKind.Regular)]
    [InlineData("mount-gram", UnitKind.Elite)]
    public void MistyMountainsRequiresTwoChosenArmyUnitsAndNazgulTogether(string region, UnitKind kind)
    {
        var state = Play(State(MistyMountains), MistyMountains);
        var plan = Plan(state, ("sauron", kind, region, 2), ("sauron", UnitKind.Nazgul, region, 1));
        var invalid = StateCodec.Copy(state);
        plan.Placements[2].RegionId = region == "moria" ? "mount-gram" : "moria";
        Reject(invalid, CommandKind.ChooseEffect, plan);
        plan.Placements[2].RegionId = region;
        state = Do(state, CommandKind.ChooseEffect, plan);
        Assert.Equal(2, state.Units.Count(u => u.Location == region && u.Kind == kind));
        Assert.Single(state.Units, u => u.Location == region && u.Kind == UnitKind.Nazgul);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void MistyMountainsAcceptsMixedArmyAndMissingNazgulWithoutDroppingArmyRecruitment()
    {
        var state = State(MistyMountains);
        Reserve(state, "sauron", UnitKind.Nazgul, 0);
        state = Play(state, MistyMountains);
        Assert.Equal(2, Option(state).Maximum);
        state = Do(state, CommandKind.ChooseEffect, Plan(state, ("sauron", UnitKind.Regular, "mount-gram", 1), ("sauron", UnitKind.Elite, "mount-gram", 1)));
        Assert.Equal(2, state.Events.Count(e => e.Kind == "EventRecruited"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void OrcsShortageCanBeDistributedWithoutPrintedRegionPriority(int dolGuldur)
    {
        var state = State(OrcsMultiplying);
        Reserve(state, "sauron", UnitKind.Regular, 2);
        state = Play(state, OrcsMultiplying);
        var plan = Plan(state, ("sauron", UnitKind.Regular, "dol-guldur", dolGuldur), ("sauron", UnitKind.Regular, "mount-gundabad", 2 - dolGuldur));
        var missing = new CommandPayload { Placements = plan.Placements.Take(1).ToList() };
        Reject(state, CommandKind.ChooseEffect, missing);
        state = Do(state, CommandKind.ChooseEffect, plan);
        Assert.Equal(dolGuldur, state.Units.Count(u => u.Location == "dol-guldur"));
        Assert.Equal(2 - dolGuldur, state.Units.Count(u => u.Location == "mount-gundabad"));
    }

    [Fact]
    public void OrcsRecruitThreeInEachStrongholdAutomaticallyWhenThereIsNoStrategicChoice()
    {
        var state = Play(State(OrcsMultiplying), OrcsMultiplying);
        Assert.Equal(3, state.Units.Count(u => u.Location == "dol-guldur"));
        Assert.Equal(3, state.Units.Count(u => u.Location == "mount-gundabad"));
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData("east-rhun")]
    [InlineData("south-rhun")]
    [InlineData("khand")]
    [InlineData("far-harad")]
    public void HordeUsesExactlyTheFourPrintedEasternBorderRegions(string region)
    {
        var state = Play(State(Horde), Horde);
        Assert.Equal(new[] { "east-rhun", "far-harad", "khand", "south-rhun" }, Option(state).Targets.Order());
        var plan = Plan(state, ("southrons", UnitKind.Regular, region, 5));
        plan.Placements[0].RegionId = "north-rhun";
        Reject(state, CommandKind.ChooseEffect, plan);
        plan.Placements[0].RegionId = region;
        state = Do(state, CommandKind.ChooseEffect, plan);
        Assert.Equal(5, state.Units.Count(u => u.Location == region));
    }

    [Fact]
    public void HordeCannotUseEnemyOccupiedRegionButCanResolveAnEmptyEffect()
    {
        var state = State(Horde);
        foreach (var region in new[] { "east-rhun", "south-rhun", "khand", "far-harad" })
            BattleFixture.Put(state, "gondor", UnitKind.Regular, region);
        state = Play(state, Horde);
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.Events, e => e.Kind == "EventRecruited");
        Assert.Contains(Horde, state.Player(Side.Shadow).Discard);
    }

    [Fact]
    public void MonstersShareRegularReserveAcrossThreeRegionsAndUseIndependentEliteReserve()
    {
        var state = State(Monsters);
        Reserve(state, "sauron", UnitKind.Regular, 1);
        state = Play(state, Monsters);
        var plan = Plan(state, ("sauron", UnitKind.Regular, "weather-hills", 1), ("sauron", UnitKind.Elite, "trollshaws", 1));
        state = Do(state, CommandKind.ChooseEffect, plan);
        Assert.Single(state.Units, u => u.Location == "weather-hills" && u.Kind == UnitKind.Regular);
        Assert.Single(state.Units, u => u.Location == "trollshaws" && u.Kind == UnitKind.Elite);
        Assert.DoesNotContain(state.Units, u => u.Location is "angmar" or "ettenmoors");
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(2, 3)]
    [InlineData(5, 0)]
    [InlineData(5, 5)]
    public void MusteringsRecruitsIndependentReservesInTheirPrintedForeignAndHomeRegions(int southrons, int sauron)
    {
        var state = State(Musterings);
        Reserve(state, "southrons", UnitKind.Regular, southrons);
        Reserve(state, "sauron", UnitKind.Regular, sauron);
        state = Play(state, Musterings);
        Assert.Equal(southrons, state.Units.Count(u => u.Location == "gorgoroth" && u.Nation == "southrons"));
        Assert.Equal(sauron, state.Units.Count(u => u.Location == "nurn" && u.Nation == "sauron"));
        Assert.Empty(state.Resolution.Frames);
    }
}
