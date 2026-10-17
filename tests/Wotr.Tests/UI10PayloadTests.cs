using System.Text.Json;
using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public sealed class UI10PayloadTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NullNestedRouteFromRawJsonIsRejectedBeforeDomainPreviewAndExecution(bool cardMarch)
    {
        var state = cardMarch
            ? ShadowStrategy02Fixture.Play(ShadowStrategy02Fixture.State(ShadowStrategyCards02.Moving), ShadowStrategyCards02.Moving)
            : BattleFixture.State();
        var payload = cardMarch
            ? new CommandPayload { Moves = [ShadowStrategy02Fixture.Move(state, "moria", "hollin")] }
            : BattleFixture.AttackPayload(state,
                [BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-tirith").Id], "minas-tirith", "osgiliath");
        Assert.Null(CharacterFixture.Engine().Validate(state, state.Pending!.Owner,
            BattleFixture.Command(state, CommandKind.MoveArmies, payload)));
        var json = UI10ProtocolFixture.Json(BattleFixture.Command(state, CommandKind.MoveArmies, payload));
        json["payload"]!["moves"]![0]!["route"] = null;
        var command = json.Deserialize<GameCommand>(BoardCatalog.Json)!;
        var before = StateCodec.Write(state);
        var engine = CharacterFixture.Engine();
        var random = new SequenceRandom();

        Assert.Equal(CommandShape.MalformedCommand, engine.Validate(state, state.Pending!.Owner, command));
        Assert.False(new PreviewService(engine).Preview(state, state.Pending.Owner, command).Legal);
        var result = engine.Execute(state, state.Pending.Owner, command, random);
        Assert.False(result.Receipt.Accepted);
        Assert.Equal(CommandShape.MalformedCommand, result.Receipt.Error);
        Assert.Equal(before, StateCodec.Write(result.State));
        Assert.Equal(0, random.Calls);
    }

    [Theory]
    [InlineData("from")]
    [InlineData("to")]
    public void NullArmyRegionFromRawJsonIsRejectedWithoutDomainMutation(string field)
    {
        var state = ShadowStrategy02Fixture.Play(ShadowStrategy02Fixture.State(ShadowStrategyCards02.Moving), ShadowStrategyCards02.Moving);
        var raw = MarchCommand(state);
        raw["payload"]!["moves"]![0]![field] = null;
        var command = raw.Deserialize<GameCommand>(BoardCatalog.Json)!;
        var before = StateCodec.Write(state);
        var random = new SequenceRandom();
        var result = CharacterFixture.Engine().Execute(state, Side.Shadow, command, random);
        Assert.False(result.Receipt.Accepted);
        Assert.Equal(CommandShape.MalformedCommand, result.Receipt.Error);
        Assert.Equal(before, StateCodec.Write(result.State));
        Assert.Equal(0, random.Calls);
    }

    [PostgresFact]
    public async Task NullNestedArmyRouteIsRejectedByPreviewAndCommitForOrdinaryMovement()
    {
        var random = new SequenceRandom();
        await using var host = new TestHost(random);
        using var client = host.Client("alice");
        var state = BattleFixture.State();
        var unit = BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-tirith");
        await UI10ProtocolFixture.Save(host, state);
        var command = UI10ProtocolFixture.Json(BattleFixture.Command(state, CommandKind.MoveArmies,
            BattleFixture.AttackPayload(state, [unit.Id], "minas-tirith", "osgiliath")));
        command["payload"]!["moves"]![0]!["route"] = null;

        await UI10ProtocolFixture.RejectedWithoutMutation(host, client, state, command, CommandShape.MalformedCommand);
        Assert.Equal(0, random.Calls);
    }

    [PostgresFact]
    public async Task MalformedArmyMarchChoicesReturnSafeReceiptsWithoutMutation()
    {
        var random = new SequenceRandom();
        await using var host = new TestHost(random);
        using var client = host.Client("bob");
        var state = ShadowStrategy02Fixture.Play(ShadowStrategy02Fixture.State(ShadowStrategyCards02.Moving), ShadowStrategyCards02.Moving);
        await UI10ProtocolFixture.Save(host, state);
        foreach (var field in new[] { "route", "from", "to", "unitIds", "characterIds" })
        {
            var command = MarchCommand(state);
            command["payload"]!["moves"]![0]![field] = null;
            await UI10ProtocolFixture.RejectedWithoutMutation(host, client, state, command, CommandShape.MalformedCommand);
        }
        // QUAL-01: a null step or a route over the limit is malformed; an unknown region is an illegal choice.
        foreach (var (route, error) in new[]
                 {
                     (new JsonArray((JsonNode?)null), CommandShape.MalformedCommand),
                     (new JsonArray("private-untrusted-marker"), "IllegalChoice"),
                     (new JsonArray(Enumerable.Repeat("moria", 129).Select(s => (JsonNode?)JsonValue.Create(s)).ToArray()), CommandShape.MalformedCommand)
                 })
        {
            var command = MarchCommand(state);
            command["payload"]!["moves"]![0]!["route"] = route;
            await UI10ProtocolFixture.RejectedWithoutMutation(host, client, state, command, error);
        }
        Assert.Equal(0, random.Calls);
    }

    [PostgresFact]
    public async Task MalformedOuterCollectionsAndForeignFigureAreRejectedWithoutRng()
    {
        var random = new SequenceRandom();
        await using var host = new TestHost(random);
        using var client = host.Client("bob");
        var state = ShadowStrategy02Fixture.Play(ShadowStrategy02Fixture.State(ShadowStrategyCards02.Moving), ShadowStrategyCards02.Moving);
        var foreign = BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-tirith");
        await UI10ProtocolFixture.Save(host, state);
        foreach (var field in new[] { "ids", "route", "moves", "placements", "losses", "companionMoves" })
        {
            var command = MarchCommand(state);
            command["payload"]![field] = null;
            await UI10ProtocolFixture.RejectedWithoutMutation(host, client, state, command, CommandShape.MalformedCommand);
        }
        foreach (var field in new[] { "route", "moves", "placements", "losses", "companionMoves" })
        {
            var command = MarchCommand(state);
            command["payload"]![field] = new JsonArray((JsonNode?)null);
            await UI10ProtocolFixture.RejectedWithoutMutation(host, client, state, command, CommandShape.MalformedCommand);
        }
        var spoof = MarchCommand(state);
        spoof["payload"]!["moves"]![0]!["unitIds"] = new JsonArray(foreign.Id);
        await UI10ProtocolFixture.RejectedWithoutMutation(host, client, state, spoof);
        Assert.Equal(0, random.Calls);
    }

    private static JsonObject MarchCommand(MatchState state) => UI10ProtocolFixture.Json(
        BattleFixture.Command(state, CommandKind.MoveArmies,
            new() { Moves = [ShadowStrategy02Fixture.Move(state, "moria", "hollin")] }));
}
