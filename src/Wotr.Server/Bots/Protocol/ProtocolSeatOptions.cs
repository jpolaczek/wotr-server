using System.Text.Json.Nodes;

namespace Wotr.Server;

// BOT-225 (owner, 2026-11-16, R32): the operator's AlphaWOTR sidecar. A CPU seat of a base game plays through the WotR Bot
// Protocol with the sidecar at Host:Port (one TCP connection per game, the sidecar starting one bot per connection); the
// scripted runtime answers only when the seat cannot (every such answer is a defect, counted and logged). The time a
// decision may take is AlphaWOTR's own (BOT-220 sets it from the production host's measurements).
public sealed record ProtocolSeatOptions(bool Enabled, string Host, int Port, TimeSpan ConnectTimeout, TimeSpan DecisionTimeout,
    IReadOnlyDictionary<string, JsonNode?> BotOptions)
{
    public TimeSpan IdleSession { get; init; } = TimeSpan.FromMinutes(30);

    public static ProtocolSeatOptions Read(IConfiguration configuration)
    {
        var section = configuration.GetSection("Wotr:Bots:Protocol");
        var options = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        if (section.GetValue<int?>("Sims") is { } sims)
            options["sims"] = sims;
        var seat = new ProtocolSeatOptions(section.GetValue<bool>("Enabled"), section["Host"] ?? "127.0.0.1",
            section.GetValue("Port", 7311), TimeSpan.FromSeconds(section.GetValue("ConnectSeconds", 20)),
            TimeSpan.FromSeconds(section.GetValue("DecisionSeconds", 60)), options)
        {
            IdleSession = TimeSpan.FromMinutes(section.GetValue("IdleSessionMinutes", 30))
        };
        if (seat.Port is < 1 or > 65535 || seat.ConnectTimeout.TotalSeconds is < 1 or > 120 ||
            seat.DecisionTimeout.TotalSeconds is < 1 or > 600 || seat.IdleSession.TotalMinutes is < 1 or > 1440 ||
            seat.BotOptions.TryGetValue("sims", out var s) && s?.GetValue<int>() is < 1 or > 100_000)
            throw new InvalidOperationException("Invalid protocol seat settings.");
        return seat;
    }
}
