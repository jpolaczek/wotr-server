using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;
using Xunit;
using static Wotr.Tests.CorrespondenceFixture;

namespace Wotr.Tests;

public class CorrespondencePrivacyTests
{
    [PostgresFact]
    public async Task HiddenStateAndOpponentsReadPositionNeverChangeTheOtherAccountsPayloads()
    {
        await using var server = await Create();
        var state = KingsFixture.Attempt();
        state.Id = Guid.NewGuid().ToString("N");
        state.Events.Add(new() { Revision = state.Revision, Kind = "Private", OnlyFor = Side.Shadow, Text = "private-shadow-event" });
        await server.Save(state);
        var indexBefore = await server.Fp.GetStringAsync("/api/matches");
        var activityBefore = await server.Fp.GetStringAsync($"/api/matches/{state.Id}/activity");
        Assert.DoesNotContain("private-shadow-event", activityBefore);
        Assert.Contains("private-shadow-event", await server.Sh.GetStringAsync($"/api/matches/{state.Id}/activity"));
        state.Player(Side.Shadow).Hand.Add("private-hand-marker");
        state.Player(Side.Shadow).Decks["strategy"].Reverse();
        state.Player(Side.Shadow).AutoDecision.Version++;
        state.Pending!.PrivateChoices.Add("private-choice-marker");
        state.Pending.SourceId = "private-source-marker";
        state.Events.Add(new() { Revision = state.Revision, Kind = "Private", OnlyFor = Side.Shadow, Text = "private-later-event" });
        await Replace(server, state);
        await Seen(server.Sh, state.Id, state.Revision);
        Assert.Equal(indexBefore, await server.Fp.GetStringAsync("/api/matches"));
        Assert.Equal(activityBefore, await server.Fp.GetStringAsync($"/api/matches/{state.Id}/activity"));
        var summary = Assert.Single((await Read<MatchListView>(server.Fp, "/api/matches")).Matches);
        Assert.Null(summary.YourDecision);
        Assert.Equal(Side.Shadow, summary.DecisionOwner);
        foreach (var field in new[] { "hand", "decks", "privateChoices", "sourceId", "randomSamples", "updatedAt", "autoDecision", "huntTiles" })
            Assert.DoesNotContain('"' + field + '"', indexBefore);

        // The same guarantee applies in the other direction during private Council setup.
        var council = CouncilSetupTests.Setup();
        council.Id = Guid.NewGuid().ToString("N");
        await server.Save(council);
        var shadowBefore = await server.Sh.GetStringAsync("/api/matches");
        council.Player(Side.FreePeoples).PrivateCompanionVersions.Add("private-council-marker");
        council.Pending!.PrivateChoices.Add("private-version-choice");
        council.Events.Add(new() { Revision = council.Revision, Kind = "Council", OnlyFor = Side.FreePeoples, Text = "private-council-event" });
        await Replace(server, council);
        await Seen(server.Fp, council.Id, council.Revision);
        Assert.Equal(shadowBefore, await server.Sh.GetStringAsync("/api/matches"));
        Assert.DoesNotContain("private-council", await server.Sh.GetStringAsync($"/api/matches/{council.Id}/activity"));
    }

    [PostgresFact]
    public async Task ActivityFiltersBeforeLimitingAndReadDoesNotAcknowledgeOrAdvanceTheGame()
    {
        await using var server = await Create();
        var state = CouncilSetupTests.Setup();
        state.Id = Guid.NewGuid().ToString("N");
        state.Revision = 80;
        state.Events.Clear();
        for (var revision = 1; revision <= 80; revision++)
            state.Events.Add(new() { Revision = revision, Kind = "Public", Text = "public-" + revision });
        for (var index = 0; index < 100; index++)
            state.Events.Add(new() { Revision = 80, Kind = "Private", OnlyFor = Side.Shadow, Text = "secret-" + index });
        await server.Save(state);
        var fp = await Read<MatchActivityView>(server.Fp, $"/api/matches/{state.Id}/activity");
        var sh = await Read<MatchActivityView>(server.Sh, $"/api/matches/{state.Id}/activity");
        Assert.True(fp.HasEarlierEntries);
        Assert.True(sh.HasEarlierEntries);
        Assert.Equal(50, fp.Entries.Count);
        Assert.Equal("public-31", fp.Entries[0].Text);
        Assert.Equal("public-80", fp.Entries[^1].Text);
        Assert.All(sh.Entries, entry => Assert.StartsWith("secret-", entry.Text));
        await Seen(server.Fp, state.Id, 78);
        var after = await Read<MatchActivityView>(server.Fp, $"/api/matches/{state.Id}/activity");
        Assert.False(after.HasEarlierEntries);
        Assert.Equal(new[] { "public-79", "public-80" }, after.Entries.Select(e => e.Text));
        Assert.Equal(78, after.AfterRevision);
        Assert.Equal(78, Assert.Single((await Read<MatchListView>(server.Fp, "/api/matches")).Matches).LastSeenRevision);
        Assert.Empty((await Read<MatchActivityView>(server.Fp, $"/api/matches/{state.Id}/activity?afterRevision=80")).Entries);
        SameState(state, (await server.Matches.Read(state.Id, server.FreePeoples.AccountId))!.State);
        var history = new Projection().For(state, Side.FreePeoples).History;
        Assert.Equal(JsonSerializer.Serialize(history), JsonSerializer.Serialize(fp.Entries));
    }

    private static async Task Replace(CorrespondenceFixture server, MatchState state)
    {
        await using var connection = await server.Database.Source.OpenConnectionAsync();
        await using var command = Database.Command(connection, null,
            "UPDATE matches SET state=$2::jsonb, updated_at=now() WHERE id=$1", state.Id, StateCodec.Write(state));
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
    }
}
