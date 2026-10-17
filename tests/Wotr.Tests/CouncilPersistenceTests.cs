using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Tests.LordsPersistenceTests;

namespace Wotr.Tests;

public class CouncilPersistenceTests
{
    [PostgresFact]
    public Task PrivateVersionsPersistWithoutLeakingThroughOpponentPreviewOrRetry() =>
        Restart(CouncilSetupTests.Setup(), CommandKind.ChooseCompanionVersions,
            new() { Ids = [LordsCharacters.Gandalf, "lords.character.boromir", CouncilCompanions.Merry], GuideId = CharacterRules.Strider });

    [PostgresFact]
    public Task HomelandDeclarationRestoresItsRevealsAndPendingShadowChoice()
    {
        var state = CouncilSetupTests.AllVersions();
        return Restart(state, CommandKind.ChooseCouncilHomelands, new() { Placements = CouncilSetupTests.Homelands(state, 1) });
    }

    [PostgresFact]
    public async Task EitherShadowTokenChoiceSurvivesRestart()
    {
        foreach (var token in CouncilTokens.All)
        {
            var state = CouncilSetupTests.AllVersions();
            state = LordsFixture.Do(state, CommandKind.ChooseCouncilHomelands, new() { Placements = CouncilSetupTests.Homelands(state, 1) });
            await Restart(state, CommandKind.ChooseCouncilToken, new() { Ids = [token] });
        }
    }

    [PostgresFact]
    public async Task TokenUsePersistsItsEffectAndTurnLimitWithoutAnyPaidDie()
    {
        await Restart(CouncilTokenTests.Actions(), CommandKind.UseCouncilToken,
            new() { AbilityId = CouncilTokens.Politics, NationId = "sauron" });
        var state = CouncilTokenTests.Actions();
        await Restart(state, CommandKind.UseCouncilToken, CouncilTokenTests.Travel(state));
    }

    [PostgresFact]
    public Task HuntDepartureRestoresTheGroupBeforeChoosingItsNextGuide()
    {
        var state = CouncilFixture.Move(CouncilFixture.Hunt(HuntTests.Three, "boromir", "gimli", "legolas"), 5, 0);
        state = LordsFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = "council.boromir-guide" });
        return Restart(state, CommandKind.SelectGuide, new() { GuideId = "base.character.gimli" });
    }

    [PostgresFact]
    public Task CancelledTilePersistsBothHobbitCostsWithoutRevivingEither()
    {
        var state = CouncilFixture.Move(CouncilFixture.Hunt(HuntTests.Eye, "merry", "pippin"), 5, 0);
        return Restart(state, CommandKind.RespondEffect,
            new() { ReactionId = CouncilGuideReactions.Cancel + "." + CouncilCompanions.Merry });
    }

    [PostgresFact]
    public Task HobbitReturnRestoresItsRegionChoiceAndTheOtherHobbitsContinuation()
    {
        var state = CouncilHobbitBattleTests.ReturnDecision(both: true);
        return Restart(state, CommandKind.ChooseEffect, new() { Ids = [state.Pending!.PrivateChoices[0]] });
    }

    [PostgresFact]
    public Task StriderPersistsFellowshipBoxDestinationWhileResolvingTheHunt()
    {
        var state = CouncilStriderTests.State(1);
        state.Dice.First(d => d.Owner == Side.Shadow).Location = DieLocation.Hunt;
        HuntTests.Pool(state, HuntTests.Three);
        state = CouncilFixture.Move(state, 5, 0);
        return Restart(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
    }
}
