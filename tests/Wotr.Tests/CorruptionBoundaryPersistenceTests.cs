using System.Text.Json;
using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public sealed class CorruptionBoundaryPersistenceTests
{
    public static IEnumerable<object[]> Cases() => CorruptionBoundaryProjectionTests.Windows.SelectMany(w =>
        new[] { false, true }.Select(v => new object[] { w, v }));

    [PostgresTheory]
    [MemberData(nameof(Cases))]
    public async Task PrivateDrawKnowledgeAndExactEyeCostSurviveRestartAndReceiptReplay(string window, bool visible)
    {
        var state = CorruptionBoundaryProjectionTests.At(window, 5, visible);
        await using var fixture = await CorrespondenceFixture.Create();
        await fixture.Save(state);
        var before = await Read();
        foreach (var side in Enum.GetValues<Side>())
        {
            var expected = GameRuntime.Projection(EffectCatalog.All).For(state, side);
            Equal(expected.ActionBoundary, before[(int)side].ActionBoundary);
            Equal(expected.CorruptionAttempt, before[(int)side].CorruptionAttempt);
        }
        Assert.Empty(before[0].CorruptionAttempt!.OwnDrawnTiles);
        Assert.Equal(CorruptionRuntime.Current(state)!.Drawn.Count, before[1].CorruptionAttempt!.OwnDrawnTiles.Count);
        await fixture.Restart();
        Equal(before, await Read());
        var choice = before[1].Options.FirstOrDefault(o => o.Kind == CommandKind.ChooseCorruptionTile);
        var command = BattleFixture.Command(state, choice is null ? CommandKind.DrawCorruptionTile : CommandKind.ChooseCorruptionTile,
            new() { Ids = choice is null ? [] : [choice.EligibleIds[0]] });
        await CorrespondenceFixture.Send(fixture.Sh, command);
        var after = await Read();
        await fixture.Restart();
        await CorrespondenceFixture.Send(fixture.Sh, command);
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
