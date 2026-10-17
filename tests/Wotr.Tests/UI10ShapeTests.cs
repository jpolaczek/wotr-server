using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public sealed class UI10ShapeTests
{
    [Theory]
    [InlineData("ids")]
    [InlineData("unitIds")]
    [InlineData("characterIds")]
    [InlineData("companionIds")]
    [InlineData("placementUnit")]
    [InlineData("placementRegion")]
    [InlineData("lossUnit")]
    [InlineData("dieId")]
    [InlineData("regionId")]
    public void NullIdentifiersInRealDecisionFamiliesAreRejectedBeforeMutation(string field)
    {
        var (state, raw) = UI10ShapeCases.Create(field);
        var command = raw.Deserialize<GameCommand>(BoardCatalog.Json)!;
        var before = StateCodec.Write(state);
        var engine = CharacterFixture.Engine();
        var random = new SequenceRandom();
        var preview = new PreviewService(engine).Preview(state, state.Pending!.Owner, command);
        Assert.False(preview.Legal);
        Assert.Equal(CommandShape.MalformedCommand, preview.Error);
        var result = engine.Execute(state, state.Pending.Owner, command, random);
        Assert.False(result.Receipt.Accepted);
        Assert.Equal(CommandShape.MalformedCommand, result.Receipt.Error);
        Assert.Equal(before, StateCodec.Write(state));
        Assert.Same(state, result.State);
        Assert.Equal(0, random.Calls);
    }

    [PostgresFact]
    public async Task NullIdentifiersAcrossDecisionFamiliesReturnSafeHttpReceipts()
    {
        var random = new SequenceRandom();
        await using var host = new TestHost(random);
        foreach (var field in UI10ShapeCases.Fields)
        {
            var (state, command) = UI10ShapeCases.Create(field);
            await UI10ProtocolFixture.Save(host, state);
            command["matchId"] = state.Id;
            using var client = host.Client(state.Pending!.Owner == Side.FreePeoples ? "alice" : "bob");
            await UI10ProtocolFixture.RejectedWithoutMutation(host, client, state, command, CommandShape.MalformedCommand);
        }
        Assert.Equal(0, random.Calls);
    }
}
