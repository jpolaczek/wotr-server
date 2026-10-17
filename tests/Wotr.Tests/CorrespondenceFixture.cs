using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

internal sealed class CorrespondenceFixture : IAsyncDisposable
{
    private AccountHost host;
    public SessionView FreePeoples { get; }
    public SessionView Shadow { get; }
    public SessionView Outsider { get; }
    public HttpClient Fp { get; private set; }
    public HttpClient Sh { get; private set; }
    public HttpClient Other { get; private set; }
    public MatchStore Matches => host.Services.GetRequiredService<MatchStore>();
    public Database Database => host.Services.GetRequiredService<Database>();

    private CorrespondenceFixture(AccountHost host, SessionView fp, SessionView sh, SessionView outsider)
    {
        this.host = host;
        FreePeoples = fp;
        Shadow = sh;
        Outsider = outsider;
        Fp = Client(fp);
        Sh = Client(sh);
        Other = Client(outsider);
    }

    public static async Task<CorrespondenceFixture> Create()
    {
        var host = new AccountHost(extra: TestBudgets.Stress);
        var accounts = host.Services.GetRequiredService<AccountService>();
        return new(host, await Register(accounts), await Register(accounts), await Register(accounts));
    }

    public async Task Restart()
    {
        await DisposeAsync();
        host = new AccountHost(extra: TestBudgets.Stress);
        Fp = Client(FreePeoples);
        Sh = Client(Shadow);
        Other = Client(Outsider);
    }

    public Task Save(MatchState state, bool reverse = false) => Matches.Create(state,
        reverse ? Shadow.AccountId : FreePeoples.AccountId,
        reverse ? FreePeoples.AccountId : Shadow.AccountId);

    public static async Task<T> Read<T>(HttpClient client, string path) =>
        (await client.GetFromJsonAsync<T>(path, BoardCatalog.Json))!;

    public static void SameState(MatchState expected, MatchState actual) =>
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(StateCodec.Write(expected)), JsonNode.Parse(StateCodec.Write(actual))),
            "Persisted state must be unchanged, including pending decisions, costs and random samples.");

    public static async Task<CommandReceipt> Send(HttpClient client, GameCommand command)
    {
        using var response = await client.PostAsJsonAsync($"/api/matches/{command.MatchId}/commands", command, BoardCatalog.Json);
        response.EnsureSuccessStatusCode();
        var receipt = (await response.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
        Assert.True(receipt.Accepted, receipt.Error);
        return receipt;
    }

    public static async Task<SeenRevisionView> Seen(HttpClient client, string id, long revision)
    {
        using var response = await client.PutAsJsonAsync($"/api/matches/{id}/seen", new SeenRevisionRequest { Revision = revision }, BoardCatalog.Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SeenRevisionView>(BoardCatalog.Json))!;
    }

    private HttpClient Client(SessionView session)
    {
        var client = Anonymous();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        return client;
    }

    public HttpClient Anonymous() => host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

    private static async Task<SessionView> Register(AccountService accounts)
    {
        return await TestAccounts.Register(accounts, "u" + Guid.NewGuid().ToString("N")[..20], "A long correspondence test password!");
    }

    public async ValueTask DisposeAsync()
    {
        Fp.Dispose();
        Sh.Dispose();
        Other.Dispose();
        await host.DisposeAsync();
    }
}
