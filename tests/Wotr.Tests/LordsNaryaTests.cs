using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsFixture;

namespace Wotr.Tests;

public class LordsNaryaTests
{
    [Theory]
    [InlineData(DieFace.Character)]
    [InlineData(DieFace.Army)]
    [InlineData(DieFace.Muster)]
    [InlineData(DieFace.ArmyMuster)]
    [InlineData(DieFace.Event)]
    [InlineData(DieFace.WillOfTheWest)]
    [InlineData(DieFace.CardDraw)]
    public void NaryaUsesAnyAvailableDieAndItsOwnRingForTheCurrentSettlementsNation(DieFace face)
    {
        var state = Ready();
        state.Dice[0].Face = face;
        var payload = Cost(state);
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples, BattleFixture.Command(state, CommandKind.UseNarya, payload));
        Assert.True(preview.Legal);
        Assert.Equal(new[] { payload.DieId, LordsDiceSetup.NaryaRing }, preview.Costs);
        Assert.Equal("gondor", Assert.Single(preview.PoliticsChanges).Id);
        Assert.Equal(before, StateCodec.Write(state));
        state = Do(StateCodec.Copy(state), CommandKind.UseNarya, payload);
        var gondor = state.Nations.Single(n => n.Id == "gondor");
        Assert.True(gondor.Active);
        Assert.Equal(0, gondor.StepsToWar);
        Assert.Equal(Side.Shadow, state.Rings.Single(r => r.Id == LordsDiceSetup.NaryaRing).Owner);
        Assert.False(state.Player(Side.FreePeoples).RingUsedThisTurn);
        Assert.Equal(DieLocation.Spent, Die(state, payload.DieId).Location);
        Assert.Empty(state.Lords!.PaidWillOfWestIds);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
    }

    [Theory]
    [InlineData("conquered")]
    [InlineData("wilderness")]
    [InlineData("shadow-settlement")]
    [InlineData("fellowship")]
    [InlineData("dead")]
    [InlineData("wrong-ring")]
    [InlineData("wrong-nation")]
    [InlineData("spent-die")]
    [InlineData("enemy-die")]
    [InlineData("no-ring")]
    [InlineData("base")]
    public void NaryaRejectsUnavailableSourcesAndForgedCostsWithoutMutation(string invalid)
    {
        var state = Ready();
        var payload = Cost(state);
        var keeper = state.Characters.Single(c => c.DefinitionId == LordsDiceCatalog.KeeperGandalf);
        switch (invalid)
        {
            case "conquered": state.Control[keeper.Location] = Side.Shadow; break;
            case "wilderness": keeper.Location = "hollin"; break;
            case "shadow-settlement": keeper.Location = "orthanc"; break;
            case "fellowship": keeper.Location = "fellowship"; break;
            case "dead": keeper.Location = "casualties"; break;
            case "wrong-ring": payload.RingId = LordsDiceSetup.VilyaRing; break;
            case "wrong-nation": payload.NationId = "elves"; break;
            case "spent-die": state.Dice[0].Location = DieLocation.Spent; break;
            case "enemy-die": payload.DieId = state.Dice.First(d => d.Owner == Side.Shadow).Id; break;
            case "no-ring": state.Rings.Single(r => r.Id == LordsDiceSetup.NaryaRing).Owner = Side.Shadow; break;
            case "base": state.Configuration.Lords = false; break;
        }
        Reject(state, CommandKind.UseNarya, payload);
    }

    [Fact]
    public void ReconqueredSettlementWorksAfterNormalRingUseAndNaryaDoesNotResetItsLimit()
    {
        var state = Ready();
        state.Control["minas-tirith"] = Side.Shadow;
        Assert.Null(KeeperRings.NaryaNation(state));
        state.Control["minas-tirith"] = Side.FreePeoples;
        state = Do(state, CommandKind.UseRing, new() { RingId = LordsDiceSetup.NenyaRing, DieId = state.Dice[0].Id, Face = DieFace.Muster });
        Assert.True(state.Player(Side.FreePeoples).RingUsedThisTurn);
        state = Do(state, CommandKind.UseNarya, Cost(state));
        Assert.True(state.Player(Side.FreePeoples).RingUsedThisTurn);
        Assert.False(state.Player(Side.FreePeoples).RingActionPending);
        Assert.Equal(Side.FreePeoples, state.Rings.Single(r => r.Id == LordsDiceSetup.VilyaRing).Owner);
    }

    [Fact]
    public void NaryaIsACompanionAbilityForWormtongueAndThreatsAndPromises()
    {
        var state = Ready();
        Put(state, LordsDiceCatalog.KeeperGandalf, "edoras");
        Put(state, CharacterRules.Saruman, "orthanc");
        state.ActiveEffects.Add(new() { Id = "worm", SourceId = ShadowCharacterCards04.Wormtongue, Owner = Side.Shadow,
            Modifier = EffectModifier.RestrictRohanActivation, ExpiresWhen = EffectExpiry.CharacterLeavesPlay, TargetId = CharacterRules.Saruman });
        state.ActiveEffects.Add(new() { Id = "threats", SourceId = ShadowStrategyCards01.Threats, Owner = Side.Shadow,
            Modifier = EffectModifier.PreventPassiveMusterAdvance, ExpiresWhen = EffectExpiry.FreeNationAdvancedByAttackOrCompanion });
        var cards = new[] { ShadowCharacterCards04.Wormtongue, ShadowStrategyCards01.Threats };
        foreach (var deck in state.Player(Side.Shadow).Decks.Values)
            deck.RemoveAll(cards.Contains);
        state.Player(Side.Shadow).Table.AddRange(cards);
        var payload = Cost(state);
        payload.NationId = "rohan";
        state = Do(state, CommandKind.UseNarya, payload);
        Assert.True(state.Nations.Single(n => n.Id == "rohan").Active);
        Assert.Empty(state.ActiveEffects);
        Assert.Empty(state.Player(Side.Shadow).Table);
        Assert.All(cards, card => Assert.Contains(card, state.Player(Side.Shadow).Discard));
    }

    internal static MatchState Ready()
    {
        var state = Actions();
        Put(state, LordsDiceCatalog.KeeperGandalf, "minas-tirith");
        state.Nations.Single(n => n.Id == "gondor").Active = false;
        return state;
    }

    internal static CommandPayload Cost(MatchState state) => new()
    {
        DieId = state.Dice[0].Id,
        RingId = LordsDiceSetup.NaryaRing,
        NationId = "gondor"
    };
}
