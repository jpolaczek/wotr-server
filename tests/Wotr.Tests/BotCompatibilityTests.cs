using System.Net;
using System.Net.WebSockets;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

[Collection(BotApiCollection.Name)]
public class BotCompatibilityTests(BotApiFixture fixture)
{
    [PostgresFact]
    public async Task AuthenticatedBotVersionIsIndependentAndHeadersCannotExemptHumans()
    {
        using var releases = new ReleaseFixture();
        releases.Publish("9.0.0", minimum: true);
        var settings = fixture.Settings();
        foreach (var setting in releases.ServerSettings())
            settings[setting.Key] = setting.Value;
        await using var server = await KestrelProcess.Start(fixture.Connection, settings);
        var seat = await fixture.Seat();
        using var bot = server.Client();
        BotApiFixture.Authorize(bot, seat.Session.Token);
        var path = $"/api/matches/{seat.State.Id}";
        Assert.Equal(HttpStatusCode.OK, (await bot.GetAsync(path)).StatusCode);

        using var human = server.Client();
        BotApiFixture.Authorize(human, seat.Human.Token, bot: false);
        foreach (var header in BotApiFixture.Headers().Where(h => h.Key != ClientCompatibility.ProtocolVersionHeader))
            human.DefaultRequestHeaders.Add(header.Key, header.Value);
        using var refused = await human.GetAsync(path);
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Contains(ClientCompatibility.ClientUpdateRequired, await refused.Content.ReadAsStringAsync());
        human.DefaultRequestHeaders.Remove(ClientCompatibility.ClientVersionHeader);
        Assert.Equal(HttpStatusCode.BadRequest, (await human.GetAsync(path)).StatusCode);
        human.DefaultRequestHeaders.Add(ClientCompatibility.ClientVersionHeader, "9.0.0");
        Assert.Equal(HttpStatusCode.OK, (await human.GetAsync(path)).StatusCode);

        using var unknown = server.Client();
        BotApiFixture.Authorize(unknown, BotTokens.SeatPrefix + SessionStore.NewToken());
        Assert.Equal(HttpStatusCode.Unauthorized, (await unknown.GetAsync(path)).StatusCode);
        unknown.DefaultRequestHeaders.Remove(BotCompatibility.ProtocolHeader);
        Assert.Equal(HttpStatusCode.Unauthorized, (await unknown.GetAsync(path)).StatusCode);
        using var unauthenticated = server.Client();
        foreach (var header in BotApiFixture.Headers())
            unauthenticated.DefaultRequestHeaders.Add(header.Key, header.Value);
        Assert.Equal(HttpStatusCode.BadRequest, (await unauthenticated.GetAsync(path)).StatusCode);
    }

    [PostgresFact]
    public async Task BotHeadersAreMandatorySingleValuedAndCheckedBeforeSocketUpgrade()
    {
        var seat = await fixture.Seat();
        var path = $"/api/matches/{seat.State.Id}";
        (string Header, string? Value)[] bad =
        [
            (BotCompatibility.ApplicationHeader, null), (BotCompatibility.ApplicationHeader, "garbage"),
            (BotCompatibility.ApplicationHeader, "0.0.9"), (BotCompatibility.ProtocolHeader, "2"),
            (ClientCompatibility.ProtocolVersionHeader, "1"), (ClientCompatibility.ProtocolVersionHeader, "3"),
            (BotCompatibility.RulesHeader, "other-rules"), (BotCompatibility.ContentHeader, "other-content")
        ];
        foreach (var (header, value) in bad)
        {
            using var bot = fixture.Client(seat.Session.Token);
            bot.DefaultRequestHeaders.Remove(header);
            if (value is not null)
                bot.DefaultRequestHeaders.Add(header, value);
            using var response = await bot.GetAsync(path);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
            Assert.Contains("BotUpdateRequired", await response.Content.ReadAsStringAsync());
        }
        foreach (var header in BotApiFixture.Headers())
        {
            using var duplicate = fixture.Client(seat.Session.Token);
            duplicate.DefaultRequestHeaders.Add(header.Key, header.Value);
            Assert.Equal(HttpStatusCode.Conflict, (await duplicate.GetAsync(path)).StatusCode);
        }
        await Assert.ThrowsAsync<WebSocketException>(() => fixture.Socket(seat.Session.Token, seat.State.Id,
            overrides: new Dictionary<string, string> { [BotCompatibility.RulesHeader] = "other-rules" }));
    }

