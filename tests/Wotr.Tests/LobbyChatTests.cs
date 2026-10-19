using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// LOBBY-01: the Open games screen's chat, the players online there and challenges between them. Other test classes
// share the database, so every assertion names this test's own accounts and messages.
public class LobbyChatTests
{
    private sealed record Player(SessionView Session, HttpClient Client) : IDisposable
    {
        public string Name => Session.Username;
        public void Dispose() => Client.Dispose();
    }

    private static async Task<Player> Join(AccountHost host)
    {
        var session = await MenuAccounts.Register(host);
        return new Player(session, MenuAccounts.Client(host, session));
    }

    private static async Task<LobbyView> Poll(Player player, long after = 0) =>
        (await player.Client.GetFromJsonAsync<LobbyView>("/api/lobby?after=" + after, BoardCatalog.Json))!;

    private static Task<HttpResponseMessage> Say(Player player, string text) =>
        player.Client.PostAsJsonAsync("/api/lobby/messages", new LobbyMessageRequest { Text = text }, BoardCatalog.Json);

    private static Task<HttpResponseMessage> Challenge(Player player, string name) =>
        player.Client.PostAsJsonAsync("/api/lobby/challenges", new ChallengeRequest { Name = name }, BoardCatalog.Json);

    private static Task<HttpResponseMessage> Answer(Player player, string id, string action) =>
        player.Client.PostAsync($"/api/lobby/challenges/{id}/{action}", null);

    private static async Task<string> Error(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ApiError>(BoardCatalog.Json))!.Error;

