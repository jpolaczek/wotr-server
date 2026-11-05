using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// The class's own database: the clock service shifts and times out every clock in its database. One per class keeps
// the parallel suite from creating a database per test; each test starts with no clock and no heartbeat.
public sealed class ClockDatabase : IAsyncLifetime
{
    private OwnDatabase? database;

    public string Connection => database?.Connection ?? throw new InvalidOperationException("No test database.");

    public async Task InitializeAsync()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WOTR_TEST_DB")))
            database = await OwnDatabase.Create();
    }

    public async Task DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }
}

// RANK-06 (R35, docs/RANKED_PLAY.md D9): the move clocks of a ranked match on a test clock; the services are off and
// the test runs them by hand. The tests of this class run one after another on its database.
public sealed class RankedClockTests(ClockDatabase database) : IClassFixture<ClockDatabase>
{
    private static readonly TimeSpan Standard = TimeSpan.FromMinutes(3);

    [PostgresFact]
    public async Task TheFirstDecisionsClockStartsWithTheMatchAndShowsOnTheView()
    {
        await using var table = await ClockTable.Start(database.Connection);
        var row = await table.ClockRow();
        Assert.Equal(table.Pending.Id, row.Decision);
        Assert.Equal(table.Started + Standard + MoveClock.Grace + MoveClock.FirstDecisionBonus, row.Deadline);
        var view = (await table.Client(table.Pending.Owner).GetFromJsonAsync<PlayerView>($"/api/matches/{table.Match}", BoardCatalog.Json))!;
        var clock = Assert.IsType<MoveClockView>(view.Clock);
        Assert.Equal((table.Pending.Owner, MoveClockKind.Big, table.Pending.Id), (clock.Owner, clock.Kind, clock.DecisionId));
        Assert.Equal(((long)(Standard + MoveClock.FirstDecisionBonus).TotalMilliseconds, (long)Standard.TotalMilliseconds, 10000L),
            (clock.RemainingMilliseconds, clock.LimitMilliseconds, clock.WarningMilliseconds));
    }

    // A commit starts the next owner's clock from now, with no credit; a refused command changes nothing.
    [PostgresFact]
    public async Task ACommitStartsTheNextDecisionsClockWithNoCredit()
    {
        await using var table = await ClockTable.Start(database.Connection);
        table.Clock.Advance(TimeSpan.FromSeconds(50));
        var refused = await table.Send(CommandKind.Pass);
        Assert.False(refused.Accepted);
        Assert.Equal(table.Started + Standard + MoveClock.Grace + MoveClock.FirstDecisionBonus, (await table.ClockRow()).Deadline);

        var accepted = await table.Send(CommandKind.ContinueFellowship);
        Assert.True(accepted.Accepted, accepted.Error);
        var next = (await table.State()).Pending!;
        var row = await table.ClockRow();
        Assert.Equal(next.Id, row.Decision);
        var limit = MoveClock.Limit(RankedPreset.Standard, MoveClock.Kind(next.Kind));
        Assert.Equal(table.Started + TimeSpan.FromSeconds(50) + limit + MoveClock.Grace, row.Deadline);
    }

    // The owner loses when the deadline passes; a command racing it is refused, and the server's timeout ends the
    // game with both ratings changed.
    [PostgresFact]
    public async Task APassedDeadlineLosesTheGameForTheDecisionsOwner()
    {
        await using var table = await ClockTable.Start(database.Connection);
        Assert.Equal(0, await table.Service.Tick());
        table.Clock.Advance(Standard + MoveClock.Grace + MoveClock.FirstDecisionBonus + TimeSpan.FromMilliseconds(1));
        Assert.Equal(MatchClocks.ExpiredRefusal, (await table.Send(CommandKind.ContinueFellowship)).Error);
        Assert.Null((await table.State()).Winner);

        Assert.Equal(1, await table.Service.Tick());
        var state = await table.State();
        Assert.Equal((TurnFlow.Other(table.Pending.Owner), VictoryCondition.TimedOut), (state.Winner!.Value, state.Victory!.Value));
        Assert.Equal(2L, await table.Scalar("SELECT count(*) FROM rating_changes WHERE match_id=$1"));
        Assert.Equal(0L, await table.Scalar("SELECT count(*) FROM match_clocks WHERE match_id=$1"));
        Assert.Equal(0, await table.Service.Tick());
    }

    // D15: a commit sent as the network drops, retried after its next decision's deadline, returns its first receipt.
    [PostgresFact]
    public async Task ARetriedCommandThatAlreadyCountedKeepsItsReceiptPastTheDeadline()
    {
        await using var table = await ClockTable.Start(database.Connection);
        var command = await table.Command(CommandKind.ContinueFellowship);
        var first = await table.Send(command);
        Assert.True(first.Accepted, first.Error);
        table.Clock.Advance(TimeSpan.FromMinutes(10));
        var retried = await table.Send(command);
        Assert.Equal((true, first.Revision), (retried.Accepted, retried.Revision));
    }

    // A restart gives every running clock the time the server was down.
    [PostgresFact]
    public async Task DowntimeMovesEveryRunningDeadline()
    {
        await using var table = await ClockTable.Start(database.Connection);
        Assert.Equal(TimeSpan.Zero, await table.Service.Recover());
        var before = (await table.ClockRow()).Deadline;
        table.Clock.Advance(TimeSpan.FromSeconds(100));
        Assert.Equal(TimeSpan.FromSeconds(100), await table.Service.Recover());
        Assert.Equal(before + TimeSpan.FromSeconds(100), (await table.ClockRow()).Deadline);
        Assert.Equal(TimeSpan.Zero, await table.Service.Recover());
    }

