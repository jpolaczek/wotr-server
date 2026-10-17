using Wotr.Contracts;

namespace Wotr.Tests;

public static class ExpansionConfigurations
{
    public static MatchConfiguration FromFlags(int flags) => new()
    {
        Lords = (flags & 1) != 0,
        Kings = (flags & 2) != 0,
        Fate = (flags & 4) != 0,
        Council = (flags & 8) != 0,
        Dismay = (flags & 16) != 0,
        VisibleCorruption = (flags & 32) != 0,
        SequentialCorruption = (flags & 64) != 0,
        RemovingRulerTokens = (flags & 128) != 0,
        NewCities = (flags & 256) != 0
    };

    // Independent source table: Council requires Lords; Kings p.18 variants;
    // Removing Ruler Tokens uses Dismay; New Cities belongs to Fate only.
    public static bool Allowed(int flags)
    {
        var config = FromFlags(flags);
        if (!config.Lords && (config.Council || config.Dismay == true || config.RemovingRulerTokens))
            return false;
        if (!config.Kings && (config.Dismay == true || config.VisibleCorruption ||
            config.SequentialCorruption || config.RemovingRulerTokens))
            return false;
        return !(config.RemovingRulerTokens && config.Dismay == false) && !(config.NewCities && !config.Fate);
    }

    public static IEnumerable<object[]> Legal() => Enumerable.Range(0, 512)
        .Where(Allowed).Select(flags => new object[] { flags });
}
