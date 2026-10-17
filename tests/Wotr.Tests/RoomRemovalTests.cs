using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// MENU-24 (WR01): the host removes the guest of a waiting room; the guest learns it (410) and cannot come back;
// nobody else can remove anyone, and a started or half-empty room refuses without effect.
public sealed class RoomRemovalTests
{
    [PostgresFact]
    public async Task TheHostRemovesTheGuestWhoCannotComeBack()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var (hostClient, guestClient, room) = await Table(host, RoomVisibility.Public);
        var before = (await hostClient.GetFromJsonAsync<RoomView>(Room(room.Id), BoardCatalog.Json))!;

        using var removed = await hostClient.PostAsync(Room(room.Id) + "/remove", null);

        removed.EnsureSuccessStatusCode();
        var after = (await removed.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!;
        Assert.Equal("", after.Shadow);
        Assert.Equal(before.Version + 1, after.Version);
        Assert.False(after.FreePeoplesAccepted || after.ShadowAccepted);
        using var gone = await guestClient.GetAsync(Room(room.Id));
        Assert.Equal(HttpStatusCode.Gone, gone.StatusCode);
        Assert.Equal("RemovedFromRoom", (await gone.Content.ReadFromJsonAsync<ApiError>(BoardCatalog.Json))!.Error);
        using var publicJoin = await guestClient.PostAsync(Room(room.Id) + "/join", null);
        Assert.Equal(HttpStatusCode.Conflict, publicJoin.StatusCode);
        Assert.Equal("RemovedFromRoom", (await publicJoin.Content.ReadFromJsonAsync<ApiError>(BoardCatalog.Json))!.Error);
        using var invited = await hostClient.PostAsync(Room(room.Id) + "/invitation", null);
        var code = (await invited.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!.JoinCode;
        Assert.NotEqual(room.JoinCode, code);
        using var byCode = await guestClient.PostAsJsonAsync("/api/rooms/join", new JoinRoomRequest { Code = code }, BoardCatalog.Json);
        Assert.Equal(HttpStatusCode.Conflict, byCode.StatusCode);
    }

    [PostgresFact]
    public async Task OnlyTheHostOfAWaitingRoomWithAGuestRemovesAnyone()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var rooms = host.Services.GetRequiredService<RoomStore>();
        var (hostClient, guestClient, room) = await Table(host, RoomVisibility.Private);
        var outsider = MenuAccounts.Client(host, await MenuAccounts.Register(host));

        Assert.Equal("HostRequired", await Refusal(guestClient, room.Id, HttpStatusCode.Conflict));
        using (var foreign = await outsider.PostAsync(Room(room.Id) + "/remove", null))
            Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);

        var hostSession = await MenuAccounts.Register(host);
        using var lonely = MenuAccounts.Client(host, hostSession);
        var empty = await rooms.Create(hostSession.AccountId, Side.FreePeoples);
        Assert.Equal("SeatEmpty", await Refusal(lonely, empty.Id, HttpStatusCode.Conflict));

        var current = (await hostClient.GetFromJsonAsync<RoomView>(Room(room.Id), BoardCatalog.Json))!;
        await Ready(hostClient, room.Id, current.Version);
        await Ready(guestClient, room.Id, current.Version);
        using (var started = await hostClient.PostAsJsonAsync(Room(room.Id) + "/start", new RoomRevisionRequest { ExpectedVersion = current.Version }, BoardCatalog.Json))
            started.EnsureSuccessStatusCode();
        Assert.Equal("MatchAlreadyStarted", await Refusal(hostClient, room.Id, HttpStatusCode.Conflict));
        var unchanged = (await guestClient.GetFromJsonAsync<RoomView>(Room(room.Id), BoardCatalog.Json))!;
        Assert.NotEqual("", unchanged.Shadow);
    }

    private static async Task<(HttpClient Host, HttpClient Guest, RoomView Room)> Table(AccountHost host, RoomVisibility visibility)
    {
        var hostSession = await MenuAccounts.Register(host);
        var guestSession = await MenuAccounts.Register(host);
        var hostClient = MenuAccounts.Client(host, hostSession);
        var guestClient = MenuAccounts.Client(host, guestSession);
        using var created = await hostClient.PostAsJsonAsync("/api/rooms",
            new CreateRoomRequest { Side = Side.FreePeoples, Name = "Removal table", Visibility = visibility }, BoardCatalog.Json);
        var room = (await created.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!;
        if (room.JoinCode.Length == 0)
        {
            using var invited = await hostClient.PostAsync(Room(room.Id) + "/invitation", null);
            room = (await invited.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!;
        }
        using var joined = await guestClient.PostAsJsonAsync("/api/rooms/join", new JoinRoomRequest { Code = room.JoinCode }, BoardCatalog.Json);
        joined.EnsureSuccessStatusCode();
        return (hostClient, guestClient, room);
    }

    private static async Task Ready(HttpClient client, string room, int version)
    {
        using var ready = await client.PutAsJsonAsync(Room(room) + "/ready", new RoomReadyRequest { ExpectedVersion = version, Ready = true }, BoardCatalog.Json);
        ready.EnsureSuccessStatusCode();
    }

    private static async Task<string> Refusal(HttpClient client, string room, HttpStatusCode status)
    {
        using var answer = await client.PostAsync(Room(room) + "/remove", null);
        Assert.Equal(status, answer.StatusCode);
        return (await answer.Content.ReadFromJsonAsync<ApiError>(BoardCatalog.Json))!.Error;
    }

    private static string Room(string id) => "/api/rooms/" + id;
}
