using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsPersistenceTests;

namespace Wotr.Tests;

public class LordsCharacterPersistenceTests
{
    [PostgresFact]
    public Task PrivateSetupRestoresBothProjectionsAndDoesNotDrawAgainOnRetry()
    {
        var state = KernelTests.NewGame();
        state.Configuration.Lords = true;
        TurnFlow.Start(state);
        return Restart(state, CommandKind.ChooseCompanionVersions,
            new() { Ids = [LordsCharacters.Gandalf], GuideId = CharacterRules.Strider });
    }

    [PostgresFact]
    public Task BalrogEntryPersistsTheChosenRecruitAndOnlyOnePaidDie()
    {
        var state = LordsCharacterFixture.State(Side.Shadow);
        var payload = LordsCharacterFixture.Entry(state, LordsCharacters.Balrog, "moria");
        payload.Placements = [new()
        {
            UnitId = state.Units.First(u => u.Nation == "sauron" && u.Kind == UnitKind.Elite && u.Location == "reserve").Id,
            RegionId = "moria"
        }];
        return Restart(state, CommandKind.EnterCharacter, payload);
    }

    [PostgresFact]
    public Task ChiefSpearAndPalantirPersistExactlyOnePrivateCardDraw()
    {
        var state = LordsChiefReactionTests.Play(LordsChiefReactionTests.BeforeEvent(true), face: DieFace.Event);
        return Restart(state, CommandKind.RespondEffect, new() { ReactionId = ChiefReactions.Spear });
    }

    [PostgresFact]
    public Task ChiefFlightRestoresTheDeclarationBeforeFollowingWithoutCost()
    {
        var state = LordsChiefReactionTests.BeforeEvent(chiefRegion: "moria");
        state.Phase = GamePhase.Fellowship;
        TurnFlow.OpenFellowshipPhase(state);
        state = LordsFixture.Do(state, CommandKind.DeclareFellowship, new() { Route = ["hollin"] });
        return Restart(state, CommandKind.RespondEffect, new() { ReactionId = ChiefReactions.Follow });
    }

    [PostgresFact]
    public Task SmeagolEntryBeforeMithrilSurvivesRestartAndNeverReentersTheTilePool()
    {
        var state = LordsSmeagolTests.State();
        state.Player(Side.FreePeoples).Table.Add(FreePeoplesCharacterCards01.Mithril);
        state = LordsSmeagolTests.Move(state, LordsSmeagolTests.FirstTile);
        Assert.True(SmeagolRules.Present(state));
        state.HuntTiles.Single(t => t.Id == HuntTests.Three).Location = "pool";
        return Restart(state, CommandKind.RespondEffect,
            new() { ReactionId = FreePeoplesCharacterCards01.ReplaceReaction }, 0);
    }

    [PostgresFact]
    public Task SmeagolCasualtyPersistsThePublicEventAndPendingGuide()
    {
        var state = LordsSmeagolTests.Move(LordsSmeagolTests.State(true), HuntTests.Three);
        return Restart(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Guide });
    }

    [PostgresFact]
    public Task FireEyePersistsBothDeathsThenRestoresTheGuideDecision()
    {
        var state = LordsFireAndShadowTests.BeforeRoute(true, "moria");
        state.Fellowship.Companions.Remove(CharacterRules.Strider);
        HuntTests.Pool(state, HuntTests.Eye);
        state = LordsFixture.Do(state, CommandKind.DeclareFellowship, new() { Route = ["moria"] }, new SequenceRandom(0));
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        return Restart(state, CommandKind.SelectGuide, new() { GuideId = "base.character.gimli" });
    }

    [PostgresFact]
    public Task EvilKnowledgePersistsAcrossTheNextSiegeRoundWithoutASecondEliteCost() =>
        Restart(LordsMouthAbilityTests.Siege(), CommandKind.UseEvilKnowledge, new());
}
