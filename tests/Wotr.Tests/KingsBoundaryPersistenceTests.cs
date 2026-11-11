using System.Text.Json;
using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public sealed class KingsBoundaryPersistenceTests
{
    public static IEnumerable<object[]> Cases() => KingsBoundaryProjectionTests.Windows.Select(w => new object[] { w });

    [PostgresTheory]
    [MemberData(nameof(Cases))]
    public async Task BothHumanViewsKeepTheirTileKnowledgeAndResumeAcrossHostRestarts(string window)
    {
        var state = KingsBoundaryProjectionTests.At(window, configuration: 5);
        await using var fixture = await CorrespondenceFixture.Create();
        await fixture.Save(state);
        var before = await Read();
        foreach (var side in Enum.GetValues<Side>())
            Equal(GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary, before[(int)side].ActionBoundary);
        Assert.All(before[0].Sovereigns.SelectMany(s => s.Tiles), t => Assert.Null(t.Value));
        Assert.All(before[1].Sovereigns.SelectMany(s => s.Tiles), t => Assert.NotNull(t.Value));
        await fixture.Restart();
        Equal(before, await Read());
        var view = before.Single(v => v.Decision is not null);
        var (kind, payload) = view.Decision!.Kind switch
        {
            DecisionKind.FellowshipPhase => (CommandKind.ContinueFellowship, new CommandPayload()),
            DecisionKind.AllocateHunt => (CommandKind.AllocateHunt, new CommandPayload()),
            DecisionKind.RulerDiscard => (CommandKind.DiscardRulerDie,
                new CommandPayload { DieId = view.Options.Single(o => o.Kind == CommandKind.DiscardRulerDie).EligibleDice[0] }),
            DecisionKind.Dismay => (CommandKind.DeclineDismay, new CommandPayload()),
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
