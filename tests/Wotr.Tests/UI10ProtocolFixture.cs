using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

internal static class UI10ProtocolFixture
{
    public static JsonObject Json(GameCommand command) =>
        JsonSerializer.SerializeToNode(command, BoardCatalog.Json)!.AsObject();

    public static async Task<MatchStore> Save(TestHost host, MatchState state)
    {
        state.Id = Guid.NewGuid().ToString("N");
        var store = host.Services.GetRequiredService<MatchStore>();
        await store.Create(state, "alice", "bob");
        return store;
    }

    public static Task<HttpResponseMessage> Post(HttpClient client, string match, string endpoint, JsonObject command) =>
        client.PostAsync($"/api/matches/{match}/{endpoint}",
            new StringContent(command.ToJsonString(), Encoding.UTF8, "application/json"));

    public static async Task<T> Response<T>(HttpResponseMessage response)
    {
        using (response)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<T>(BoardCatalog.Json))!;
        }
    }

    public static async Task RejectedWithoutMutation(TestHost host, HttpClient client, MatchState state,
        JsonObject command, string error = "IllegalChoice")
    {
        var store = host.Services.GetRequiredService<MatchStore>();
        var before = StateCodec.Write((await store.Read(state.Id, "alice"))!.State);
        var preview = await Response<ActionPreview>(await Post(client, state.Id, "preview", command));
        Assert.False(preview.Legal);
        Assert.Equal(error, preview.Error);
        Assert.Empty(preview.Costs);
        Assert.Empty(preview.Placements);
        var receiptPath = $"/api/matches/{state.Id}/receipts/{command["commandId"]!.GetValue<string>()}";
        using (var missing = await client.GetAsync(receiptPath))
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var receipt = await Response<CommandReceipt>(await Post(client, state.Id, "commands", command));
        Assert.False(receipt.Accepted);
        Assert.Equal(error, receipt.Error);
        Assert.Equal(state.Revision, receipt.Revision);
        Assert.Equal(before, StateCodec.Write((await store.Read(state.Id, "alice"))!.State));
        // QUAL-03: malformed input is answered without a stored receipt; a domain rejection keeps its receipt.
        if (error == CommandShape.MalformedCommand)
        {
            using var none = await client.GetAsync(receiptPath);
            Assert.Equal(HttpStatusCode.NotFound, none.StatusCode);
            return;
        }
        var stored = await Response<CommandReceipt>(await client.GetAsync(receiptPath));
        Assert.Equal(JsonSerializer.Serialize(receipt), JsonSerializer.Serialize(stored));
    }
}
