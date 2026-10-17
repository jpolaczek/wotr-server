namespace Wotr.Server;

// ADMIN-01: what the admin panel shows. No email address, password data, token, account id or network address ever
// leaves in these views; an email column would be a deliberate later change (docs/server/ADMIN-PANEL.md).
public sealed record AdminSignInRequest(string? Username, string? Password);

public sealed record AdminSessionView(string Token, string Username, DateTimeOffset ExpiresAt);

public sealed record AdminSummaryView(
    long Users,
    long ConfirmedUsers,
    long SignedInLastWeek,
    long TablesCreated,
    long TablesWaiting,
    long GamesStarted,
    long GamesFinished,
    long GamesInProgress,
    long GamesRemovedUnfinished,
    long FreePeoplesWins,
    long ShadowWins,
    long TestSessions,
    long DemoGames,
    DateTimeOffset At);

public sealed record AdminUserView(
    string Username,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? LastSignInAt,
    bool EmailConfirmed,
    long TablesHosted,
    long Games,
    long GamesFinished,
    long Wins);

public sealed record AdminUserPage(IReadOnlyList<AdminUserView> Users, long Total, int Page, int PageSize);
