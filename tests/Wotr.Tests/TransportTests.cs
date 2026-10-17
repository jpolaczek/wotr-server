using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public sealed class TestHost(IRandomSource? random = null, EffectCatalog? effects = null, IReadOnlyDictionary<string, string?>? settings = null,
    TimeProvider? clock = null)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        var values = new Dictionary<string, string?>(settings ?? new Dictionary<string, string?>()) { ["WOTR_DB"] = PostgresTests.Connection };
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(values));
        builder.ConfigureServices(services => { services.RemoveAll<ISessionResolver>(); services.AddSingleton<ISessionResolver, TestSessions>(); });
        builder.ConfigureServices(services =>
        {
            if (random is not null)
            {
                services.RemoveAll<IRandomSource>();
                services.AddSingleton(random);
            }
            if (effects is not null)
            {
                services.RemoveAll<EffectCatalog>();
                services.AddSingleton(effects);
            }
            if (clock is not null)
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(clock);
            }
        });
    }
    private sealed class TestSessions : ISessionResolver
    {
        public Task<string?> Resolve(string token, CancellationToken cancellation) => Task.FromResult<string?>(token is "alice" or "bob" or "mallory" ? token : null);
    }
    public HttpClient Client(string? token = null)
    {
        var client = CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });
        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
public class TransportTests
{
    [PostgresFact]
    public async Task AuthorizedHttpAndWebSocketReturnOnlyRoleViewAndRevision()
    {
        await using var host = new TestHost();
        using var alice = host.Client("alice");
        var store = host.Services.GetRequiredService<MatchStore>();
        var state = KernelTests.NewGame();
        state.Id = Guid.NewGuid().ToString("N");
        state.Player(Side.Shadow).Hand.Add("never-on-wire");
        await store.Create(state, "alice", "bob");
        var response = await alice.GetAsync($"/api/matches/{state.Id}");
        response.EnsureSuccessStatusCode();
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("never-on-wire", raw);
        var view = await response.Content.ReadFromJsonAsync<PlayerView>(BoardCatalog.Json);
        Assert.Equal(Side.FreePeoples, view!.You);
        using var anonymous = host.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/matches/{state.Id}")).StatusCode);
        using var mallory = host.Client("mallory");
        Assert.Equal(HttpStatusCode.NotFound, (await mallory.GetAsync($"/api/matches/{state.Id}")).StatusCode);
        var ws = host.Server.CreateWebSocketClient();
        ws.ConfigureRequest = r => r.Headers.Authorization = "Bearer alice";
        using var socket = await ws.ConnectAsync(new Uri($"wss://localhost/api/matches/{state.Id}/updates"), CancellationToken.None);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var buffer = new byte[1024];
        var message = await socket.ReceiveAsync(buffer, timeout.Token);
        var notice = Encoding.UTF8.GetString(buffer, 0, message.Count);
        Assert.Contains(state.Id, notice);
        Assert.DoesNotContain("never-on-wire", notice);
        Assert.DoesNotContain("hand", notice);
        socket.Abort();
    }
}
