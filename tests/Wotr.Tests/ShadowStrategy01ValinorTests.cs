using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards01;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy01Fixture;

namespace Wotr.Tests;

public class ShadowStrategy01ValinorTests
{
    [Fact]
    public void ValinorHitsOnlyElvesCapsDiceAndKeepsLeadersWithOtherNations()
    {
        var state = State(Valinor);
        for (var i = 0; i < 4; i++)
            BattleFixture.Put(state, "elves", UnitKind.Regular, "lorien");
        var other = BattleFixture.Put(state, "north", UnitKind.Regular, "lorien");
        var leader = BattleFixture.Put(state, "elves", UnitKind.Leader, "lorien");
        Put(state, "base.character.legolas", "lorien");
        state = Play(state, Valinor, 5, 5, 5, 5, 5);
        Assert.Equal(5, EventLosses.Current(state)!.Hits);
        Assert.Equal(5, state.RandomSamples.Count);
        Reject(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(other.Id)] });
        var elves = state.Units.Where(u => u.Location == "lorien" && u.Nation == "elves" && u.Kind == UnitKind.Regular).ToList();
        state = Do(state, CommandKind.TakeEventLosses, new() { Losses = elves.Select(u => BattleFixture.Loss(u.Id)).ToList() });
        Assert.Equal("lorien", state.Units.Single(u => u.Id == leader.Id).Location);
        Assert.Equal("lorien", CharacterRules.InPlay(state, "base.character.legolas")!.Location);
        Assert.Empty(state.PoliticalReactions);
    }

    [Fact]
    public void ValinorRequiresCapturedStrongholdSkipsBothKindsOfSiegeAndResolvesEveryOtherRegion()
    {
        var state = State(Valinor);
        state.Control["grey-havens"] = Side.FreePeoples;
        Reject(state, CommandKind.PlayEvent, EventCost(state, Valinor));
        state.Control["grey-havens"] = Side.Shadow;
        BattleFixture.Put(state, "elves", UnitKind.Regular, "grey-havens");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "grey-havens").Garrison = true;
        BattleFixture.Put(state, "elves", UnitKind.Regular, "woodland-realm").Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "woodland-realm");
        BattleFixture.Put(state, "elves", UnitKind.Regular, "rivendell");
        state = Play(state, Valinor);
        Assert.Equal(new[] { "lorien", "rivendell" }, Shadow01Fixture.Choices(state).EligibleIds.Order());
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["grey-havens"] });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["rivendell"] }, new SequenceRandom(0, 0, 0));
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(2, state.Events.Count(e => e.Kind == "ReturnToValinor"));
    }

    [Fact]
    public void ValinorEliminatesUnaccompaniedLeaderButNotCompanion()
    {
        var state = State(Valinor);
        state.Units.Single(u => u.Location == "lorien" && u.Kind == UnitKind.Elite).Location = "reserve";
        var leader = BattleFixture.Put(state, "elves", UnitKind.Leader, "lorien");
        Put(state, "base.character.legolas", "lorien");
        state = Play(state, Valinor, 5);
        var unit = state.Units.Single(u => u.Location == "lorien" && u.Kind == UnitKind.Regular);
        state = Do(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(unit.Id)] });
        Assert.Equal("casualties", state.Units.Single(u => u.Id == leader.Id).Location);
        Assert.Equal("lorien", CharacterRules.InPlay(state, "base.character.legolas")!.Location);
    }
}
