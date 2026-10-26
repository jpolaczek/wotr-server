using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards03;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy03Fixture;

namespace Wotr.Tests;

public class ShadowStrategy03PreviewTests
{
    [Fact]
    public void PlacementPreviewIsExactAndDoesNotMutateOrRevealOpponentHand()
    {
        var state = Play(State(NewPower), NewPower);
        var plan = Plan(state, ("isengard", UnitKind.Regular, "north-dunland", 2),
            ("isengard", UnitKind.Regular, "south-dunland", 2), ("isengard", UnitKind.Elite, "orthanc", 2));
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow, BattleFixture.Command(state, CommandKind.ChooseEffect, plan));
        Assert.True(preview.Legal);
        Assert.Equal(6, preview.Placements.Count);
        Assert.Equal(JsonSerializer.Serialize(plan.Placements), JsonSerializer.Serialize(preview.Placements));
        Assert.Equal(before, StateCodec.Write(state));
        var options = JsonSerializer.Serialize(new EffectOptions().GetOptions(state, Side.Shadow));
        EventTests.GiveCard(state, Side.FreePeoples, FreePeoplesCharacterCards02.Alone);
        Assert.Equal(options, JsonSerializer.Serialize(new EffectOptions().GetOptions(state, Side.Shadow)));
        Assert.Empty(new EffectOptions().GetOptions(state, Side.FreePeoples));
    }

    [Fact]
    public void OpponentPrivateCardsDoNotChangeRecruitmentProjection()
    {
        var state = Play(State(ManyKings), ManyKings);
        EventTests.GiveCard(state, Side.FreePeoples, FreePeoplesCharacterCards02.Alone);
        var projection = new Projection(new EffectOptions());
        var before = JsonSerializer.Serialize(projection.For(state, Side.Shadow));
        var enemy = state.Player(Side.FreePeoples);
        // UI-361: a hand card's deck is public (BASE 2.4.1 pp. 14, 29), so the hidden card changes within its deck.
        var hidden = enemy.Hand[0];
        enemy.Hand[0] = enemy.Decks["character"][0];
        enemy.Decks["character"][0] = hidden;
        Assert.Equal(before, JsonSerializer.Serialize(projection.For(state, Side.Shadow)));
        Assert.Null(projection.For(state, Side.FreePeoples).Decision);
        Assert.Empty(projection.For(state, Side.FreePeoples).Options);
    }

    [Fact]
    public void UpgradePreviewIdentifiesBothReplacementsWithoutRemovingRegulars()
    {
        var state = State(HillTrolls);
        var first = BattleFixture.Put(state, "sauron", UnitKind.Regular, "orthanc");
        var second = BattleFixture.Put(state, "sauron", UnitKind.Regular, "moria");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "gorgoroth");
        state = Play(state, HillTrolls);
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow,
            BattleFixture.Command(state, CommandKind.ChooseEffect, new() { Ids = [first.Id, second.Id] }));
        Assert.True(preview.Legal);
        Assert.Equal(new[] { first.Id, second.Id }, preview.RemovedIds);
        Assert.Equal(new[] { "orthanc", "moria" }, preview.Placements.Select(p => p.RegionId));
        Assert.All(preview.Placements, p => Assert.Equal(UnitKind.Elite, state.Units.Single(u => u.Id == p.UnitId).Kind));
        Assert.Equal(before, StateCodec.Write(state));
    }
}
