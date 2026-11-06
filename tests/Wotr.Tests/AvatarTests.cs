using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// AVATAR-02: the avatar on the account, the account page's own session, and every view that names a player. A real database.
public sealed class AvatarTests(ClockDatabase database) : IClassFixture<ClockDatabase>
{
    private const string Password = MenuAccounts.Password;

    private AccountHost Host() => new(database.Connection, expansions: true, extra: TestBudgets.Stress);

    private static HttpClient Anonymous(AccountHost host) =>
        host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

    private static HttpClient Bearer(AccountHost host, string token)
    {
        var client = Anonymous(host);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<AccountPageEndpoints.AccountSessionView> SignIn(AccountHost host, SessionView account)
    {
        using var client = Anonymous(host);
        using var answer = await client.PostAsJsonAsync("/api/account/session",
            new LoginRequest { Username = account.Username, Password = Password });
        answer.EnsureSuccessStatusCode();
        return (await answer.Content.ReadFromJsonAsync<AccountPageEndpoints.AccountSessionView>())!;
    }

    private static async Task Choose(AccountHost host, SessionView account, string avatar)
    {
        using var page = Bearer(host, (await SignIn(host, account)).Token);
        using var answer = await page.PutAsJsonAsync("/api/account/avatar", new { avatar });
        answer.EnsureSuccessStatusCode();
    }

    private static async Task<T> Read<T>(HttpClient client, string path) =>
        (await client.GetFromJsonAsync<T>(path, BoardCatalog.Json))!;

    private static RoomStore Rooms(AccountHost host) => host.Services.GetRequiredService<RoomStore>();

    [PostgresFact]
    public async Task RegistrationsDrawBothDefaultsAndNothingElse()
    {
        using var host = Host();
        var avatars = new HashSet<string>();
        for (var index = 0; index < 200; index++)
            avatars.Add((await MenuAccounts.Register(host)).Avatar);
        Assert.Equal(new HashSet<string> { AvatarIds.Eye, AvatarIds.Ring }, avatars);
    }

    [PostgresFact]
    public async Task TheAccountPageReadsAndChangesTheAvatarAndSignOutEndsItsSession()
    {
        using var host = Host();
        var account = await MenuAccounts.Register(host);
        var session = await SignIn(host, account);
        Assert.Equal(account.Username, session.Name);
        Assert.Equal(account.Avatar, session.Avatar);
        Assert.InRange(session.ExpiresAt - DateTimeOffset.UtcNow, TimeSpan.FromHours(7.9), TimeSpan.FromHours(8.1));

        using var page = Bearer(host, session.Token);
        using var read = await page.GetAsync("/api/account");
        Assert.Equal("no-store", read.Headers.CacheControl?.ToString());
        Assert.Equal(account.Username, (await read.Content.ReadFromJsonAsync<AccountView>())!.Name);

        using var chosen = await page.PutAsJsonAsync("/api/account/avatar", new { avatar = "rohan" });
        Assert.Equal("rohan", (await chosen.Content.ReadFromJsonAsync<AccountView>())!.Avatar);
        Assert.Equal("rohan", (await Read<AccountView>(page, "/api/account")).Avatar);

        using var signedOut = await page.DeleteAsync("/api/account/session");
        Assert.Equal(HttpStatusCode.NoContent, signedOut.StatusCode);
        using var gone = await page.GetAsync("/api/account");
        Assert.Equal(HttpStatusCode.Unauthorized, gone.StatusCode);
    }

    [PostgresFact]
    public async Task TheAccountPageIsToldOnlyTheNameAndTheAvatar()
    {
        using var host = Host();
        var account = await MenuAccounts.Register(host);
        using var page = Bearer(host, (await SignIn(host, account)).Token);
        using var document = JsonDocument.Parse(await page.GetStringAsync("/api/account"));
        var names = document.RootElement.EnumerateObject().Select(property => property.Name).Order();
        Assert.Equal(new[] { "avatar", "name" }, names);
    }

    [PostgresTheory]
    [InlineData("gandalf")]
    [InlineData("")]
    [InlineData("EYE")]
    [InlineData(null)]
    public async Task AnUnknownAvatarIsRefusedAndTheOldOneStays(string? avatar)
    {
        using var host = Host();
        var account = await MenuAccounts.Register(host);
        using var page = Bearer(host, (await SignIn(host, account)).Token);
        using var answer = await page.PutAsJsonAsync("/api/account/avatar", new { avatar });
        Assert.Equal(HttpStatusCode.BadRequest, answer.StatusCode);
        Assert.Equal("UnknownAvatar", (await answer.Content.ReadFromJsonAsync<ApiError>(BoardCatalog.Json))!.Error);
        Assert.Equal(account.Avatar, (await Read<AccountView>(page, "/api/account")).Avatar);
    }

    [PostgresFact]
    public async Task AWrongPasswordIsRefusedAndRepeatedOnesAreThrottled()
    {
        using var host = Host();
        var account = await MenuAccounts.Register(host);
        using var client = Anonymous(host);
        var wrong = new LoginRequest { Username = account.Username, Password = "not the password at all" };
        using var first = await client.PostAsJsonAsync("/api/account/session", wrong);
        Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
        var throttled = false;
        for (var attempt = 0; attempt < 40 && !throttled; attempt++)
        {
            using var answer = await client.PostAsJsonAsync("/api/account/session", wrong);
            throttled = answer.StatusCode == HttpStatusCode.TooManyRequests;
        }
        Assert.True(throttled);
    }

    [PostgresFact]
    public async Task AGameSessionCannotUseTheAccountPage()
    {
        using var host = Host();
        var account = await MenuAccounts.Register(host);
        using var game = MenuAccounts.Client(host, account);
        using var read = await game.GetAsync("/api/account");
        Assert.Equal(HttpStatusCode.Unauthorized, read.StatusCode);
        using var change = await game.PutAsJsonAsync("/api/account/avatar", new { avatar = "rohan" });
        Assert.Equal(HttpStatusCode.Unauthorized, change.StatusCode);
        using var signOut = await game.DeleteAsync("/api/account/session");
        Assert.Equal(HttpStatusCode.Unauthorized, signOut.StatusCode);
        using var stillPlaying = await game.GetAsync("/api/rooms");
        Assert.Equal(HttpStatusCode.OK, stillPlaying.StatusCode);
    }

    [PostgresFact]
    public async Task AnAccountSessionReachesNoGameRoute()
    {
        using var host = Host();
        var account = await MenuAccounts.Register(host);
        var token = (await SignIn(host, account)).Token;
        using var page = Bearer(host, token);
        foreach (var path in new[] { "/api/rooms", "/api/lobby", "/api/matches", "/api/auth/me", "/api/ranked/queue" })
        {
            using var answer = await page.GetAsync(path);
            Assert.Equal(HttpStatusCode.Unauthorized, answer.StatusCode);
        }
        using var logout = await page.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, logout.StatusCode);
        Assert.Null(await host.Services.GetRequiredService<SessionStore>().Resolve(token));
    }

