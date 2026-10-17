using Wotr.Contracts;

namespace Wotr.Server;

// DS12: public before login. Returns only release metadata: no account, session or match data.
public static class ClientReleaseEndpoints
{
    public const string Path = "/api/client/releases";
    // DIST-09: installers for the public game page, which sends no client version headers.
    public const string DownloadsPath = "/api/client/downloads";

    public static void Map(WebApplication app)
    {
        app.MapGet(Path, Releases);
        app.MapGet(DownloadsPath, Downloads);
    }

    private static IResult Releases(string? platform, string? version, string? protocol, HttpContext context,
        ClientCompatibilityPolicy policy, ReleaseCatalogStore releases, TimeProvider clock)
    {
        context.Response.Headers.CacheControl = "no-store";
        if (platform is null || !ClientPlatforms.IsKnown(platform))
            return Results.BadRequest(new { error = ClientCompatibility.UnknownPlatform });
        var catalog = releases.Current();
        var now = clock.GetUtcNow();
        var status = ClientCompatibilityStatus.Unknown;
        if (version is not null || protocol is not null)
        {
            var verdict = policy.CheckDeclared(version, protocol, catalog);
            if (verdict == CompatibilityVerdict.Invalid)
                return Results.BadRequest(new { error = ClientCompatibility.InvalidClientVersion });
            status = verdict switch
            {
                CompatibilityVerdict.ClientUpdateRequired => ClientCompatibilityStatus.ClientUpdateRequired,
                CompatibilityVerdict.ServerUpdateRequired => ClientCompatibilityStatus.ServerUpdateRequired,
                _ => ClientCompatibilityStatus.Compatible
            };
        }
        return Results.Ok(new ClientReleaseView
        {
            Platform = platform,
            MinimumVersion = catalog.Minimum(now)?.Text ?? "",
            MinimumProtocolVersion = policy.Protocols.Minimum,
            MaximumProtocolVersion = policy.Protocols.Maximum,
            Status = status,
            Latest = catalog.Latest(platform),
            ServerTime = now,
            ScheduledMinimum = Scheduled(catalog.Upcoming(now)),
            Installed = ClientVersion.TryParse(version, out var installed) ? catalog.Release(platform, installed) : null
        });
    }

    private static IResult Downloads(HttpContext context, ReleaseCatalogStore releases)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(new ClientDownloadsView { Latest = releases.Current().LatestInstallers() });
    }

    private static ClientScheduledMinimum? Scheduled(SnapshotSchedule? upcoming) => upcoming is null
        ? null
        : new ClientScheduledMinimum { Version = upcoming.Version.Text, EffectiveAt = upcoming.EffectiveAt };
}
