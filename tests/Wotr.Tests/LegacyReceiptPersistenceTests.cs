using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
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

public class LegacyReceiptPersistenceTests
{
    [PostgresFact]
    public async Task HistoricalCommittedMoveRetriesAcrossRestartAndTwoDevicesWithoutSpendingOrRollingAgain()
    {
        var id = Guid.NewGuid().ToString("N");
        var rawCommand = Replace(LegacyContractFixture.Read("command.json"), id);
        var command = JsonSerializer.Deserialize<GameCommand>(rawCommand, BoardCatalog.Json)!;
        var after = Replace(LegacyContractFixture.Read("after.json"), id);
        var expected = StateCodec.Read(after);
        await using (var host = new TestHost())
            await Import(host, id, after, rawCommand, command);

        var random = new SequenceRandom();
        await using var restarted = new TestHost(random);
        using var firstDevice = restarted.Client("alice");
        using var secondDevice = restarted.Client("alice");
        var path = $"/api/matches/{id}/commands";
        var results = await Task.WhenAll(firstDevice.PostAsJsonAsync(path, command, BoardCatalog.Json),
            secondDevice.PostAsJsonAsync(path, command, BoardCatalog.Json));
        foreach (var response in results)
        {
            using (response)
            {
                response.EnsureSuccessStatusCode();
                var receipt = (await response.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
                Assert.True(receipt.Accepted, receipt.Error);
                Assert.Equal(expected.Revision, receipt.Revision);
            }
        }
        command.Payload.CouncilTokenId = "forged-extra-cost";
        using var conflict = await firstDevice.PostAsJsonAsync(path, command, BoardCatalog.Json);
        Assert.Equal("CommandIdConflict", (await conflict.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!.Error);
        var saved = (await restarted.Services.GetRequiredService<MatchStore>().Read(id, "alice"))!.State;
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(StateCodec.Write(expected)), JsonNode.Parse(StateCodec.Write(saved))));
        Assert.Equal(0, random.Calls);
        using var opponent = restarted.Client("bob");
        using var hidden = await opponent.GetAsync($"/api/matches/{id}/receipts/{command.CommandId}");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
    }

    private static async Task Import(TestHost host, string id, string state, string rawCommand, GameCommand command)
    {
        var database = host.Services.GetRequiredService<Database>();
        await using var connection = await database.Source.OpenConnectionAsync();
        await using var insert = Database.Command(connection, null,
            "INSERT INTO matches(id,fp_account,sh_account,revision,state) VALUES ($1,'alice','bob',$2,$3::jsonb)",
            id, StateCodec.Read(state).Revision, state);
        await insert.ExecuteNonQueryAsync();
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawCommand)));
        await using var receipt = Database.Command(connection, null,
            "INSERT INTO command_receipts(match_id,account_id,command_id,fingerprint,receipt) VALUES ($1,'alice',$2,$3,$4::jsonb)",
            id, command.CommandId, fingerprint, LegacyContractFixture.Read("receipt.json"));
        await receipt.ExecuteNonQueryAsync();
    }

    private static string Replace(string json, string id) => json.Replace("legacy-base-move", id, StringComparison.Ordinal);
}
