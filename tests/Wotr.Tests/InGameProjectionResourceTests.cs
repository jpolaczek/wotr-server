using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.InGameProjectionTests;

namespace Wotr.Tests;

public class InGameProjectionResourceTests
{
    [Fact]
    public void CouncilHasSeparateTokenAndBoundChieftainCostsAndSharesItsTurnLimit()
    {
        var state = KingsIntegrationFixture.Actions();
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        die.Face = DieFace.Muster;
        TurnFlow.SelectNext(state, Side.Shadow);
        var token = CouncilTokens.Politics;
        var politics = Availability(state, Side.Shadow, token, "council");
        Assert.True(politics.CanAct);
        Assert.True(CouncilTokens.ValidUse(state, new() { AbilityId = token, NationId = politics.Targets[0] }));
        var entry = Availability(state, Side.Shadow, token, "council-chieftain");
        Assert.True(entry.CanAct);
        Assert.Contains(die.Id, entry.EligibleDice);
        var target = KingsCouncilEntry.Candidates(state, Side.Shadow).First();
        var command = BattleFixture.Command(state, CommandKind.EnterCharacter,
            new() { CouncilTokenId = token, CharacterId = target.DefinitionId, RegionId = target.Region, DieId = die.Id });
        Assert.Null(LordsFixture.Engine().Validate(state, Side.Shadow, command));
        state.Lords!.Council!.LastTokenTurn = state.Turn;
        Assert.False(Availability(state, Side.Shadow, token, "council").CanAct);
        Assert.False(Availability(state, Side.Shadow, token, "council-chieftain").CanAct);
        Assert.NotNull(LordsFixture.Engine().Validate(state, Side.Shadow, command));
        Assert.DoesNotContain(LordsFixture.Views().For(state, Side.FreePeoples).Availability, a => a.SubjectId == token);
    }

    [Fact]
    public void KeeperAbilitiesUseTheirActualWindowsAndOrdinaryRingUseDoesNotConsumeThem()
    {
        var narya = LordsNaryaTests.Ready();
        narya.Player(Side.FreePeoples).RingUsedThisTurn = true;
        Assert.True(Availability(narya, Side.FreePeoples, LordsDiceSetup.NaryaRing, "keeper").CanAct);
        Assert.Null(LordsFixture.Engine().Validate(narya, Side.FreePeoples,
            BattleFixture.Command(narya, CommandKind.UseNarya, LordsNaryaTests.Cost(narya))));
        var nenya = LordsNenyaTests.Window();
        Assert.True(Availability(nenya, Side.FreePeoples, LordsDiceSetup.NenyaRing, "keeper").CanAct);
        Assert.Null(LordsFixture.Engine().Validate(nenya, Side.FreePeoples,
            BattleFixture.Command(nenya, CommandKind.RespondEffect, new() { ReactionId = NenyaReaction.Id })));
        var vilya = LordsVilyaTests.Ready();
        vilya = LordsFixture.Do(vilya, CommandKind.MoveFellowship, new() { DieId = vilya.Dice[0].Id });
        var retaining = Availability(vilya, Side.FreePeoples, LordsDiceSetup.VilyaRing, "keeper");
        Assert.True(retaining.CanAct);
        Assert.Null(LordsFixture.Engine().Validate(vilya, Side.FreePeoples,
            BattleFixture.Command(vilya, CommandKind.UseVilya, new() { RingId = LordsDiceSetup.VilyaRing, DieId = retaining.EligibleDice[0] })));
    }

    [Fact]
    public void DismayExplainsBothRulersWithoutGivingTheOpponentItsControls()
    {
        var state = KingsIntegrationFixture.DismayWindow();
        var payload = KingsIntegrationFixture.Dismay(state);
        var token = payload.Ids.Single();
        var description = Availability(state, Side.Shadow, token, "dismay");
        Assert.True(description.CanAct);
        Assert.Contains(payload.DieId, description.EligibleDice);
        Assert.Null(LordsFixture.Engine().Validate(state, Side.Shadow,
            BattleFixture.Command(state, CommandKind.UseDismay, payload)));
        Assert.DoesNotContain(LordsFixture.Views().For(state, Side.FreePeoples).Availability, a => a.ActionId == "dismay");
        foreach (var die in state.Dice.Where(d => d.Source == SovereignCatalog.RulerSource))
            die.Location = DieLocation.Discarded;
        Assert.False(Availability(state, Side.Shadow, token, "dismay").ConditionsMet);
    }

    [Fact]
    public void SpecialDieCandidatesAndConditionalRecoveryRemainConsistentWithValidation()
    {
        var state = LordsDiceTests.BothSidesRolling();
        TurnFlow.RollAndBeginActions(state, new SequenceRandom(Enumerable.Repeat(5, 15).ToArray()));
        var actor = state.Pending!.Owner;
        var view = LordsFixture.Views().For(state, actor);
        foreach (var candidate in LordsDiceRoll.LegalChoices(state, actor))
        {
            Assert.True(Availability(state, actor, candidate, "keep-special").CanAct);
            Assert.Null(LordsFixture.Engine().Validate(state, actor,
                BattleFixture.Command(state, CommandKind.ChooseSpecialDie, new() { DieId = candidate })));
            Assert.Contains("At Recover", view.Dice.Single(d => d.Id == candidate).RecoveryDescription);
        }
        Assert.All(view.Availability.Where(a => a.ActionId == "keep-special"), a =>
            Assert.Contains(view.Dice, d => d.Id == a.SubjectId && d.Owner == actor));
    }

    [Fact]
    public void CleansingRequiresThePoliticalWindowAndDoesNotRevealOrChooseTheTile()
    {
        var state = KingsPoliticsTests.Ready();
        var description = Availability(state, Side.FreePeoples, SovereignCatalog.Dain, "cleanse");
        Assert.True(description.ConditionsMet);
        Assert.False(description.CanAct);
        Assert.Empty(description.Targets);
        state = KingsFixture.Do(state, CommandKind.AdvancePolitics,
            new() { DieId = KingsFixture.Die(state).Id, NationId = "dwarves" });
        var offered = Availability(state, Side.FreePeoples, SovereignCatalog.Dain, "cleanse");
        Assert.True(offered.CanAct);
        Assert.Empty(offered.EligibleDice);
        Assert.Null(LordsFixture.Views().For(state, Side.FreePeoples).Sovereigns.Single(s => s.Id == SovereignCatalog.Dain).Corruption);
        Assert.Null(LordsFixture.Engine().Validate(state, Side.FreePeoples,
            BattleFixture.Command(state, CommandKind.ChooseEffect, new() { Ids = ["remove"] })));
    }
}
