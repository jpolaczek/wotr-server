using Wotr.Contracts;

namespace Wotr.Server;

// LOBBY-01: how long a player stays online after their menu's last poll, and how long a challenge waits for an answer
// (Wotr__Lobby__OnlineSeconds / ChallengeSeconds; LobbyLimits gives the defaults).
public sealed record LobbyOptions(TimeSpan Online, TimeSpan ChallengeLifetime)
{
    public const string OnlineKey = "Wotr:Lobby:OnlineSeconds";
    public const string ChallengeKey = "Wotr:Lobby:ChallengeSeconds";

    public static readonly LobbyOptions Default = new(
        TimeSpan.FromSeconds(LobbyLimits.OnlineSeconds), TimeSpan.FromSeconds(LobbyLimits.ChallengeSeconds));

    public static LobbyOptions Read(IConfiguration configuration) => new(
        TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue(OnlineKey, (double)LobbyLimits.OnlineSeconds))),
        TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue(ChallengeKey, (double)LobbyLimits.ChallengeSeconds))));
}
