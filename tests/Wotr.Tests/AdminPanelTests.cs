using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// ADMIN-01: the admin panel answers only an admin session of a configured account, counts games from the match ledger
// (a removed match still counts; tests and demo games apart) and never returns a sensitive field.
public class AdminPanelTests
{
    private const string Password = "An admin panel test password";
    private static readonly string[] UserFields =
        ["username", "createdAt", "lastSignInAt", "emailConfirmed", "tablesHosted", "games", "gamesFinished", "wins"];

    [Fact]
    public void ConfigurationNamesAccountsInAnyCaseAndRefusesMalformedNames()
    {
        var options = AdminOptions.Read(Configuration("Alice, bob_2;carol.x"));
        Assert.True(options.Enabled);
        Assert.True(options.Allows("ALICE"));
        Assert.True(options.Allows("Bob_2"));
        Assert.False(options.Allows("dave"));
        Assert.False(AdminOptions.Read(Configuration("")).Enabled);
        Assert.Throws<InvalidOperationException>(() => AdminOptions.Read(Configuration("alice, not an@address")));
    }

    [PostgresFact]
    public async Task OnlyAnAdminSessionOfAConfiguredAccountReadsThePanel()
    {
        var admin = Name();
        var player = Name();
        using var host = Host(PostgresTests.Connection, admin.ToUpperInvariant());
        using var client = Client(host);
        var adminGame = await Register(host, admin);
        var playerGame = await Register(host, player);
        await AssertHidden(client, null);
        await AssertHidden(client, adminGame.Token);
        await AssertHidden(client, playerGame.Token);
        Assert.Equal(HttpStatusCode.NotFound, (await SignIn(client, player, Password)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SignIn(client, admin, "a wrong but long enough password")).StatusCode);
        using (var nonsense = await client.PostAsync("/api/admin/session", new StringContent("nonsense", Encoding.UTF8, "application/json")))
            Assert.Equal(HttpStatusCode.NotFound, nonsense.StatusCode);
        using var signIn = await SignIn(client, admin.ToLowerInvariant(), Password);
        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
        Assert.Equal("no-store", signIn.Headers.CacheControl?.ToString());
        var session = (await signIn.Content.ReadFromJsonAsync<AdminSessionView>())!;
        Assert.Equal(admin, session.Username);
        Assert.InRange(session.ExpiresAt, DateTimeOffset.UtcNow.AddHours(7), DateTimeOffset.UtcNow.AddHours(9));
        using (var summary = await Get(client, "/api/admin/summary", session.Token))
        {
            Assert.Equal(HttpStatusCode.OK, summary.StatusCode);
            Assert.Equal("no-store", summary.Headers.CacheControl?.ToString());
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        await AssertHidden(client, session.Token);
    }

    [PostgresFact]
    public async Task SummaryAndUsersCountGamesFromTheLedger()
    {
        await using var database = await OwnDatabase.Create();
        var (a, b, c) = (Name(), Name(), Name());
        using var host = Host(database.Connection, a);
        using var client = Client(host);
        var admin = await Register(host, a);
        var free = await Register(host, b);
        var shadow = await Register(host, c);
        var data = host.Services.GetRequiredService<Database>();
        await Execute(data, "UPDATE accounts SET email_verified_at=now() WHERE id=$1", shadow.AccountId);
        await Execute(data, "INSERT INTO accounts(id,username,normalized_name,password_hash,technical) VALUES ('tech','Tech','#TECH','!',true)");
        var store = new MatchStore(data, new GameEngine(new Conceding()), new SequenceRandom());
        await PlayToTheEnd(store, free.AccountId, shadow.AccountId);
        await store.Create(AwaitingReaction(), admin.AccountId, free.AccountId);
        var removed = AwaitingReaction();
        await store.Create(removed, shadow.AccountId, free.AccountId);
        Assert.True(await host.Services.GetRequiredService<MatchRemoval>().Delete(removed.Id, shadow.AccountId));
        await InsertMatch(data, free.AccountId, "seat", MatchKind.Test);
        await InsertMatch(data, free.AccountId, "cpu", MatchKind.Demo);
        await host.Services.GetRequiredService<RoomStore>().Create(free.AccountId,
            new CreateRoomRequest { Side = Side.FreePeoples, Visibility = RoomVisibility.Private, Name = "" });
        var token = await AdminToken(client, a);

        using var summaryResponse = await Get(client, "/api/admin/summary", token);
        var summary = (await summaryResponse.Content.ReadFromJsonAsync<AdminSummaryView>())!;
        Assert.Equal((3, 1, 3), (summary.Users, summary.ConfirmedUsers, summary.SignedInLastWeek));
        Assert.Equal((1, 1), (summary.TablesCreated, summary.TablesWaiting));
        Assert.Equal((3, 1, 1, 1), (summary.GamesStarted, summary.GamesFinished, summary.GamesInProgress, summary.GamesRemovedUnfinished));
        Assert.Equal((0, 1), (summary.FreePeoplesWins, summary.ShadowWins));
        Assert.Equal((1, 1), (summary.TestSessions, summary.DemoGames));

        var first = await Users(client, token, 0, 2);
        Assert.Equal(3, first.Total);
        Assert.Equal([c, b], first.Users.Select(u => u.Username));
        var second = await Users(client, token, 1, 2);
        Assert.Equal([a], second.Users.Select(u => u.Username));
        var player = first.Users[1];
        Assert.Equal((3, 1, 0, 1, false), (player.Games, player.GamesFinished, player.Wins, player.TablesHosted, player.EmailConfirmed));
        var winner = first.Users[0];
        Assert.Equal((2, 1, 1, true), (winner.Games, winner.GamesFinished, winner.Wins, winner.EmailConfirmed));
        Assert.NotNull(winner.CreatedAt);
        Assert.NotNull(winner.LastSignInAt);
        Assert.Equal(HttpStatusCode.BadRequest, (await Get(client, "/api/admin/users?size=500", token)).StatusCode);
    }

    [PostgresFact]
    public async Task UserListCarriesNoSensitiveField()
    {
        var admin = Name();
        using var host = Host(PostgresTests.Connection, admin);
        using var client = Client(host);
        await Register(host, admin);
        var token = await AdminToken(client, admin);
        using var response = await Get(client, "/api/admin/users?size=100", token);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        foreach (var user in document.RootElement.GetProperty("users").EnumerateArray())
            Assert.Equal(UserFields, user.EnumerateObject().Select(p => p.Name));
        Assert.DoesNotContain("@", json, StringComparison.Ordinal);
        Assert.DoesNotContain(token, json, StringComparison.Ordinal);
    }

    // The migration dates the sessions and accounts already there and carries every existing match and table into the
    // ledgers, with the kind and result each one has.
    [PostgresFact]
    public async Task MigrationDatesExistingAccountsAndLedgersExistingMatches()
    {
        await using var admin = NpgsqlDataSource.Create(PostgresTests.Connection);
        var schema = "admin_" + Guid.NewGuid().ToString("N");
        await using (var create = admin.CreateCommand("CREATE SCHEMA " + schema))
            await create.ExecuteNonQueryAsync();
        try
        {
            var settings = new NpgsqlConnectionStringBuilder(PostgresTests.Connection) { SearchPath = schema };
            await using var source = NpgsqlDataSource.Create(settings.ConnectionString);
            var database = new Database(source);
            await MigrateBefore(source, 14);
            var expires = new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
            await Execute(database, "INSERT INTO accounts(id,username,normalized_name,password_hash) VALUES ('fp','Free','FREE','x'),('sh','Dark','DARK','x')");
            await Execute(database, "INSERT INTO sessions(token_hash,account_id,expires_at) VALUES ('t1','fp',$1),('t2','fp',$2)", expires, expires.AddDays(3));
            await Execute(database, "INSERT INTO matches(id,fp_account,sh_account,revision,state) VALUES ('won','fp','sh',9,'{\"winner\":\"Shadow\"}'::jsonb),('open','fp','sh',1,'{}'::jsonb)");
            await Execute(database, "INSERT INTO test_matches(match_id,owner_account,scenario_id) VALUES ('open','fp','setup')");
            await Execute(database, "INSERT INTO rooms(id,join_code,fp_account,state,host_account) VALUES ('room','code','fp','{}'::jsonb,'fp')");
            await database.Migrate();
            Assert.Equal(expires.AddDays(-30).UtcDateTime, await Scalar<DateTime>(database, "SELECT created_at FROM accounts WHERE id='fp'"));
            Assert.Equal(expires.AddDays(-27).UtcDateTime, await Scalar<DateTime>(database, "SELECT max(created_at) FROM sessions WHERE account_id='fp'"));
            Assert.Equal("game", await Scalar<string>(database, "SELECT purpose FROM sessions WHERE token_hash='t1'"));
            Assert.Equal("game|Shadow|true", await Scalar<string>(database,
                "SELECT kind || '|' || winner || '|' || (finished_at IS NOT NULL) FROM match_ledger WHERE match_id='won'"));
            Assert.Equal("test", await Scalar<string>(database, "SELECT kind FROM match_ledger WHERE match_id='open'"));
            Assert.Equal("fp", await Scalar<string>(database, "SELECT host_account FROM room_ledger WHERE room_id='room'"));
        }
        finally
        {
            await using var drop = admin.CreateCommand("DROP SCHEMA " + schema + " CASCADE");
            await drop.ExecuteNonQueryAsync();
        }
    }

    // DeclineReaction ends the game for the Shadow, so a save with a winner reaches the ledger through MatchStore.
    private sealed class Conceding : ICommandHandler
    {
        public bool Handles(CommandKind kind) => kind == CommandKind.DeclineReaction;
        public string? Validate(MatchState state, Side actor, GameCommand command) => null;
        public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random)
        {
            state.Resolution.Resume(state);
            state.Winner = Side.Shadow;
        }
    }

