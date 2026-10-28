using System.Runtime.Loader;
using Wotr.Contracts;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public sealed class EmbeddedBotRuntimeTests
{
    private static string Fixture => typeof(Wotr.Bot.Runtime.RuntimeEntryPoint).Assembly.Location;
    private static BotRuntimeRequest Request() => new(2, Guid.NewGuid().ToString("N"), "0.2.0",
        Protocol.Version, Protocol.RulesVersion, Protocol.ContentVersion, "fixture-match", Side.FreePeoples,
        new PlayerView
        {
            MatchId = "fixture-match", Revision = 12, You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
            Decision = new() { Id = "decision-12" }, Options = [new() { Kind = CommandKind.ContinueFellowship }]
        }, [], [], [], null, 27, 1000);

    [Fact]
    public void MissingLibraryKeepsTheHostAvailableAndCpuUnready()
    {
        using var absent = EmbeddedBotRuntime.Load(null);
        Assert.Equal("NotConfigured", absent.Status);
        Assert.False(absent.GameplayReady);
        Assert.Equal("RuntimeUnavailable", absent.Decide(Request()).Reason);
        using var missing = EmbeddedBotRuntime.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll"));
        Assert.Equal("Unavailable", missing.Status);
    }

    [Fact]
    public void LoaderRejectsWrongVersionAndWrongEntrypoint()
    {
        using var incompatible = EmbeddedBotRuntime.Load(Fixture, "99.0.0");
        Assert.Equal("Incompatible", incompatible.Status);
        Assert.False(incompatible.GameplayReady);
        using var wrong = EmbeddedBotRuntime.Load(typeof(PlayerView).Assembly.Location);
        Assert.Equal("LoadFailed", wrong.Status);
    }

    [Fact]
    public void FixtureLoadsInItsOwnContextAndReturnsOnlyAnUncommittedProposal()
    {
        using var runtime = EmbeddedBotRuntime.Load(Fixture);
        Assert.Equal("Loaded", runtime.Status);
        Assert.Contains(AssemblyLoadContext.All, context => context.IsCollectible &&
            context.Assemblies.Any(assembly => assembly.GetName().Name == "Wotr.RuntimeFixture"));
        var request = Request();
        var response = runtime.Decide(request);
        Assert.Equal("Proposal", response.Status);
        Assert.Equal(request.InvocationId, response.InvocationId);
        Assert.Equal(12, request.Player.Revision);
        Assert.Null(request.Memory);
        runtime.Description!.Profiles[0] = "mutated";
        Assert.Contains("free-peoples-strong", runtime.Description!.Profiles);
    }

    [Theory]
    [InlineData("foreign")]
    [InlineData("future")]
    [InlineData("correlation")]
    [InlineData("malformed")]
    [InlineData("large")]
    [InlineData("throw")]
    public void InvalidOrFailedRuntimeCannotReturnAnExecutableCommandOrExposeExceptions(string mode)
    {
        using var runtime = EmbeddedBotRuntime.Load(Fixture);
        var response = runtime.Decide(Request() with { Memory = mode });
        Assert.Equal("Refused", response.Status);
        Assert.Null(response.Command);
        Assert.Null(response.ProposedMemory);
        Assert.DoesNotContain("fixture-private-secret", response.Reason);
    }

    [Fact]
    public void CancelledDisposedForeignOrHumanDecisionInputsCannotProduceMoves()
    {
        using var runtime = EmbeddedBotRuntime.Load(Fixture);
        Assert.Equal("Cancelled", runtime.Decide(Request(), new(true)).Reason);
        Assert.Equal("InvalidInput", runtime.Decide(Request() with { Side = Side.Shadow }).Reason);
        var humanTurn = Request();
        humanTurn.Player.DecisionOwner = Side.Shadow;
        Assert.Equal("InvalidOutput", runtime.Decide(humanTurn).Reason);
        runtime.Dispose();
        Assert.False(runtime.GameplayReady);
        Assert.Equal("RuntimeUnavailable", runtime.Decide(Request()).Reason);
    }
}
