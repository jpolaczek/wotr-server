using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain.BotProtocol;
using Xunit;

namespace Wotr.Tests;

// BOT-225: AlphaWOTR answers any of a die's recruitments with the die's first "recruit"; its info.engine (JSON text, cut at
// 240 characters) names the Voice of Saruman's variant, which the seat follows.
public sealed class BotProtocolHintsTests
{
    private static readonly GameCommand Plain = new() { Kind = CommandKind.Recruit };
    private static readonly GameCommand Settlements = new() { Kind = CommandKind.UseCharacterAbility, Payload = new() { AbilityId = "voice-recruit" } };
    private static readonly GameCommand Swap = new() { Kind = CommandKind.UseCharacterAbility, Payload = new() { AbilityId = "voice-upgrade" } };

    private static ProtocolNode Action() => new("ACTION", null, new[]
    {
        new ProtocolBranch(ProtocolAtom.Of("pass"), "Pass", new GameCommand { Kind = CommandKind.Pass }),
        new ProtocolBranch(ProtocolAtom.Of("use_die", ("die", 4), ("face", "MUSTER"), ("action", "recruit")), "Recruit", Plain),
        new ProtocolBranch(ProtocolAtom.Of("use_die", ("die", 4), ("face", "MUSTER"), ("action", "recruit"), ("nation", "ISENGARD"),
            ("saruman", "settlements")), "Voice: settlements", Settlements),
        new ProtocolBranch(ProtocolAtom.Of("use_die", ("die", 4), ("face", "MUSTER"), ("action", "recruit"), ("nation", "ISENGARD"),
            ("saruman", "swap2")), "Voice: upgrade", Swap)
    });

    private static JsonNode Info(string engine) => new JsonObject { ["how"] = "search", ["engine"] = engine };

    [Fact]
    public void TheEnginesVariantChoosesTheVoiceAndAPlainMusterStaysPlain()
    {
        var question = Action();
        Assert.Equal(2, ProtocolHints.Adjust(question, 1, Info(
            "{\"type\":\"muster_recruit\",\"die\":4,\"params\":{\"nation\":\"ISENGARD\",\"region\":\"orthanc\",\"type\":\"regular\",\"saruman\":\"settlements\",\"placements\":[\"orthanc\",\"north dunland\",\"south dunland\"]}}")));
        Assert.Equal(3, ProtocolHints.Adjust(question, 1, Info(
            "{\"type\":\"muster_elite\",\"die\":4,\"params\":{\"nation\":\"ISENGARD\",\"region\":\"orthanc\",\"saruman\":\"swap2\"}}")));
        Assert.Equal(1, ProtocolHints.Adjust(question, 2, Info(
            "{\"type\":\"muster_recruit\",\"die\":4,\"params\":{\"nation\":\"SAURON\",\"region\":\"barad dur\",\"type\":\"regular\"}}")));
        // A cut text still names its variant; no info, or another action, changes nothing.
        Assert.Equal(3, ProtocolHints.Adjust(question, 1, Info("{\"type\":\"muster_elite\",\"die\":4,\"params\":{\"saruman\":\"swap2\",\"placem")));
        Assert.Equal(1, ProtocolHints.Adjust(question, 1, null));
        Assert.Equal(0, ProtocolHints.Adjust(question, 0, Info("{\"type\":\"muster_elite\",\"die\":4,\"params\":{\"saruman\":\"swap2\"}}")));
    }

    [Fact]
    public void TheEnginesLossIsTheOneTakenWhateverItsAdapterAnswered()
    {
        GameCommand Loss(string id) => new() { Kind = CommandKind.TakeBattleLosses, Payload = new() { CardId = id } };
        var question = new ProtocolNode("CASUALTY", new JsonObject { ["hits"] = 2 }, new[]
        {
            new ProtocolBranch(ProtocolAtom.Of("casualty", ("nation", "GONDOR"), ("unit", "regular"), ("effect", "remove")), "a", Loss("a")),
            new ProtocolBranch(ProtocolAtom.Of("casualty", ("nation", "NORTH"), ("unit", "regular"), ("effect", "remove")), "b", Loss("b")),
            new ProtocolBranch(ProtocolAtom.Of("casualty", ("nation", "NORTH"), ("unit", "elite"), ("effect", "reduce")), "c", Loss("c")),
            new ProtocolBranch(ProtocolAtom.Of("casualty", ("nation", "NORTH"), ("unit", "elite"), ("effect", "remove")), "d", Loss("d"))
        });
        string Engine(string nation, string operation) =>
            $"{{\"type\":\"casualty_allocate\",\"die\":-1,\"params\":{{\"casualties\":2,\"nation\":\"{nation}\",\"operation\":\"{operation}\"}}}}";
        Assert.Equal(2, ProtocolHints.Adjust(question, 0, Info(Engine("THE_NORTH", "reduce_elite"))));
        Assert.Equal(3, ProtocolHints.Adjust(question, 0, Info(Engine("THE_NORTH", "remove_elite"))));
        Assert.Equal(1, ProtocolHints.Adjust(question, 0, Info(Engine("THE_NORTH", "remove_regular"))));
        Assert.Equal(0, ProtocolHints.Adjust(question, 1, Info(Engine("GONDOR", "remove_regular"))));
        Assert.Equal(1, ProtocolHints.Adjust(question, 1, Info(Engine("ROHAN", "remove_regular"))));
    }
}
