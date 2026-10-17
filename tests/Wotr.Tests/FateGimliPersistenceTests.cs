using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Tests.LordsPersistenceTests;

namespace Wotr.Tests;

public class FateGimliPersistenceTests
{
    [PostgresFact]
    public async Task BothPrivateVersionsAndDefaultSurviveRestartWithoutLeaking()
    {
        foreach (var version in new[] { "", FateGimli.Standard, FateGimli.Alternate })
        {
            var ids = new List<string> { CharacterRules.Grey };
            if (version != "")
                ids.Add(version);
            await Restart(FateGimliFixture.Setup(), CommandKind.ChooseCompanionVersions,
                new() { Ids = ids, GuideId = CharacterRules.Grey });
        }
    }

    [PostgresFact]
    public async Task EachHomelandAndTheShadowTokenChoicePersistSeparately()
    {
        foreach (var region in FateGimli.Homelands)
        {
            var state = FateGimliFixture.Versions(FateGimli.Alternate);
            var p = new CommandPayload { Placements = [new() { UnitId = FateGimli.Figure, RegionId = region }] };
            await Restart(state, CommandKind.ChooseCouncilHomelands, p);
            state = KingsFixture.Do(state, CommandKind.ChooseCouncilHomelands, p);
            await Restart(state, CommandKind.ChooseCouncilToken, new() { Ids = [CouncilTokens.Travel] });
        }
    }

    [PostgresFact]
    public async Task BothPoliticalAbilitiesPersistOneDieAndTheirDifferentActivation()
    {
        foreach (var version in new[] { FateGimli.Standard, FateGimli.Alternate })
        {
            var state = FateGimliFixture.Actions(version, "ered-luin");
            state.Nations.Single(n => n.Id == "dwarves").Active = false;
            state.Nations.Single(n => n.Id == "dwarves").StepsToWar = 2;
            await Restart(state, CommandKind.UseCharacterAbility, FateGimliFixture.Ability(state));
        }
    }

    [PostgresFact]
    public async Task GuideReactionNextGuideAndCommonDepartureEachRestoreTheirContinuation()
    {
        var state = CouncilFixture.Move(FateGimliFixture.Hunt(HuntTests.Three, "gimli", "boromir", "legolas"), 5, 0);
        var p = new CommandPayload { ReactionId = FateGimli.GuideReaction };
        await Restart(state, CommandKind.RespondEffect, p);
        state = KingsFixture.Do(state, CommandKind.RespondEffect, p);
        p = new() { GuideId = "base.character.boromir" };
        await Restart(state, CommandKind.SelectGuide, p);
        state = KingsFixture.Do(state, CommandKind.SelectGuide, p);
        state = KingsFixture.Do(state, CommandKind.DeclineReaction);
        state = KingsFixture.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        await Restart(state, CommandKind.SeparateCompanions,
            new() { Ids = [FateGimli.Figure], Route = ["hollin", "north-dunland", "south-dunland"] });
    }
}
