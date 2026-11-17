using System.Text.Json.Nodes;

namespace Wotr.Server;

// BOT-225 (owner, 2026-11-16, R32): the operator's AlphaWOTR sidecar. A CPU seat of a base game plays through the WotR Bot
// Protocol with the sidecar at Host:Port (one TCP connection per game, the sidecar starting one bot per connection). The
// scripted runtime never plays in its place (owner, 2026-11-17): a decision the seat cannot answer waits with its error and
// is asked again. The time a decision may take is AlphaWOTR's own (BOT-220 sets it from the production host's measurements).
public sealed record ProtocolSeatOptions(bool Enabled, string Host, int Port, TimeSpan ConnectTimeout, TimeSpan DecisionTimeout,
    IReadOnlyDictionary<string, JsonNode?> BotOptions)
{
    public TimeSpan IdleSession { get; init; } = TimeSpan.FromMinutes(30);
    // The games limit counts an unfinished AlphaWOTR game whose last move is this recent (owner, 2026-11-17): its session may
    // have closed while its player thinks (IdleSession), yet it still holds its place; one left alone longer gives it back.
    public TimeSpan ActiveGame { get; init; } = TimeSpan.FromMinutes(30);
    // Release: the accounts that get AlphaWOTR for a new CPU game while it is not yet open to everyone (the owner's playtest);
    // empty = every account once the seat is enabled.
    public IReadOnlySet<string> Accounts { get; init; } = new HashSet<string>(StringComparer.Ordinal);
    // At most this many AlphaWOTR games at once (0 = no limit); a new CPU game beyond it is refused (CpuBusy).
    public int MaxGames { get; init; }
    // A decision waiting this long with an error is reported to the operator (a critical log line).
    public TimeSpan StallAlert { get; init; } = TimeSpan.FromMinutes(5);

    public bool For(string account) => Enabled && (Accounts.Count == 0 || Accounts.Contains(account));
    // BOT-224: where a decision the seat could not answer is written for replay (the CPU side's own view, the questions
    // asked and the answers); off unless the operator names a folder.
    public string? DumpDirectory { get; init; }

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
            IdleSession = TimeSpan.FromMinutes(section.GetValue("IdleSessionMinutes", 30)),
            ActiveGame = TimeSpan.FromMinutes(section.GetValue("ActiveGameMinutes", 30)),
            Accounts = (section["Accounts"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.Ordinal),
            MaxGames = section.GetValue("MaxGames", 0),
            StallAlert = TimeSpan.FromMinutes(section.GetValue("StallAlertMinutes", 5)),
            DumpDirectory = section["DumpDirectory"] is { Length: > 0 } dump ? dump : null
        };
        if (seat.Port is < 1 or > 65535 || seat.ConnectTimeout.TotalSeconds is < 1 or > 120 ||
            seat.DecisionTimeout.TotalSeconds is < 1 or > 600 || seat.IdleSession.TotalMinutes is < 1 or > 1440 ||
            seat.ActiveGame.TotalMinutes is < 1 or > 1440 ||
            seat.MaxGames is < 0 or > 64 || seat.StallAlert.TotalMinutes is < 1 or > 1440 ||
            seat.BotOptions.TryGetValue("sims", out var s) && s?.GetValue<int>() is < 1 or > 100_000)
            throw new InvalidOperationException("Invalid protocol seat settings.");
        return seat;
    }
}
