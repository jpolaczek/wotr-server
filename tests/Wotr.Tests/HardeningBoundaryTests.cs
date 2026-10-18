using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class HardeningBoundaryTests
{
    [PostgresFact]
    public async Task AuthenticationLimitReturnsBoundedRetryAfterWithoutLoggingCredentials()
    {
        using var logs = new OperationalLogs();
        await using var source = new AccountHost();
        await using var host = source.WithWebHostBuilder(b => b.ConfigureLogging(l => l.AddProvider(logs)));
        using var client = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var secret = "credential-sentinel-" + Guid.NewGuid().ToString("N");
        // A new name per attempt: this is the per-address limit, not the per-name lock (SEC-02, SignInLockTests).
        var prefix = "missing-" + Guid.NewGuid().ToString("N")[..12] + "-";
        LoginRequest Attempt(int attempt) => new() { Username = prefix + attempt, Password = secret };
        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var rejected = await client.PostAsJsonAsync("/api/auth/login", Attempt(attempt));
            Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        }
        using var limited = await client.PostAsJsonAsync("/api/auth/login", Attempt(20));
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.InRange(limited.Headers.RetryAfter!.Delta!.Value.TotalSeconds, 1, 60);
        Assert.Equal("TooManyRequests", (await limited.Content.ReadFromJsonAsync<JsonObject>())!["error"]!.GetValue<string>());
        using var health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.DoesNotContain(secret, logs.Text);
        Assert.DoesNotContain(prefix, logs.Text);
    }

    [PostgresFact]
    public async Task BrokenAndFutureSavesFailClosedWithoutExposingTheirPrivateContents()
    {
        using var logs = new OperationalLogs();
        await using var source = new TestHost();
        await using var host = source.WithWebHostBuilder(b => b.ConfigureLogging(l => l.AddProvider(logs)));
        using var player = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var outsider = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        player.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "alice");
        outsider.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "mallory");
        var database = host.Services.GetRequiredService<Database>();
        await using var connection = await database.Source.OpenConnectionAsync();
        var secret = "save-secret-" + Guid.NewGuid().ToString("N");
        foreach (var problem in new[] { "future", "missing-header", "null-players", "scalar" })
        {
            var state = KernelTests.NewGame();
            state.Id = Guid.NewGuid().ToString("N");
            state.Player(Side.Shadow).Hand.Add(secret);
            var raw = Broken(state, problem, secret);
            await using var insert = Database.Command(connection, null,
                "INSERT INTO matches(id,fp_account,sh_account,revision,state) VALUES ($1,'alice','bob',0,$2::jsonb)", state.Id, raw);
            await insert.ExecuteNonQueryAsync();
            using var denied = await outsider.GetAsync($"/api/matches/{state.Id}");
            Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
            using var response = await player.GetAsync($"/api/matches/{state.Id}");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(secret, body);
            var expected = problem == "future" ? "UnsupportedSaveVersion" : "CorruptSave";
            Assert.Equal(expected, JsonNode.Parse(body)!["error"]!.GetValue<string>());
        }
        Assert.DoesNotContain(secret, logs.Text);
        Assert.Contains("CorruptSave", logs.Text);
        Assert.Contains("UnsupportedSaveVersion", logs.Text);
    }

    private static string Broken(MatchState state, string problem, string secret)
    {
        var json = JsonNode.Parse(StateCodec.Write(state))!.AsObject();
        switch (problem)
        {
            case "future": json["schemaVersion"] = 999; break;
            case "missing-header": json.Remove("schemaVersion"); break;
            case "null-players": json["players"] = null; break;
            case "scalar": return System.Text.Json.JsonSerializer.Serialize(secret);
        }
        return json.ToJsonString();
    }
}
