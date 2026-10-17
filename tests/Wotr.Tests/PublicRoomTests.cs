using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Server;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class PublicRoomTests
{
    [PostgresFact]
    public async Task PublicListIsAuthenticatedBoundedAndContainsNoPrivateRoomOrInvitation()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var alice = await MenuAccounts.Register(host);
        var bob = await MenuAccounts.Register(host);
        using var client = MenuAccounts.Client(host, bob);
        var store = host.Services.GetRequiredService<RoomStore>();
        var publicRoom = await store.Create(alice.AccountId, new CreateRoomRequest() { Name = "A public game", Visibility = RoomVisibility.Public, Side = Side.Shadow });
        var privateRoom = await store.Create(alice.AccountId, new CreateRoomRequest() { Name = "A private game", Visibility = RoomVisibility.Private });
        var ownRoom = await store.Create(bob.AccountId, new CreateRoomRequest() { Name = "My public game", Visibility = RoomVisibility.Public });
        using var anonymous = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/rooms/public")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/rooms/public?limit=0")).StatusCode);
        var seen = new HashSet<string>();
        var cursor = "";
        do
        {
            using var response = await client.GetAsync("/api/rooms/public?limit=1&cursor=" + cursor);
            var json = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("joinCode", json);
            Assert.DoesNotContain(privateRoom.Id, json);
            Assert.DoesNotContain(ownRoom.Id, json);
            var page = (await response.Content.ReadFromJsonAsync<PublicRoomListView>(BoardCatalog.Json))!;
            Assert.InRange(page.Rooms.Count, 0, 1);
            foreach (var room in page.Rooms)
                Assert.True(seen.Add(room.Id));
            cursor = page.NextCursor;
        } while (cursor.Length > 0);
        Assert.Contains(publicRoom.Id, seen);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/rooms/" + privateRoom.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/rooms/" + privateRoom.Id + "/join", null)).StatusCode);
        using var joined = await client.PostAsync("/api/rooms/" + publicRoom.Id + "/join", null);
        Assert.Equal(HttpStatusCode.OK, joined.StatusCode);
        var view = (await joined.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!;
        Assert.Equal(Side.FreePeoples, view.You);
        Assert.False(view.IsHost);
        Assert.Empty(view.JoinCode);
        Assert.Equal("A public game", view.Name);
        Assert.Equal(alice.Username, view.Host);
        Assert.DoesNotContain((await store.ListPublic(alice.AccountId, "", 50)).Rooms, r => r.Id == publicRoom.Id);
    }

    [PostgresFact]
    public async Task CompetingPublicJoinsTakeExactlyOneSeatAndRetryDoesNotTakeAnother()
    {
        using var host = new AccountHost();
        var alice = await MenuAccounts.Register(host);
        var bob = await MenuAccounts.Register(host);
        var charlie = await MenuAccounts.Register(host);
        using var first = MenuAccounts.Client(host, bob);
        using var second = MenuAccounts.Client(host, charlie);
        var store = host.Services.GetRequiredService<RoomStore>();
        var room = await store.Create(alice.AccountId, new CreateRoomRequest() { Name = "Race to join", Visibility = RoomVisibility.Public });
        var route = "/api/rooms/" + room.Id + "/join";
        var responses = await Task.WhenAll(first.PostAsync(route, null), second.PostAsync(route, null));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        var winner = responses[0].IsSuccessStatusCode ? first : second;
        var before = (await winner.GetFromJsonAsync<RoomView>("/api/rooms/" + room.Id, BoardCatalog.Json))!;
        using var retry = await winner.PostAsync(route, null);
        var after = (await retry.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!;
        Assert.Equal(before.Version, after.Version);
        Assert.Equal(before.Shadow, after.Shadow);
        foreach (var response in responses)
            response.Dispose();
    }
}

internal static class MenuAccounts
{
    public static Task<SessionView> Register(AccountHost host) =>
        TestAccounts.Register(host.Services.GetRequiredService<AccountService>(), "menu_" + Guid.NewGuid().ToString("N")[..20],
            "menu test password long enough");

    public static HttpClient Client(AccountHost host, SessionView session)
    {
        var client = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        return client;
    }
}
