using System.Runtime.Loader;
using Wotr.Contracts;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public sealed class EmbeddedBotRuntimeTests
{
    private static string Fixture => typeof(Wotr.Bot.Runtime.RuntimeEntryPoint).Assembly.Location;
    private static BotRuntimeRequest Request() => new(3, Guid.NewGuid().ToString("N"), "0.2.0",
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

    // BOT-91: a local operator host writes each real request with its answer, in the private bot's stored-position
    // format, only when a dump directory is configured.
    [Fact]
    public void ALocalHostDumpsItsRealRequestsOnlyWhenAsked()
    {
        var directory = Path.Combine(Path.GetTempPath(), "wotr-dump-" + Guid.NewGuid().ToString("N"));
        var options = new BotHostOptions(true, Fixture, "0.2.0", 1, TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(500),
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)) { AllowUnpackaged = true, RequestDumpDirectory = directory };
        try
        {
            using (var runtime = EmbeddedBotRuntime.LoadConfigured(options))
            {
                Assert.Equal("Loaded", runtime.Status);
                var request = Request();
                Assert.Equal("Proposal", runtime.Decide(request).Status);
                var file = Assert.Single(Directory.GetFiles(directory));
                using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(file));
                Assert.Equal("real-request", document.RootElement.GetProperty("provenance").GetString());
                Assert.Equal(request.InvocationId, document.RootElement.GetProperty("request").GetProperty("invocationId").GetString());
                Assert.Equal("Proposal", document.RootElement.GetProperty("response").GetProperty("status").GetString());
            }
            using (var quiet = EmbeddedBotRuntime.LoadConfigured(options with { RequestDumpDirectory = null }))
                quiet.Decide(Request());
            Assert.Single(Directory.GetFiles(directory));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
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

    // BOT-20: a local game paused the CPU for minutes. Its Ruler token's Fellowship move is offered in
    // RulerTokenActions, not Options, and the host refused that proposal as foreign output on every retry.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ARulerTokenActionIsAProposalOnlyWhileItsTokenOffersIt(bool enabled)
    {
        using var runtime = EmbeddedBotRuntime.Load(Fixture);
        var request = Request() with { Memory = "ruler-token" };
        request.Player.RulerTokenActions =
            [new() { TokenId = "token", AbilityId = "ability", Action = new() { Kind = CommandKind.MoveFellowship, Enabled = enabled } }];
        var response = runtime.Decide(request);
        if (enabled)
        {
            Assert.Equal("Proposal", response.Status);
            Assert.Equal(CommandKind.MoveFellowship, response.Command!.Kind);
        }
        else
        {
            Assert.Equal("Refused", response.Status);
            Assert.StartsWith("InvalidOutput:NotOffered:MoveFellowship", response.Reason);
        }
    }

    [Fact]
    public void CancelledDisposedForeignOrHumanDecisionInputsCannotProduceMoves()
    {
        using var runtime = EmbeddedBotRuntime.Load(Fixture);
        Assert.Equal("Cancelled", runtime.Decide(Request(), new(true)).Reason);
        Assert.Equal("InvalidInput", runtime.Decide(Request() with { Side = Side.Shadow }).Reason);
        var humanTurn = Request();
        humanTurn.Player.DecisionOwner = Side.Shadow;
        Assert.StartsWith("InvalidOutput", runtime.Decide(humanTurn).Reason);
        runtime.Dispose();
        Assert.False(runtime.GameplayReady);
        Assert.Equal("RuntimeUnavailable", runtime.Decide(Request()).Reason);
    }
}
