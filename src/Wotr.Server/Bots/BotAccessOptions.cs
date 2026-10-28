using System.Text.RegularExpressions;
using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

public sealed record BotAccessOptions(bool Enabled, string ServiceId, string ControlTokenHash,
    ClientVersion MinimumVersion, TimeSpan SeatLifetime, IReadOnlySet<string> Features)
{
    public static readonly string[] KnownFeatures = ["base", "lords", "kings", "fate", "council", "dismay",
        "visible-corruption", "sequential-corruption", "removing-ruler-tokens", "new-cities"];

    public static BotAccessOptions Read(IConfiguration configuration)
    {
        var section = configuration.GetSection("Wotr:Bots");
        var enabled = section.GetValue<bool>("Enabled");
        var id = section["ServiceId"] ?? "";
        var hash = section["ControlTokenHash"] ?? "";
        var version = section["MinimumVersion"] ?? "0.1.0";
        var seconds = section.GetValue("SeatLifetimeSeconds", 300);
        var features = (section["Features"] ?? "base").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);
        if (!ClientVersion.TryParse(version, out var minimum) || seconds is < 5 or > 900 ||
            !features.Contains("base") || features.Any(feature => !KnownFeatures.Contains(feature, StringComparer.Ordinal)) ||
            enabled && (!Regex.IsMatch(id, "^[a-zA-Z0-9_-]{1,64}$") || !Regex.IsMatch(hash, "^[A-Fa-f0-9]{64}$")))
            throw new InvalidOperationException("Invalid Wotr:Bots identity, compatibility or lifetime configuration.");
        return new(enabled, id, hash.ToUpperInvariant(), minimum!, TimeSpan.FromSeconds(seconds), features);
    }

    public bool Supports(MatchConfiguration configuration) => Supports(configuration, Features);

    public static bool Supports(MatchConfiguration configuration, IReadOnlySet<string> features)
    {
        var c = ConfigurationRules.Normalize(configuration);
        return ConfigurationRules.IsValid(c) &&
            features.Contains("base") &&
            (!c.Lords || features.Contains("lords")) && (!c.Kings || features.Contains("kings")) &&
            (!c.Fate || features.Contains("fate")) && (!c.Council || features.Contains("council")) &&
            (c.Dismay != true || features.Contains("dismay")) &&
            (!c.VisibleCorruption || features.Contains("visible-corruption")) &&
            (!c.SequentialCorruption || features.Contains("sequential-corruption")) &&
            (!c.RemovingRulerTokens || features.Contains("removing-ruler-tokens")) && (!c.NewCities || features.Contains("new-cities"));
    }
}
