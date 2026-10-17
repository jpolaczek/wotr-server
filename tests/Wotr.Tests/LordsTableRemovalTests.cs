using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class LordsTableRemovalTests
{
    [Theory]
    [InlineData(CharacterRules.Grey, true)]
    [InlineData(LordsDiceCatalog.KeeperGandalf, true)]
    [InlineData(CharacterRules.Gollum, true)]
    [InlineData(CharacterRules.Strider, false)]
    public void GatesAcceptsCharacterOnlyForEitherGreyOrGollumGuide(string guide, bool allowed)
    {
        var state = LordsShadowCardsFixture.Play(LordsShadowCardsFixture.State(), LordsShadowCards.Gates);
        if (guide == LordsDiceCatalog.KeeperGandalf)
        {
            var gandalf = CharacterCatalog.Companion(state, CharacterRules.Grey);
            CharacterLifecycle.ApplyVersion(gandalf, CharacterCatalog.All[guide]);
            if (!state.Characters.Contains(gandalf))
                state.Characters.Add(gandalf);
            state.Fellowship.Guide = CharacterRules.Grey;
        }
        else
            state.Fellowship.Guide = guide;
        var die = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available);
        die.Face = DieFace.Character;
        var payload = new CommandPayload { DieId = die.Id };
        if (!allowed)
        {
            LordsFixture.Reject(state, CommandKind.DiscardGates, payload);
            die.Face = DieFace.WillOfTheWest;
        }
        state = LordsFixture.Do(state, CommandKind.DiscardGates, payload);
        Assert.DoesNotContain(LordsShadowCards.Gates, state.Player(Side.Shadow).Table);
        Assert.Empty(state.ActiveEffects);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LoreRequiresWillOrAnyDieAndChosenAvailableRing(bool ring)
    {
        var state = LordsShadowCardsFixture.Play(LordsShadowCardsFixture.State(), LordsShadowCards.Lore);
        var die = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available);
        var id = die.Id;
        die.Face = DieFace.Army;
        var payload = new CommandPayload { DieId = id };
        LordsFixture.Reject(state, CommandKind.DiscardLore, payload);
        if (ring)
            payload.RingId = LordsDiceSetup.NenyaRing;
        else
            die.Face = DieFace.WillOfTheWest;
        state = LordsFixture.Do(state, CommandKind.DiscardLore, payload);
        Assert.DoesNotContain(LordsShadowCards.Lore, state.Player(Side.Shadow).Table);
        Assert.Empty(state.ActiveEffects);
        Assert.Equal(ring ? Side.Shadow : Side.FreePeoples, state.Rings.Single(r => r.Id == LordsDiceSetup.NenyaRing).Owner);
        Assert.Equal(ring, state.Player(Side.FreePeoples).RingUsedThisTurn);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == id).Location);
    }

    [Fact]
    public void LoreRejectsMissingOpponentAndAlreadyUsedRingWithoutSpendingAnything()
    {
        var state = LordsShadowCardsFixture.Play(LordsShadowCardsFixture.State(), LordsShadowCards.Lore);
        var die = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available);
        var payload = new CommandPayload { DieId = die.Id, RingId = LordsDiceSetup.NenyaRing };
        state.Player(Side.FreePeoples).RingUsedThisTurn = true;
        LordsFixture.Reject(state, CommandKind.DiscardLore, payload);
        state.Player(Side.FreePeoples).RingUsedThisTurn = false;
        state.Rings.Single(r => r.Id == payload.RingId).Owner = Side.Shadow;
        LordsFixture.Reject(state, CommandKind.DiscardLore, payload);
        payload.RingId = "missing";
        LordsFixture.Reject(state, CommandKind.DiscardLore, payload);
    }
}
