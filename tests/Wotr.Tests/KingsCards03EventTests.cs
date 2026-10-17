using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Domain.KingsCards03;
using static Wotr.Tests.KingsCards03Fixture;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsCards03EventTests
{
    public static IEnumerable<object[]> Cards => Ids.Select(id => new object[] { id });

    [Theory]
    [MemberData(nameof(Cards))]
    public void EveryEventValidatesCostModuleOwnerHandAndWindowWithoutMutation(string card)
    {
        var state = Ready(card);
        var payload = EventCost(state, card, CardCatalog.All[card].EventType);
        var command = BattleFixture.Command(state, CommandKind.PlayEvent, payload);
        var before = StateCodec.Write(state);
        Assert.True(new PreviewService(LordsFixture.Engine()).Preview(state, Side.Shadow, command).Legal);
        Assert.Equal(before, StateCodec.Write(state));
        Assert.Equal("NotAuthorized", LordsFixture.Engine().Validate(state, Side.FreePeoples, command));
        payload.CardCost = DieFace.Character;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        payload.CardCost = CardCatalog.All[card].EventType;
        state.Configuration.Kings = false;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        state.Configuration.Kings = true;
        state.Player(Side.Shadow).Hand.Remove(card);
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        state.Player(Side.Shadow).Hand.Add(card);
        state.Dice.Single(d => d.Id == payload.DieId).Location = DieLocation.Spent;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        state.Dice.Single(d => d.Id == payload.DieId).Location = DieLocation.Available;
        state.Phase = GamePhase.Fellowship;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
    }

    [Theory]
    [MemberData(nameof(Cards))]
    public void KingsMetadataEnablesOnlyItsOwnReplacementAndBothPrograms(string card)
    {
        var state = Ready(card);
        Assert.Contains(card, EffectCatalog.All.Events.Keys);
        Assert.Contains(card, EffectCatalog.All.Combats.Keys);
        Assert.True(CardCatalog.Enabled(state, card));
        var replaced = CardCatalog.Kings[card].Replaces;
        if (replaced.Length > 0)
            Assert.False(CardCatalog.Enabled(state, replaced));
        state.Configuration.Kings = false;
        Assert.False(CardCatalog.Enabled(state, card));
        if (replaced.Length > 0)
            Assert.True(CardCatalog.Enabled(state, replaced));
    }
}
