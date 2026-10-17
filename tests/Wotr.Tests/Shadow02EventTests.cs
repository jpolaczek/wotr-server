using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards02;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;

namespace Wotr.Tests;

public class Shadow02EventTests
{
    [Theory]
    [InlineData(FoulThing)]
    [InlineData(Candles)]
    [InlineData(Search)]
    [InlineData(Weather)]
    [InlineData(Strike)]
    [InlineData(Wound)]
    public void EveryEventRequiresOwnedCardCorrectCostActorAndWindow(string card)
    {
        var state = BeforeEvent(card);
        state.Fellowship.Progress = 1;
        state.Fellowship.Hidden = false;
        var cost = EventCost(state, card);
        var command = BattleFixture.Command(state, CommandKind.PlayEvent, cost);
        Assert.Null(Engine().Validate(state, Side.Shadow, command));
        Assert.NotNull(Engine().Validate(state, Side.FreePeoples, command));
        cost.CardCost = DieFace.Army;
        Reject(state, CommandKind.PlayEvent, cost);
        cost.CardCost = DieFace.Character;
        var die = state.Dice.Single(d => d.Id == cost.DieId);
        die.Location = DieLocation.Spent;
        Reject(state, CommandKind.PlayEvent, cost);
        die.Location = DieLocation.Available;
        HandRules.Remove(state.Player(Side.Shadow), card);
        Reject(state, CommandKind.PlayEvent, cost);
        HandRules.Add(state.Player(Side.Shadow), card);
        die.Face = DieFace.Event;
        cost.CardCost = DieFace.Event;
        Assert.Null(Engine().Validate(state, Side.Shadow, command));
        state.Pending!.Kind = DecisionKind.CharacterEvent;
        Reject(state, CommandKind.PlayEvent, cost);
    }

    [Theory]
    [InlineData(FoulThing)]
    [InlineData(Candles)]
    public void OriginalFreeSettlementsProtectEvenWhenCapturedButFortificationsDoNot(string card)
    {
        var state = BeforeEvent(card);
        var condition = EffectCatalog.Base.Events[card].Condition;
        foreach (var region in BoardRules.Regions.Values)
        {
            state.Fellowship.RegionId = region.Id;
            state.Control[region.Id] = Side.Shadow;
            Assert.Equal(!BoardRules.IsSettlement(region) || region.OriginalOwner != Side.FreePeoples, condition.CanPlay(state, Side.Shadow));
            state.Control[region.Id] = Side.FreePeoples;
            Assert.Equal(!BoardRules.IsSettlement(region) || region.OriginalOwner != Side.FreePeoples, condition.CanPlay(state, Side.Shadow));
        }
        state.Fellowship.MordorStep = 3;
        Assert.True(condition.CanPlay(state, Side.Shadow));
    }

    [Theory]
    [InlineData(Search)]
    [InlineData(Weather)]
    [InlineData(Strike)]
    public void ProgressConditionRequiresFellowshipTrackAndPermitsFreeSettlements(string card)
    {
        var state = BeforeEvent(card, "rivendell");
        Reject(state, CommandKind.PlayEvent, EventCost(state, card));
        state.Fellowship.Progress = 1;
        Assert.Null(Engine().Validate(state, Side.Shadow, BattleFixture.Command(state, CommandKind.PlayEvent, EventCost(state, card))));
        state.Fellowship.MordorStep = 1;
        Reject(state, CommandKind.PlayEvent, EventCost(state, card));
    }

    [Theory]
    [InlineData(false, false, 2)]
    [InlineData(false, true, 2)]
    [InlineData(true, false, 1)]
    [InlineData(true, true, 1)]
    public void CandlesUsesThreeDiceAndGollumsThresholdWithoutHuntInterventions(bool gollum, bool mordor, int damage)
    {
        var state = BeforeEvent(Candles, mordor: mordor);
        state.Player(Side.FreePeoples).Table.AddRange([FreePeoplesCharacterCards01.Axe, FreePeoplesCharacterCards01.Mithril, FreePeoplesCharacterCards02.Staff]);
        if (gollum)
        {
            state.Fellowship.Companions.Clear();
            state.Fellowship.Guide = CharacterRules.Gollum;
        }
        state.Fellowship.Corruption = 2;
        var companions = state.Fellowship.Companions.Count;
        state = Play(state, Candles, 2, 3, 5);
        Assert.Equal(2 + damage, state.Fellowship.Corruption);
        Assert.Equal(companions, state.Fellowship.Companions.Count);
        Assert.True(state.Fellowship.Hidden);
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Equal(3, state.Player(Side.FreePeoples).Table.Count);
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.Events, e => e.Kind.StartsWith("Hunt", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0, 2, false)]
    [InlineData(3, 5, true)]
    [InlineData(4, 5, false)]
    [InlineData(11, 12, true)]
    public void WoundRequiresRevealedFellowshipAndUsesCorrectCorruptionBoundary(int before, int after, bool mordor)
    {
        var state = BeforeEvent(Wound, mordor: mordor);
        state.Fellowship.Corruption = before;
        Reject(state, CommandKind.PlayEvent, EventCost(state, Wound));
        state.Fellowship.Hidden = false;
        state.Player(Side.FreePeoples).Table.Add(FreePeoplesCharacterCards02.Staff);
        var companions = state.Fellowship.Companions.Count;
        state = Play(state, Wound);
        Assert.Equal(after, state.Fellowship.Corruption);
        Assert.Equal(companions, state.Fellowship.Companions.Count);
        Assert.Empty(state.RandomSamples);
        Assert.DoesNotContain(state.Events, e => e.Kind.StartsWith("Hunt", StringComparison.Ordinal));
        Assert.Equal(after >= 12 ? Side.Shadow : null, state.Winner);
        if (after >= 12)
            Assert.Null(state.Pending);
    }

    [Fact]
    public void CandlesChecksVictoryImmediately()
    {
        var state = BeforeEvent(Candles);
        state.Fellowship.Corruption = 10;
        state = Play(state, Candles, 3, 3, 0);
        Assert.Equal(VictoryCondition.RingCorrupted, state.Victory);
        Assert.Null(state.Pending);
        Assert.Equal(3, state.RandomSamples.Count);
    }
}