    [PostgresFact]
    public async Task TrustedFeaturesGateExpansionAccessIndependentlyOfUnityModuleMinimum()
    {
        var state = MatchFactory.Create("fixture", new SequenceRandom(new int[300]), new() { Fate = true });
        var seat = await fixture.Seat(state);
        var path = $"/api/matches/{seat.State.Id}";
        using var unsupported = fixture.Client(seat.Session.Token);
        unsupported.DefaultRequestHeaders.Add("X-Wotr-Bot-Features", "base,fate");
        using var refusal = await unsupported.GetAsync(path);
        Assert.Equal(HttpStatusCode.Conflict, refusal.StatusCode);
        Assert.Contains("BotConfigurationUnsupported", await refusal.Content.ReadAsStringAsync());
        await Assert.ThrowsAsync<WebSocketException>(() => fixture.Socket(seat.Session.Token, seat.State.Id));

        var settings = fixture.Settings();
        settings["Wotr:Bots:Features"] = "base,fate";
        settings[ReleaseScope.ModulesKey] = "fate";
        settings[ModuleClientPolicy.Key] = "fate=9.0.0";
        await using var server = await KestrelProcess.Start(fixture.Connection, settings);
        using var bot = server.Client();
        BotApiFixture.Authorize(bot, seat.Session.Token);
        Assert.Equal(HttpStatusCode.OK, (await bot.GetAsync(path)).StatusCode);
        using var human = server.Client();
        BotApiFixture.Authorize(human, seat.Human.Token, bot: false);
        Assert.Equal(HttpStatusCode.Conflict, (await human.GetAsync(path)).StatusCode);
        human.DefaultRequestHeaders.Remove(ClientCompatibility.ClientVersionHeader);
        human.DefaultRequestHeaders.Add(ClientCompatibility.ClientVersionHeader, "9.0.0");
        await BotPlayerApiParityTests.EqualResponses(bot.GetAsync(path), human.GetAsync(path));
    }

    [PostgresFact]
    public async Task DisabledServiceAndRotatedControlIdentityCannotAuthenticate()
    {
        var seat = await fixture.Seat();
        var settings = fixture.Settings();
        settings["Wotr:Bots:Enabled"] = "false";
        await using (var disabled = await KestrelProcess.Start(fixture.Connection, settings))
        {
            foreach (var token in new[] { seat.Session.Token, fixture.ControlToken })
            {
                using var client = disabled.Client();
                BotApiFixture.Authorize(client, token);
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/runtime")).StatusCode);
            }
        }
        settings["Wotr:Bots:Enabled"] = "true";
        settings["Wotr:Bots:ControlTokenHash"] = SessionStore.Hash(BotTokens.ControlPrefix + SessionStore.NewToken());
        await using var rotated = await KestrelProcess.Start(fixture.Connection, settings);
        using var control = rotated.Client();
        BotApiFixture.Authorize(control, fixture.ControlToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await control.GetAsync("/api/runtime")).StatusCode);
    }

    [PostgresFact]
    public async Task HumanTokensThatCoincidentallyBeginWithBotPrefixesRemainHumanSessions()
    {
        foreach (var prefix in new[] { BotTokens.ControlPrefix, BotTokens.SeatPrefix })
        {
            var seat = await fixture.Seat();
            var humanToken = prefix + SessionStore.NewToken()[prefix.Length..];
            Assert.Equal(43, humanToken.Length);
            await fixture.Sql("UPDATE sessions SET token_hash=$2 WHERE token_hash=$1",
                SessionStore.Hash(seat.Human.Token), SessionStore.Hash(humanToken));
            using var human = fixture.Client(humanToken, bot: false);
            Assert.Equal(HttpStatusCode.OK, (await human.GetAsync($"/api/matches/{seat.State.Id}")).StatusCode);
            using var stream = await fixture.Socket(humanToken, seat.State.Id, bot: false);
            Assert.Equal(WebSocketMessageType.Text, (await BotApiFixture.Receive(stream)).Type);
            stream.Abort();
        }
    }

}
