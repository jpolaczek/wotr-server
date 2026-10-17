using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// AUTH-02 (D16): email confirmation and password reset against PostgreSQL, with a recording sender and a clock the
// test moves. Each test starts at its own random date, so the daily limits count only its own links.
public class AccountEmailTests
{
    private const string Password = "An account email test password";
    private const string PublicUrl = "https://wotr.example.test";

    [PostgresFact]
    public async Task RegistrationSendsOneConfirmationLinkThatConfirmsTheAddressOnce()
    {
        using var mail = new MailHost();
        var (name, email) = await mail.Register();
        var message = await mail.Mail.Next();
        Assert.Equal(email, message.To);
        Assert.Equal("verify-email", message.Kind);
        Assert.StartsWith(PublicUrl + "/account/verify#token=", Link(message));
        Assert.Contains(name, message.Text);
        Assert.False(await mail.Confirmed(name));

        Assert.Equal(HttpStatusCode.NoContent, (await mail.Post("api/account/verify-email", new { token = Token(message) })).StatusCode);
        Assert.True(await mail.Confirmed(name));
        await mail.Refused("api/account/verify-email", new { token = Token(message) }, "InvalidToken");
        await mail.Refused("api/account/verify-email", new { token = "not a token" }, "InvalidToken");
        Assert.Empty(mail.Mail.Sent);
    }

    [PostgresFact]
    public async Task ConfirmationLinkExpiresAfterSevenDays()
    {
        using var mail = new MailHost();
        var (name, _) = await mail.Register();
        var message = await mail.Mail.Next();
        mail.Clock.Advance(AccountTokens.VerifyLifetime + TimeSpan.FromSeconds(1));
        await mail.Refused("api/account/verify-email", new { token = Token(message) }, "InvalidToken");
        Assert.False(await mail.Confirmed(name));
    }

    [PostgresFact]
    public async Task ResetGoesOnlyToAConfirmedAddressAndTheAnswerNeverChanges()
    {
        using var mail = new MailHost();
        var (name, email) = await mail.Register();
        await mail.Mail.Next();
        var legacy = await mail.LegacyAccount();

        foreach (var login in new[] { "nobody_" + Guid.NewGuid().ToString("N")[..12], "nobody@example.test", legacy, "not a login" })
            Assert.Equal(HttpStatusCode.Accepted, (await mail.Post("api/account/password-reset", new { login })).StatusCode);
        await mail.Mail.Nothing();

        // Not confirmed yet: the confirmation link comes again instead of a reset link.
        mail.Clock.Advance(TimeSpan.FromMinutes(3));
        Assert.Equal(HttpStatusCode.Accepted, (await mail.Post("api/account/password-reset", new { login = name })).StatusCode);
        var confirmation = await mail.Mail.Next();
        Assert.Equal("verify-email", confirmation.Kind);
        await mail.Post("api/account/verify-email", new { token = Token(confirmation) });

        mail.Clock.Advance(TimeSpan.FromMinutes(3));
        Assert.Equal(HttpStatusCode.Accepted, (await mail.Post("api/account/password-reset", new { login = email.ToUpperInvariant() })).StatusCode);
        var reset = await mail.Mail.Next();
        Assert.Equal("reset-password", reset.Kind);
        Assert.Equal(email, reset.To);
        Assert.StartsWith(PublicUrl + "/account/reset#token=", Link(reset));
    }

