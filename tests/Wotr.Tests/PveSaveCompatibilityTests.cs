using System.Net;
using System.Net.Http.Json;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

[Collection(PveCollection.Name)]
public sealed class PveSaveCompatibilityTests(PveFixture fixture)
{
    [PostgresFact]
    public async Task CompletedPveExportsOrdinaryReplayToHumanAndObserverWithSeparateStatistics()
    {
        var human = await fixture.Human();
        var room = await fixture.Reserve(human, await fixture.Room(human));
        var cpu = await fixture.ReadyCpu(room);
        room = await fixture.Start(human, room);
        var watcher = await fixture.Human();
        Assert.Equal("", (await fixture.Rooms.JoinPublic(room.Id, watcher.AccountId, RoomRole.Observer)).Error);

        // Replace the fixture's initial position with a verified near-victory scenario and record its initial frame.
        // This tests export integration, not the strategic bot's ability to complete a game.
        var state = TestScenarioCatalog.Find("sh-military")!.Create(room.MatchId);
        await using (var connection = await fixture.Database.Source.OpenConnectionAsync())
        await using (var transaction = await connection.BeginTransactionAsync())
        {
            await using var setup = Database.Command(connection, transaction,
                "UPDATE matches SET state=$2::jsonb,revision=$3 WHERE id=$1", state.Id, StateCodec.Write(state), state.Revision);
            await setup.ExecuteNonQueryAsync();
            await using var frames = Database.Command(connection, transaction, "DELETE FROM match_frames WHERE match_id=$1", state.Id);
            await frames.ExecuteNonQueryAsync();
            await using var events = Database.Command(connection, transaction, "DELETE FROM match_events WHERE match_id=$1", state.Id);
            await events.ExecuteNonQueryAsync();
            await using var initial = Database.Command(connection, transaction,
                "INSERT INTO match_frames(match_id,step,revision,turn,phase,free_peoples,shadow) VALUES ($1,0,$2,$3,$4,$5::jsonb,$6::jsonb)",
                state.Id, state.Revision, state.Turn, state.Phase.ToString(),
                MatchFrames.View(state, Side.FreePeoples), MatchFrames.View(state, Side.Shadow));
            await initial.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        using var bot = fixture.BotClient(cpu.Session.Token);
        using var move = await bot.PostAsJsonAsync($"/api/matches/{room.MatchId}/commands", new GameCommand
        {
            MatchId = state.Id, ExpectedRevision = state.Revision, DecisionId = state.Pending!.Id,
            CommandId = Guid.NewGuid().ToString("N"), Kind = CommandKind.DiscardDie,
            Payload = new() { DieId = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Id }
        }, BoardCatalog.Json);
        Assert.True((await move.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!.Accepted);
        foreach (var (session, route) in new[] { (human, "matches"), (watcher, "observe") })
        {
            using var client = fixture.HumanClient(session);
            using var download = await client.GetAsync($"/api/{route}/{state.Id}/replay");
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            using var archive = new ZipArchive(new MemoryStream(await download.Content.ReadAsByteArrayAsync()));
            using var stream = archive.GetEntry(ReplayFormat.ManifestEntry)!.Open();
            var manifest = (await JsonSerializer.DeserializeAsync<ReplayManifest>(stream, BoardCatalog.Json))!;
            Assert.True(manifest.Complete);
            Assert.Equal(Side.Shadow, manifest.Winner);
            Assert.Contains(manifest.Players, p => p.Name == "CPU");
        }
        using var forbidden = await bot.GetAsync($"/api/matches/{state.Id}/replay");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(1L, await fixture.Scalar("SELECT count(*) FROM match_ledger WHERE match_id=$1 AND kind='pve' AND finished_at IS NOT NULL", state.Id));
    }

    [PostgresFact]
    public async Task ThreePveGamesCoexistWithHumanGameAndReplaceableLegacyDemo()
    {
        var human = await fixture.Human();
        var matches = new List<string>();
        for (var index = 0; index < 3; index++)
        {
            var reserved = await fixture.Reserve(human, await fixture.Room(human));
            await fixture.ReadyCpu(reserved);
            matches.Add((await fixture.Start(human, reserved)).MatchId);
        }
        var opponent = await fixture.Human();
        var room = await fixture.Room(human);
        var joined = (await fixture.Rooms.JoinPublic(room.Id, opponent.AccountId)).Room!;
        await fixture.Rooms.Confirm(room.Id, opponent.AccountId, joined.Version);
        var ordinary = await fixture.Start(human, joined);
        var demoRoom = await fixture.Room(human);
        var demos = fixture.Host.Services.GetRequiredService<DemoSessionStore>();
        var demo = await demos.Start(human.AccountId, demoRoom.Id);
        Assert.Equal("", demo.Error);
        var indexStore = fixture.Host.Services.GetRequiredService<MatchIndexStore>();
        var list = await indexStore.List(human.AccountId, MatchListFilter.All, "", 50);
        Assert.Equal(5, list.Matches.Count);
        Assert.Equal(3, list.Matches.Count(m => m.IsPve));
        Assert.All(list.Matches.Where(m => m.IsPve), m => Assert.Equal("shadow-strong", m.Bot!.ProfileId));
        Assert.False(list.Matches.Single(m => m.MatchId == ordinary.MatchId).IsPve);
        var replacement = await demos.Start(human.AccountId, demoRoom.Id);
        Assert.NotEqual(demo.Demo!.MatchId, replacement.Demo!.MatchId);
        list = await indexStore.List(human.AccountId, MatchListFilter.All, "", 50);
        Assert.Equal(5, list.Matches.Count);
        Assert.All(matches, id => Assert.Contains(list.Matches, m => m.MatchId == id));
        Assert.Equal(3L, await fixture.Scalar("SELECT count(*) FROM match_ledger WHERE fp_account=$1 AND kind='pve'", human.AccountId));
        Assert.Equal(1L, await fixture.Scalar("SELECT count(*) FROM match_ledger WHERE fp_account=$1 AND kind='game'", human.AccountId));
        var statistics = await fixture.Host.Services.GetRequiredService<AdminStatisticsStore>().Summary(default);
        Assert.True(statistics.PveGames >= 3);
    }

    [PostgresFact]
    public async Task StartedAssignmentSurvivesRestartAndUsesOrdinaryCommandsReceiptsAndPrivacy()
    {
        var human = await fixture.Human();
        var room = await fixture.Reserve(human, await fixture.Room(human));
        var cpu = await fixture.ReadyCpu(room);
        room = await fixture.Start(human, room);
        fixture.RestartHost();
        using var client = fixture.HumanClient(human);
        var view = (await client.GetFromJsonAsync<PlayerView>($"/api/matches/{room.MatchId}", BoardCatalog.Json))!;
        var command = new GameCommand
        {
            MatchId = view.MatchId, ExpectedRevision = view.Revision, DecisionId = view.Decision!.Id,
            CommandId = Guid.NewGuid().ToString("N"), Kind = CommandKind.ContinueFellowship
        };
        using var sent = await client.PostAsJsonAsync($"/api/matches/{room.MatchId}/commands", command, BoardCatalog.Json);
        Assert.True((await sent.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!.Accepted);
        using var bot = fixture.BotClient(cpu.Session.Token);
        var own = (await bot.GetFromJsonAsync<PlayerView>($"/api/matches/{room.MatchId}", BoardCatalog.Json))!;
        Assert.Equal(Side.Shadow, own.You);
        Assert.DoesNotContain(view.Hand[0], own.Hand);
        var allocation = own.Options.Single(o => o.Kind == CommandKind.AllocateHunt);
        var move = new GameCommand
        {
            MatchId = own.MatchId, ExpectedRevision = own.Revision, DecisionId = own.Decision!.Id,
            CommandId = Guid.NewGuid().ToString("N"), Kind = CommandKind.AllocateHunt,
            Payload = new() { Ids = allocation.EligibleIds.Take(allocation.Minimum).ToList() }
        };
        using var accepted = await bot.PostAsJsonAsync($"/api/matches/{room.MatchId}/commands", move, BoardCatalog.Json);
        var receipt = (await accepted.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
        Assert.True(receipt.Accepted, receipt.Error);
        using var retry = await bot.PostAsJsonAsync($"/api/matches/{room.MatchId}/commands", move, BoardCatalog.Json);
        Assert.Equal(receipt.Revision, (await retry.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!.Revision);
        using var observer = await bot.GetAsync($"/api/observe/{room.MatchId}");
        Assert.Equal(HttpStatusCode.Forbidden, observer.StatusCode);
        using var history = await bot.GetAsync($"/api/matches/{room.MatchId}/actions");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        Assert.Equal("started", await fixture.Scalar("SELECT status FROM bot_assignments WHERE id=$1", cpu.Grant));
        Assert.True((long)(await fixture.Scalar("SELECT count(*) FROM match_frames WHERE match_id=$1", room.MatchId))! >= 3);
        Assert.Equal("MatchAlreadyStarted", (await fixture.Rooms.AddBot(room.Id, human.AccountId, PveFixture.Request(room))).Error);
        Assert.True(await fixture.Host.Services.GetRequiredService<MatchRemoval>().Delete(room.MatchId, human.AccountId));
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM bot_assignments WHERE id=$1", cpu.Grant));
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM bot_seat_grants WHERE id=$1", cpu.Grant));
        Assert.Null(await fixture.Credentials.ResolveSeat(cpu.Session.Token, default));
        Assert.Equal("pve", await fixture.Scalar("SELECT kind FROM match_ledger WHERE match_id=$1", room.MatchId));
    }
}