    [PostgresFact]
    public async Task ANormalGameHasNoClock()
    {
        await using var table = await ClockTable.Start(database.Connection);
        var state = TestScenarioCatalog.Find("sh-military")!.Create(Guid.NewGuid().ToString("N"));
        await table.Host.Services.GetRequiredService<MatchStore>().Create(state, table.FreePeoplesAccount, table.ShadowAccount);
        var view = (await table.Client(Side.FreePeoples).GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!;
        Assert.Null(view.Clock);
        await using var data = NpgsqlDataSource.Create(table.Connection);
        await using var count = data.CreateCommand("SELECT count(*) FROM match_clocks WHERE match_id=$1");
        count.Parameters.Add(new NpgsqlParameter { Value = state.Id });
        Assert.Equal(0L, await count.ExecuteScalarAsync());
    }

    private sealed class ClockTable : IAsyncDisposable
    {
        public string Connection { get; private init; } = "";
        public AccountHost Host { get; private init; } = null!;
        public TestClock Clock { get; private init; } = null!;
        public MoveClockService Service => Host.Services.GetRequiredService<MoveClockService>();
        public string Match { get; private init; } = "";
        public string FreePeoplesAccount { get; private init; } = "";
        public string ShadowAccount { get; private init; } = "";
        public DateTimeOffset Started { get; private init; }
        public PendingDecision Pending { get; private init; } = null!;
        private HttpClient freePeoples = null!;
        private HttpClient shadow = null!;

        public static async Task<ClockTable> Start(string connection)
        {
            await Clear(connection);
            var clock = new TestClock(new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero));
            var settings = new Dictionary<string, string?>(TestBudgets.Stress)
            {
                [RankedOptions.PairingIntervalKey] = "0", [RankedOptions.ClockIntervalKey] = "0"
            };
            var host = new AccountHost(connection, extra: settings, services: s => s.AddSingleton<TimeProvider>(clock));
            var fp = await MenuAccounts.Register(host);
            var sh = await MenuAccounts.Register(host);
            var start = await host.Services.GetRequiredService<RankedMatches>().Start(fp.AccountId, sh.AccountId, new(), RankedPreset.Standard);
            Assert.Equal("", start.Error);
            var state = (await host.Services.GetRequiredService<MatchStore>().Read(start.MatchId, fp.AccountId))!.State;
            Assert.Equal((DecisionKind.FellowshipPhase, Side.FreePeoples), (state.Pending!.Kind, state.Pending.Owner));
            return new()
            {
                Connection = connection, Host = host, Clock = clock, Match = start.MatchId, FreePeoplesAccount = fp.AccountId,
                ShadowAccount = sh.AccountId, Started = clock.GetUtcNow(), Pending = state.Pending,
                freePeoples = MenuAccounts.Client(host, fp), shadow = MenuAccounts.Client(host, sh)
            };
        }

        public HttpClient Client(Side side) => side == Side.FreePeoples ? freePeoples : shadow;

        public async Task<MatchState> State() =>
            (await Host.Services.GetRequiredService<MatchStore>().Read(Match, FreePeoplesAccount))!.State;

        public async Task<CommandReceipt> Send(CommandKind kind) => await Send(await Command(kind));

        // The pending decision's owner sends it; sending the same pair again is a client's retry.
        public async Task<CommandReceipt> Send((GameCommand Command, string Account) sent) =>
            (await Host.Services.GetRequiredService<MatchStore>().Execute(sent.Command, sent.Account))!;

        public async Task<(GameCommand Command, string Account)> Command(CommandKind kind)
        {
            var state = await State();
            var command = new GameCommand
            {
                MatchId = Match, CommandId = Guid.NewGuid().ToString("N"), Kind = kind, DecisionId = state.Pending!.Id,
                ExpectedRevision = state.Revision, Payload = new()
            };
            return (command, state.Pending.Owner == Side.FreePeoples ? FreePeoplesAccount : ShadowAccount);
        }

        public async Task<(string Decision, DateTimeOffset Deadline)> ClockRow()
        {
            await using var data = NpgsqlDataSource.Create(Connection);
            await using var command = data.CreateCommand("SELECT decision_id, deadline FROM match_clocks WHERE match_id=$1");
            command.Parameters.Add(new NpgsqlParameter { Value = Match });
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            return (reader.GetString(0), reader.GetFieldValue<DateTimeOffset>(1));
        }

        public async Task<object?> Scalar(string sql)
        {
            await using var data = NpgsqlDataSource.Create(Connection);
            await using var command = data.CreateCommand(sql);
            command.Parameters.Add(new NpgsqlParameter { Value = Match });
            return await command.ExecuteScalarAsync();
        }

        public ValueTask DisposeAsync()
        {
            Host.Dispose();
            return ValueTask.CompletedTask;
        }

        // A test starts with no running clock and no heartbeat from the tests before it.
        private static async Task Clear(string connection)
        {
            await using var data = NpgsqlDataSource.Create(connection);
            foreach (var table in new[] { "match_clocks", "server_heartbeat" })
            {
                await using var clear = data.CreateCommand("DELETE FROM " + table);
                try
                {
                    await clear.ExecuteNonQueryAsync();
                }
                catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.UndefinedTable)
                {
                    // The class's first host has not migrated its database yet.
                }
            }
        }
    }
}