    [PostgresFact]
    public async Task ResetChangesThePasswordSignsOutEverywhereAndWorksOnce()
    {
        using var mail = new MailHost();
        var (name, email) = await mail.ConfirmedAccount();
        var first = (await mail.Accounts.Login(new LoginRequest { Username = name, Password = Password }))!;
        // AUTH-05: the account name signs in, in any case; the address does not.
        Assert.Null(await mail.Accounts.Login(new LoginRequest { Username = email, Password = Password }));
        var second = (await mail.Accounts.Login(new LoginRequest { Username = name.ToUpperInvariant(), Password = Password }))!;
        var reset = await mail.RequestReset(name);

        await mail.Refused("api/account/password-reset/confirm", new { token = Token(reset), password = "short" }, "InvalidPassword");
        const string newPassword = "A completely new password";
        Assert.Equal(HttpStatusCode.NoContent,
            (await mail.Post("api/account/password-reset/confirm", new { token = Token(reset), password = newPassword })).StatusCode);

        var sessions = mail.Services.GetRequiredService<SessionStore>();
        Assert.Null(await sessions.Resolve(first.Token));
        Assert.Null(await sessions.Resolve(second.Token));
        Assert.Null(await mail.Accounts.Login(new LoginRequest { Username = name, Password = Password }));
        Assert.NotNull(await mail.Accounts.Login(new LoginRequest { Username = name, Password = newPassword }));
        await mail.Refused("api/account/password-reset/confirm", new { token = Token(reset), password = "Yet another long password" }, "InvalidToken");
    }

    [PostgresFact]
    public async Task ANewerResetLinkReplacesTheOlderAndALinkLastsOneHour()
    {
        using var mail = new MailHost();
        var (name, _) = await mail.ConfirmedAccount();
        var older = await mail.RequestReset(name);
        mail.Clock.Advance(TimeSpan.FromMinutes(3));
        var newer = await mail.RequestReset(name);
        await mail.Refused("api/account/password-reset/confirm", new { token = Token(older), password = "A replaced link password" }, "InvalidToken");

        mail.Clock.Advance(AccountTokens.ResetLifetime + TimeSpan.FromSeconds(1));
        await mail.Refused("api/account/password-reset/confirm", new { token = Token(newer), password = "An expired link password" }, "InvalidToken");
        Assert.NotNull(await mail.Accounts.Login(new LoginRequest { Username = name, Password = Password }));
    }

    [PostgresFact]
    public async Task AccountLimitsStopEmailsWithoutChangingTheAnswer()
    {
        using var mail = new MailHost();
        var (name, _) = await mail.ConfirmedAccount();
        await mail.RequestReset(name);
        Assert.Equal(HttpStatusCode.Accepted, (await mail.Post("api/account/password-reset", new { login = name })).StatusCode);
        await mail.Mail.Nothing();

        // Registration, confirmation and one reset so far: ten links a day in all.
        for (var sent = 2; sent < AccountRecovery.AccountDailyLimit; sent++)
        {
            mail.Clock.Advance(TimeSpan.FromMinutes(3));
            await mail.RequestReset(name);
        }
        mail.Clock.Advance(TimeSpan.FromMinutes(3));
        Assert.Equal(HttpStatusCode.Accepted, (await mail.Post("api/account/password-reset", new { login = name })).StatusCode);
        await mail.Mail.Nothing();
        mail.Clock.Advance(TimeSpan.FromDays(1));
        await mail.RequestReset(name);
    }

    // Found on production: a reset asked for right after confirming the address was held back by the confirmation
    // link's cooldown. Each kind of email has its own.
    [PostgresFact]
    public async Task AConfirmationLinkJustSentDoesNotHoldBackAReset()
    {
        using var mail = new MailHost();
        var (name, _) = await mail.Register();
        var confirmation = await mail.Mail.Next();
        Assert.Equal(HttpStatusCode.NoContent, (await mail.Post("api/account/verify-email", new { token = Token(confirmation) })).StatusCode);
        mail.Clock.Advance(TimeSpan.FromSeconds(30));
        await mail.RequestReset(name);
        Assert.Equal(HttpStatusCode.Accepted, (await mail.Post("api/account/password-reset", new { login = name })).StatusCode);
        await mail.Mail.Nothing();
    }

    [PostgresFact]
    public async Task TheDailyLimitForAllAccountsStopsEmails()
    {
        using var mail = new MailHost();
        var (name, _) = await mail.ConfirmedAccount();
        await mail.Seed(AccountRecovery.DailyLimit);
        mail.Clock.Advance(TimeSpan.FromMinutes(3));
        Assert.Equal(HttpStatusCode.Accepted, (await mail.Post("api/account/password-reset", new { login = name })).StatusCode);
        await mail.Mail.Nothing();
        // A registration still creates the account; its confirmation link waits for the next day.
        var (other, _) = await mail.Register();
        await mail.Mail.Nothing();
        Assert.NotNull(await mail.Accounts.Login(new LoginRequest { Username = other, Password = Password }));
    }

