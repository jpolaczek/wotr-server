using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards01;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy01Fixture;

namespace Wotr.Tests;

public class ShadowStrategy01PreviewTests
{
    [Theory]
    [InlineData(UrukHai, 0, false)]
    [InlineData(UrukHai, 1, true)]
    [InlineData(UrukHai, 3, true)]
    [InlineData(Dawn, 0, true)]
    [InlineData(Dawn, 1, true)]
    [InlineData(Dawn, 2, false)]
    public void SacrificePreviewSeparatesKnownLossesFromFutureRandomResults(string card, int sacrifices, bool unknown)
    {
        var state = Shadow01Fixture.Reveal(Combat(card, own: card == Dawn ? 2 : 3), card);
        var command = BattleFixture.Command(state, CommandKind.TakeBattleLosses, Sacrifice(state, sacrifices));
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow, command);
        Assert.True(preview.Legal);
        Assert.Equal(unknown, preview.HasUnknownResult);
        Assert.Equal(sacrifices, preview.RemovedIds.Count);
        Assert.Equal(before, StateCodec.Write(state));
    }

    [Fact]
    public void StormcrowPreviewIncludesLastArmyLeaderLossAndKeepsCompanion()
    {
        var state = State(Stormcrow);
        var elite = BattleFixture.Put(state, "gondor", UnitKind.Elite, "dol-amroth");
        var leader = BattleFixture.Put(state, "gondor", UnitKind.Leader, "dol-amroth");
        var companion = Put(state, CharacterRules.Grey, "dol-amroth");
        state = Play(state, Stormcrow);
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples,
            BattleFixture.Command(state, CommandKind.ChooseEffect, new() { Ids = [elite.Id] }));
        Assert.True(preview.Legal);
        Assert.Equal(new[] { elite.Id, leader.Id }.Order(), preview.RemovedIds.Order());
        Assert.DoesNotContain(companion.Id, preview.RemovedIds);
        Assert.Empty(preview.Placements);
        Assert.Equal(before, StateCodec.Write(state));
    }
}
