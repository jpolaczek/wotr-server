using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class MenuLifecycleTests
{
    [PostgresFact]
    public async Task PublicHostCanShareACodeWithoutExposingItInThePublicList()
    {
        using var host = new AccountHost();
        var alice = await MenuAccounts.Register(host);
        var bob = await MenuAccounts.Register(host);
        using var owner = MenuAccounts.Client(host, alice);
        using var guest = MenuAccounts.Client(host, bob);
        var store = host.Services.GetRequiredService<RoomStore>();
        var room = await store.Create(alice.AccountId, new CreateRoomRequest { Name = "Public with code", Visibility = RoomVisibility.Public });
        var route = "/api/rooms/" + room.Id;
        using var response = await owner.PostAsync(route + "/invitation", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var invitation = (await response.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!;
        Assert.True(RoomCode(invitation.JoinCode));
        // The shared test database holds other tests' public rooms: page until this one is listed.
        var publicJson = await PublicPageWith(guest, room.Id);
        Assert.Contains(room.Id, publicJson);
        Assert.DoesNotContain(invitation.JoinCode, publicJson);
        Assert.DoesNotContain("joinCode", publicJson);
        using var joined = await guest.PostAsJsonAsync("/api/rooms/join", new JoinRoomRequest { Code = invitation.JoinCode });
        Assert.Equal(HttpStatusCode.OK, joined.StatusCode);
        Assert.Empty((await joined.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!.JoinCode);
        Assert.Equal(HttpStatusCode.NoContent, (await guest.PostAsync(route + "/leave", null)).StatusCode);
        Assert.Null((await store.Join(invitation.JoinCode, bob.AccountId)).Room);
        Assert.NotEqual(invitation.JoinCode, (await store.Invitation(room.Id, alice.AccountId)).Room!.JoinCode);
    }

    [PostgresFact]
    public async Task PrivateInvitationIsHostOnlyStableAndInvalidatedByGuestLeave()
    {
        using var host = new AccountHost();
        var alice = await MenuAccounts.Register(host);
        var bob = await MenuAccounts.Register(host);
        using var owner = MenuAccounts.Client(host, alice);
        using var guest = MenuAccounts.Client(host, bob);
        var store = host.Services.GetRequiredService<RoomStore>();
        var room = await store.Create(alice.AccountId, new CreateRoomRequest { Name = "Secret game", Visibility = RoomVisibility.Private });
        Assert.Empty(room.JoinCode);
        var route = "/api/rooms/" + room.Id;
        using var invitationResponse = await owner.PostAsync(route + "/invitation", null);
        Assert.Equal(HttpStatusCode.OK, invitationResponse.StatusCode);
        var invitation = (await invitationResponse.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!;
        Assert.True(RoomCode(invitation.JoinCode));
        Assert.Equal(invitation.JoinCode, (await store.Invitation(room.Id, alice.AccountId)).Room!.JoinCode);
        using var join = await guest.PostAsJsonAsync("/api/rooms/join", new JoinRoomRequest { Code = invitation.JoinCode });
        var joined = (await join.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!;
        Assert.Empty(joined.JoinCode);
        Assert.Equal("HostRequired", (await store.Invitation(room.Id, bob.AccountId)).Error);
        Assert.NotNull((await store.Join(invitation.JoinCode, bob.AccountId)).Room);
        await store.Confirm(room.Id, alice.AccountId, joined.Version);
        await store.Confirm(room.Id, bob.AccountId, joined.Version);
        Assert.Equal(HttpStatusCode.NoContent, (await guest.PostAsync(route + "/leave", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await guest.PostAsync(route + "/leave", null)).StatusCode);
        var left = (await store.Read(room.Id, alice.AccountId))!;
        Assert.Empty(left.Shadow);
        Assert.False(left.FreePeoplesAccepted);
        Assert.False(left.ShadowAccepted);
        Assert.Empty(left.JoinCode);
        Assert.Null((await store.Join(invitation.JoinCode, bob.AccountId)).Room);
        var next = (await store.Invitation(room.Id, alice.AccountId)).Room!;
        Assert.NotEqual(invitation.JoinCode, next.JoinCode);
        Assert.NotNull((await store.Join(next.JoinCode, bob.AccountId)).Room);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsync(route + "/leave", null)).StatusCode);
        Assert.Equal(RoomStatus.Closed, (await store.Read(room.Id, bob.AccountId))!.Status);
        Assert.Empty(await store.List(alice.AccountId));
        Assert.Null((await store.Join(next.JoinCode, bob.AccountId)).Room);
    }

    [PostgresFact]
    public async Task ReadinessCanBeWithdrawnAndOnlyHostStartsExactlyOneNamedBaseGame()
    {
        using var host = new AccountHost();
        var alice = await MenuAccounts.Register(host);
        var bob = await MenuAccounts.Register(host);
        var store = host.Services.GetRequiredService<RoomStore>();
        var room = await store.Create(alice.AccountId, new CreateRoomRequest { Name = "Persistent game", Visibility = RoomVisibility.Public, Side = Side.Shadow });
        var joined = (await store.JoinPublic(room.Id, bob.AccountId)).Room!;
        Assert.Equal("ExpansionsUnavailable", (await store.Configure(room.Id, alice.AccountId, new()
        {
            ExpectedVersion = joined.Version, Configuration = new() { Lords = true }
        })).Error);
        await store.Confirm(room.Id, alice.AccountId, joined.Version);
        await store.Confirm(room.Id, bob.AccountId, joined.Version);
        using var guest = MenuAccounts.Client(host, bob);
        using var unready = await guest.PutAsJsonAsync("/api/rooms/" + room.Id + "/ready", new RoomReadyRequest
        {
            ExpectedVersion = joined.Version, Ready = false
        });
        Assert.Equal(HttpStatusCode.OK, unready.StatusCode);
        Assert.Equal("ConfirmationRequired", (await store.Start(room.Id, alice.AccountId, joined.Version)).Error);
        Assert.Equal("HostRequired", (await store.Start(room.Id, bob.AccountId, joined.Version)).Error);
        await store.Confirm(room.Id, bob.AccountId, joined.Version);
        var starts = await Task.WhenAll(store.Start(room.Id, alice.AccountId, joined.Version), store.Start(room.Id, alice.AccountId, joined.Version));
        Assert.NotEmpty(starts[0].Room!.MatchId);
        Assert.Equal(starts[0].Room!.MatchId, starts[1].Room!.MatchId);
        var matchId = starts[0].Room!.MatchId;
        var summary = Assert.Single((await host.Services.GetRequiredService<MatchIndexStore>().List(alice.AccountId, MatchListFilter.All, "", 20)).Matches);
        Assert.Equal("Persistent game", summary.Name);
        Assert.True(Wotr.Domain.ConfigurationRules.IsBase(summary.Configuration));
        await store.Leave(room.Id, alice.AccountId);
        await store.Leave(room.Id, bob.AccountId);
        Assert.Equal(matchId, (await store.Read(room.Id, bob.AccountId))!.MatchId);
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        var restarted = new RoomStore(new Database(data), new ServerRandom());
        Assert.Equal(matchId, (await restarted.Read(room.Id, bob.AccountId))!.MatchId);
        Assert.Equal(RoomStatus.Started, (await restarted.Read(room.Id, alice.AccountId))!.Status);
    }

    [PostgresFact]
    public async Task LeaveAndStartAreSerializedWithoutRemovingAnyStartedParticipant()
    {
        using var host = new AccountHost();
        var alice = await MenuAccounts.Register(host);
        var bob = await MenuAccounts.Register(host);
        var store = host.Services.GetRequiredService<RoomStore>();
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var room = await store.Create(alice.AccountId, new CreateRoomRequest { Name = "Race game", Visibility = RoomVisibility.Public });
            var joined = (await store.JoinPublic(room.Id, bob.AccountId)).Room!;
            await store.Confirm(room.Id, alice.AccountId, joined.Version);
            await store.Confirm(room.Id, bob.AccountId, joined.Version);
            var start = store.Start(room.Id, alice.AccountId, joined.Version);
            var leaving = store.Leave(room.Id, attempt % 2 == 0 ? bob.AccountId : alice.AccountId);
            await Task.WhenAll(start, leaving);
            var current = (await store.Read(room.Id, alice.AccountId))!;
            if (current.MatchId.Length > 0)
            {
                Assert.Equal(alice.Username, current.FreePeoples);
                Assert.Equal(bob.Username, current.Shadow);
                Assert.Equal(RoomStatus.Started, current.Status);
                Assert.NotNull((await store.Read(room.Id, bob.AccountId)));
            }
            else
            {
                Assert.NotEqual("", start.Result.Error);
                Assert.True(current.Status == RoomStatus.Closed || current.Shadow.Length == 0);
            }
        }
    }

    private static async Task<string> PublicPageWith(HttpClient client, string roomId)
    {
        var cursor = "";
        for (var page = 0; page < 200; page++)
        {
            var json = await client.GetStringAsync("/api/rooms/public?limit=50&cursor=" + Uri.EscapeDataString(cursor));
            if (json.Contains(roomId, StringComparison.Ordinal))
                return json;
            cursor = System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("nextCursor").GetString() ?? "";
            if (cursor.Length == 0)
                return json;
        }
        throw new InvalidOperationException("The public room list did not end.");
    }

    // A table's join code: 256 random bits, URL-safe Base64 without padding (the account invitations' old format).
    private static bool RoomCode(string code) =>
        code.Length == 43 && code.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
}