    [PostgresFact]
    public async Task AnAddressSendsAtMostFiveEmailRequestsInTenMinutes()
    {
        using var mail = new MailHost(permits: 5);
        var answers = new List<HttpStatusCode>();
        for (var request = 0; request < 6; request++)
            answers.Add((await mail.Post("api/account/password-reset", new { login = "nobody@example.test" })).StatusCode);
        Assert.Equal(Enumerable.Repeat(HttpStatusCode.Accepted, 5).Append(HttpStatusCode.TooManyRequests), answers);
    }

    [PostgresFact]
    public async Task SetEmailGivesAnOlderAccountAnAddressAndAConfirmationLink()
    {
        using var mail = new MailHost();
        var legacy = await mail.LegacyAccount();
        var (_, taken) = await mail.Register();
        await mail.Mail.Next();
        Assert.Equal(SetEmailResult.NoAccount, (await mail.Accounts.SetEmail("nobody_" + Guid.NewGuid().ToString("N")[..10], "a@example.test")).Result);
        Assert.Equal(SetEmailResult.InvalidEmail, (await mail.Accounts.SetEmail(legacy, "not an address")).Result);
        Assert.Equal(SetEmailResult.EmailTaken, (await mail.Accounts.SetEmail(legacy, taken.ToUpperInvariant())).Result);

        var address = TestAccounts.Email(legacy);
        var (result, message) = await mail.Accounts.SetEmail(legacy, address);
        Assert.Equal(SetEmailResult.Done, result);
        Assert.Equal(address, message!.To);
        // AUTH-05: no sign-in by address.
        Assert.Null(await mail.Accounts.Login(new LoginRequest { Username = address, Password = Password }));
        Assert.NotNull(await mail.Accounts.Login(new LoginRequest { Username = legacy, Password = Password }));

        // A new address closes the links sent to the old one.
        mail.Clock.Advance(TimeSpan.FromMinutes(3));
        var (_, again) = await mail.Accounts.SetEmail(legacy, "second." + address);
        await mail.Refused("api/account/verify-email", new { token = Token(message) }, "InvalidToken");
        Assert.Equal(HttpStatusCode.NoContent, (await mail.Post("api/account/verify-email", new { token = Token(again!) })).StatusCode);
    }

    [PostgresFact]
    public async Task TheDatabaseKeepsOnlyHashesOfLinks()
    {
        using var mail = new MailHost();
        var (name, _) = await mail.Register();
        var token = Token(await mail.Mail.Next());
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand(
            "SELECT t.token_hash FROM account_tokens t JOIN accounts a ON a.id=t.account_id WHERE a.normalized_name=$1");
        command.Parameters.AddWithValue(name.ToUpperInvariant());
        Assert.Equal(SessionStore.Hash(token), await command.ExecuteScalarAsync());
    }

    private static string Link(EmailMessage message) =>
        message.Text.Split('\n').Single(line => line.StartsWith(PublicUrl, StringComparison.Ordinal));

    private static string Token(EmailMessage message) => Link(message).Split("#token=")[1];

    private sealed class MailHost : IDisposable
    {
        private readonly AccountHost host;
        private readonly HttpClient client;

        public MailHost(int permits = 1000)
        {
            Clock = new TestClock(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(Random.Shared.Next(0, 2_500_000)));
            host = new AccountHost(extra: new Dictionary<string, string?>
            {
                [EmailOptions.PublicUrlKey] = PublicUrl,
                [EmailOptions.PermitsPerAddressKey] = permits.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }, services: services =>
            {
                services.AddSingleton<IEmailSender>(Mail);
                services.AddSingleton<TimeProvider>(Clock);
            });
            client = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        }

        public RecordingEmailSender Mail { get; } = new();
        public TestClock Clock { get; }
        public IServiceProvider Services => host.Services;
        public AccountService Accounts => host.Services.GetRequiredService<AccountService>();