    private static async Task<ChallengeView> Sent(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ChallengeView>(BoardCatalog.Json))!;
    }

    private static async Task Execute(string sql, string? value = null)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand(sql);
        if (value is not null)
            command.Parameters.Add(new NpgsqlParameter { Value = value });
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> Count(string sql, string value)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand(sql);
        command.Parameters.Add(new NpgsqlParameter { Value = value });
        return (long)(await command.ExecuteScalarAsync())!;
    }

    [PostgresFact]
    public async Task APollMarksThePlayerOnlineAndAStalePlayerDropsOut()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        using var alice = await Join(host);
        using var bob = await Join(host);

        await Poll(bob);
        var seen = await Poll(alice);

        Assert.Contains(seen.Online, p => p.Name == alice.Name && p.You);
        Assert.Contains(seen.Online, p => p.Name == bob.Name && !p.You);
        await Execute("UPDATE lobby_presence SET seen_at = now() - interval '1 minute' WHERE account_id = $1", bob.Session.AccountId);
        Assert.DoesNotContain((await Poll(alice)).Online, p => p.Name == bob.Name);
        using var anonymous = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/lobby")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/lobby/messages",
            new LobbyMessageRequest { Text = "hi" }, BoardCatalog.Json)).StatusCode);
    }

    [PostgresFact]
    public async Task TheChatKeepsTrimmedMessagesUnderTheirAuthorAndRefusesBadText()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        using var alice = await Join(host);
        using var bob = await Join(host);
        var before = (await Poll(bob)).Latest;

        using var posted = await Say(alice, "   Anyone for a game tonight?  ");

        Assert.Equal(HttpStatusCode.OK, posted.StatusCode);
        var message = (await posted.Content.ReadFromJsonAsync<LobbyMessage>(BoardCatalog.Json))!;
        Assert.Equal(("Anyone for a game tonight?", alice.Name), (message.Text, message.Author));
        var read = await Poll(bob, before);
        Assert.Contains(read.Messages, m => m.Sequence == message.Sequence && m.Author == alice.Name);
        Assert.True(read.Latest >= message.Sequence);
        Assert.DoesNotContain((await Poll(bob, message.Sequence)).Messages, m => m.Sequence <= message.Sequence);
        foreach (var bad in new[] { "", "    ", new string('x', LobbyLimits.LongestMessage + 1), "two\nlines" })
        {
            using var refused = await Say(alice, bad);
            Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
            Assert.Equal("InvalidMessage", await Error(refused));
        }
    }

    // LOBBY-03: messages are relayed in memory for a minute (at most MessagesKept), never stored; a first read gets no
    // history, only the sequence to go on from.
    [PostgresFact]
    public async Task TheChatIsRelayedBrieflyInMemoryAndNeverStored()
    {
        var clock = new TestClock(DateTimeOffset.UtcNow);
        using var host = new AccountHost(extra: TestBudgets.Stress, services: services => services.AddSingleton<TimeProvider>(clock));
        var alice = await MenuAccounts.Register(host);
        var chat = host.Services.GetRequiredService<LobbyChat>();
        var start = chat.Read(0).Latest;
        LobbyMessage? last = null;
        for (var i = 0; i < LobbyLimits.MessagesKept + 5; i++)
            last = await chat.Post(alice.AccountId, "message " + i);

        Assert.Equal(LobbyLimits.MessagesKept, chat.Relayed);
        var first = chat.Read(0);
        Assert.Empty(first.Messages);
        Assert.Equal(last!.Sequence, first.Latest);
        var recent = chat.Read(last.Sequence - 3).Messages;
        Assert.Equal(new[] { "message " + (LobbyLimits.MessagesKept + 2), "message " + (LobbyLimits.MessagesKept + 3),
            "message " + (LobbyLimits.MessagesKept + 4) }, recent.Select(m => m.Text));
        Assert.Equal(LobbyLimits.MessagesKept, chat.Read(start).Messages.Count);
        Assert.Empty(chat.Read(last.Sequence + 1000).Messages);

        clock.Advance(LobbyChat.RelayWindow);
        Assert.Empty(chat.Read(start).Messages);
        Assert.Equal(0, chat.Relayed);
        Assert.Equal(0, await Count("SELECT count(*) FROM information_schema.tables WHERE table_name = $1", "lobby_messages"));
    }

    [PostgresFact]
    public async Task AChallengeGoesOnlyToAnotherOnlinePlayerOnceAndAFewAtATime()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        using var alice = await Join(host);
        using var bob = await Join(host);
        using var carol = await Join(host);
        using var dave = await Join(host);
        using var erin = await Join(host);
        using var frank = await Join(host);
        foreach (var player in new[] { alice, bob, dave, erin, frank })
            await Poll(player);

        using (var unknown = await Challenge(alice, "nobody_" + Guid.NewGuid().ToString("N")[..8]))
        {
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
            Assert.Equal("PlayerNotFound", await Error(unknown));
        }
        foreach (var (target, reason) in new[] { (alice.Name, "SelfChallenge"), (carol.Name, "PlayerOffline") })
        {
            using var refused = await Challenge(alice, target);
            Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
            Assert.Equal(reason, await Error(refused));
        }
        var challenge = await Sent(await Challenge(alice, bob.Name.ToUpperInvariant()));
        Assert.Equal((alice.Name, bob.Name, ChallengeStatus.Pending), (challenge.From, challenge.To, challenge.Status));
        foreach (var (from, to) in new[] { (alice, bob.Name), (bob, alice.Name) })
        {
            using var again = await Challenge(from, to);
            Assert.Equal("AlreadyChallenged", await Error(again));
        }
        await Sent(await Challenge(alice, dave.Name));
        await Sent(await Challenge(alice, erin.Name));
        using (var tooMany = await Challenge(alice, frank.Name))
            Assert.Equal("TooManyChallenges", await Error(tooMany));
        Assert.Contains((await Poll(bob)).Incoming, c => c.Id == challenge.Id && c.From == alice.Name);
        Assert.Contains((await Poll(alice)).Outgoing, c => c.Id == challenge.Id && c.Status == ChallengeStatus.Pending);
    }

    [PostgresFact]
    public async Task AcceptingSetsThePrivateTableOfTheChallengerWithBothSeated()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        using var alice = await Join(host);
        using var bob = await Join(host);
        await Poll(alice);
        await Poll(bob);
        var challenge = await Sent(await Challenge(alice, bob.Name));

        using (var foreign = await Answer(alice, challenge.Id, "accept"))
            Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        using var accepted = await Answer(bob, challenge.Id, "accept");

        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        var room = (await accepted.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!;
        Assert.Equal((alice.Name, alice.Name, bob.Name), (room.Host, room.FreePeoples, room.Shadow));
        Assert.Equal((RoomVisibility.Private, Side.Shadow, false), (room.Visibility, room.You, room.IsHost));
        Assert.Equal(ChallengeAcceptance.TableName(alice.Name, bob.Name), room.Name);
        using (var twice = await Answer(bob, challenge.Id, "accept"))
        {
            Assert.Equal(HttpStatusCode.Conflict, twice.StatusCode);
            Assert.Equal("ChallengeClosed", await Error(twice));
        }
        var outgoing = (await Poll(alice)).Outgoing.Single(c => c.Id == challenge.Id);
        Assert.Equal((ChallengeStatus.Accepted, room.Id), (outgoing.Status, outgoing.RoomId));
        Assert.DoesNotContain((await Poll(bob)).Incoming, c => c.Id == challenge.Id);
        var rooms = host.Services.GetRequiredService<RoomStore>();
        var hostView = (await rooms.Read(room.Id, alice.Session.AccountId))!;
        Assert.True(hostView.IsHost);
        Assert.Equal(bob.Name, hostView.Shadow);
        // EXP-01: the table is an ordinary named room, so its host may choose the released modules (Testing: all).
        var fate = await rooms.Configure(room.Id, alice.Session.AccountId,
            new() { ExpectedVersion = hostView.Version, Configuration = new() { Fate = true, NewCities = true } });
        Assert.Empty(fate.Error);
        Assert.True(fate.Room!.Configuration.Fate);
    }

    // The seat cannot be taken (a trigger refuses this one player's seating): the half-set table goes and the challenge
    // can be accepted again once the fault is gone.
    [PostgresFact]
    public async Task AFailedAcceptanceLeavesNoTableAndGivesTheChallengeBack()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        using var alice = await Join(host);
        using var bob = await Join(host);
        await Poll(alice);
        await Poll(bob);
        var challenge = await Sent(await Challenge(alice, bob.Name));
        var fault = "lobby_test_" + Guid.NewGuid().ToString("N");
        await Execute($"CREATE FUNCTION {fault}() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'test fault'; END $$");
        try
        {
            await Execute($"CREATE TRIGGER {fault} BEFORE UPDATE ON rooms FOR EACH ROW " +
                $"WHEN (NEW.sh_account = '{bob.Session.AccountId}') EXECUTE FUNCTION {fault}()");
            var acceptance = host.Services.GetRequiredService<ChallengeAcceptance>();

            await Assert.ThrowsAsync<PostgresException>(() => acceptance.Accept(bob.Session.AccountId, challenge.Id));

            Assert.Equal(0L, await Count("SELECT count(*) FROM rooms WHERE fp_account = $1", alice.Session.AccountId));
            Assert.Contains((await Poll(bob)).Incoming, c => c.Id == challenge.Id && c.Status == ChallengeStatus.Pending);
        }
        finally
        {
            await Execute($"DROP TRIGGER IF EXISTS {fault} ON rooms");
            await Execute($"DROP FUNCTION {fault}()");
        }
        using var accepted = await Answer(bob, challenge.Id, "accept");
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
    }

    [PostgresFact]
    public async Task DeclineCancelAndExpiryCloseAChallenge()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        using var alice = await Join(host);
        using var bob = await Join(host);
        await Poll(alice);
        await Poll(bob);

        var declined = await Sent(await Challenge(alice, bob.Name));
        using (var decline = await Answer(bob, declined.Id, "decline"))
            Assert.Equal(HttpStatusCode.NoContent, decline.StatusCode);
        using (var again = await Answer(bob, declined.Id, "decline"))
            Assert.Equal("ChallengeClosed", await Error(again));
        Assert.Equal(ChallengeStatus.Declined, (await Poll(alice)).Outgoing.Single(c => c.Id == declined.Id).Status);

        var cancelled = await Sent(await Challenge(alice, bob.Name));
        using (var foreign = await Answer(bob, cancelled.Id, "cancel"))
            Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        using (var cancel = await Answer(alice, cancelled.Id, "cancel"))
            Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        Assert.DoesNotContain((await Poll(bob)).Incoming, c => c.Id == cancelled.Id);
        using (var late = await Answer(bob, cancelled.Id, "accept"))
            Assert.Equal("ChallengeClosed", await Error(late));

        var expired = await Sent(await Challenge(alice, bob.Name));
        await Execute("UPDATE lobby_challenges SET expires_at = now() - interval '1 second' WHERE id = $1", expired.Id);
        using (var tooLate = await Answer(bob, expired.Id, "accept"))
        {
            Assert.Equal(HttpStatusCode.Conflict, tooLate.StatusCode);
            Assert.Equal("ChallengeClosed", await Error(tooLate));
        }
        Assert.DoesNotContain((await Poll(bob)).Incoming, c => c.Id == expired.Id);
        Assert.Equal(ChallengeStatus.Expired, (await Poll(alice)).Outgoing.Single(c => c.Id == expired.Id).Status);
        await Sent(await Challenge(alice, bob.Name));
    }
}
