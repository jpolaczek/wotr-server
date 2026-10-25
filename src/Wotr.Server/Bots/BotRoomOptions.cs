using System.Text.RegularExpressions;

namespace Wotr.Server;

// Admission stays off until the operator has accepted the advertised policy/capabilities.
public sealed record BotRoomOptions(bool Enabled, string PolicyVersion)
{
    public static BotRoomOptions Read(IConfiguration configuration)
    {
        var enabled = configuration.GetValue<bool>("Wotr:Bots:RoomsEnabled");
        var version = configuration["Wotr:Bots:AcceptedPolicyVersion"] ?? "";
        if (enabled && !Regex.IsMatch(version, "^[a-zA-Z0-9][a-zA-Z0-9._-]{0,63}$", RegexOptions.CultureInvariant))
            throw new InvalidOperationException("CPU rooms require an accepted policy version.");
        return new(enabled, version);
    }
}
