#nullable enable
using System;
using System.Collections.Generic;

namespace Wotr.Contracts
{
    // DIST-05: every game request and WebSocket handshake identifies the client build.
    public static class ClientCompatibility
    {
        public const string ClientVersionHeader = "X-Wotr-Client-Version";
        public const string ProtocolVersionHeader = "X-Wotr-Protocol-Version";
        public const string ClientUpdateRequired = "ClientUpdateRequired";
        public const string ServerUpdateRequired = "ServerUpdateRequired";
        public const string InvalidClientVersion = "InvalidClientVersion";
        public const string UnknownPlatform = "UnknownPlatform";
    }

    public static class ClientPlatforms
    {
        public const string Windows = "windows-x64";
        public const string MacOS = "macos-universal";
        // LNX-01 (D18): Linux x86_64 as an AppImage; the Steam Deck runs the same build.
        public const string Linux = "linux-x64";
        public static readonly IReadOnlyList<string> All = new[] { Windows, MacOS, Linux };

        public static bool IsKnown(string platform) => platform == Windows || platform == MacOS || platform == Linux;
    }

    public enum ClientCompatibilityStatus
    {
        Unknown,
        Compatible,
        ClientUpdateRequired,
        ServerUpdateRequired
    }

    // DS10: one identity for the build manifest, the embedded runtime copy and the signed release manifest.
    public sealed class ClientBuildInfo
    {
        public const int CurrentSchema = 1;
        public int Schema { get; set; } = CurrentSchema;
        public string Version { get; set; } = "";
        public string SourceCommit { get; set; } = "";
        public string Platform { get; set; } = "";
        public bool Development { get; set; }
        public int ProtocolVersion { get; set; }
        public string RulesVersion { get; set; } = "";
        public string ContentVersion { get; set; } = "";
        public string PublicContentVersion { get; set; } = "";
        public string PublicContentSha256 { get; set; } = "";
        public string UnityVersion { get; set; } = "";
    }

    // GET /api/client/releases: public metadata only, before login and without account or match data.
    // MinimumVersion and Status describe what the server enforces now; ScheduledMinimum (DIST-09)
    // announces a higher minimum from a future time, so clients can download it before it applies.
    public sealed class ClientReleaseView
    {
        public string Channel { get; set; } = "stable";
        public string Platform { get; set; } = "";
        public string MinimumVersion { get; set; } = "";
        public int MinimumProtocolVersion { get; set; }
        public int MaximumProtocolVersion { get; set; }
        public ClientCompatibilityStatus Status { get; set; }
        public ClientReleaseOffer? Latest { get; set; }
        public DateTimeOffset ServerTime { get; set; }
        public ClientScheduledMinimum? ScheduledMinimum { get; set; }
        // The declared version's own release, for repairing an installation; null once it is withdrawn.
        public ClientReleaseOffer? Installed { get; set; }
    }

    public sealed class ClientScheduledMinimum
    {
        public string Version { get; set; } = "";
        public DateTimeOffset EffectiveAt { get; set; }
    }

    public sealed class ClientReleaseOffer
    {
        public string Version { get; set; } = "";
        public int ProtocolVersion { get; set; }
        public string ManifestUrl { get; set; } = "";
        public string SignatureUrl { get; set; } = "";
        public DateTimeOffset PublishedAt { get; set; }
    }

    // DIST-09: the signed release manifest as clients read it. The server's Wotr.Releases type writes the
    // same fields; the client verifies the Ed25519 signature over the exact bytes before trusting them.
    public sealed class ClientReleaseManifest
    {
        public const int CurrentSchema = 2;
        public int Schema { get; set; } = CurrentSchema;
        public string Channel { get; set; } = "stable";
        public string Version { get; set; } = "";
        public string Platform { get; set; } = "";
        public int ProtocolVersion { get; set; }
        public string SourceCommit { get; set; } = "";
        public ClientReleaseFile Package { get; set; } = new ClientReleaseFile();
        public ClientReleaseFile? Installer { get; set; }
        public string Notes { get; set; } = "";
        public DateTimeOffset CreatedAt { get; set; }
    }

    public sealed class ClientReleaseFile
    {
        public string FileName { get; set; } = "";
        public string Url { get; set; } = "";
        public long Size { get; set; }
        public string Sha256 { get; set; } = "";
    }

    // GET /api/client/downloads: installers of the newest announced release for the public game page.
    public sealed class ClientDownloadsView
    {
        public ClientDownloadsRelease? Latest { get; set; }
    }

    public sealed class ClientDownloadsRelease
    {
        public string Version { get; set; } = "";
        public DateTimeOffset PublishedAt { get; set; }
        public List<ClientInstallerOffer> Installers { get; set; } = new List<ClientInstallerOffer>();
    }

    public sealed class ClientInstallerOffer
    {
        public string Platform { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Url { get; set; } = "";
        public long Size { get; set; }
        public string Sha256 { get; set; } = "";
    }
}
