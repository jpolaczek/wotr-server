using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// MENU-24 (WR02): the players of a room exchange messages through the server's memory: in order, from a given
// number on, at most the latest 50, only between members and only plain single-line text of 1–200 characters.
public sealed class RoomChatTests
{
    [PostgresFact]
    public async Task BothPlayersReadTheMessagesInOrderFromAGivenNumberOn()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var (alice, bob, room, _) = await Table(host);

        await Send(alice, room, "  Ready when you are  ");
        var view = await Send(bob, room, "One moment");

        Assert.Equal(2, view.Latest);
        Assert.Equal(new[] { "Ready when you are", "One moment" }, view.Messages.Select(m => m.Text));
        Assert.Equal(new[] { Side.FreePeoples, Side.Shadow }, view.Messages.Select(m => m.Side));
        Assert.All(view.Messages, m => Assert.NotEqual("", m.Author));
        var later = (await alice.GetFromJsonAsync<RoomChatView>(Chat(room) + "?after=1", BoardCatalog.Json))!;
        Assert.Equal("One moment", Assert.Single(later.Messages).Text);
        var beyond = (await alice.GetFromJsonAsync<RoomChatView>(Chat(room) + "?after=99", BoardCatalog.Json))!;
        Assert.Equal(2, beyond.Messages.Count);
    }

    [PostgresFact]
    public async Task OnlyMembersWritePlainLinesOfAtMost200CharactersAndOnly50Stay()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var (alice, _, room, _) = await Table(host);
        var outsider = MenuAccounts.Client(host, await MenuAccounts.Register(host));

        foreach (var text in new[] { "", "   ", new string('a', 201), "two\nlines", "bell\u0007" })
        {
            using var refused = await alice.PostAsJsonAsync(Chat(room), new RoomChatRequest { Text = text }, BoardCatalog.Json);
            Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        }
        using (var foreign = await outsider.PostAsJsonAsync(Chat(room), new RoomChatRequest { Text = "hello" }, BoardCatalog.Json))
            Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        using (var foreignRead = await outsider.GetAsync(Chat(room)))
            Assert.Equal(HttpStatusCode.NotFound, foreignRead.StatusCode);
        var markup = await Send(alice, room, "<b>bold</b> " + new string('x', 188));
        Assert.Equal("<b>bold</b> " + new string('x', 188), markup.Messages.Single().Text);

        for (var i = 0; i < 55; i++)
            await Send(alice, room, "message " + i);
        var view = (await alice.GetFromJsonAsync<RoomChatView>(Chat(room), BoardCatalog.Json))!;
        Assert.Equal(RoomChat.Kept, view.Messages.Count);
        Assert.Equal("message 54", view.Messages[^1].Text);
        Assert.Equal(56, view.Latest);
    }

    [PostgresFact]
    public async Task ANewServerProcessStartsWithAnEmptyChat()
    {
        using var first = new AccountHost(extra: TestBudgets.Stress);
        var (alice, _, room, account) = await Table(first);
        await Send(alice, room, "before the restart");
        var fresh = new RoomChat(first.Services.GetRequiredService<RoomStore>(), TimeProvider.System);
        var read = await fresh.Read(room, account, 0);
        Assert.Empty(read.View!.Messages);
    }

    // The host writes in a room nobody has joined yet, created through the API like the client does.
    [PostgresFact]
    public async Task TheHostWritesInARoomNobodyHasJoined()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        using var alice = MenuAccounts.Client(host, await MenuAccounts.Register(host));
        using var created = await alice.PostAsJsonAsync("/api/rooms", new CreateRoomRequest { Side = Side.FreePeoples }, BoardCatalog.Json);
        created.EnsureSuccessStatusCode();
        var room = (await created.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!.Id;

        var view = await Send(alice, room, "anyone here?");

        Assert.Equal("anyone here?", Assert.Single(view.Messages).Text);
    }

    private static async Task<(HttpClient Alice, HttpClient Bob, string Room, string AliceAccount)> Table(AccountHost host)
    {
        var alice = await MenuAccounts.Register(host);
        var bob = await MenuAccounts.Register(host);
        var rooms = host.Services.GetRequiredService<RoomStore>();
        var room = await rooms.Create(alice.AccountId, Side.FreePeoples);
        await rooms.Join(room.JoinCode, bob.AccountId);
        return (MenuAccounts.Client(host, alice), MenuAccounts.Client(host, bob), room.Id, alice.AccountId);
    }

    private static async Task<RoomChatView> Send(HttpClient client, string room, string text)
    {
        using var sent = await client.PostAsJsonAsync(Chat(room), new RoomChatRequest { Text = text }, BoardCatalog.Json);
        sent.EnsureSuccessStatusCode();
        return (await sent.Content.ReadFromJsonAsync<RoomChatView>(BoardCatalog.Json))!;
    }

    private static string Chat(string room) => "/api/rooms/" + room + "/chat";
}
