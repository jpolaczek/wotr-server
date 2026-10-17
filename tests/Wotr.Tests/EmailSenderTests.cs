using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// AUTH-02: the Resend request and the email configuration that the server refuses to start with.
public class EmailSenderTests
{
    [Fact]
    public async Task ResendGetsTheKeySenderRecipientAndBothBodies()
    {
        var handler = new Recording(HttpStatusCode.OK, "{\"id\":\"4ef9a417\"}");
        using var sender = new ResendEmailSender(Options(), NullLogger<ResendEmailSender>.Instance, handler);
        await sender.Send(new EmailMessage("player@example.test", "Subject", "Text body", "<p>Html body</p>", "reset-password"), default);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(ResendEmailSender.Endpoint, request.Uri);
        Assert.Equal("Bearer re_test_key", request.Authorization);
        using var body = JsonDocument.Parse(request.Body);
        Assert.Equal("War of the Ring Online <no-reply@example.test>", body.RootElement.GetProperty("from").GetString());
        Assert.Equal("player@example.test", body.RootElement.GetProperty("to")[0].GetString());
        Assert.Equal("Subject", body.RootElement.GetProperty("subject").GetString());
        Assert.Equal("Text body", body.RootElement.GetProperty("text").GetString());
        Assert.Equal("<p>Html body</p>", body.RootElement.GetProperty("html").GetString());
    }

    [Fact]
    public async Task ARefusalIsLoggedNotThrown()
    {
        var handler = new Recording((HttpStatusCode)422, "{\"name\":\"validation_error\",\"message\":\"Invalid `to` field.\"}");
        using var sender = new ResendEmailSender(Options(), NullLogger<ResendEmailSender>.Instance, handler);
        await sender.Send(new EmailMessage("player@example.test", "Subject", "Text", "<p>Html</p>", "verify-email"), default);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public void ConfigurationWithoutAProviderSendsNothingAndLinksPointToThePublicAddress()
    {
        var options = EmailOptions.Read(Configuration(new() { [EmailOptions.PublicUrlKey] = "https://wotr.example.test/" }), Environment("Production"));
        Assert.Equal(EmailProvider.None, options.Provider);
        Assert.Equal("https://wotr.example.test/account/reset#token=abc", options.Link("reset", "abc").AbsoluteUri);
    }

    [Theory]
    [InlineData("log", "Production", null, null, "https://wotr.example.test")]
    [InlineData("resend", "Production", null, "War of the Ring Online <no-reply@example.test>", "https://wotr.example.test")]
    [InlineData("resend", "Production", "re_key", "", "https://wotr.example.test")]
    [InlineData("resend", "Production", "re_key", "War of the Ring Online <no-reply@example.test>", "http://wotr.example.test")]
    [InlineData("resend", "Production", "re_key", "War of the Ring Online <no-reply@example.test>", "")]
    [InlineData("pigeon", "Production", null, null, "https://wotr.example.test")]
    public void BrokenConfigurationStopsTheServer(string provider, string environment, string? key, string? from, string publicUrl)
    {
        var keyFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(keyFile, key ?? "");
            var settings = new Dictionary<string, string?>
            {
                [EmailOptions.ProviderKey] = provider,
                [EmailOptions.FromKey] = from,
                [EmailOptions.KeyFileKey] = keyFile,
                [EmailOptions.PublicUrlKey] = publicUrl
            };
            Assert.Throws<InvalidOperationException>(() => EmailOptions.Read(Configuration(settings), Environment(environment)));
        }
        finally
        {
            File.Delete(keyFile);
        }
    }

    [Fact]
    public void ResendReadsTheKeyFromItsFile()
    {
        var keyFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(keyFile, "re_test_key\n");
            var options = EmailOptions.Read(Configuration(new()
            {
                [EmailOptions.ProviderKey] = "resend",
                [EmailOptions.FromKey] = "War of the Ring Online <no-reply@example.test>",
                [EmailOptions.KeyFileKey] = keyFile,
                [EmailOptions.PublicUrlKey] = "https://wotr.example.test"
            }), Environment("Production"));
            Assert.Equal("re_test_key", options.ApiKey);
        }
        finally
        {
            File.Delete(keyFile);
        }
    }

    private static EmailOptions Options() => new(EmailProvider.Resend, "War of the Ring Online <no-reply@example.test>", "re_test_key",
        new Uri("https://wotr.example.test/"));

    private static IConfiguration Configuration(Dictionary<string, string?> settings) =>
        new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

    private static IHostEnvironment Environment(string name) => new Host { EnvironmentName = name };

    private sealed class Host : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "";
        public string ApplicationName { get; set; } = "Wotr.Server";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class Recording(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public List<(Uri Uri, string Authorization, string Body)> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
        {
            Requests.Add((request.RequestUri!, request.Headers.Authorization!.ToString(), await request.Content!.ReadAsStringAsync(cancellation)));
            return new HttpResponseMessage(status) { Content = new StringContent(body) };
        }
    }
}
