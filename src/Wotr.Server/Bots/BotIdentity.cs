using System.Security.Claims;
using Wotr.Contracts;

namespace Wotr.Server;

public sealed record BotSeatAuthority(string TokenHash, string GrantId, string ServiceId, long Generation,
    string ParticipantId, string? RoomId, string? MatchId, Side Side)
{
    public override string ToString() => "BotSeatAuthority [redacted]";
}

public sealed record BotIdentity(string ServiceId, BotSeatAuthority? Seat)
{
    public bool IsControl => Seat is null;
    public ClaimsPrincipal Principal(string scheme) => new(new ClaimsIdentity([
        new(ClaimTypes.NameIdentifier, Seat?.ParticipantId ?? "bot-service:" + ServiceId),
        new("wotr:bot", IsControl ? "control" : "seat")], scheme));
    public static BotIdentity? Of(HttpContext context) => context.Items[typeof(BotIdentity)] as BotIdentity;
    public override string ToString() => "BotIdentity [redacted]";
}

public static class BotTokens
{
    public const string ControlPrefix = "wbc1_";
    public const string SeatPrefix = "wbs1_";
    public static bool IsBot(string token) => token.StartsWith(ControlPrefix, StringComparison.Ordinal) || token.StartsWith(SeatPrefix, StringComparison.Ordinal);
}

public sealed class BotAuthorityExpiredException : Exception;
