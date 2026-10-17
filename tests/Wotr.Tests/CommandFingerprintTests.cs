using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class CommandFingerprintTests
{
    [Fact]
    public void VersionTwoKeepsTheFrozenCanonicalIdentityAndCost()
    {
        Assert.Equal("2:7C07AE391CB37DAFF76666ACD3671712537C51532C47E7DB4C8D266EA887F27D",
            CommandFingerprint.Create(LegacyContractFixture.Command()));
    }

    [Fact]
    public void StepwiseModeCannotReuseAnAtomicLegacyReceipt()
    {
        var command = LegacyContractFixture.Command();
        var saved = LegacyContractFixture.Read("fingerprint.txt");
        command.Payload.Stepwise = true;
        Assert.False(CommandFingerprint.Matches(saved, command, CommandFingerprint.Create(command)));
    }

    [Fact]
    public void ActualPreviousEngineCommandMatchesAfterAddingBothTokenFields()
    {
        var command = LegacyContractFixture.Command();
        var saved = LegacyContractFixture.Read("fingerprint.txt");
        var expanded = Hash(JsonSerializer.Serialize(command, BoardCatalog.Json));
        Assert.NotEqual(saved, expanded); // Demonstrates the former failure, before any helper normalization.
        Assert.True(CommandFingerprint.Matches(saved, command, CommandFingerprint.Create(command)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void KnownLegacyShapesPreserveNestedValuesAndRejectNewNonDefaultCosts(int generation)
    {
        var command = LegacyContractFixture.Command();
        command.Payload.Moves.Add(new() { From = "a", To = "b", Route = ["a", "b"], UnitIds = ["one", "two"] });
        command.Payload.CompanionMoves.Add(new() { Ids = ["gimli"], Route = ["a", "b"] });
        command.Payload.Losses.Add(new() { UnitId = "elite", Kind = BattleLossKind.Downgrade });
        command.Payload.AutoDecision = new() { BattleId = "battle", ExpectedVersion = 3, HandVersion = 5, Enabled = true };
        var node = JsonSerializer.SerializeToNode(command, BoardCatalog.Json)!;
        node["payload"]!.AsObject().Remove("stepwise");
        if (generation < 2)
            node["payload"]!.AsObject().Remove("councilTokenId");
        if (generation < 1)
            node["payload"]!.AsObject().Remove("rulerTokenId");
        var saved = Hash(node.ToJsonString(BoardCatalog.Json));
        Assert.True(CommandFingerprint.Matches(saved, command, CommandFingerprint.Create(command)));
        command.Payload.CouncilTokenId = "new-cost";
        Assert.False(CommandFingerprint.Matches(saved, command, CommandFingerprint.Create(command)));
    }

    [Theory]
    [InlineData("match")]
    [InlineData("id")]
    [InlineData("revision")]
    [InlineData("decision")]
    [InlineData("kind")]
    [InlineData("die")]
    [InlineData("ruler")]
    [InlineData("council")]
    [InlineData("route")]
    [InlineData("null-list")]
    [InlineData("stepwise")]
    public void VersionTwoBindsIdentityCostsAndOrderedChoices(string change)
    {
        var command = LegacyContractFixture.Command();
        command.Payload.Route = ["one", "two"];
        var before = CommandFingerprint.Create(command);
        Assert.StartsWith("2:", before);
        switch (change)
        {
            case "match":
                command.MatchId = "other";
                break;
            case "id":
                command.CommandId = "other";
                break;
            case "revision":
                command.ExpectedRevision++;
                break;
            case "decision":
                command.DecisionId = "other";
                break;
            case "kind":
                command.Kind = CommandKind.DiscardDie;
                break;
            case "die":
                command.Payload.DieId = "other";
                break;
            case "ruler":
                command.Payload.RulerTokenId = "other";
                break;
            case "council":
                command.Payload.CouncilTokenId = "other";
                break;
            case "route":
                command.Payload.Route.Reverse();
                break;
            case "null-list":
                command.Payload.Ids = null!;
                break;
            case "stepwise":
                command.Payload.Stepwise = true;
                break;
        }
        Assert.False(CommandFingerprint.Matches(before, command, CommandFingerprint.Create(command)));
    }

    [Fact]
    public void OmittedNeutralFieldsAndReorderedJsonProduceTheSameVersionTwoFingerprint()
    {
        var command = LegacyContractFixture.Command();
        var extended = JsonSerializer.SerializeToNode(command, BoardCatalog.Json)!.AsObject();
        var reversed = new JsonObject(extended.Reverse().Select(p => new KeyValuePair<string, JsonNode?>(p.Key, p.Value?.DeepClone())));
        var same = reversed.Deserialize<GameCommand>(BoardCatalog.Json)!;
        Assert.Equal(CommandFingerprint.Create(command), CommandFingerprint.Create(same));
        Assert.False(CommandFingerprint.Matches("3:unknown", command, CommandFingerprint.Create(command)));
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