    [PostgresFact]
    public async Task ARunningClientLearnsItsOwnAvatarOnItsNextRead()
    {
        using var host = Host();
        var account = await MenuAccounts.Register(host);
        using var game = MenuAccounts.Client(host, account);
        Assert.Equal(account.Avatar, (await Read<SessionView>(game, "/api/auth/me")).Avatar);

        await Choose(host, account, "elves");

        var me = await Read<SessionView>(game, "/api/auth/me");
        Assert.Equal(("elves", account.Username, ""), (me.Avatar, me.Username, me.Token));
    }

    [PostgresFact]
    public async Task SigningInNamesTheCurrentAvatar()
    {
        using var host = Host();
        var account = await MenuAccounts.Register(host);
        await Choose(host, account, "sauron");
        using var client = Anonymous(host);
        using var answer = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Username = account.Username, Password = Password });
        Assert.Equal("sauron", (await answer.Content.ReadFromJsonAsync<SessionView>(BoardCatalog.Json))!.Avatar);
    }

    // The trap AccountRename's copies would have set: a version bump un-readies a player who has already accepted.
    [PostgresFact]
    public async Task AChangeShowsInAnOpenRoomWithoutTouchingItsVersionOrReadiness()
    {
        using var host = Host();
        var (owner, guest, watcher) = (await MenuAccounts.Register(host), await MenuAccounts.Register(host), await MenuAccounts.Register(host));
        var rooms = Rooms(host);
        var room = await rooms.Create(owner.AccountId, Side.FreePeoples);
        Assert.Equal((owner.Avatar, owner.Avatar, ""), (room.HostAvatar, room.FreePeoplesAvatar, room.ShadowAvatar));
        await rooms.Join(room.JoinCode, guest.AccountId);
        var watching = (await rooms.Join(room.JoinCode, watcher.AccountId, RoomRole.Observer)).Room!;
        Assert.Equal(watcher.Avatar, Assert.Single(watching.Observers).Avatar);
        var accepted = (await rooms.Confirm(room.Id, owner.AccountId, watching.Version)).Room!;
        Assert.True(accepted.FreePeoplesAccepted);

        await Choose(host, guest, "dwarves");
        await Choose(host, watcher, "rohan");
        await Choose(host, owner, "gondor");

        var after = (await rooms.Read(room.Id, owner.AccountId))!;
        Assert.Equal(("gondor", "gondor", "dwarves"), (after.HostAvatar, after.FreePeoplesAvatar, after.ShadowAvatar));
        Assert.Equal("rohan", Assert.Single(after.Observers).Avatar);
        Assert.Equal(accepted.Version, after.Version);
        Assert.True(after.FreePeoplesAccepted);
        Assert.Contains(await rooms.List(guest.AccountId), r => r.Id == room.Id && r.ShadowAvatar == "dwarves");
    }

    [PostgresFact]
    public async Task TheOpenGamesListTheOnlineListAndChallengesShowTheChange()
    {
        using var host = Host();
        var (first, second) = (await MenuAccounts.Register(host), await MenuAccounts.Register(host));
        using var firstClient = MenuAccounts.Client(host, first);
        using var secondClient = MenuAccounts.Client(host, second);
        await Choose(host, first, "north");
        await Choose(host, second, "southrons");
        var request = new CreateRoomRequest { Name = "An open game", Visibility = RoomVisibility.Public, Side = Side.Shadow };
        var open = await Rooms(host).Create(first.AccountId, request);
        Assert.Contains((await Rooms(host).ListPublic(second.AccountId, "", 50)).Rooms, r => r.Id == open.Id && r.HostAvatar == "north");

        await Read<LobbyView>(firstClient, "/api/lobby");
        using var sent = await firstClient.PostAsJsonAsync("/api/lobby/challenges", new ChallengeRequest { Name = second.Username }, BoardCatalog.Json);
        await Read<LobbyView>(secondClient, "/api/lobby");
        using var challenge = await firstClient.PostAsJsonAsync("/api/lobby/challenges", new ChallengeRequest { Name = second.Username }, BoardCatalog.Json);

        var lobby = await Read<LobbyView>(secondClient, "/api/lobby");
        Assert.Contains(lobby.Online, player => player.Name == first.Username && player.Avatar == "north");
        Assert.Contains(lobby.Online, player => player is { You: true, Avatar: "southrons" });
        var incoming = Assert.Single(lobby.Incoming);
        Assert.Equal(("north", "southrons"), (incoming.FromAvatar, incoming.ToAvatar));

        await Choose(host, first, "isengard");
        Assert.Equal("isengard", Assert.Single((await Read<LobbyView>(secondClient, "/api/lobby")).Incoming).FromAvatar);
    }

    [PostgresFact]
    public async Task MyGamesShowTheOpponentsCurrentAvatar()
    {
        using var host = Host();
        var (first, second) = (await MenuAccounts.Register(host), await MenuAccounts.Register(host));
        var rooms = Rooms(host);
        var room = await rooms.Create(first.AccountId, Side.FreePeoples);
        await rooms.Join(room.JoinCode, second.AccountId);
        var version = (await rooms.Read(room.Id, first.AccountId))!.Version;
        RoomView? ready = null;
        foreach (var seat in new[] { first.AccountId, second.AccountId })
            ready = (await rooms.Confirm(room.Id, seat, version)).Room!;
        Assert.Equal("", (await rooms.Start(room.Id, first.AccountId, ready!.Version)).Error);
        using var client = MenuAccounts.Client(host, first);

        Assert.Equal(second.Avatar, Assert.Single((await Read<MatchListView>(client, "/api/matches")).Matches).OpponentAvatar);

        await Choose(host, second, "elves");

        Assert.Equal("elves", Assert.Single((await Read<MatchListView>(client, "/api/matches")).Matches).OpponentAvatar);
    }

    [PostgresFact]
    public async Task ARankedSearchShowsOnesOwnAvatarAndThePairingTheOpponents()
    {
        using var host = Host();
        var (first, second) = (await MenuAccounts.Register(host), await MenuAccounts.Register(host));
        await Choose(host, first, "gondor");
        await Choose(host, second, "sauron");
        using var firstClient = MenuAccounts.Client(host, first);
        using var secondClient = MenuAccounts.Client(host, second);
        Assert.Equal("gondor", (await Read<RankedSearchView>(firstClient, "/api/ranked/queue")).Avatar);
        foreach (var client in new[] { firstClient, secondClient })
        {
            using var search = await client.PostAsJsonAsync("/api/ranked/queue", new RankedSearchRequest(), BoardCatalog.Json);
            search.EnsureSuccessStatusCode();
        }

        Assert.Equal(1, await host.Services.GetRequiredService<RankedQueue>().PairOnce());

        var seen = await Read<RankedSearchView>(firstClient, "/api/ranked/queue");
        Assert.Equal((RankedSearchStatus.Matched, "gondor", "sauron"), (seen.Status, seen.Avatar, seen.OpponentAvatar));
    }

    [PostgresFact]
    public async Task ChatLinesKeepTheAvatarTheyWereSentWithAndTheNextOneHasTheNewOne()
    {
        using var host = Host();
        var author = await MenuAccounts.Register(host);
        await Choose(host, author, "gondor");
        using var client = MenuAccounts.Client(host, author);
        var room = await Rooms(host).Create(author.AccountId, Side.FreePeoples);
        var roomChat = $"/api/rooms/{room.Id}/chat";
        await Say(client, roomChat, new RoomChatRequest { Text = "before" });
        var opening = await Say(client, "/api/lobby/messages", new LobbyMessageRequest { Text = "opening" });
        await Say(client, "/api/lobby/messages", new LobbyMessageRequest { Text = "before" });

        await Choose(host, author, "north");
        await Say(client, roomChat, new RoomChatRequest { Text = "after" });
        await Say(client, "/api/lobby/messages", new LobbyMessageRequest { Text = "after" });

        var lines = (await Read<RoomChatView>(client, roomChat)).Messages;
        Assert.Equal(new[] { "gondor", "north" }, lines.Select(line => line.AuthorAvatar));
        var sequence = JsonDocument.Parse(opening).RootElement.GetProperty("sequence").GetInt64();
        var lobby = (await Read<LobbyView>(client, $"/api/lobby?after={sequence}")).Messages;
        Assert.Equal(new[] { "gondor", "north" }, lobby.Select(line => line.AuthorAvatar));
    }

    private static async Task<string> Say<T>(HttpClient client, string path, T request)
    {
        using var answer = await client.PostAsJsonAsync(path, request, BoardCatalog.Json);
        answer.EnsureSuccessStatusCode();
        return await answer.Content.ReadAsStringAsync();
    }

    [PostgresFact]
    public async Task AChangeRacingARenameLeavesBothInPlace()
    {
        using var host = Host();
        var account = await MenuAccounts.Register(host);
        using var game = MenuAccounts.Client(host, account);
        using var page = Bearer(host, (await SignIn(host, account)).Token);
        var name = "renamed" + Guid.NewGuid().ToString("N")[..12];

        var rename = game.PostAsJsonAsync("/api/account/name", new LoginRequest { Username = name, Password = Password });
        var avatar = page.PutAsJsonAsync("/api/account/avatar", new { avatar = "isengard" });
        await Task.WhenAll(rename, avatar);

        (await rename).EnsureSuccessStatusCode();
        (await avatar).EnsureSuccessStatusCode();
        var view = await Read<AccountView>(page, "/api/account");
        Assert.Equal((name, "isengard"), (view.Name, view.Avatar));
    }

    // Migration 022 on a schema of its own, stopped just before it: every account there gets the Eye or the Ring.
    [PostgresFact]
    public async Task TheMigrationGivesExistingAccountsBothDefaultsAndNothingElse()
    {
        await using var admin = NpgsqlDataSource.Create(database.Connection);
        var schema = "avatars_" + Guid.NewGuid().ToString("N");
        await using (var create = admin.CreateCommand("CREATE SCHEMA " + schema))
            await create.ExecuteNonQueryAsync();
        try
        {
            var settings = new NpgsqlConnectionStringBuilder(database.Connection) { SearchPath = schema };
            await using var data = NpgsqlDataSource.Create(settings.ConnectionString);
            await using var connection = await data.OpenConnectionAsync();
            await ApplyMigrations(connection, version => version < 22);
            for (var index = 0; index < 60; index++)
            {
                await using var insert = Database.Command(connection, null,
                    "INSERT INTO accounts(id,username,normalized_name,password_hash,is_admin) VALUES ($1,$2,$3,'legacy',false)",
                    "legacy" + index, "legacy_" + index, "LEGACY_" + index);
                await insert.ExecuteNonQueryAsync();
            }

            await ApplyMigrations(connection, version => version == 22);

            await using var read = connection.CreateCommand();
            read.CommandText = "SELECT string_agg(DISTINCT avatar, ',' ORDER BY avatar) FROM accounts";
            Assert.Equal("eye,ring", await read.ExecuteScalarAsync());
            await using var session = Database.Command(connection, null,
                "INSERT INTO sessions(token_hash,account_id,expires_at,purpose) VALUES ('hash','legacy0',now(),'account')");
            Assert.Equal(1, await session.ExecuteNonQueryAsync());
        }
        finally
        {
            await using var drop = admin.CreateCommand("DROP SCHEMA " + schema + " CASCADE");
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task ApplyMigrations(NpgsqlConnection connection, Func<int, bool> wanted)
    {
        var assembly = typeof(Database).Assembly;
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.EndsWith(".sql", StringComparison.Ordinal)).Order(StringComparer.Ordinal))
        {
            var version = int.Parse(name.Split('.')[^2].Split('_')[0], System.Globalization.CultureInfo.InvariantCulture);
            if (!wanted(version))
                continue;
            using var stream = assembly.GetManifestResourceStream(name)!;
            await using var ddl = new NpgsqlCommand(await new StreamReader(stream).ReadToEndAsync(), connection);
            await ddl.ExecuteNonQueryAsync();
        }
    }
}