        public async Task<(string Name, string Email)> Register()
        {
            var name = "mail_" + Guid.NewGuid().ToString("N")[..20];
            var email = TestAccounts.Email(name);
            var response = await Post("api/auth/register", new RegisterRequest { Username = name, Email = email, Password = Password });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (name, email);
        }

        public async Task<(string Name, string Email)> ConfirmedAccount()
        {
            var account = await Register();
            var message = await Mail.Next();
            Assert.Equal(HttpStatusCode.NoContent, (await Post("api/account/verify-email", new { token = Token(message) })).StatusCode);
            Clock.Advance(TimeSpan.FromMinutes(3));
            return account;
        }

        public async Task<EmailMessage> RequestReset(string login)
        {
            Assert.Equal(HttpStatusCode.Accepted, (await Post("api/account/password-reset", new { login })).StatusCode);
            var message = await Mail.Next();
            Assert.Equal("reset-password", message.Kind);
            return message;
        }

        // An account from before AUTH-02: no address, password Password.
        public async Task<string> LegacyAccount()
        {
            var name = "old_" + Guid.NewGuid().ToString("N")[..20];
            var registered = await Accounts.Register(new RegisterRequest { Username = name, Email = TestAccounts.Email(name), Password = Password });
            await Mail.Next();
            await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
            await using var clear = data.CreateCommand(
                "UPDATE accounts SET email=NULL, normalized_email=NULL, email_verified_at=NULL WHERE id=$1");
            clear.Parameters.AddWithValue(registered.Session!.AccountId);
            await clear.ExecuteNonQueryAsync();
            Clock.Advance(TimeSpan.FromMinutes(3));
            return name;
        }

        // Links of another account sent at this test's time, to reach the daily limit for everyone.
        public async Task Seed(int links)
        {
            var name = "seed_" + Guid.NewGuid().ToString("N")[..20];
            var account = (await TestAccounts.Register(AccountService.WithoutEmail(new Database(Services.GetRequiredService<NpgsqlDataSource>())),
                name, Password)).AccountId;
            await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
            for (var link = 0; link < links; link++)
            {
                await using var insert = data.CreateCommand(
                    "INSERT INTO account_tokens(token_hash,account_id,purpose,email,created_at,expires_at) VALUES ($1,$2,'verify-email','x',$3,$3)");
                insert.Parameters.AddWithValue(SessionStore.NewToken());
                insert.Parameters.AddWithValue(account);
                insert.Parameters.AddWithValue(Clock.GetUtcNow());
                await insert.ExecuteNonQueryAsync();
            }
        }

        public async Task<bool> Confirmed(string name)
        {
            await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
            await using var command = data.CreateCommand("SELECT email_verified_at IS NOT NULL FROM accounts WHERE normalized_name=$1");
            command.Parameters.AddWithValue(name.ToUpperInvariant());
            return await command.ExecuteScalarAsync() is true;
        }

        public Task<HttpResponseMessage> Post(string path, object body) => client.PostAsJsonAsync(path, body);

        public async Task Refused(string path, object body, string code)
        {
            using var response = await Post(path, body);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(code, (await response.Content.ReadFromJsonAsync<ApiError>())!.Error);
        }

        public void Dispose()
        {
            client.Dispose();
            host.Dispose();
        }
    }

    // Messages arrive from EmailOutbox's worker; Next waits for one, Nothing proves none came.
    private sealed class RecordingEmailSender : IEmailSender
    {
        private readonly BlockingCollection<EmailMessage> messages = new();

        public IReadOnlyCollection<EmailMessage> Sent => messages.ToArray();

        public Task Send(EmailMessage message, CancellationToken cancellation)
        {
            messages.Add(message, cancellation);
            return Task.CompletedTask;
        }

        public Task<EmailMessage> Next() => Task.Run(() =>
            messages.TryTake(out var message, TimeSpan.FromSeconds(10)) ? message : throw new TimeoutException("No email was sent."));

        public async Task Nothing()
        {
            await Task.Delay(300);
            Assert.Empty(messages);
        }
    }
}
