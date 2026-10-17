using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.KingsCards02;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsWelcomeTests
{
    internal static MatchState OnTable()
    {
        var state = Play(State(Side.Shadow), Welcome);
        Assert.Contains(Welcome, state.Player(Side.Shadow).Table);
        Assert.DoesNotContain(Welcome, state.Player(Side.Shadow).Discard);
        return state;
    }

    [Fact]
    public void TableRestrictionsPersistAndDiscardRestoresBothRules()
    {
        var state = StateCodec.Copy(OnTable());
        Assert.False(KingsCardRestrictions.CanRemoveCorruption(state));
        Assert.False(KingsCardRestrictions.CanAwaken(state, SovereignCatalog.Theoden));
        state.Nations.Single(n => n.Id == "rohan").StepsToWar = 0;
        Assert.True(KingsCardRestrictions.CanAwaken(state, SovereignCatalog.Theoden));
        var die = Die(state);
        die.Face = DieFace.WillOfTheWest;
        state = Do(state, CommandKind.DiscardWelcome, new() { DieId = die.Id });
        Assert.Contains(Welcome, state.Player(Side.Shadow).Discard);
        Assert.DoesNotContain(Welcome, state.Player(Side.Shadow).Table);
        Assert.True(KingsCardRestrictions.CanRemoveCorruption(state));
        Assert.True(KingsCardRestrictions.CanAwaken(state, SovereignCatalog.Dain));
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == die.Id).Location);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CharacterCostNeedsDistinctCompanionWithInitialOrAwakenedSovereign(bool awakened)
    {
        var state = OnTable();
        if (awakened)
            SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Dain), "erebor");
        var die = Die(state);
        die.Face = DieFace.Character;
        var payload = new CommandPayload { DieId = die.Id };
        LordsFixture.Reject(state, CommandKind.DiscardWelcome, payload);
        Companion(state, Gimli, "eastemnet");
        LordsFixture.Reject(state, CommandKind.DiscardWelcome, payload);
        state.Characters.Single(c => c.Id == Gimli).Location = "erebor";
        Assert.Contains(die.Id, KingsWelcomeRemoval.Option(state, Side.FreePeoples)!.EligibleDice);
        state = Do(state, CommandKind.DiscardWelcome, payload);
        Assert.DoesNotContain(Welcome, state.Player(Side.Shadow).Table);
    }

    [Fact]
    public void CorruptedPairWrongDieRingWrongOwnerAndMissingCardCannotPayRemoval()
    {
        var state = OnTable();
        Companion(state, Boromir, "minas-tirith");
        Tile(state, SovereignCatalog.Denethor, HuntTests.Three);
        var die = Die(state);
        die.Face = DieFace.Character;
        LordsFixture.Reject(state, CommandKind.DiscardWelcome, new() { DieId = die.Id });
        die.Face = DieFace.Muster;
        LordsFixture.Reject(state, CommandKind.DiscardWelcome, new() { DieId = die.Id });
        die.Face = DieFace.WillOfTheWest;
        LordsFixture.Reject(state, CommandKind.DiscardWelcome, new() { DieId = die.Id, RingId = state.Rings[0].Id });
        Assert.Null(KingsWelcomeRemoval.Option(state, Side.Shadow));
        state.Player(Side.Shadow).Table.Clear();
        LordsFixture.Reject(state, CommandKind.DiscardWelcome, new() { DieId = die.Id });
    }

    [Fact]
    public void OrdinaryTurnBoundaryDoesNotExpireWelcome()
    {
        var state = OnTable();
        ActiveEffects.ExpireBoundary(state, EffectDuration.EndTurn);
        Assert.Contains(Welcome, state.Player(Side.Shadow).Table);
        Assert.False(KingsCardRestrictions.CanRemoveCorruption(state));
    }
}
