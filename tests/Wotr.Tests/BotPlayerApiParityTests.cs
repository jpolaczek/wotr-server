using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

[Collection(BotApiCollection.Name)]
public class BotPlayerApiParityTests(BotApiFixture fixture)
{
    [PostgresTheory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public async Task ProjectionOptionsPreviewsCommandsReceiptsHistoryAndErrorsAreIdentical(Side side)
    {
        var seat = await fixture.MovingSeat(side);
        using var bot = fixture.Client(seat.Session.Token);
        using var human = fixture.Client(seat.Human.Token, bot: false);
        var path = $"/api/matches/{seat.State.Id}";
        await EqualResponses(bot.GetAsync(path), human.GetAsync(path));
        var command = BotApiFixture.Move(seat);
        await EqualResponses(bot.PostAsJsonAsync(path + "/preview", command, BoardCatalog.Json),
            human.PostAsJsonAsync(path + "/preview", command, BoardCatalog.Json));

        var illegal = Clone(command);
        illegal.CommandId = Guid.NewGuid().ToString("N");
        illegal.Payload.Moves[0].UnitIds = ["not-your-unit"];
        var before = StateCodec.Write((await fixture.Matches.Read(seat.State.Id, seat.Participant))!.State);
        await EqualResponses(bot.PostAsJsonAsync(path + "/preview", illegal, BoardCatalog.Json),
            human.PostAsJsonAsync(path + "/preview", illegal, BoardCatalog.Json));
        var rejected = await BotApiFixture.Send(bot, illegal);
        Assert.False(rejected.Accepted);
        Assert.Equal(Json(rejected), Json(await BotApiFixture.Send(human, illegal)));
        Assert.Equal(before, StateCodec.Write((await fixture.Matches.Read(seat.State.Id, seat.Participant))!.State));
        await EqualResponses(bot.GetAsync(path + "/receipts/" + illegal.CommandId), human.GetAsync(path + "/receipts/" + illegal.CommandId));

        var accepted = await BotApiFixture.Send(side == Side.Shadow ? bot : human, command);
        Assert.True(accepted.Accepted, accepted.Error);
        Assert.Equal(seat.State.Revision + 1, accepted.Revision);
        Assert.Equal(Json(accepted), Json(await BotApiFixture.Send(side == Side.Shadow ? human : bot, command)));
        await EqualResponses(bot.GetAsync(path + "/receipts/" + command.CommandId), human.GetAsync(path + "/receipts/" + command.CommandId));
        // Ordinary retention removes obsolete rejection receipts after an accepted revision.
        await EqualResponses(bot.GetAsync(path + "/receipts/" + illegal.CommandId), human.GetAsync(path + "/receipts/" + illegal.CommandId), HttpStatusCode.NotFound);
        await EqualResponses(bot.GetAsync(path + "/receipts/unknown"), human.GetAsync(path + "/receipts/unknown"), HttpStatusCode.NotFound);
        await EqualResponses(bot.GetAsync(path + "/actions?limit=1"), human.GetAsync(path + "/actions?limit=1"));
        var page = (await bot.GetFromJsonAsync<ActionLogPage>(path + "/actions", BoardCatalog.Json))!;
        Assert.NotEmpty(page.Actions);
        foreach (var action in page.Actions)
            await EqualResponses(bot.GetAsync(path + "/actions/" + action.Id), human.GetAsync(path + "/actions/" + action.Id));
        await EqualResponses(bot.GetAsync(path + "/actions?limit=-1"), human.GetAsync(path + "/actions?limit=-1"), HttpStatusCode.BadRequest);
        await EqualResponses(bot.GetAsync(path + "/actions/unknown"), human.GetAsync(path + "/actions/unknown"), HttpStatusCode.NotFound);

        var conflict = Clone(command);
        conflict.Payload.DieId = "changed";
        var conflictReceipt = await BotApiFixture.Send(bot, conflict);
        Assert.Equal("CommandIdConflict", conflictReceipt.Error);
        Assert.Equal(Json(conflictReceipt), Json(await BotApiFixture.Send(human, conflict)));
        var stale = Clone(command);
        stale.CommandId = Guid.NewGuid().ToString("N");
        Assert.Equal("StaleRevision", (await BotApiFixture.Send(bot, stale)).Error);

        // The opponent owns the next action. A seat token never grants ownership of it.
        var current = (await fixture.Matches.Read(seat.State.Id, seat.Participant))!.State;
        Assert.NotEqual(side, current.Pending!.Owner);
        var opponentMove = BattleFixture.Command(current, CommandKind.DiscardDie, new()
        {
            DieId = current.Dice.First(d => d.Owner != side && d.Location == DieLocation.Available).Id
        });
        var refused = await BotApiFixture.Send(bot, opponentMove);
        Assert.False(refused.Accepted);
        Assert.Equal(Json(refused), Json(await BotApiFixture.Send(human, opponentMove)));
        Assert.Equal(StateCodec.Write(current), StateCodec.Write((await fixture.Matches.Read(seat.State.Id, seat.Participant))!.State));
        await EqualResponses(bot.GetAsync(path), human.GetAsync(path));
    }

    [PostgresTheory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public async Task ActualBattleReportsAndWebSocketVersionsMatchTheHumanSeat(Side side)
    {
        var state = BattleFixture.State();
        var unit = BattleFixture.Put(state, "gondor", UnitKind.Regular, BattleFixture.Origin);
        BattleFixture.Put(state, "sauron", UnitKind.Regular, BattleFixture.Target);
        state = BattleFixture.NoCards(BattleFixture.Begin(state, [unit.Id]), 1, 1);
        var battle = BattleRuntime.Active(state)!.Id;
        var seat = await fixture.Seat(state, side);
        using var bot = fixture.Client(seat.Session.Token);
        using var human = fixture.Client(seat.Human.Token, bot: false);
        var path = $"/api/matches/{seat.State.Id}";
        await EqualResponses(bot.GetAsync(path + "/battles/" + Uri.EscapeDataString(battle)),
            human.GetAsync(path + "/battles/" + Uri.EscapeDataString(battle)));
        await EqualResponses(bot.GetAsync(path + "/battles/unknown"), human.GetAsync(path + "/battles/unknown"), HttpStatusCode.NotFound);
        using var botStream = await fixture.Socket(seat.Session.Token, seat.State.Id);
        using var humanStream = await fixture.Socket(seat.Human.Token, seat.State.Id, bot: false);
        Assert.Equal(await BotApiFixture.Receive(humanStream), await BotApiFixture.Receive(botStream));
        botStream.Abort();
        humanStream.Abort();
    }

    [PostgresFact]
    public async Task RuntimeMetadataIsSharedAndContainsNoCredentialOrStrategyState()
    {
        var seat = await fixture.Seat();
        using var bot = fixture.Client(seat.Session.Token);
        using var control = fixture.Client(fixture.ControlToken);
        using var human = fixture.Client(seat.Human.Token, bot: false);
        await EqualResponses(bot.GetAsync("/api/runtime"), human.GetAsync("/api/runtime"));
        await EqualResponses(control.GetAsync("/api/runtime"), human.GetAsync("/api/runtime"));
        var json = await bot.GetStringAsync("/api/runtime");
        using var runtime = JsonDocument.Parse(json);
        Assert.Equal(Protocol.Version, runtime.RootElement.GetProperty("protocolVersion").GetInt32());
        Assert.Equal(Protocol.RulesVersion, runtime.RootElement.GetProperty("rulesVersion").GetString());
        Assert.Equal(Protocol.ContentVersion, runtime.RootElement.GetProperty("contentVersion").GetString());
        Assert.DoesNotContain(seat.Session.Token, json);
        Assert.DoesNotContain(SessionStore.Hash(fixture.ControlToken), json);
        Assert.DoesNotContain("plan", json, StringComparison.OrdinalIgnoreCase);
    }

    internal static async Task EqualResponses(Task<HttpResponseMessage> left, Task<HttpResponseMessage> right,
        HttpStatusCode expected = HttpStatusCode.OK)
    {
        using var first = await left;
        using var second = await right;
        Assert.Equal(expected, first.StatusCode);
        Assert.Equal(expected, second.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value, BoardCatalog.Json);
    private static GameCommand Clone(GameCommand command) => JsonSerializer.Deserialize<GameCommand>(Json(command), BoardCatalog.Json)!;
}
