using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.ActionLogFamilyTests;

namespace Wotr.Tests;

public class ActionLogExpansionFactsTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CorruptionRecordsOnlyThePublicTileInformationOfItsVariant(bool sequential, bool visible)
    {
        var state = KingsIntegrationFixture.Actions(new() { Kings = true, Lords = true,
            SequentialCorruption = sequential, VisibleCorruption = visible });
        KingsFixture.Eyes(state, 2);
        KingsFixture.Pool(state, KingsCorruptionTests.One);
        state = KingsFixture.Do(state, CommandKind.AttemptCorruption, new()
        {
            CharacterId = SovereignCatalog.Thranduil, DieId = CorruptionRules.Eyes(state).First().Id
        }, 0);
        if (!sequential)
            state = KingsFixture.Do(state, CommandKind.ChooseCorruptionTile, new() { Ids = [KingsCorruptionTests.One] });
        var publicSteps = Steps(state);
        var placed = publicSteps.Single(s => s.Kind == "CorruptionAdded");
        var sovereign = Assert.Single(placed.Facts.Sovereigns);
        Assert.Equal(visible ? KingsCorruptionTests.One : "", Assert.Single(sovereign.Tiles).TileId);
        Assert.Equal(visible ? 1 : (int?)null, sovereign.Corruption);
        if (!visible)
            Assert.DoesNotContain(KingsCorruptionTests.One, Json(publicSteps));
        Assert.Equal(DieFace.Eye, Assert.Single(publicSteps.SelectMany(s => s.Costs), c => c.Kind == ActionCostKind.Die).Use);
        Assert.Equal(ActionLogStatus.Completed, publicSteps.Last().Status);
        Assert.Equal(Json(publicSteps), Json(Steps(StateCodec.Copy(state))));
    }

    [Theory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public void DismaySpendsOneTokenAndDoesNotClaimToSpendTheRerolledDie(Side target)
    {
        var state = KingsIntegrationFixture.DismayWindow();
        var payload = KingsIntegrationFixture.Dismay(state, target);
        state = KingsFixture.Do(state, CommandKind.UseDismay, payload, 5);
        var step = Steps(state).Single(s => s.Command == CommandKind.UseDismay);
        var cost = Assert.Single(step.Costs);
        Assert.Equal(ActionCostKind.RulerToken, cost.Kind);
        Assert.Equal(payload.Ids[0], cost.Id);
        Assert.Equal(Side.Shadow, cost.Owner);
        var die = Assert.Single(step.Facts.Dice, d => d.Id == payload.DieId);
        Assert.Equal(DieFace.Character, die.Before);
        Assert.Equal(state.Dice.Single(d => d.Id == payload.DieId).Face, die.After);
        Assert.Equal(target, die.Owner);
    }

    [Theory]
    [InlineData(SovereignCatalog.Brand)]
    [InlineData(SovereignCatalog.Dain)]
    public void RulerConversionKeepsTheOriginalFaceAndSeparateTokenCost(string sovereign)
    {
        var (state, payload) = KingsRulerTokenTests.Recruitment(sovereign);
        state = KingsFixture.Do(state, CommandKind.UseCharacterAbility, payload);
        var step = Steps(state).Single(s => s.Command == CommandKind.UseCharacterAbility);
        Assert.Equal(2, step.Costs.Count);
        var die = Assert.Single(step.Costs, c => c.Kind == ActionCostKind.Die);
        Assert.Equal(DieFace.Character, die.Face);
        Assert.Equal(DieFace.Muster, die.Use);
        Assert.Equal(payload.RulerTokenId, Assert.Single(step.Costs, c => c.Kind == ActionCostKind.RulerToken).Id);
        Assert.All(payload.Placements, p => Assert.Contains(step.Facts.Figures, f => f.After?.Id == p.UnitId && f.To == p.RegionId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CouncilTokenHasItsOwnCostAndRecordsEitherPoliticsOrActualTravel(bool travel)
    {
        var state = CouncilTokenTests.Actions();
        state.Nations.Single(n => n.Id == "sauron").StepsToWar = 2;
        var payload = travel ? CouncilTokenTests.Travel(state) : new CommandPayload { AbilityId = CouncilTokens.Politics, NationId = "sauron" };
        state = LordsFixture.Do(state, CommandKind.UseCouncilToken, payload);
        var steps = Steps(state);
        var cost = Assert.Single(steps.SelectMany(s => s.Costs));
        Assert.Equal(ActionCostKind.CouncilToken, cost.Kind);
        Assert.Equal(payload.AbilityId, cost.Id);
        if (travel)
            Assert.Equal(payload.CompanionMoves[0].Route, Assert.Single(steps.SelectMany(s => s.Movements)).Route);
        else
            Assert.Contains(steps.SelectMany(s => s.Facts.Nations), n => n.Id == "sauron" && n.StepsToWar == 1);
    }

    [Theory]
    [InlineData(DieFace.Army)]
    [InlineData(DieFace.WillOfTheWest)]
    public void NaryaRecordsBothCostsAndTheCompletePoliticalChange(DieFace face)
    {
        var state = LordsNaryaTests.Ready();
        state.Dice[0].Face = face;
        state = LordsFixture.Do(state, CommandKind.UseNarya, LordsNaryaTests.Cost(state));
        var step = Steps(state).Single(s => s.Command == CommandKind.UseNarya);
        Assert.Equal(2, step.Costs.Count);
        Assert.Equal(face, Assert.Single(step.Costs, c => c.Kind == ActionCostKind.Die).Face);
        Assert.Equal(LordsDiceSetup.NaryaRing, Assert.Single(step.Costs, c => c.Kind == ActionCostKind.Ring).Id);
        Assert.Contains(step.Facts.Nations, n => n.Id == "gondor" && n.WasActive == false && n.Active && n.StepsToWar == 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VilyaContinuationKeepsTheMovementGroupAndChargesOnlyAnAcceptedRing(bool accept)
    {
        var state = LordsVilyaTests.Ready();
        var die = state.Dice[0].Id;
        state = LordsFixture.Do(state, CommandKind.MoveFellowship, new() { DieId = die });
        var move = Steps(state).Single(s => s.Command == CommandKind.MoveFellowship);
        Assert.Equal(ActionLogStatus.InProgress, move.Status);
        state = LordsFixture.Do(state, accept ? CommandKind.UseVilya : CommandKind.DeclineVilya,
            new() { DieId = die, RingId = LordsDiceSetup.VilyaRing });
        var step = Steps(state).Single(s => s.Command == (accept ? CommandKind.UseVilya : CommandKind.DeclineVilya));
        Assert.Equal(move.ActionId, step.ActionId);
        Assert.Equal(accept ? 1 : 0, step.Costs.Count);
        if (accept)
            Assert.Equal(ActionCostKind.Ring, step.Costs[0].Kind);
        Assert.Equal(ActionLogStatus.Completed, step.Status);
    }

    [Theory]
    [InlineData("dale", "erebor", 2)]
    [InlineData("dale", "iron-hills", 0)]
    public void FateMovementRecordsControlAndActualVictoryPoints(string from, string destination, int points)
    {
        var state = FateFixture.Actions();
        FateFixture.Clear(state, from);
        FateFixture.Clear(state, destination);
        var unit = BattleFixture.Put(state, "dwarves", UnitKind.Regular, from);
        var nation = state.Nations.Single(n => n.Id == "dwarves");
        nation.Active = true;
        nation.StepsToWar = 0;
        var die = KingsFixture.Die(state);
        die.Face = DieFace.Army;
        state = KingsFixture.Do(state, CommandKind.MoveArmies, new()
        {
            DieId = die.Id, MovementCost = DieFace.Army,
            Moves = [new() { From = from, To = destination, UnitIds = [unit.Id] }]
        });
        var step = Steps(state).Single(s => s.Command == CommandKind.MoveArmies);
        Assert.Equal(new[] { from, destination }, Assert.Single(step.Movements).Route);
        if (points > 0)
        {
            Assert.Contains(step.Facts.Control, c => c.RegionId == destination && c.Before == Side.Shadow && c.After == Side.FreePeoples);
            Assert.Equal(points, step.Facts.Outcome!.FreePeoplesPoints);
        }
        else
            Assert.Empty(step.Facts.Control);
    }
}
