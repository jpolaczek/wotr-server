using Wotr.Contracts;

namespace Wotr.Domain;

public static class ConfigurationRules
{
    public static bool DismayEnabled(MatchConfiguration configuration) =>
        configuration.Dismay ?? (configuration.Lords && configuration.Kings);

    public static MatchConfiguration Normalize(MatchConfiguration input) => new()
    {
        Lords = input.Lords,
        Kings = input.Kings,
        Fate = input.Fate,
        Council = input.Council,
        Dismay = DismayEnabled(input),
        VisibleCorruption = input.VisibleCorruption,
        SequentialCorruption = input.SequentialCorruption,
        RemovingRulerTokens = input.RemovingRulerTokens,
        NewCities = input.NewCities
    };

    public static bool IsValid(MatchConfiguration config) => Conflict(config) is null;

    // UI-96: the base game alone, without any expansion module or variant.
    public static bool IsBase(MatchConfiguration config) =>
        !config.Lords && !config.Kings && !config.Fate && !config.Council && !DismayEnabled(config) &&
        !config.VisibleCorruption && !config.SequentialCorruption && !config.RemovingRulerTokens && !config.NewCities;

    public static ConfigurationConflict? Conflict(MatchConfiguration config)
    {
        if (config.Council && !config.Lords)
            return new("Council", "L03", "LORDS pp.30–31", "Lords");
        if (DismayEnabled(config) && !(config.Lords && config.Kings))
            return new("Dismay", "X02", "KINGS pp.17–18", "Lords + Kings");
        if (config.VisibleCorruption && !config.Kings)
            return new("Visible Corruption", "X02", "KINGS p.18", "Kings");
        if (config.SequentialCorruption && !config.Kings)
            return new("Sequential Corruption", "X02", "KINGS p.18", "Kings");
        if (config.RemovingRulerTokens && !(config.Lords && config.Kings && DismayEnabled(config)))
            return new("Removing Ruler Tokens", "X02", "KINGS p.18", "Lords + Kings + Dismay");
        if (config.NewCities && !config.Fate)
            return new("New Cities", "F01", "FATE-WOTR018: New Cities", "Fate");
        return null;
    }

    // All MVP rule modules and their client decision windows are installed.
    public static bool IsImplemented(MatchConfiguration config) => IsValid(config);
}
