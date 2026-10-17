using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsBoundaryTests
{
    [Theory]
    [InlineData(KingsTiles.One, 1)]
    [InlineData(KingsTiles.Two, 2)]
    public void KingsTilesKeepTheirPrintedDamageForOrdinaryHunt(string tile, int damage)
    {
        var state = Actions();
        Eyes(state, 1);
        Die(state).Face = DieFace.Character;
        HuntTests.Pool(state, HuntTests.Three, tile);
        state = Do(state, CommandKind.MoveFellowship, new() { DieId = Die(state).Id }, 5, 1);
        Assert.Equal(damage, HuntRuntime.Active(state)!.Damage);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal(damage, state.Fellowship.Corruption);
        Assert.Equal("discard", state.HuntTiles.Single(t => t.Id == tile).Location);
    }

    [Theory]
    [InlineData(SovereignStatus.Awakened)]
    [InlineData(SovereignStatus.Corrupted)]
    [InlineData(SovereignStatus.Removed)]
    public void NonInitialSovereignsRejectAttemptsAndTileAttachmentWithoutMutation(SovereignStatus status)
    {
        var state = Actions(Side.Shadow);
        Eyes(state, 3);
        var sovereign = Sovereign(state, SovereignCatalog.Brand);
        sovereign.Status = status;
        var payload = new CommandPayload { CharacterId = sovereign.Id, DieId = CorruptionRules.Eyes(state).First().Id };
        LordsFixture.Reject(state, CommandKind.AttemptCorruption, payload);
        var before = StateCodec.Write(state);
        Assert.Throws<InvalidOperationException>(() => CorruptionRules.Attach(state, sovereign, KingsTiles.One));
        Assert.Equal(before, StateCodec.Write(state));
    }

    [Fact]
    public void PoliticalRemovalRandomizesAmongHiddenTilesAndNeverChoosesKings()
    {
        var state = KingsPoliticsTests.Ready();
        var sovereign = Sovereign(state, SovereignCatalog.Dain);
        sovereign.Tiles.Remove(HuntTests.Three);
        state.HuntTiles.Single(t => t.Id == HuntTests.Three).Location = "pool";
        var two = HuntPool.Definitions.Values.First(t => t.Pool == "standard" && t.Value == "2" && !KingsTiles.IsKings(t.Id)).Id;
        CorruptionRules.Attach(state, sovereign, KingsCorruptionTests.One);
        CorruptionRules.Attach(state, sovereign, two);
        state = Do(state, CommandKind.AdvancePolitics, new() { NationId = "dwarves", DieId = Die(state).Id });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["remove"] }, 1);
        Assert.Equal(new[] { KingsTiles.One, KingsCorruptionTests.One }, Sovereign(state, sovereign.Id).Tiles);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == two).Location);
        Assert.DoesNotContain(two, Json(state, Side.FreePeoples));
    }

    [Theory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public void DaringAndCorruptedBrandRespectDefenderPriorityAtInitiativeZero(Side attacker)
    {
        var state = KingsBattleTests.Battle(SovereignCatalog.Brand, false, attacker);
        var fp = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        var strider = new CharacterState { Id = CharacterRules.Strider, Location = fp.Region };
        CharacterLifecycle.ApplyVersion(strider, CharacterCatalog.All[strider.Id]);
        state.Characters.Add(strider);
        fp.Participants.Add(strider.Id);
        state = KingsBattleTests.Cards(state, FreePeoplesCharacterCards02.Alone, KingsCards03.Valinor);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["replace"] }, 0, 0, 0, 0, 0);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(attacker == Side.FreePeoples, battle.Force(Side.FreePeoples).Round.CardCancelled);
        Assert.Equal(attacker == Side.Shadow, battle.Force(Side.Shadow).Round.CardCancelled);
        Assert.True(battle.Kings!.BrandUsed);
        Assert.NotEmpty(battle.Force(Side.Shadow).Round.CardId);
    }

    [Fact]
    public void AnarionAcceptsRulerWillButDoesNotSurviveDenethorsDeathDuringTheEvent()
    {
        var state = KingsAnarionTests.Ready();
        var die = state.Dice.Single(d => d.Owner == Side.FreePeoples && d.Source == SovereignCatalog.RulerSource);
        die.Face = DieFace.WillOfTheWest;
        state.Player(Side.FreePeoples).Hand = ["base.fp.strategy.15"];
        state = Do(state, CommandKind.PlayEvent, new() { CardId = "base.fp.strategy.15", CardCost = DieFace.Event, DieId = die.Id });
        Assert.True(EffectRuntime.Current(state)!.Effect!.AnarionDrawPending);
        CharacterLifecycle.Eliminate(state, state.Characters.Single(c => c.Id == SovereignCatalog.Denethor));
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [state.Pending!.PrivateChoices[0]] });
        Assert.Single(state.Player(Side.FreePeoples).Hand);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == die.Id).Location);
    }

    [Fact]
    public void ThranduilDoesNotAutomaticallyReduceAnOrdinaryRulerMovement()
    {
        var state = KingsFarSightedTests.Ready();
        Eyes(state, 2);
        var die = state.Dice.Single(d => d.Owner == Side.FreePeoples && d.Source == SovereignCatalog.RulerSource);
        state = Do(state, CommandKind.MoveFellowship, new() { DieId = die.Id }, 0, 0);
        Assert.Equal(KingsFixture.SetupSamples + 2, state.RandomSamples.Count);
        Assert.Null(HuntRuntime.Active(state));
        Assert.Equal(DieLocation.Hunt, state.Dice.Single(d => d.Id == die.Id).Location);
    }

    [Fact]
    public void FarSightedStillRejectsExcessRangeAndACompanionRouteIntoABesiegedStronghold()
    {
        var state = KingsFarSightedTests.Ready();
        var payload = new CommandPayload
        {
            DieId = state.Dice.Single(d => d.Owner == Side.FreePeoples && d.Source == SovereignCatalog.RulerSource).Id,
            AbilityId = KingsFarSighted.Ability, Ids = ["base.character.merry"],
            Route = ["rivendell", "trollshaws", "hollin", "moria"]
        };
        LordsFixture.Reject(state, CommandKind.SeparateCompanions, payload);
        state.Fellowship.RegionId = "dimrill-dale";
        foreach (var unit in state.Units.Where(u => u.Location == "lorien"))
            unit.Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "lorien");
        payload.Route = ["dimrill-dale", "lorien"];
        LordsFixture.Reject(state, CommandKind.SeparateCompanions, payload);
    }
}