    private static async Task PlayToTheEnd(MatchStore store, string fp, string sh)
    {
        var state = AwaitingReaction();
        await store.Create(state, fp, sh);
        var receipt = await store.Execute(KernelTests.Command(state, CommandKind.DeclineReaction), fp);
        Assert.True(receipt!.Accepted, receipt.Error);
    }

    private static MatchState AwaitingReaction()
    {
        var state = KernelTests.NewGame();
        state.Id = Guid.NewGuid().ToString("N");
        state.Open(DecisionKind.Action, Side.Shadow, []);
        state.Resolution.Suspend(state, "test", 1);
        state.Open(DecisionKind.Reaction, Side.FreePeoples, [CommandKind.DeclineReaction]);
        return state;
    }

    private static async Task InsertMatch(Database database, string fp, string sh, MatchKind kind)
    {
        var state = KernelTests.NewGame();
        state.Id = Guid.NewGuid().ToString("N");
        await using var connection = await database.Source.OpenConnectionAsync();
        await MatchStore.Insert(connection, null, state, fp, sh, kind, CancellationToken.None);
    }

    private static async Task MigrateBefore(NpgsqlDataSource source, int version)
    {
        await using var connection = await source.OpenConnectionAsync();
        await using (var ledger = new NpgsqlCommand("CREATE TABLE schema_migrations (version integer PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now())", connection))
            await ledger.ExecuteNonQueryAsync();
        var assembly = typeof(Database).Assembly;
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.EndsWith(".sql", StringComparison.Ordinal)).Order())
        {
            var number = int.Parse(name.Split('.')[^2].Split('_')[0], System.Globalization.CultureInfo.InvariantCulture);
            if (number >= version)
                continue;
            using var stream = assembly.GetManifestResourceStream(name)!;
            await using (var ddl = new NpgsqlCommand(await new StreamReader(stream).ReadToEndAsync(), connection))
                await ddl.ExecuteNonQueryAsync();
            await using var record = Database.Command(connection, null, "INSERT INTO schema_migrations(version) VALUES ($1)", number);
            await record.ExecuteNonQueryAsync();
        }
    }

    private static AccountHost Host(string connection, string admins) =>
        new(connection, extra: new Dictionary<string, string?> { [AdminOptions.ConfigurationKey] = admins });

    private static HttpClient Client(AccountHost host) => host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

    private static Task<SessionView> Register(AccountHost host, string name) =>
        TestAccounts.Register(host.Services.GetRequiredService<AccountService>(), name, Password);

    private static Task<HttpResponseMessage> SignIn(HttpClient client, string username, string password) =>
        client.PostAsJsonAsync("/api/admin/session", new AdminSignInRequest(username, password));

    private static async Task<string> AdminToken(HttpClient client, string username)
    {
        using var response = await SignIn(client, username, Password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AdminSessionView>())!.Token;
    }

    private static async Task<HttpResponseMessage> Get(HttpClient client, string path, string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private static async Task<AdminUserPage> Users(HttpClient client, string token, int page, int size)
    {
        using var response = await Get(client, $"/api/admin/users?page={page}&size={size}", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AdminUserPage>())!;
    }

    // Anything but an administrator gets the empty 404 of a path that does not exist.
    private static async Task AssertHidden(HttpClient client, string? token)
    {
        foreach (var path in new[] { "/api/admin/summary", "/api/admin/users" })
        {
            using var response = await Get(client, path, token);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsStringAsync());
        }
    }

    private static async Task Execute(Database database, string sql, params object[] values)
    {
        await using var connection = await database.Source.OpenConnectionAsync();
        await using var command = Database.Command(connection, null, sql, values);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> Scalar<T>(Database database, string sql)
    {
        await using var connection = await database.Source.OpenConnectionAsync();
        await using var command = Database.Command(connection, null, sql);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private static IConfiguration Configuration(string admins) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { [AdminOptions.ConfigurationKey] = admins }).Build();

    private static string Name() => "admin_" + Guid.NewGuid().ToString("N")[..20];
}
