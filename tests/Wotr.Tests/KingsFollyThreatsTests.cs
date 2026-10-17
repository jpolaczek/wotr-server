using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.KingsCards03;
using static Wotr.Tests.KingsCards03Fixture;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsFollyThreatsTests
{
    [Fact]
    public void FollyHasNoSiegeOrLeaderCostAndPersistsUntilManuallyRemoved()
    {
        var state = Ready(Denethor);
        var leader = BattleFixture.Put(state, "gondor", UnitKind.Leader, "minas-tirith");
        var unit = BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-tirith");
        state = Play(state, Denethor);
        Assert.Contains(Denethor, state.Player(Side.Shadow).Table);
        Assert.Equal("minas-tirith", state.Units.Single(u => u.Id == leader.Id).Location);
        Assert.Equal("minas-tirith", state.Units.Single(u => u.Id == unit.Id).Location);
        Assert.False(KingsCardRestrictions.CanAwaken(state, SovereignCatalog.Denethor));
        ActiveEffects.CheckConditions(state);
        Assert.Contains(Denethor, state.Player(Side.Shadow).Table);
    }

    [Theory]
    [InlineData(SovereignStatus.Initial, true)]
    [InlineData(SovereignStatus.Corrupted, true)]
    [InlineData(SovereignStatus.Awakened, false)]
    [InlineData(SovereignStatus.Removed, false)]
    public void FollyRequiresPresentNonAwakenedDenethor(SovereignStatus status, bool legal)
    {
        var state = Ready(Denethor);
        if (status == SovereignStatus.Corrupted)
            Corrupt(state, SovereignCatalog.Denethor);
        else if (status == SovereignStatus.Awakened)
            SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Denethor), "minas-tirith");
        else if (status == SovereignStatus.Removed)
            SovereignLifecycle.Remove(state, Sovereign(state, SovereignCatalog.Denethor));
        Assert.Equal(legal, new KingsDenethorCondition().CanPlay(state, Side.Shadow));
        if (!legal)
            LordsFixture.Reject(state, CommandKind.PlayEvent, Cost(state, Denethor));
    }

    [Fact]
    public void FollyBlocksTwoDistinctRegionsAndOnlyFreePeoplesCards()
    {
        var state = Table(Denethor);
        state.Units.Single(u => u.Id == SovereignCatalog.Denethor).Location = "osgiliath";
        foreach (var region in new[] { "minas-tirith", "osgiliath", "pelargir" })
        {
            var battle = new BattleState { Region = region };
            Assert.Equal(region != "pelargir", DenethorRules.BlocksCards(state, battle, Side.FreePeoples));
            Assert.False(DenethorRules.BlocksCards(state, battle, Side.Shadow));
        }
        SovereignLifecycle.Remove(state, Sovereign(state, SovereignCatalog.Denethor));
        Assert.True(DenethorRules.BlocksCards(state, new() { Region = "minas-tirith" }, Side.FreePeoples));
    }

    [Theory]
    [InlineData(Boromir)]
    [InlineData(CharacterRules.Grey)]
    [InlineData(CharacterRules.White)]
    [InlineData(CharacterRules.Aragorn)]
    public void FollyAnyDieRequiresNamedCompanionInDenethorsRegion(string character)
    {
        var state = Table(Denethor);
        var die = Die(state);
        die.Face = DieFace.Army;
        var payload = new CommandPayload { DieId = die.Id };
        LordsFixture.Reject(state, CommandKind.DiscardDenethor, payload);
        var companion = LordsFixture.Put(state, character, "osgiliath");
        LordsFixture.Reject(state, CommandKind.DiscardDenethor, payload);
        companion.Location = "minas-tirith";
        state = Do(state, CommandKind.DiscardDenethor, payload);
        Assert.Contains(Denethor, state.Player(Side.Shadow).Discard);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == die.Id).Location);
    }

    [Fact]
    public void FollyWillRemovalNeedsNoCompanionAndRejectsUnrelatedCharacter()
    {
        var state = Table(Denethor);
        Companion(state, Gimli, "minas-tirith");
        var die = Die(state);
        LordsFixture.Reject(state, CommandKind.DiscardDenethor, new() { DieId = die.Id });
        die.Face = DieFace.WillOfTheWest;
        state = Do(state, CommandKind.DiscardDenethor, new() { DieId = die.Id });
        Assert.True(KingsCardRestrictions.CanAwaken(state, SovereignCatalog.Denethor));
    }

    [Theory]
    [InlineData(DieFace.Muster)]
    [InlineData(DieFace.ArmyMuster)]
    [InlineData(DieFace.WillOfTheWest)]
    public void ThreatsBlocksNormalMusterAwakeningButKeepsRulerAlternative(DieFace face)
    {
        var state = Table(Threats);
        state.Nations.Single(n => n.Id == "gondor").Active = true;
        var die = Die(state);
        die.Face = face;
        var payload = new CommandPayload { CharacterId = SovereignCatalog.Denethor, Route = ["minas-tirith"], DieId = die.Id };
        LordsFixture.Reject(state, CommandKind.AwakenSovereign, payload);
        var ruler = Ruler(state, face);
        payload.DieId = ruler.Id;
        state = Do(state, CommandKind.AwakenSovereign, payload);
        Assert.Equal(SovereignStatus.Awakened, Sovereign(state, SovereignCatalog.Denethor).Status);
        Assert.Contains(Threats, state.Player(Side.Shadow).Table);
    }

    [Fact]
    public void ThreatsAllowsEventAwakeningAndBlocksOnlyPassiveMusterAdvance()
    {
        var state = Table(Threats);
        var nation = state.Nations.Single(n => n.Id == "north");
        nation.Active = false;
        Assert.False(PoliticalRules.CanMusterAdvance(state, nation));
        nation.Active = true;
        Assert.True(PoliticalRules.CanMusterAdvance(state, nation));
        Companion(state, Gimli, "minas-tirith");
        state = Play(state, KingsFreePeoplesCards01.Aid);
        state = Do(state, CommandKind.MoveCompanions);
        Assert.Equal(SovereignStatus.Awakened, Sovereign(state, SovereignCatalog.Denethor).Status);
        Assert.Contains(Threats, state.Player(Side.Shadow).Table);
    }

    [Theory]
    [InlineData(PoliticalAdvanceCause.Attack, true)]
    [InlineData(PoliticalAdvanceCause.CompanionAbility, true)]
    [InlineData(PoliticalAdvanceCause.Event, false)]
    [InlineData(PoliticalAdvanceCause.SettlementCapture, false)]
    [InlineData(PoliticalAdvanceCause.ActionDie, false)]
    public void ThreatsExpiresOnlyOnPrintedPoliticalAdvanceCauses(PoliticalAdvanceCause cause, bool expires)
    {
        var state = Table(Threats);
        var nation = state.Nations.Single(n => n.Id == "north");
        nation.StepsToWar = 2;
        PoliticalRules.Advance(state, "north", cause);
        Assert.Equal(!expires, state.Player(Side.Shadow).Table.Contains(Threats));
        Assert.Equal(expires, state.Player(Side.Shadow).Discard.Contains(Threats));
    }

    [Fact]
    public void ActivationAloneKeepsThreatsButSovereignCorruptionDiscardsItImmediately()
    {
        var state = Table(Threats);
        PoliticalRules.Activate(state, "north");
        Assert.Contains(Threats, state.Player(Side.Shadow).Table);
        Tile(state, SovereignCatalog.Brand);
        Tile(state, SovereignCatalog.Brand, HuntTests.Three);
        Assert.Contains(Threats, state.Player(Side.Shadow).Discard);
        Assert.DoesNotContain(state.ActiveEffects, e => e.SourceId == Threats);
    }
}
