using System.Net.Http.Headers;
using System.Text.Json;

namespace Wotr.Server;

// AUTH-02: POST https://api.resend.com/emails with the key as a bearer token. A failure is logged with the HTTP
// status and Resend's error name only; the caller never retries on its own, the player asks for a new link.
public sealed class ResendEmailSender : IEmailSender, IDisposable
{
    public static readonly Uri Endpoint = new("https://api.resend.com/emails");
    private readonly HttpClient http;
    private readonly EmailOptions options;
    private readonly ILogger<ResendEmailSender> logger;

    public ResendEmailSender(EmailOptions options, ILogger<ResendEmailSender> logger, HttpMessageHandler? handler = null)
    {
        this.options = options;
        this.logger = logger;
        http = new HttpClient(handler ?? new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(10) })
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("wotr-server");
    }

    public async Task Send(EmailMessage message, CancellationToken cancellation)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(new
            {
                from = options.From,
                to = new[] { message.To },
                subject = message.Subject,
                text = message.Text,
                html = message.Html
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        using var response = await http.SendAsync(request, cancellation);
        if (response.IsSuccessStatusCode)
            return;
        logger.LogWarning("Resend refused a {Kind} email: {Status} {Error}.", message.Kind, (int)response.StatusCode,
            await ErrorName(response, cancellation));
    }

    public void Dispose() => http.Dispose();

    private static async Task<string> ErrorName(HttpResponseMessage response, CancellationToken cancellation)
    {
        try
        {
            using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellation), cancellationToken: cancellation);
            return body.RootElement.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String
                ? name.GetString() ?? "" : "";
        }
        catch (JsonException)
        {
            return "";
        }
    }
}
