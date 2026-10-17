using Npgsql;
using Wotr.Contracts;

namespace Wotr.Server;

// OBS-02: who reads a match's history — one of its players, always as their own side, or an observer of its room
// reading as the side they chose.
public sealed record MatchReader(string Account, Side? Perspective = null)
{
    public static MatchReader Observer(string account, Side perspective) => new(account, perspective);

    internal Task<(Side Viewer, bool Indexed)?> Authorize(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string match, CancellationToken cancellation) =>
        Perspective is { } side
            ? ActionLogQuery.AuthorizeObserver(connection, transaction, match, Account, side, cancellation)
            : ActionLogQuery.Authorize(connection, transaction, match, Account, cancellation);
}
