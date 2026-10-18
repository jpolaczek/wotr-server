using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// SEC-02: after a series of failed sign-ins an account name is locked for a while, from every address and even with
// the right password; names that do not exist lock the same way. Game, admin panel and rename share the lock.
public class SignInLockTests
{
    private const string Password = "menu test password long enough";
    private const string WrongPassword = "not the password of this account";
    private static readonly SignInLimits Limits = new(3, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));

    private static SignInGuard Guard(TestClock clock, SignInLimits? limits = null) =>
        new(limits ?? SignInLimits.Default, clock, NullLogger<SignInGuard>.Instance);

    private static string Name() => "locked_" + Guid.NewGuid().ToString("N")[..12];

    [Fact]
    public void TenFailuresWithinTheWindowLockTheNameForFifteenMinutes()
    {
        var clock = new TestClock();
        var guard = Guard(clock);

        for (var failure = 1; failure < 10; failure++)
            guard.Failed("Alice");
        Assert.Null(guard.LockedFor("alice"));
        guard.Failed(" ALICE ");

        Assert.Equal(TimeSpan.FromMinutes(15), guard.LockedFor("alice"));
        Assert.Null(guard.LockedFor("bob"));
        clock.Advance(TimeSpan.FromMinutes(14));
        Assert.Equal(TimeSpan.FromMinutes(1), guard.LockedFor("Alice"));
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Null(guard.LockedFor("Alice"));
        guard.Failed("Alice");
        Assert.Null(guard.LockedFor("Alice"));
    }

    [Fact]
    public void FailuresOutsideTheWindowAndBeforeASuccessDoNotAddUp()
    {
        var clock = new TestClock();
        var guard = Guard(clock);

        for (var failure = 0; failure < 9; failure++)
            guard.Failed("alice");
        clock.Advance(TimeSpan.FromMinutes(15));
        guard.Failed("alice");
        Assert.Null(guard.LockedFor("alice"));
        for (var failure = 0; failure < 8; failure++)
            guard.Failed("alice");
        guard.Succeeded("ALICE");
        guard.Failed("alice");
        Assert.Null(guard.LockedFor("alice"));
    }

    [Fact]
    public void TheCountsStayBoundedAndTheLimitsMustBePositive()
    {
        var clock = new TestClock();
        var guard = Guard(clock);

        for (var name = 0; name < SignInGuard.Capacity + 50; name++)
            guard.Failed("name" + name);

        // While every name is active, the one active longest ago makes room; once windows end, they are forgotten.
        Assert.Equal(SignInGuard.Capacity, guard.Tracked);
        clock.Advance(TimeSpan.FromMinutes(15));
        guard.Failed("another");
        Assert.Equal(1, guard.Tracked);
        var limits = SignInLimits.Read(new ConfigurationBuilder().Build());
        Assert.Equal(SignInLimits.Default, limits);
        Assert.Throws<InvalidOperationException>(() => SignInLimits.Read(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [SignInLimits.LockKey] = "0" }).Build()));
    }

    private static AccountHost Host(TestClock clock, string administrator = "") =>
        new(extra: new Dictionary<string, string?>
            {
                [SignInLimits.FailuresKey] = Limits.Failures.ToString(),
                [AdminOptions.ConfigurationKey] = administrator
            },
            services: services => services.AddSingleton<TimeProvider>(clock));

    private static Task<HttpResponseMessage> SignIn(HttpClient client, string name, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Username = name, Password = password }, BoardCatalog.Json);

    private static async Task AssertLocked(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.InRange(response.Headers.RetryAfter!.Delta!.Value.TotalSeconds, 1, Limits.Lock.TotalSeconds);
        Assert.Equal("SignInLocked",
            (await response.Content.ReadFromJsonAsync<JsonObject>())!["error"]!.GetValue<string>());
    }

    [PostgresFact]
    public async Task ALockedNameIsRefusedEvenWithItsPasswordUntilTheLockEnds()
    {
        var clock = new TestClock(DateTimeOffset.UtcNow);
        using var host = Host(clock);
        var alice = await MenuAccounts.Register(host);
        var bob = await MenuAccounts.Register(host);
        using var client = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        for (var failure = 0; failure < Limits.Failures; failure++)
        {
            using var refused = await SignIn(client, alice.Username, WrongPassword);
            Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
        }

        using (var locked = await SignIn(client, alice.Username.ToUpperInvariant(), Password))
            await AssertLocked(locked);
        using (var other = await SignIn(client, bob.Username, Password))
            Assert.Equal(HttpStatusCode.OK, other.StatusCode);
        clock.Advance(Limits.Lock);
        using (var unlocked = await SignIn(client, alice.Username, Password))
            Assert.Equal(HttpStatusCode.OK, unlocked.StatusCode);
    }

    [PostgresFact]
    public async Task AnUnknownNameLocksLikeAnAccount()
    {
        using var host = Host(new TestClock(DateTimeOffset.UtcNow));
        using var client = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var nobody = Name();

        for (var failure = 0; failure < Limits.Failures; failure++)
        {
            using var refused = await SignIn(client, nobody, WrongPassword);
            Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
        }

        using var locked = await SignIn(client, nobody, WrongPassword);
        await AssertLocked(locked);
    }

    [PostgresFact]
    public async Task TheAdminPanelAndTheRenameShareTheLock()
    {
        var admin = Name();
        using var host = Host(new TestClock(DateTimeOffset.UtcNow), admin);
        var adminAccount = await TestAccounts.Register(host.Services.GetRequiredService<AccountService>(), admin, Password);
        var player = await MenuAccounts.Register(host);
        using var anonymous = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var signedIn = MenuAccounts.Client(host, player);

        for (var failure = 0; failure < Limits.Failures; failure++)
        {
            using var refused = await anonymous.PostAsJsonAsync("/api/admin/session",
                new LoginRequest { Username = admin, Password = WrongPassword }, BoardCatalog.Json);
            Assert.Equal(HttpStatusCode.NotFound, refused.StatusCode);
        }
        using (var adminLocked = await anonymous.PostAsJsonAsync("/api/admin/session",
                   new LoginRequest { Username = admin, Password = Password }, BoardCatalog.Json))
            await AssertLocked(adminLocked);
        using (var gameLocked = await SignIn(anonymous, adminAccount.Username, Password))
            await AssertLocked(gameLocked);

        for (var failure = 0; failure < Limits.Failures; failure++)
        {
            using var wrong = await signedIn.PostAsJsonAsync("/api/account/name",
                new LoginRequest { Username = Name(), Password = WrongPassword }, BoardCatalog.Json);
            Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        }
        using (var renameLocked = await signedIn.PostAsJsonAsync("/api/account/name",
                   new LoginRequest { Username = Name(), Password = Password }, BoardCatalog.Json))
            await AssertLocked(renameLocked);
        using (var playerLocked = await SignIn(anonymous, player.Username, Password))
            await AssertLocked(playerLocked);
    }
}
