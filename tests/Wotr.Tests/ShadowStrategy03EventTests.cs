using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards03;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy03Fixture;

namespace Wotr.Tests;

public class ShadowStrategy03EventTests
{
    public static IEnumerable<object[]> EveryCard() => Cards.Select(card => new object[] { card });

    [Theory]
    [MemberData(nameof(EveryCard))]
    public void EveryCardRequiresItsOwnerWindowAndPrintedCost(string card)
    {
        var state = State(card);
        var payload = EventCost(state, card);
        var command = BattleFixture.Command(state, CommandKind.PlayEvent, payload);
        Assert.Null(Engine().Validate(state, Side.Shadow, command));
        Assert.NotNull(Engine().Validate(state, Side.FreePeoples, command));
        payload.CardCost = card is HalfOrcs or OlogHai or HillTrolls ? DieFace.Muster : DieFace.Army;
        state.Dice.Single(d => d.Id == payload.DieId).Face = payload.CardCost;
        Reject(state, CommandKind.PlayEvent, payload);
        payload = EventCost(state, card);
        HandRules.Remove(state.Player(Side.Shadow), card);
        Reject(state, CommandKind.PlayEvent, payload);
        HandRules.Add(state.Player(Side.Shadow), card);
        state.Pending!.Kind = DecisionKind.Losses;
        Reject(state, CommandKind.PlayEvent, payload);
    }

