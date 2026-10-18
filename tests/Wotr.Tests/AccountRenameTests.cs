using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// AUTH-06: a player changes their account name with their current password; the operator renames any account. The
// name follows the registration rules, stays unique in any case, never takes an administrator's name, and its copies in
// open rooms follow.
public class AccountRenameTests
{
    private const string Password = "menu test password long enough";
    private const string Administrator = "boss";

    private static AccountHost Host() =>
        new(extra: new Dictionary<string, string?> { [AdminOptions.ConfigurationKey] = Administrator });

    private static string Name() => "renamed_" + Guid.NewGuid().ToString("N")[..12];

    private static Task<HttpResponseMessage> Rename(HttpClient client, string name, string password = Password) =>
        client.PostAsJsonAsync("/api/account/name", new LoginRequest { Username = name, Password = password }, BoardCatalog.Json);

    private static async Task<string> Error(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ApiError>(BoardCatalog.Json))!.Error;

    [PostgresFact]
    public async Task APlayerRenamesWithTheirPasswordAndTheirRoomsFollow()
    {
        using var host = Host();
        var alice = await MenuAccounts.Register(host);
        var bob = await MenuAccounts.Register(host);
        var rooms = host.Services.GetRequiredService<RoomStore>();
        var accounts = host.Services.GetRequiredService<AccountService>();
        var hosted = await rooms.Create(alice.AccountId, new CreateRoomRequest { Name = "Alice's table", Visibility = RoomVisibility.Public, Side = Side.FreePeoples });
        await rooms.JoinPublic(hosted.Id, bob.AccountId, RoomRole.Player);
        var watched = await rooms.Create(bob.AccountId, new CreateRoomRequest { Name = "Bob's table", Visibility = RoomVisibility.Public });
        await rooms.JoinPublic(watched.Id, alice.AccountId, RoomRole.Observer);
        var before = (await rooms.Read(hosted.Id, alice.AccountId))!;
        using var client = MenuAccounts.Client(host, alice);
        var name = Name();

        using var response = await Rename(client, name);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(name, (await response.Content.ReadFromJsonAsync<SessionView>(BoardCatalog.Json))!.Username);
        Assert.Null(await accounts.Login(new LoginRequest { Username = alice.Username, Password = Password }));
        Assert.Equal(name, (await accounts.Login(new LoginRequest { Username = name.ToUpperInvariant(), Password = Password }))!.Username);
        var after = (await rooms.Read(hosted.Id, alice.AccountId))!;
        Assert.Equal((name, name), (after.Host, after.FreePeoples));
        Assert.True(after.Version > before.Version, "The room's members read it again.");
        Assert.Contains((await rooms.Read(watched.Id, bob.AccountId))!.Observers, o => o.Name == name);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/rooms")).StatusCode);
    }

    [PostgresFact]
    public async Task ARenameRefusesAWrongPasswordBadTakenAndReservedNames()
    {
        using var host = Host();
        var alice = await MenuAccounts.Register(host);
        var bob = await MenuAccounts.Register(host);
        using var client = MenuAccounts.Client(host, alice);
        using var anonymous = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        Assert.Equal(HttpStatusCode.Unauthorized, (await Rename(anonymous, Name())).StatusCode);
        using (var wrong = await Rename(client, Name(), "not the right password"))
        {
            Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
            Assert.Equal("WrongPassword", await Error(wrong));
        }
        foreach (var invalid in new[] { "ab", "has space", "name@example.com", new string('a', 33), "zażółć", "" })
        {
            using var refused = await Rename(client, invalid);
            Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
            Assert.Equal("InvalidName", await Error(refused));
        }
        foreach (var taken in new[] { bob.Username.ToUpperInvariant(), Administrator.ToUpperInvariant() })
        {
            using var refused = await Rename(client, taken);
            Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
            Assert.Equal("NameTaken", await Error(refused));
        }
        using var sameName = await Rename(client, alice.Username.ToUpperInvariant());
        Assert.Equal(HttpStatusCode.OK, sameName.StatusCode);
        using var register = await anonymous.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest { Username = Administrator, Email = TestAccounts.Email(Name()), Password = Password }, BoardCatalog.Json);
        Assert.Equal(HttpStatusCode.Conflict, register.StatusCode);
    }

    [PostgresFact]
    public async Task TheOperatorRenamesAnAccountByItsCurrentName()
    {
        using var host = Host();
        var alice = await MenuAccounts.Register(host);
        var bob = await MenuAccounts.Register(host);
        var rename = host.Services.GetRequiredService<AccountRename>();
        var name = Name();

        Assert.Equal(RenameError.NoAccount, (await rename.RenameByOperator("nobody_" + Name(), Name())).Error);
        Assert.Equal(RenameError.NameTaken, (await rename.RenameByOperator(alice.Username, bob.Username)).Error);
        Assert.Equal(RenameError.InvalidName, (await rename.RenameByOperator(alice.Username, "x")).Error);
        Assert.Equal(new RenameResult(RenameError.None, name), await rename.RenameByOperator(alice.Username.ToUpperInvariant(), name));

        Assert.Equal(("old", "new"), OperatorCommands.Rename(["--rename", "old", "new"]));
        Assert.Equal(["--urls", "x"], OperatorCommands.HostArguments(["--rename", "old", "new", "--urls", "x", "--migrate"]));
        Assert.Throws<ArgumentException>(() => OperatorCommands.Rename(["--rename", "old"]));
    }
}
