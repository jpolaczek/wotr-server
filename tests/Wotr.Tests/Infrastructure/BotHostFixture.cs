using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

internal sealed class BotHostFixture : IAsyncDisposable
{
    private readonly IsolatedPostgres postgres = new();
    public string Connection => postgres.Connection;
    public AccountHost Host { get; private set; } = null!;
    public Database Database => Host.Services.GetRequiredService<Database>();
    public RoomStore Rooms => Host.Services.GetRequiredService<RoomStore>();
    public MatchStore Matches => Host.Services.GetRequiredService<MatchStore>();
    public BotJobStore Jobs => Host.Services.GetRequiredService<BotJobStore>();
    public BotInputStore Inputs => Host.Services.GetRequiredService<BotInputStore>();
    public EmbeddedBotRuntime Runtime => Host.Services.GetRequiredService<EmbeddedBotRuntime>();
    public static string Library => typeof(Wotr.Bot.Runtime.RuntimeEntryPoint).Assembly.Location;
    public Dictionary<string, string?> Settings { get; } = new(TestBudgets.Stress)
    {
        ["Wotr:Bots:Enabled"] = "false", ["Wotr:Bots:RuntimeEnabled"] = "true",
        ["Wotr:Bots:RuntimeAssembly"] = Library, ["Wotr:Bots:RuntimeVersion"] = "0.2.0",
        ["Wotr:Bots:AllowUnpackagedRuntime"] = "true",
        ["Wotr:Bots:RoomsEnabled"] = "true", ["Wotr:Bots:AcceptedPolicyVersion"] = "fixture-1",
        ["Wotr:Bots:Features"] = "base", ["Wotr:Bots:Workers"] = "1",
        ["Wotr:Bots:PollMilliseconds"] = "50", ["Wotr:Bots:RuntimeLeaseSeconds"] = "5",
        ["Wotr:Bots:RetrySeconds"] = "1", ["Wotr:Rooms:EmptySeconds"] = "3600"
    };

    public static async Task<BotHostFixture> Create(IReadOnlyDictionary<string, string?>? settings = null)
    {
        var fixture = new BotHostFixture();
        foreach (var setting in settings ?? new Dictionary<string, string?>())
            fixture.Settings[setting.Key] = setting.Value;
        await fixture.postgres.Start();
        fixture.Host = new(fixture.Connection, expansions: false, extra: fixture.Settings, services: services =>
        {
            var scheduler = services.Single(s => s.ServiceType == typeof(IHostedService) && s.ImplementationType == typeof(BotScheduler));
            services.Remove(scheduler); // Deterministic direct scheduling, then actual Kestrel in restart tests.
        });
        _ = fixture.Database;
        return fixture;
    }

    public async Task<(SessionView Human, RoomView Room, BotWork Work)> Reserve(Side bot = Side.FreePeoples)
    {
        var human = await MenuAccounts.Register(Host);
        var room = await Rooms.Create(human.AccountId, new CreateRoomRequest { Side = TurnFlow.Other(bot), Name = "Runtime fixture" });
        var reserved = await Rooms.AddBot(room.Id, human.AccountId, PveFixture.Request(room));
        Assert.Equal("", reserved.Error);
        return (human, reserved.Room!, (await Jobs.Claim("fixture-owner"))!);
    }

    public async Task<(SessionView Human, RoomView Room, BotWork Work)> Game(Side bot = Side.FreePeoples,
        MatchState? seed = null, bool advanceHuman = true)
    {
        var (human, room, ready) = await Reserve(bot);
        Assert.True(await Rooms.ReadyRuntime(ready));
        Assert.Equal("", (await Rooms.SetReady(room.Id, human.AccountId, new() { ExpectedVersion = room.Version, Ready = true })).Error);
        var started = await Rooms.Start(room.Id, human.AccountId, room.Version);
        Assert.Equal("", started.Error);
        room = started.Room!;
        if (seed is not null)
            await Replace(room.MatchId, seed);
        else if (advanceHuman && bot == Side.Shadow)
        {
            var match = (await Matches.Read(room.MatchId, human.AccountId))!;
            Assert.True((await Matches.Execute(KernelTests.Command(match.State, CommandKind.ContinueFellowship), human.AccountId))!.Accepted);
        }
        return (human, room, (await Jobs.Claim("fixture-owner"))!);
    }

    public async Task Replace(string match, MatchState state)
    {
        state.Id = match;
        await Execute("DELETE FROM match_events WHERE match_id=$1", match);
        await Execute("UPDATE matches SET state=$2::jsonb,revision=$3,events_indexed=false,recorded=false WHERE id=$1",
            match, StateCodec.Write(state), state.Revision);
    }

    public async Task<(BotInput Input, BotRuntimeResponse Proposal)> Stage(BotWork work, string? mode = null)
    {
        if (mode is not null)
        {
            await Execute("UPDATE bot_runtime_jobs SET memory=$2 WHERE assignment_id=$1", work.AssignmentId, mode);
            work = work with { Memory = mode };
        }
        var input = (await Inputs.Read(work))!;
        var proposal = Runtime.Decide(input.Request);
        if (proposal.Status == "NeedPreviews")
        {
            input = input with { Request = (await Inputs.AddPreviews(work, input.Request, proposal))! };
            proposal = Runtime.Decide(input.Request);
        }
        Assert.Equal("Proposal", proposal.Status);
        await Jobs.Stage(work, input.Request, proposal, input.HistoryPosition);
        return (input, proposal);
    }

    public async Task<object?> Scalar(string sql, params object[] values)
    {
        await using var connection = await Database.Source.OpenConnectionAsync();
        await using var command = Wotr.Server.Database.Command(connection, null, sql, values);
        return await command.ExecuteScalarAsync();
    }

    public async Task Execute(string sql, params object[] values) => _ = await Scalar(sql, values);

    public async Task Until(string sql, object expected, params object[] values)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (!Equals(expected, await Scalar(sql, values)))
            await Task.Delay(20, timeout.Token);
    }

    public async Task Expire(BotWork work) => await Execute(
        "UPDATE bot_runtime_jobs SET lease_until='-infinity',available_at='-infinity' WHERE assignment_id=$1", work.AssignmentId);

    public async ValueTask DisposeAsync()
    {
        Host.Dispose();
        await postgres.DisposeAsync();
    }
}
