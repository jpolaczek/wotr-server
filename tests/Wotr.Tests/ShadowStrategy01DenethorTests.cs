using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards01;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy01Fixture;

namespace Wotr.Tests;

public class ShadowStrategy01DenethorTests
{
    private const string Region = "minas-tirith";
    private const string FreeCard = "base.fp.strategy.01";

    [Fact]
    public void ShadowChoosesOneFreePeoplesLeaderOfAnyNationWithoutLosingACompanion()
    {
        var state = State(Denethor);
        var companion = Put(state, CharacterRules.Grey, Region);
        companion.Garrison = true;
        var outside = BattleFixture.Put(state, "elves", UnitKind.Leader, "lorien");
        var leaders = state.Units.Where(u => u.Location == Region && u.Kind == UnitKind.Leader).ToArray();
        state = Play(state, Denethor);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Equal("ArmyFigure", state.Pending.Presentation);
        Assert.Contains(Denethor, state.Player(Side.Shadow).Table);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Resolving);
        var choices = Shadow01Fixture.Choices(state);
        Assert.Equal(leaders.Select(u => u.Id).Order(), choices.EligibleIds.Order());
        Reject(state, CommandKind.ChooseEffect, new());
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [companion.Id] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [outside.Id] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = leaders.Select(u => u.Id).ToList() });

        var north = leaders.Single(u => u.Nation == "north");
        var command = BattleFixture.Command(state, CommandKind.ChooseEffect, new() { Ids = [north.Id] });
        Assert.NotNull(Engine().Validate(state, Side.FreePeoples, command));
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow, command);
        Assert.True(preview.Legal);
        Assert.Equal(new[] { north.Id }, preview.RemovedIds);
        Assert.Equal(before, StateCodec.Write(state));
        state = Do(StateCodec.Copy(state), command.Kind, command.Payload);
        Assert.Equal("casualties", state.Units.Single(u => u.Id == north.Id).Location);
        Assert.Single(state.Units, u => u.Location == Region && u.Kind == UnitKind.Leader);
        Assert.Equal(Region, CharacterRules.InPlay(state, CharacterRules.Grey)!.Location);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void MissingOrSingleLeaderCompletesWithoutInventingAChoice(int leaderCount)
    {
        var state = State(Denethor);
        foreach (var leader in state.Units.Where(u => u.Location == Region && u.Kind == UnitKind.Leader).Skip(leaderCount))
            BoardRules.ReturnToReserve(leader);
        Put(state, CharacterRules.Grey, Region).Garrison = true;
        state = Play(state, Denethor);
        Assert.Contains(Denethor, state.Player(Side.Shadow).Table);
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.Units, u => u.Location == Region && u.Kind == UnitKind.Leader);
        Assert.Equal(Region, CharacterRules.InPlay(state, CharacterRules.Grey)!.Location);
        Assert.Equal(leaderCount, state.Units.Count(u => u.Location == "casualties" && u.Kind == UnitKind.Leader));
    }

    [Theory]
    [InlineData("no-garrison")]
    [InlineData("no-besieging-army")]
    [InlineData("shadow-garrison")]
    [InlineData("other-stronghold")]
    public void RequiresFreePeoplesActuallyBesiegedInMinasTirith(string reason)
    {
        var state = State(Denethor);
        switch (reason)
        {
            case "no-garrison":
                foreach (var unit in state.Units.Where(u => u.Location == Region && BoardRules.Owner(u) == Side.FreePeoples))
                    unit.Garrison = false;
                break;
            case "no-besieging-army":
                foreach (var unit in state.Units.Where(u => u.Location == Region && BoardRules.Owner(u) == Side.Shadow && BoardRules.IsArmyUnit(u)))
                    BoardRules.ReturnToReserve(unit);
                break;
            case "shadow-garrison":
                state.Control[Region] = Side.Shadow;
                foreach (var unit in state.Units.Where(u => u.Location == Region))
                    unit.Garrison = BoardRules.Owner(unit) == Side.Shadow;
                break;
            case "other-stronghold":
                foreach (var unit in state.Units.Where(u => u.Location == Region))
                    unit.Location = "dol-amroth";
                break;
        }
        Reject(state, CommandKind.PlayEvent, EventCost(state, Denethor));
        Assert.DoesNotContain(new EffectOptions().GetOptions(state, Side.Shadow)
            .SelectMany(option => option.EligibleIds), id => id == Denethor);
    }

    [Theory]
    [InlineData(BattleMode.Siege)]
    [InlineData(BattleMode.Sortie)]
    [InlineData(BattleMode.Relief)]
    public void PublicRestrictionSkipsFreePeoplesCardsInEitherRoleAndEveryRound(BattleMode mode)
    {
        var state = BeginRestrictedBattle(RestrictedBattleState(mode), mode);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(mode, battle.Mode);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Equal(DecisionKind.CombatCard, state.Pending.Kind);
        Assert.False(new BattleCards(EffectCatalog.Base).CanCommit(state, battle, Side.FreePeoples, FreeCard));
        state = Do(state, CommandKind.CommitCombatCard, random: new SequenceRandom(0, 0, 0));
        Assert.NotEqual(DecisionKind.CombatCard, state.Pending!.Kind);
        Assert.Contains(FreeCard, state.Player(Side.FreePeoples).Hand);
        Assert.Contains(state.Events, e => e.Kind == "CombatCardCommitted" && e.Text == "FreePeoples declined a combat card.");

        if (mode == BattleMode.Siege)
        {
            var elite = state.Units.Single(u => u.Location == Region && u.Kind == UnitKind.Elite);
            state = Do(state, CommandKind.ExtendSiege, new() { Ids = [elite.Id] });
        }
        else
        {
            state = Do(state, CommandKind.ContinueBattle);
            if (state.Pending!.Kind == DecisionKind.BattleRetreat)
                state = Do(state, CommandKind.HoldBattle);
        }
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        Assert.False(new BattleCards(EffectCatalog.Base).CanCommit(state, BattleRuntime.Active(state)!, Side.FreePeoples, FreeCard));
        Assert.Contains(Denethor, state.Player(Side.Shadow).Table);
    }

    [Theory]
    [InlineData(BattleMode.Siege)]
    [InlineData(BattleMode.Sortie)]
    [InlineData(BattleMode.Relief)]
    public void AutomaticDeclineNeverDependsOnTheHiddenFreePeoplesHand(BattleMode mode)
    {
        var first = RestrictedBattleState(mode);
        var second = StateCodec.Copy(first);
        second.Player(Side.FreePeoples).Hand[0] = "base.fp.character.05";
        second.Player(Side.FreePeoples).Decks["character"].Reverse();
        first = BeginRestrictedBattle(first, mode);
        second = BeginRestrictedBattle(second, mode);
        var projection = new Projection(new CoreOptions(), new BattleOptions(), new EffectOptions());
        Assert.Equal(Json(projection.For(first, Side.Shadow)), Json(projection.For(second, Side.Shadow)));
        first = Do(first, CommandKind.CommitCombatCard, random: new SequenceRandom(0, 0, 0));
        second = Do(second, CommandKind.CommitCombatCard, random: new SequenceRandom(0, 0, 0));
        Assert.Equal(Json(projection.For(first, Side.Shadow)), Json(projection.For(second, Side.Shadow)));
        Assert.Equal(first.RandomSamples.Count, second.RandomSamples.Count);
    }

    [Fact]
    public void RestrictionDoesNotApplyToAnAttackInAnotherRegion()
    {
        var state = RestrictedBattleState(BattleMode.Sortie);
        var attacker = BattleFixture.Put(state, "gondor", UnitKind.Regular, "osgiliath");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "north-ithilien");
        state = Do(state, CommandKind.AttackArmy,
            BattleFixture.AttackPayload(state, [attacker.Id], "osgiliath", "north-ithilien"));
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Equal(DecisionKind.CombatCard, state.Pending.Kind);
        Assert.True(new BattleCards(EffectCatalog.Base).CanCommit(state, BattleRuntime.Active(state)!, Side.FreePeoples, FreeCard));
    }

    [Theory]
    [InlineData(CharacterRules.Grey, true, true)]
    [InlineData(CharacterRules.White, true, true)]
    [InlineData(CharacterRules.Aragorn, true, true)]
    [InlineData(CharacterRules.Strider, true, false)]
    [InlineData("base.character.boromir", true, false)]
    [InlineData(CharacterRules.Grey, false, false)]
    [InlineData(CharacterRules.White, false, false)]
    [InlineData(CharacterRules.Aragorn, false, false)]
    public void AnyDieRemovalRequiresThePrintedCharacterInsideTheBesiegedGarrison(string character, bool garrison, bool anyDie)
    {
        var state = RemovalState();
        Put(state, character, garrison ? Region : "osgiliath").Garrison = garrison;
        var cost = Cost(state, DieFace.Army);
        Assert.Equal(anyDie, DenethorRemoval.Option(state, Side.FreePeoples)?.EligibleDice.Contains(cost.DieId) == true);
        if (!anyDie)
        {
            Reject(state, CommandKind.DiscardDenethor, cost);
            state.Dice.Single(d => d.Id == cost.DieId).Face = DieFace.WillOfTheWest;
        }
        state = Do(StateCodec.Copy(state), CommandKind.DiscardDenethor, cost);
        Assert.DoesNotContain(Denethor, state.Player(Side.Shadow).Table);
        Assert.Contains(Denethor, state.Player(Side.Shadow).Discard);
        Assert.DoesNotContain(state.ActiveEffects, effect => effect.SourceId == Denethor);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == cost.DieId).Location);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
    }

    [Fact]
    public void SameRegionOutsideGarrisonDoesNotAllowOrdinaryDie()
    {
        var state = RemovalState();
        Put(state, CharacterRules.Grey, Region).Garrison = false;
        Reject(state, CommandKind.DiscardDenethor, Cost(state, DieFace.Army));
    }

    [Theory]
    [InlineData(DieFace.Army)]
    [InlineData(DieFace.Character)]
    [InlineData(DieFace.Muster)]
    [InlineData(DieFace.ArmyMuster)]
    [InlineData(DieFace.Event)]
    [InlineData(DieFace.WillOfTheWest)]
    public void EligibleCharacterAcceptsEachAvailableActionResult(DieFace face)
    {
        var state = RemovalState();
        Put(state, CharacterRules.Grey, Region).Garrison = true;
        var cost = Cost(state, face);
        state = Do(state, CommandKind.DiscardDenethor, cost);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == cost.DieId).Location);
        Assert.DoesNotContain(Denethor, state.Player(Side.Shadow).Table);
    }

    [Theory]
    [InlineData("ordinary-without-character")]
    [InlineData("spent-die")]
    [InlineData("foreign-die")]
    [InlineData("unknown-die")]
    [InlineData("ring")]
    [InlineData("absent-table")]
    [InlineData("reaction-window")]
    [InlineData("shadow-action")]
    public void IllegalRemovalNeverSpendsResourcesOrDiscardsTheCard(string reason)
    {
        var state = RemovalState();
        var payload = Cost(state, DieFace.WillOfTheWest);
        switch (reason)
        {
            case "ordinary-without-character":
                state.Dice.Single(d => d.Id == payload.DieId).Face = DieFace.Army;
                break;
            case "spent-die":
                state.Dice.Single(d => d.Id == payload.DieId).Location = DieLocation.Spent;
                break;
            case "foreign-die":
                payload.DieId = state.Dice.First(d => d.Owner == Side.Shadow).Id;
                break;
            case "unknown-die":
                payload.DieId = "unknown-die";
                break;
            case "ring":
                payload.RingId = state.Rings.First(r => r.Owner == Side.FreePeoples).Id;
                break;
            case "absent-table":
                TableCardRules.Discard(state, Side.Shadow, Denethor);
                break;
            case "reaction-window":
                state.Open(DecisionKind.Reaction, Side.FreePeoples, [CommandKind.DeclineReaction]);
                break;
            case "shadow-action":
                TurnFlow.SelectNext(state, Side.Shadow);
                break;
        }
        Reject(state, CommandKind.DiscardDenethor, payload);
    }

    [Fact]
    public void ShadowCannotSubmitTheFreePeoplesRemovalDecision()
    {
        var state = RemovalState();
        var command = BattleFixture.Command(state, CommandKind.DiscardDenethor, Cost(state, DieFace.WillOfTheWest));
        var before = StateCodec.Write(state);
        var random = new SequenceRandom();
        var result = Engine().Execute(state, Side.Shadow, command, random);
        Assert.False(result.Receipt.Accepted);
        Assert.Equal(before, StateCodec.Write(result.State));
        Assert.Equal(0, random.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EndingTheSiegeExpiresTheCardWhetherOrNotMinasTirithFalls(bool captured)
    {
        var state = RestrictedBattleState(BattleMode.Siege);
        var elite = state.Units.Single(u => u.Location == Region && u.Kind == UnitKind.Elite);
        BoardRules.ReturnToReserve(elite);
        state = BeginRestrictedBattle(state, BattleMode.Siege);
        state = Do(state, CommandKind.CommitCombatCard,
            random: new SequenceRandom(captured ? [5, 0] : [0, 5]));
        var loser = state.Units.Single(u => u.Location == Region && BoardRules.IsArmyUnit(u) &&
            BoardRules.Owner(u) == (captured ? Side.FreePeoples : Side.Shadow));
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(loser.Id)] });
        Assert.Null(BattleRuntime.Active(state));
        Assert.False(DenethorRules.Siege(state));
        Assert.DoesNotContain(Denethor, state.Player(Side.Shadow).Table);
        Assert.Contains(Denethor, state.Player(Side.Shadow).Discard);
        Assert.DoesNotContain(state.ActiveEffects, effect => effect.SourceId == Denethor);
        Assert.Equal(captured ? Side.Shadow : Side.FreePeoples, state.Control[Region]);
    }

    [Fact]
    public void StoppingOneAssaultDoesNotExpireTheOngoingSiegeEffect()
    {
        var state = BeginRestrictedBattle(RestrictedBattleState(BattleMode.Siege), BattleMode.Siege);
        state = Do(state, CommandKind.CommitCombatCard, random: new SequenceRandom(0, 0, 0));
        Assert.Equal(DecisionKind.SiegeContinuation, state.Pending!.Kind);
        state = Do(state, CommandKind.StopBattle);
        Assert.Null(BattleRuntime.Active(state));
        Assert.True(DenethorRules.Siege(state));
        Assert.Contains(Denethor, state.Player(Side.Shadow).Table);
    }

    private static MatchState RemovalState()
    {
        var state = State(Denethor);
        DenethorTable(state);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        return state;
    }

    private static MatchState RestrictedBattleState(BattleMode mode)
    {
        var state = BattleFixture.State(mode == BattleMode.Siege ? Side.Shadow : Side.FreePeoples);
        BattleFixture.Put(state, "gondor", UnitKind.Regular, Region).Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, Region);
        BattleFixture.Put(state, "sauron", UnitKind.Elite, Region);
        if (mode == BattleMode.Relief)
            BattleFixture.Put(state, "gondor", UnitKind.Regular, "osgiliath");
        DenethorTable(state);
        EventTests.GiveCard(state, Side.FreePeoples, FreeCard);
        return state;
    }

    private static MatchState BeginRestrictedBattle(MatchState state, BattleMode mode)
    {
        var actor = mode == BattleMode.Siege ? Side.Shadow : Side.FreePeoples;
        var from = mode == BattleMode.Relief ? "osgiliath" : Region;
        var ids = state.Units.Where(u => u.Location == from && BoardRules.Owner(u) == actor &&
            u.Garrison == (mode == BattleMode.Sortie)).Select(u => u.Id);
        return Do(state, CommandKind.AttackArmy, BattleFixture.AttackPayload(state, ids, from, Region));
    }

    private static string Json(PlayerView view) => JsonSerializer.Serialize(view, BoardCatalog.Json);
}
