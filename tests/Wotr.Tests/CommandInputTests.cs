using System.Text.Json;
using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

// QUAL-01 (review R02): well-formed JSON with an explicit null, an unknown enum number, an oversized text or a
// broken nested element is malformed input. Preview and execution reject it as MalformedCommand without an
// exception, mutation, history or randomness, also in fields the command's kind does not use.
public sealed class CommandInputTests
{
    private static readonly string Long = new('x', CommandShape.MaxTextLength + 1);

    public static TheoryData<string, string> Mutations => new()
    {
        { "matchId", "null" }, { "decisionId", "null" }, { "kind", "999" }, { "payload", "null" },
        { "payload.dieId", "null" }, { "payload.ringId", "null" }, { "payload.rulerTokenId", "null" },
        { "payload.councilTokenId", "null" }, { "payload.deck", "null" }, { "payload.nationId", "null" },
        { "payload.cardId", "null" }, { "payload.reactionId", "null" }, { "payload.guideId", "null" },
        { "payload.regionId", "null" }, { "payload.characterId", "null" }, { "payload.abilityId", "null" },
        { "payload.cardId", "\"" + Long + "\"" },
        { "payload.face", "999" }, { "payload.movementCost", "999" }, { "payload.cardCost", "999" },
        { "payload.huntResponse", "999" },
        { "payload.ids", "null" }, { "payload.ids", "[null]" }, { "payload.route", "[\"\"]" },
        { "payload.placements", "[null]" }, { "payload.placements", "[{\"unitId\":null,\"regionId\":\"rivendell\"}]" },
        { "payload.moves", "[{\"from\":\"a\",\"to\":\"b\",\"unitIds\":[null],\"characterIds\":[],\"route\":[]}]" },
        { "payload.moves", "[{\"from\":null,\"to\":\"b\",\"unitIds\":[],\"characterIds\":[],\"route\":[]}]" },
        { "payload.losses", "[{\"unitId\":null,\"kind\":\"Remove\"}]" }, { "payload.losses", "[{\"unitId\":\"u\",\"kind\":9}]" },
        { "payload.companionMoves", "[{\"ids\":[null],\"route\":[]}]" },
        { "payload.autoDecision", "{\"battleId\":null,\"expectedVersion\":0,\"handVersion\":0,\"enabled\":true}" }
    };

    [Theory]
    [MemberData(nameof(Mutations))]
    public void MalformedInputIsRejectedBeforeAnyRuleReadsIt(string path, string json)
    {
        var state = FellowshipPhase();
        var command = Mutate(Valid(state), path, json).Deserialize<GameCommand>(BoardCatalog.Json)!;
        var before = StateCodec.Write(state);
        var engine = CharacterFixture.Engine();
        var random = new SequenceRandom();
        var preview = new PreviewService(engine).Preview(state, Side.FreePeoples, command);
        Assert.False(preview.Legal);
        Assert.Equal(CommandShape.MalformedCommand, preview.Error);
        Assert.Empty(preview.Costs);
        var result = engine.Execute(state, Side.FreePeoples, command, random);
        Assert.False(result.Receipt.Accepted);
        Assert.Equal(CommandShape.MalformedCommand, result.Receipt.Error);
        Assert.Equal(state.Revision, result.Receipt.Revision);
        Assert.Same(state, result.State);
        Assert.Equal(before, StateCodec.Write(state));
        Assert.Equal(0, random.Calls);
    }

    [Fact]
    public void WellFormedCommandsKeepTheirMeaningAndEmptyTextStaysNotChosen()
    {
        var state = FellowshipPhase();
        var command = Valid(state).Deserialize<GameCommand>(BoardCatalog.Json)!;
        Assert.True(CommandShape.IsValid(command));
        Assert.Equal("", command.Payload.RegionId);
        var result = CharacterFixture.Engine().Execute(state, Side.FreePeoples, command, new SequenceRandom());
        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        var wrongMatch = Valid(state).Deserialize<GameCommand>(BoardCatalog.Json)!;
        wrongMatch.MatchId = "another";
        Assert.Equal("NotAuthorized", CharacterFixture.Engine().Validate(state, Side.FreePeoples, wrongMatch));
    }

    [PostgresFact]
    public async Task ReviewReproductionsReturnAControlledReceiptInsteadOfAServerError()
    {
        var random = new SequenceRandom();
        await using var host = new TestHost(random);
        foreach (var field in new[] { "rulerTokenId", "councilTokenId", "regionId" })
        {
            var state = FellowshipPhase();
            await UI10ProtocolFixture.Save(host, state);
            var command = Mutate(Valid(state), "payload." + field, "null");
            using var client = host.Client("alice");
            await UI10ProtocolFixture.RejectedWithoutMutation(host, client, state, command, CommandShape.MalformedCommand);
        }
        Assert.Equal(0, random.Calls);
    }

    private static MatchState FellowshipPhase()
    {
        var state = KernelTests.NewGame();
        TurnFlow.Start(state);
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
        return state;
    }

    private static JsonObject Valid(MatchState state)
    {
        var command = KernelTests.Command(state, CommandKind.ContinueFellowship);
        command.Payload = new CommandPayload();
        return UI10ProtocolFixture.Json(command);
    }

    private static JsonObject Mutate(JsonObject command, string path, string json)
    {
        var parts = path.Split('.');
        var target = parts.Length == 1 ? command : command[parts[0]]!.AsObject();
        target[parts[^1]] = JsonNode.Parse(json);
        return command;
    }
}