    [Theory]
    [InlineData(HalfOrcs, "isengard")]
    [InlineData(OlogHai, "sauron")]
    [InlineData(HillTrolls, "sauron")]
    public void ArmyEventsRequirePrintedNationAtWarEvenWithoutTargets(string card, string nation)
    {
        var state = State(card);
        state.Nations.Single(n => n.Id == nation).StepsToWar = 1;
        Reject(state, CommandKind.PlayEvent, EventCost(state, card));
        state.Nations.Single(n => n.Id == nation).StepsToWar = 0;
        state = Play(state, card);
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.Events, e => e.Kind is "EventRecruited" or "EventUnitReplaced");
    }

    [Theory]
    [InlineData(HalfOrcs, "isengard", UnitKind.Regular)]
    [InlineData(HalfOrcs, "isengard", UnitKind.Elite)]
    [InlineData(OlogHai, "sauron", UnitKind.Regular)]
    [InlineData(OlogHai, "sauron", UnitKind.Elite)]
    public void ArmyRecruitmentChoosesAnyShadowArmyAndPrintedNationKind(string card, string nation, UnitKind kind)
    {
        var state = State(card);
        state.Control["westemnet"] = Side.Shadow;
        BattleFixture.Put(state, "southrons", UnitKind.Regular, "westemnet");
        BattleFixture.Put(state, "southrons", UnitKind.Elite, "eastemnet");
        BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "fangorn");
        Put(state, CharacterRules.Saruman, "orthanc");
        state = Play(state, card);
        Assert.Equal(new[] { "eastemnet", "westemnet" }, state.Pending!.PrivateChoices.Order());
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["fangorn"] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["orthanc"] });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["eastemnet"] });
        var recruit = state.Units.Single(u => u.Nation == nation && u.Kind == kind && state.Pending!.PrivateChoices.Contains(u.Id));
        var id = recruit.Id;
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [id] });
        Assert.Equal("eastemnet", state.Units.Single(u => u.Id == id).Location);
        Assert.Single(state.Events, e => e.Kind == "EventRecruited");
    }

    [Theory]
    [InlineData(HalfOrcs, "isengard", false)]
    [InlineData(HalfOrcs, "isengard", true)]
    [InlineData(OlogHai, "sauron", false)]
    [InlineData(OlogHai, "sauron", true)]
    public void ArmyRecruitmentPreservesSiegeSideAndAllowsChoosingOldExcess(string card, string nation, bool inside)
    {
        var state = State(card);
        for (var i = 0; i < (inside ? 5 : 10); i++)
            BattleFixture.Put(state, "southrons", UnitKind.Regular, "minas-tirith").Garrison = inside;
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-tirith").Garrison = !inside;
        state.Control["minas-tirith"] = inside ? Side.Shadow : Side.FreePeoples;
        state = Play(state, card);
        var id = state.Units.Single(u => u.Nation == nation && u.Kind == UnitKind.Elite && state.Pending!.PrivateChoices.Contains(u.Id)).Id;
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [id] });
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.Equal(inside, state.Units.Single(u => u.Id == id).Garrison);
        var old = state.Units.First(u => u.Nation == "southrons" && u.Location == "minas-tirith").Id;
        state = Do(state, CommandKind.RemoveExcess, new() { Ids = [old] });
        Assert.Equal("reserve", state.Units.Single(u => u.Id == old).Location);
        Assert.Equal("minas-tirith", state.Units.Single(u => u.Id == id).Location);
    }

    [Theory]
    [InlineData(HalfOrcs, "isengard", 0)]
    [InlineData(HalfOrcs, "isengard", 1)]
    [InlineData(OlogHai, "sauron", 0)]
    [InlineData(OlogHai, "sauron", 1)]
    public void ArmyRecruitmentPerformsOnlyAvailableFiguresWithoutExtraDecision(string card, string nation, int remaining)
    {
        var state = State(card);
        state.Control["westemnet"] = Side.Shadow;
        BattleFixture.Put(state, "southrons", UnitKind.Regular, "westemnet");
        Reserve(state, nation, UnitKind.Regular, remaining);
        Reserve(state, nation, UnitKind.Elite, 0);
        state = Play(state, card);
        Assert.Equal(remaining, state.Events.Count(e => e.Kind == "EventRecruited"));
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HillTrollsReplacesTwoChosenRegularsInOneOrTwoRegions(bool split)
    {
        var state = State(HillTrolls);
        var first = BattleFixture.Put(state, "sauron", UnitKind.Regular, "orthanc");
        first.Garrison = true;
        var second = BattleFixture.Put(state, "sauron", UnitKind.Regular, split ? "moria" : "orthanc");
        second.Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "gorgoroth");
        var wrong = BattleFixture.Put(state, "isengard", UnitKind.Regular, "orthanc");
        state = Play(state, HillTrolls);
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [first.Id, first.Id] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [first.Id, wrong.Id] });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [first.Id, second.Id] });
        Assert.All(new[] { first.Id, second.Id }, id => Assert.Equal("reserve", state.Units.Single(u => u.Id == id).Location));
        Assert.Equal(split ? 1 : 2, state.Units.Count(u => u.Nation == "sauron" && u.Kind == UnitKind.Elite && u.Location == "orthanc" && u.Garrison));
        Assert.Equal(split ? 1 : 0, state.Units.Count(u => u.Nation == "sauron" && u.Kind == UnitKind.Elite && u.Location == "moria" && u.Garrison));
        Assert.Equal(2, state.Events.Count(e => e.Kind == "EventUnitReplaced"));
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 2)]
    [InlineData(2, 0)]
    [InlineData(2, 1)]
    [InlineData(2, 2)]
    public void HillTrollsNeverRemovesARegularWithoutAnElite(int regulars, int elites)
    {
        var state = State(HillTrolls);
        for (var i = 0; i < regulars; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, "gorgoroth");
        Reserve(state, "sauron", UnitKind.Elite, elites);
        state = Play(state, HillTrolls);
        var expected = Math.Min(regulars, elites);
        if (state.Pending!.Kind == DecisionKind.EffectChoice)
            state = Do(state, CommandKind.ChooseEffect, new() { Ids = state.Pending.PrivateChoices.Take(expected).ToList() });
        Assert.Equal(expected, state.Units.Count(u => u.Kind == UnitKind.Elite && u.Location == "gorgoroth"));
        Assert.Equal(regulars - expected, state.Units.Count(u => u.Kind == UnitKind.Regular && u.Location == "gorgoroth"));
        Assert.Empty(state.Resolution.Frames);
    }
}
