using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Wotr.Domain;

public static class EventIdentity
{
    // Legacy entries keep their identity. New entries persist a visibility-scoped identity.
    public static string For(string matchId, long position) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            matchId + "/" + position.ToString(CultureInfo.InvariantCulture))));

    public static string For(string matchId, long position, GameEvent item) => item.HistoryId ?? For(matchId, position);

    internal static string ForRevision(string matchId, long revision, string scope, int ordinal) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            matchId + "/revision/" + revision.ToString(CultureInfo.InvariantCulture) + "/" + scope + "/" + ordinal.ToString(CultureInfo.InvariantCulture))));
}
