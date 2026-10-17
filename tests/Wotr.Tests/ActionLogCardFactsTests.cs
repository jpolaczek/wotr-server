using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.ActionLogFamilyTests;

namespace Wotr.Tests;

public class ActionLogCardFactsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void DeadMenKeepsJourneyRollLossRetreatRecruitmentAndActualDecisionOwners(int recruits)
    {
        var state = DeadMenCardTests.RetreatChoice();
        var destination = state.Pending!.PrivateChoices[0];
        state = CharacterFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = [destination] });
        var placement = state.Pending!.PrivateChoices.Take(recruits).ToList();
        state = CharacterFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = placement });
        var steps = Steps(state);
        var played = steps.Single(s => s.Command == CommandKind.PlayEvent);
        Assert.Equal(FreePeoplesCharacterCards04.DeadMen, Assert.Single(played.Facts.Cards).Id);
        Assert.Equal(2, played.Costs.Count);
        Assert.Single(played.Costs, c => c.Kind == ActionCostKind.Card);
        var journey = Assert.Single(steps.Single(s => s.Kind == "CompanionsMoved").Movements);
        Assert.Equal(new[] { "edoras", "pelargir" }, journey.Route);
        Assert.Equal(2, journey.Participants.Count);
        Assert.All(journey.Participants, p => Assert.NotEmpty(p.DefinitionId));
        var roll = steps.Single(s => s.Kind == "DeadMenAttack").Facts;
        Assert.Equal(new[] { 1 }, roll.Rolls);
        Assert.Equal(1, roll.Hits);
        var losses = steps.Single(s => s.Kind == "EventLosses").Facts;
        Assert.Equal(1, Assert.Single(losses.LossDetails).Hits);
        var retreat = Assert.Single(steps.Single(s => s.Kind == "EventRetreat").Movements);
        Assert.Equal(destination, retreat.To);
        Assert.Contains(retreat.Participants, p => p.DefinitionId == CharacterRules.WitchKing);
        Assert.Contains(steps, s => s.Command == CommandKind.ChooseEffect && s.Actor == Side.Shadow);
        Assert.Equal(recruits, steps.SelectMany(s => s.Facts.Figures).Count(f => placement.Contains(f.After?.Id ?? "") && f.To == "pelargir"));
        Assert.Equal(ActionLogStatus.Completed, steps.Last().Status);
        Assert.All(steps, s => Assert.Equal(played.ActionId, s.ActionId));
    }

    [Fact]
    public void EaglesRecordNaturalResultsAndOnlyTheSurvivingNazgulTravel()
    {
        var state = EaglesCardTests.RetreatChoice();
        state = CharacterFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = ["dol-guldur"] });
        var steps = Steps(state);
        var attack = steps.Single(s => s.Kind == "EaglesAttack").Facts;
        Assert.Equal(new[] { 1, 5, 4 }, attack.Rolls);
        Assert.Equal(5, attack.HitTarget);
        Assert.Equal(1, attack.Hits);
        var travel = Assert.Single(steps.Single(s => s.Kind == "NazgulRetreated").Movements);
        Assert.Equal(2, travel.Participants.Count);
        Assert.All(travel.Participants, p => Assert.Equal(UnitKind.Nazgul, p.UnitKind));
        Assert.Equal(new[] { EaglesCardTests.Target, "dol-guldur" }, travel.Route);
        Assert.DoesNotContain(travel.Participants, p => p.DefinitionId == CharacterRules.WitchKing);
    }

    [Fact]
    public void APassIsFreeAndTheLastDieKeepsAutomaticPhaseChangesSeparate()
    {
        var state = TurnTests.Actions();
        state = LordsFixture.Do(state, CommandKind.Pass);
        var pass = Steps(state).Single(s => s.Command == CommandKind.Pass);
        Assert.Equal(ActionCostKind.Free, Assert.Single(pass.Costs).Kind);
        foreach (var die in state.Dice)
            die.Location = DieLocation.Spent;
        var last = state.Dice.First(d => d.Owner == Side.Shadow);
        last.Location = DieLocation.Available;
        last.Face = DieFace.Army;
        TurnFlow.SelectNext(state, Side.Shadow);
        state = LordsFixture.Do(state, CommandKind.DiscardDie, new() { DieId = last.Id });
        var steps = Steps(state).Where(s => s.Revision == state.Revision).ToList();
        Assert.Equal(last.Id, Assert.Single(steps.SelectMany(s => s.Costs)).Id);
        Assert.Contains(steps, s => s.Facts.Phase == GamePhase.RecoverDraw && s.Automatic && s.Actor is null);
        Assert.Contains(steps, s => s.Facts.Phase == GamePhase.Fellowship && s.Automatic && s.Actor is null);
        Assert.All(steps.Where(s => s.Facts.Phase.HasValue), s => Assert.Equal(ActionContextKind.General, s.Context.Kind));
        Assert.Equal(2, state.Turn);
    }

    [Fact]
    public void MixedArmyKeepsTheCharacterVersionAfterLaterElimination()
    {
        var (state, command, _) = ActionLogFixture.Army();
        var king = CharacterFixture.Put(state, CharacterRules.WitchKing, "morannon");
        command.Payload.Moves[0].CharacterIds.Add(king.Id);
        state = ActionLogFixture.Execute(state, command);
        var original = Json(Steps(state));
        var moved = Assert.Single(Steps(state).SelectMany(s => s.Movements));
        Assert.Equal(6, moved.Participants.Count);
        Assert.Equal(CharacterRules.WitchKing, Assert.Single(moved.Participants, p => p.Id == king.Id).DefinitionId);
        CharacterLifecycle.Eliminate(state, state.Characters.Single(c => c.Id == king.Id));
        Assert.Equal(original, Json(Steps(state)));
    }
}
