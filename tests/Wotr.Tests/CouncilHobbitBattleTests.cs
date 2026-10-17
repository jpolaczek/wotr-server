using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class CouncilHobbitBattleTests
{
    internal static MatchState LossDecision(bool fpAttacks = true, bool both = false)
    {
        var state = CouncilFixture.Actions();
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        foreach (var nation in state.Nations)
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
        foreach (var die in state.Dice.Where(d => d.Source == "standard"))
            die.Face = DieFace.Army;
        TurnFlow.SelectNext(state, fpAttacks ? Side.FreePeoples : Side.Shadow);
        var fpRegion = fpAttacks ? BattleFixture.Origin : BattleFixture.Target;
        var shRegion = fpAttacks ? BattleFixture.Target : BattleFixture.Origin;
        var fp = BattleFixture.Put(state, "gondor", UnitKind.Regular, fpRegion);
        var sh = BattleFixture.Put(state, "sauron", UnitKind.Regular, shRegion);
        var merry = LordsCharacterFixture.Put(state, CouncilCompanions.Merry, fpRegion, "base.character.merry");
        var attackers = new List<string> { fpAttacks ? fp.Id : sh.Id };
        if (fpAttacks)
            attackers.Add(merry.Id);
        if (both)
        {
            var pippin = LordsCharacterFixture.Put(state, CouncilCompanions.Pippin, fpRegion, "base.character.pippin");
            if (fpAttacks)
                attackers.Add(pippin.Id);
        }
        state = LordsFixture.Do(state, CommandKind.AttackArmy, BattleFixture.AttackPayload(state, attackers));
        state = LordsFixture.Do(state, CommandKind.CommitCombatCard);
        state = LordsFixture.Do(state, CommandKind.CommitCombatCard, random: fpAttacks ? new SequenceRandom(0, 5) : new SequenceRandom(5, 0));
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = LordsFixture.Do(state, CommandKind.RerollBattle);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        return state;
    }

    internal static MatchState ReturnDecision(bool fpAttacks = true, bool both = false)
    {
        var state = LossDecision(fpAttacks, both);
        var regular = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.FreePeoples)).Single(BoardRules.IsArmyUnit);
        return LordsFixture.Do(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(regular.Id)] });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnnihilatedHobbitsChooseAdjacentRegionsIndividuallyBeforeBattleResumes(bool fpAttacks)
    {
        var state = ReturnDecision(fpAttacks, true);
        var origin = fpAttacks ? BattleFixture.Origin : BattleFixture.Target;
        foreach (var id in new[] { "base.character.merry", "base.character.pippin" })
        {
            Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
            Assert.Equal(id, state.Pending.SourceId);
            var candidates = new EffectOptions().GetOptions(state, Side.FreePeoples).Single().EligibleIds;
            Assert.All(candidates, region => Assert.Contains(region, BoardRules.Regions[origin].Neighbors));
            LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [origin] });
            LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = ["barad-dur"] });
            var target = candidates.First();
            var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.FreePeoples,
                BattleFixture.Command(state, CommandKind.ChooseEffect, new() { Ids = [target] }));
            Assert.Equal(id, Assert.Single(preview.Placements).UnitId);
            state = LordsFixture.Do(StateCodec.Copy(state), CommandKind.ChooseEffect, new() { Ids = [target] });
            Assert.Equal(target, state.Characters.Single(c => c.Id == id).Location);
        }
        Assert.Empty(state.Lords!.Council!.BattleReturns);
        Assert.DoesNotContain(state.Resolution.Frames, f => f.EffectId == CouncilHobbitReturn.ProgramId);
        if (!fpAttacks)
            Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
    }

    [Fact]
    public void ReturningHobbitCannotEnterBesiegedStrongholdOrCrossImpassableBorder()
    {
        var state = ReturnDecision(false);
        var garrison = BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-tirith");
        garrison.Garrison = true;
        var options = new EffectOptions().GetOptions(state, Side.FreePeoples).Single().EligibleIds;
        Assert.DoesNotContain("minas-tirith", options);
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = ["minas-tirith"] });
        var execution = EffectRuntime.Current(state)!.Effect!;
        execution.RegionId = "lorien";
        Assert.DoesNotContain("moria", BoardRules.Regions["lorien"].Neighbors);
        Assert.DoesNotContain("moria", new HobbitReturnStep().Candidates(state, execution));
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = ["moria"] });
    }

    [Fact]
    public void CancelledAbilityOrEliminationOutsideBattleDoesNotReturnHobbit()
    {
        var state = LossDecision();
        BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.CancelledAbilities.Add("base.character.merry");
        var regular = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.FreePeoples)).Single(BoardRules.IsArmyUnit);
        state = LordsFixture.Do(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(regular.Id)] });
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == "base.character.merry").Location);
        Assert.Empty(state.Lords!.Council!.BattleReturns);
        Assert.DoesNotContain(state.Events, e => e.Kind == "HobbitReturned");

        state = CouncilFixture.Actions();
        var merry = LordsCharacterFixture.Put(state, CouncilCompanions.Merry, "hollin");
        CharacterLifecycle.Eliminate(state, merry);
        Assert.False(CouncilHobbitReturn.BeginPending(state));
        Assert.Equal("casualties", merry.Location);
    }

    [Fact]
    public void CombatCardEliminationReturnsHobbitBeforeItsParentEffectContinues()
    {
        var state = LossDecision();
        // Keep the real battle, then open the real Black Breath choice instead of the ordinary loss frame.
        state.Resolution.Frames.RemoveAt(state.Resolution.Frames.Count - 1);
        var battle = BattleRuntime.Active(state)!;
        battle.Stage = BattleStage.RoundEnd;
        battle.Force(Side.Shadow).Round.Dice = [new() { RerollHit = true }];
        EffectRuntime.BeginProgram(state, Side.Shadow, ShadowCharacterCards02.Strike, "combat.black-breath");
        var engine = LordsFixture.Engine();
        new EffectRuntime(EffectCatalog.Base).Continue(state, new SequenceRandom());
        var command = BattleFixture.Command(state, CommandKind.ChooseEffect, new() { Ids = ["base.character.merry"] });
        var result = engine.Execute(state, Side.Shadow, command, new SequenceRandom());
        Assert.True(result.Receipt.Accepted);
        Assert.Equal(Side.FreePeoples, result.State.Pending!.Owner);
        Assert.Equal(CouncilHobbitReturn.ProgramId, EffectRuntime.Current(result.State)!.EffectId);
        Assert.Equal(DieLocation.Resolving, result.State.Dice[0].Location);
        Assert.Contains(result.State.Resolution.Frames, f => f.EffectId == "combat.black-breath");
    }
}
