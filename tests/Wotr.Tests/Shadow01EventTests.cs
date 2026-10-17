using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards01;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;

namespace Wotr.Tests;

public class Shadow01EventTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void SpecialCardsPrepareOnlyTheirTileAndNeverDuplicateIt(int index, bool mordor)
    {
        var (card, tile) = SpecialTiles[index];
        var state = Play(BeforeEvent(card, mordor: mordor), card);
        Assert.Equal(mordor ? "pool" : "prepared", state.HuntTiles.Single(t => t.Id == tile).Location);
        Assert.Equal(3, state.HuntTiles.Count(t => HuntPool.Definitions[t.Id].Pool == "special-sh" && t.Location == "set-aside"));
        Assert.Contains(card, state.Player(Side.Shadow).Discard);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Empty(state.RandomSamples);
        TurnFlow.SelectNext(state, Side.Shadow);
        HandRules.Add(state.Player(Side.Shadow), card);
        Reject(state, CommandKind.PlayEvent, EventCost(state, card));
        if (!mordor)
        {
            state.Phase = GamePhase.Fellowship;
            state.Fellowship.RegionId = "morannon";
            TurnFlow.OpenFellowshipPhase(state);
            state = Do(state, CommandKind.EnterMordor);
            Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == tile).Location);
        }
    }

    [Theory]
    [InlineData(0, 4, false)]
    [InlineData(1, 2, true)]
    [InlineData(2, 3, false)]
    [InlineData(3, 1, true)]
    public void PreparedTilesResolveTheirActualMordorDamageRevealAndStop(int index, int damage, bool reveal)
    {
        var (card, tile) = SpecialTiles[index];
        var state = Play(BeforeEvent(card, mordor: true), card);
        Pool(state, tile);
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Take(2))
            die.Location = DieLocation.Hunt;
        TurnFlow.SelectNext(state, Side.FreePeoples);
        var cost = Cost(state, DieFace.Character);
        state = Do(state, CommandKind.MoveFellowship, cost, new SequenceRandom(0, 3));
        var hunt = HuntRuntime.Active(state)!;
        Assert.Equal(damage, hunt.Damage);
        Assert.Equal(reveal, hunt.Reveal);
        Assert.True(hunt.Stop);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal(damage, state.Fellowship.Corruption);
        Assert.Equal(2, state.Fellowship.MordorStep);
        Assert.Equal(!reveal, state.Fellowship.Hidden);
        Assert.Equal("discard", state.HuntTiles.Single(t => t.Id == tile).Location);
    }

    [Theory]
    [InlineData("rivendell", false)]
    [InlineData("minas-tirith", false)]
    [InlineData("the-shire", false)]
    [InlineData("moria", true)]
    [InlineData("osgiliath", true)]
    [InlineData("hollin", true)]
    [InlineData("mordor-track", true)]
    public void HuntEventsUseOriginalSettlementRegardlessOfControlOrProgress(string region, bool legal)
    {
        foreach (var card in new[] { OrcPatrol, IsildursBane })
        {
            var state = BeforeEvent(card, region, region == MordorRules.Track);
            var payload = EventCost(state, card);
            state.Fellowship.Progress = 7;
            foreach (var control in new[] { Side.FreePeoples, Side.Shadow })
            {
                state.Control[region] = control;
                var error = Engine().Validate(state, Side.Shadow, BattleFixture.Command(state, CommandKind.PlayEvent, payload));
                Assert.Equal(legal, error is null);
                if (!legal)
                    Reject(state, CommandKind.PlayEvent, payload);
            }
        }
    }

    [Theory]
    [InlineData(OrcPatrol, HuntTests.Eye)]
    [InlineData(IsildursBane, HuntTests.Eye)]
    [InlineData(OrcPatrol, "base.hunt.special-sh.eye.1.1")]
    [InlineData(IsildursBane, "base.hunt.special-sh.eye.1.1")]
    [InlineData(OrcPatrol, "base.hunt.special-fp.-2.0.1")]
    [InlineData(IsildursBane, "base.hunt.special-fp.0.0.1")]
    public void EyeAndFellowshipTilesAreDiscardedWithoutHealingRevealOrFurtherChoices(string card, string tile)
    {
        var state = BeforeEvent(card, mordor: true);
        state.Fellowship.Corruption = 4;
        var attempt = state.Fellowship.LastMordorAttemptTurn;
        state.Player(Side.FreePeoples).Table.Add(FreePeoplesCharacterCards01.Axe);
        Pool(state, tile);
        state = Play(state, card, 0);
        Assert.Equal(4, state.Fellowship.Corruption);
        Assert.True(state.Fellowship.Hidden);
        Assert.Equal(2, state.Fellowship.MordorStep);
        Assert.Equal(attempt, state.Fellowship.LastMordorAttemptTurn);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.RandomSamples);
        Assert.Single(state.Events, e => e.Kind == "HuntTileIgnored");
        Assert.DoesNotContain(state.Events, e => e.Kind == "HuntCorruption");
        Assert.Contains(FreePeoplesCharacterCards01.Axe, state.Player(Side.FreePeoples).Table);
    }

    [Theory]
    [InlineData(OrcPatrol)]
    [InlineData(IsildursBane)]
    public void ShelobRollsOnceWithoutMovementAndStaffPreventsTheWholeDraw(string card)
    {
        var state = BeforeEvent(card, mordor: true);
        Pool(state, SpecialTiles[0].Tile);
        state = Play(state, card, 0, 3);
        if (card == OrcPatrol)
        {
            Assert.False(HuntRuntime.Active(state)!.Stop);
            Assert.Equal(4, HuntRuntime.Active(state)!.TileDie);
            state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        }
        Assert.Equal(4, state.Fellowship.Corruption);
        Assert.Equal(2, state.Fellowship.MordorStep);
        Assert.Equal(2, state.RandomSamples.Count);
        state = BeforeEvent(card);
        state.Player(Side.FreePeoples).Table.Add(FreePeoplesCharacterCards02.Staff);
        state = Play(state, card);
        Assert.Equal("hunt.draw", state.Pending!.Window);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = FreePeoplesCharacterCards02.StaffReaction });
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Empty(state.RandomSamples);
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }
}
