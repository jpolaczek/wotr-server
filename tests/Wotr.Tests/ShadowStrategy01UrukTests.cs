using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards01;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy01Fixture;

namespace Wotr.Tests;

public class ShadowStrategy01UrukTests
{
    private const string Region = "minas-tirith";

    [Theory]
    [InlineData("saruman-absent")]
    [InlineData("isengard-absent")]
    [InlineData("isengard-elsewhere")]
    [InlineData("isengard-in-garrison")]
    [InlineData("field-battle")]
    public void RequiresSarumanAndAnIsengardArmyUnitAmongTheBesiegers(string reason)
    {
        var state = State(UrukHai);
        var isengard = state.Units.Single(u => u.Location == Region && u.Nation == "isengard");
        switch (reason)
        {
            case "saruman-absent":
                CharacterLifecycle.Eliminate(state, CharacterRules.InPlay(state, CharacterRules.Saruman)!);
                break;
            case "isengard-absent":
                BoardRules.ReturnToReserve(isengard);
                break;
            case "isengard-elsewhere":
                isengard.Location = "orthanc";
                break;
            case "isengard-in-garrison":
                isengard.Garrison = true;
                break;
            case "field-battle":
                foreach (var unit in state.Units.Where(u => u.Location == Region && BoardRules.Owner(u) == Side.FreePeoples))
                    unit.Garrison = false;
                break;
        }
        Reject(state, CommandKind.PlayEvent, EventCost(state, UrukHai));
        Assert.DoesNotContain(new EffectOptions().GetOptions(state, Side.Shadow)
            .SelectMany(option => option.EligibleIds), id => id == UrukHai);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("saruman")]
    [InlineData("isengard")]
    public void ThreeRoundsRemainMandatoryAfterTheSourceFiguresAreLostThenEliteExtensionResumes(string eliminated)
    {
        var state = BeginUruk(elite: true);
        for (var round = 1; round <= 3; round++)
        {
            var battle = BattleRuntime.Active(state)!;
            Assert.Equal(round, battle.Round);
            Assert.Equal(3, battle.MandatorySiegeRounds);
            Reject(state, CommandKind.StopBattle, new());
            Reject(state, CommandKind.FinishArmyEvent, new());
            if (round == 2)
                RemoveRequirement(state, eliminated);
            state = Shadow04Fixture.BlankRound(StateCodec.Copy(state));
            Assert.Single(state.Dice, d => d.Location == DieLocation.Resolving);
        }
        Assert.Equal(DecisionKind.SiegeContinuation, state.Pending!.Kind);
        var elite = state.Units.Single(u => u.Location == Region && u.Kind == UnitKind.Elite);
        state = Do(state, CommandKind.ExtendSiege, new() { Ids = [elite.Id] });
        Assert.Equal(4, BattleRuntime.Active(state)!.Round);
        Assert.Equal("reserve", state.Units.Single(u => u.Id == elite.Id).Location);
        state = Shadow04Fixture.BlankRound(state);
        Assert.Null(BattleRuntime.Active(state));
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void AllIsengardUnitsMayRemainWithTheLegalRearGuard()
    {
        var state = State(UrukHai);
        var isengard = state.Units.Single(u => u.Location == Region && u.Nation == "isengard");
        state = Play(state, UrukHai);
        var option = new BattleOptions().GetOptions(state, Side.Shadow).Single();
        Assert.Empty(Assert.Single(option.Armies).RequiredAnyIds);
        var payload = Shadow04Fixture.Army(state, Region, Region);
        payload.Moves[0].UnitIds = state.Units.Where(u => u.Location == Region &&
            u.Nation == "sauron" && BoardRules.IsArmyUnit(u)).Select(u => u.Id).ToList();
        payload.Moves[0].CharacterIds.Clear();
        state = Do(state, CommandKind.AttackArmy, payload);
        var battle = BattleRuntime.Active(state)!;
        Assert.DoesNotContain(isengard.Id, battle.Attacker.Participants);
        Assert.Equal(3, battle.MandatorySiegeRounds);
        Assert.Equal(Region, state.Units.Single(u => u.Id == isengard.Id).Location);
        Assert.All(BattleRules.Units(state, battle.Attacker), u => Assert.Equal("sauron", u.Nation));
    }

    [Fact]
    public void AnotherSiegeCannotBorrowTheIsengardUnitFromTheEligibleArmy()
    {
        var state = State(UrukHai);
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "dol-amroth");
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "dol-amroth").Garrison = true;
        state = Play(state, UrukHai);
        var option = new BattleOptions().GetOptions(state, Side.Shadow).Single();
        Assert.Equal(Region, Assert.Single(option.Armies).RegionId);
        Reject(state, CommandKind.AttackArmy, Shadow04Fixture.Army(state, "dol-amroth", "dol-amroth"));
        state = Do(state, CommandKind.AttackArmy, Shadow04Fixture.Army(state, Region, Region));
        Assert.Equal(Region, BattleRuntime.Active(state)!.Region);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FirstRoundRequiresACompanionButTheSecondRoundAlwaysAllowsCards(bool companion)
    {
        var state = State(UrukHai);
        if (companion)
            Put(state, "base.character.boromir", Region).Garrison = true;
        const string card = "base.fp.strategy.01";
        EventTests.GiveCard(state, Side.FreePeoples, card);
        state = Play(state, UrukHai);
        state = Do(state, CommandKind.AttackArmy, Shadow04Fixture.Army(state, Region, Region));
        Assert.Equal(companion, new BattleCards(EffectCatalog.Base).CanCommit(state, BattleRuntime.Active(state)!, Side.FreePeoples, card));
        state = Do(state, CommandKind.CommitCombatCard, random: new SequenceRandom(new int[10]));
        if (companion)
        {
            Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
            Assert.Equal(Side.FreePeoples, state.Pending.Owner);
            state = Do(state, CommandKind.CommitCombatCard, random: new SequenceRandom(new int[10]));
        }
        else
            Assert.NotEqual(DecisionKind.CombatCard, state.Pending!.Kind);
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = Do(state, CommandKind.RerollBattle);
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);
        state = Do(state, CommandKind.CommitCombatCard);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Contains(card, new BattleOptions().GetOptions(state, Side.FreePeoples).Single().EligibleIds);
        Assert.False(BattleProjection.View(state, Side.FreePeoples)!.CompanionRequiredForCard);
    }

    [Fact]
    public void DenethorStillProhibitsCardsWhenACompanionSatisfiesUrukHai()
    {
        var state = State(UrukHai);
        Put(state, "base.character.boromir", Region).Garrison = true;
        DenethorTable(state);
        const string card = "base.fp.strategy.01";
        EventTests.GiveCard(state, Side.FreePeoples, card);
        state = Play(state, UrukHai);
        state = Do(state, CommandKind.AttackArmy, Shadow04Fixture.Army(state, Region, Region));
        Assert.False(new BattleCards(EffectCatalog.Base).CanCommit(state, BattleRuntime.Active(state)!, Side.FreePeoples, card));
        state = Do(state, CommandKind.CommitCombatCard, random: new SequenceRandom(new int[10]));
        Assert.NotEqual(DecisionKind.CombatCard, state.Pending!.Kind);
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = Do(state, CommandKind.RerollBattle);
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);
        Assert.False(new BattleCards(EffectCatalog.Base).CanCommit(state, BattleRuntime.Active(state)!, Side.FreePeoples, card));
        Assert.Contains(card, state.Player(Side.FreePeoples).Hand);
        Assert.Contains(Denethor, state.Player(Side.Shadow).Table);
    }

    [Theory]
    [InlineData("second-cost")]
    [InlineData("ability")]
    [InlineData("route")]
    [InlineData("second-army")]
    public void FreeAttackRejectsAnInjectedCostOrUnrelatedMovement(string reason)
    {
        var state = Play(State(UrukHai), UrukHai);
        var payload = Shadow04Fixture.Army(state, Region, Region);
        switch (reason)
        {
            case "second-cost":
                payload.DieId = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Id;
                break;
            case "ability":
                payload.AbilityId = CharacterRules.Saruman;
                break;
            case "route":
                payload.Route = [Region];
                break;
            case "second-army":
                payload.Moves.Add(new() { From = "orthanc", To = "fords-of-isen" });
                break;
        }
        Reject(state, CommandKind.AttackArmy, payload);
        Reject(state, CommandKind.MoveArmies, new());
        Reject(state, CommandKind.FinishArmyEvent, new());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EliminatingEitherArmyEndsTheAssaultBeforeThreeRounds(bool attackerDies)
    {
        var state = State(UrukHai);
        if (attackerDies)
            foreach (var unit in state.Units.Where(u => u.Location == Region && u.Nation == "sauron" && u.Kind == UnitKind.Regular).Skip(1))
                BoardRules.ReturnToReserve(unit);
        state = Play(state, UrukHai);
        state = Do(state, CommandKind.AttackArmy, Shadow04Fixture.Army(state, Region, Region));
        state = Do(state, CommandKind.CommitCombatCard,
            random: new SequenceRandom(attackerDies ? [0, 0, 5, 5, 5] : [5, 5, 5, 0, 0, 0, 0]));
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = Do(state, CommandKind.RerollBattle);
        var loser = attackerDies ? Side.Shadow : Side.FreePeoples;
        var units = state.Units.Where(u => u.Location == Region && BoardRules.Owner(u) == loser && BoardRules.IsArmyUnit(u));
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = units.Select(u => BattleFixture.Loss(u.Id)).ToList() });
        Assert.Null(BattleRuntime.Active(state));
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(attackerDies ? Side.FreePeoples : Side.Shadow, state.Control[Region]);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PalantirWaitsThroughAllThreeMandatoryRoundsAndThePaidExtension(bool killSaruman)
    {
        var state = State(UrukHai);
        BattleFixture.Put(state, "sauron", UnitKind.Elite, Region);
        Shadow04Fixture.Table(state, ShadowCharacterCards04.Palantir);
        var cost = EventCost(state, UrukHai);
        state.Dice.Single(d => d.Id == cost.DieId).Face = DieFace.Event;
        cost.CardCost = DieFace.Event;
        state = Do(state, CommandKind.PlayEvent, cost);
        state = Do(state, CommandKind.AttackArmy, Shadow04Fixture.Army(state, Region, Region));
        for (var round = 1; round <= 3; round++)
        {
            state = Shadow04Fixture.BlankRound(StateCodec.Copy(state));
            Assert.Empty(state.Player(Side.Shadow).Hand);
            Assert.Single(state.Dice, d => d.Location == DieLocation.Resolving);
        }
        Assert.Equal(DecisionKind.SiegeContinuation, state.Pending!.Kind);
        var elite = state.Units.Single(u => u.Location == Region && u.Kind == UnitKind.Elite);
        state = Do(state, CommandKind.ExtendSiege, new() { Ids = [elite.Id] });
        if (killSaruman)
            CharacterLifecycle.Eliminate(state, CharacterRules.InPlay(state, CharacterRules.Saruman)!);
        state = Shadow04Fixture.BlankRound(StateCodec.Copy(state));
        Assert.Null(BattleRuntime.Active(state));
        if (!killSaruman)
        {
            Assert.Equal("Deck", state.Pending!.Presentation);
            Assert.Empty(state.Player(Side.Shadow).Hand);
            Assert.Single(state.Dice, d => d.Location == DieLocation.Resolving);
            state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["strategy"] });
        }
        Assert.Equal(killSaruman ? 0 : 1, state.Player(Side.Shadow).Hand.Count);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Empty(state.Resolution.Frames);
    }

    private static MatchState BeginUruk(bool elite = false)
    {
        var state = State(UrukHai);
        if (elite)
            BattleFixture.Put(state, "sauron", UnitKind.Elite, Region);
        state = Play(state, UrukHai);
        return Do(state, CommandKind.AttackArmy, Shadow04Fixture.Army(state, Region, Region));
    }

    private static void RemoveRequirement(MatchState state, string eliminated)
    {
        if (eliminated == "saruman")
            CharacterLifecycle.Eliminate(state, CharacterRules.InPlay(state, CharacterRules.Saruman)!);
        if (eliminated == "isengard")
        {
            var unit = state.Units.Single(u => u.Location == Region && u.Nation == "isengard");
            CasualtyRules.Eliminate(state, [unit.Id], CasualtyCause.Battle,
                BattleRuntime.Active(state)!.Attacker.Participants.ToHashSet());
        }
    }
}
