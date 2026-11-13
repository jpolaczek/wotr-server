using System.Text.Json;
using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public sealed class LordsBoundaryPersistenceTests
{
    public static IEnumerable<object[]> Cases() => LordsBoundaryProjectionTests.Windows.Select(w => new object[] { w });

    [PostgresTheory]
    [MemberData(nameof(Cases))]
    public async Task HumanResponsesKeepLordsFactsAndResumeAfterRestart(string window)
    {
        var state = LordsBoundaryProjectionTests.At(window, fate: true);
        await using var fixture = await CorrespondenceFixture.Create();
        await fixture.Save(state);
        var before = await Read();
        foreach (var side in Enum.GetValues<Side>())
            Equal(GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary, before[(int)side].ActionBoundary);
        await fixture.Restart();
        Equal(before, await Read());
        var view = before.Single(v => v.Decision is not null);
        var (kind, payload) = view.Decision!.Kind switch
        {
            DecisionKind.CouncilVersions => (CommandKind.ChooseCompanionVersions,
                new CommandPayload { Ids = [CharacterRules.Grey], GuideId = CharacterRules.Grey }),
            DecisionKind.CouncilHomelands => (CommandKind.ChooseCouncilHomelands, new CommandPayload()),
            DecisionKind.CouncilToken => (CommandKind.ChooseCouncilToken, new CommandPayload { Ids = [CouncilTokens.Politics] }),
            DecisionKind.FellowshipPhase => (CommandKind.ContinueFellowship, new CommandPayload()),
            DecisionKind.AllocateHunt => (CommandKind.AllocateHunt, new CommandPayload()),
            DecisionKind.SpecialDieChoice => (CommandKind.ChooseSpecialDie,
                new CommandPayload { DieId = view.Options.Single(o => o.Kind == CommandKind.ChooseSpecialDie).EligibleDice[0] }),
            DecisionKind.ActionSequence => (CommandKind.FinishActionSequence, new CommandPayload()),
            DecisionKind.KeepActionDie => (CommandKind.DeclineVilya, new CommandPayload()),
            _ => (CommandKind.DiscardDie,
                new CommandPayload { DieId = view.Options.Single(o => o.Kind == CommandKind.DiscardDie).EligibleDice[0] })
        };
        var command = BattleFixture.Command(state, kind, payload);
        await CorrespondenceFixture.Send(view.You == Side.FreePeoples ? fixture.Fp : fixture.Sh, command);
        var after = await Read();
        await fixture.Restart();
        await CorrespondenceFixture.Send(view.You == Side.FreePeoples ? fixture.Fp : fixture.Sh, command);
        Equal(after, await Read());

        async Task<PlayerView[]> Read() =>
        [
            await CorrespondenceFixture.Read<PlayerView>(fixture.Fp, $"/api/matches/{state.Id}"),
            await CorrespondenceFixture.Read<PlayerView>(fixture.Sh, $"/api/matches/{state.Id}")
        ];
    }

    private static void Equal<T>(T expected, T actual) => Assert.True(JsonNode.DeepEquals(
        JsonSerializer.SerializeToNode(expected), JsonSerializer.SerializeToNode(actual)));
}
