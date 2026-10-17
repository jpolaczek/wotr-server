using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class ExpansionLobbyTests
{
    [PostgresFact]
    public async Task All93AcceptedConfigurationsStartTheirActualSetupAndRemainImmutableThroughHttp()
    {
        await using var host = new AccountHost(extra: TestBudgets.Stress);
        using var fp = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var sh = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(host, fp);
        await Login(host, sh);
        var count = 0;
        foreach (var flags in Enumerable.Range(0, 512).Where(ExpansionConfigurations.Allowed))
        {
            var configuration = ExpansionConfigurations.FromFlags(flags);
            var room = await Prepare(fp, sh, configuration);
            var version = new RoomRevisionRequest { ExpectedVersion = room.Version };
            var started = await Post<RoomView>(fp, $"api/rooms/{room.Id}/start", version);
            Assert.NotEmpty(started.MatchId);
            var again = await Post<RoomView>(sh, $"api/rooms/{room.Id}/start", version);
            Assert.Equal(started.MatchId, again.MatchId);
            var fpView = await fp.GetFromJsonAsync<PlayerView>($"api/matches/{started.MatchId}", BoardCatalog.Json);
            var shView = await sh.GetFromJsonAsync<PlayerView>($"api/matches/{started.MatchId}", BoardCatalog.Json);
            CheckSetup(configuration, fpView!, shView!);
            var changed = await fp.PutAsJsonAsync($"api/rooms/{room.Id}/configuration", new ConfigureRoomRequest
            {
                ExpectedVersion = room.Version, Configuration = new()
            }, BoardCatalog.Json);
            await Error(changed, "MatchAlreadyStarted");
            var saved = await sh.GetFromJsonAsync<RoomView>($"api/rooms/{room.Id}", BoardCatalog.Json);
            Assert.Equal(Json(configuration), Json(saved!.Configuration));
            count++;
        }
        Assert.Equal(93, count);
    }

    private static async Task<RoomView> Prepare(HttpClient fp, HttpClient sh, MatchConfiguration configuration)
    {
        var room = await Post<RoomView>(fp, "api/rooms", new CreateRoomRequest { Side = Side.FreePeoples });
        await Post<RoomView>(sh, "api/rooms/join", new JoinRoomRequest { Code = room.JoinCode });
        var original = new RoomRevisionRequest { ExpectedVersion = room.Version };
        await Post<RoomView>(fp, $"api/rooms/{room.Id}/confirm", original);
        await Post<RoomView>(sh, $"api/rooms/{room.Id}/confirm", original);
        var response = await fp.PutAsJsonAsync($"api/rooms/{room.Id}/configuration", new ConfigureRoomRequest
        {
            ExpectedVersion = room.Version, Configuration = configuration
        }, BoardCatalog.Json);
        response.EnsureSuccessStatusCode();
        room = (await response.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!;
        Assert.False(room.FreePeoplesAccepted);
        Assert.False(room.ShadowAccepted);
        Assert.Empty(room.StartUnavailableReason);
        await Error(await fp.PostAsJsonAsync($"api/rooms/{room.Id}/start", original, BoardCatalog.Json), "StaleConfiguration");
        var current = new RoomRevisionRequest { ExpectedVersion = room.Version };
        await Post<RoomView>(fp, $"api/rooms/{room.Id}/confirm", current);
        await Error(await fp.PostAsJsonAsync($"api/rooms/{room.Id}/start", current, BoardCatalog.Json), "ConfirmationRequired");
        await Post<RoomView>(sh, $"api/rooms/{room.Id}/confirm", current);
        return room;
    }

    private static void CheckSetup(MatchConfiguration configuration, PlayerView fp, PlayerView sh)
    {
        Assert.Equal(Json(configuration), Json(fp.Configuration));
        Assert.Equal(Json(configuration), Json(sh.Configuration));
        Assert.Equal(Side.FreePeoples, fp.You);
        Assert.Equal(Side.Shadow, sh.You);
        Assert.Equal(configuration.Lords ? DecisionKind.CouncilVersions : DecisionKind.FellowshipPhase, fp.Decision!.Kind);
        Assert.Null(sh.Decision);
        Assert.Empty(sh.Options);
        Assert.Empty(sh.OwnCompanions);
        Assert.Empty(sh.OwnCompanionVersions);
        Assert.Equal(configuration.Kings ? configuration.Fate ? 3 : 5 : 0, sh.Sovereigns.Count);
        Assert.Equal(configuration.Fate, sh.RegionOverrides.Count > 0);
        Assert.Equal(configuration.Lords, fp.Rings.All(r => r.Id.StartsWith("lords.")));
        Assert.All(fp.Hand, card => Assert.DoesNotContain(card, sh.Hand));
    }

    private static async Task Login(AccountHost host, HttpClient client)
    {
        var username = "t34-" + Guid.NewGuid().ToString("N")[..20];
        var session = await Post<SessionView>(client, "api/auth/register", new RegisterRequest
        {
            Username = username, Email = TestAccounts.Email(username), Password = "Expansion acceptance password!"
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
    }

    private static async Task<T> Post<T>(HttpClient client, string path, object body)
    {
        using var response = await client.PostAsJsonAsync(path, body, BoardCatalog.Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(BoardCatalog.Json))!;
    }

    private static async Task Error(HttpResponseMessage response, string expected)
    {
        using (response)
        {
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(expected, (await response.Content.ReadFromJsonAsync<ApiError>(BoardCatalog.Json))!.Error);
        }
    }

    private static string Json(MatchConfiguration configuration) => JsonSerializer.Serialize(configuration, BoardCatalog.Json);
}
