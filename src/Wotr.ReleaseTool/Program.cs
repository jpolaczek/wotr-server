using System.Globalization;
using System.Text.RegularExpressions;
using Wotr.Contracts;
using Wotr.Releases;

namespace Wotr.ReleaseTool;

// DIST-05/DIST-09 release commands. keygen, sign and verify-upload run on the release machine that holds
// the private key; publish, minimum, withdraw, verify and status run on the distribution host with the
// public key only.
public static partial class Program
{
    private const string Usage = """
        Usage:
          keygen        --private-key <file> --public-key <file>
          sign          --build-info <build-info.json> --package <file> --installer <file> --notes <file>
                        --downloads-url <https://…/> --private-key <file> --output <upload root>
          verify-upload --upload <upload root> --downloads-url <https://…/> --public-key <base64|@file> --version <v>
          publish       --root <distribution> --downloads-url <https://…/> --public-key <base64|@file> --version <v>
                        [--minimum [--at <ISO 8601 time with offset>]]
          minimum       --root <distribution> --downloads-url <https://…/> --public-key <base64|@file>
                        (--version <v> [--at <ISO 8601 time with offset>] | --cancel-scheduled)
          withdraw      --root <distribution> --downloads-url <https://…/> --public-key <base64|@file> --version <v>
          verify        --root <distribution> --downloads-url <https://…/> --public-key <base64|@file>
          status        --root <distribution> --downloads-url <https://…/> --public-key <base64|@file>
        """;

    private static readonly string[] Flags = ["--minimum", "--cancel-scheduled"];

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
                throw new ReleaseRejected(Usage);
            var options = Options.Parse(args.Skip(1));
            Console.WriteLine(Run(args[0], options));
            return 0;
        }
        catch (ReleaseRejected error)
        {
            Console.Error.WriteLine("REFUSED: " + error.Message);
            return 2;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine("FAILED: " + error.Message);
            return 1;
        }
    }

    private static string Run(string command, Options options) => command switch
    {
        "keygen" => KeyGen(options),
        "sign" => Sign(options),
        "verify-upload" => VerifyUpload(options),
        "publish" => Describe(Publisher(options).Publish(options.Required("--version"), options.Flag("--minimum"), At(options))),
        "minimum" => Minimum(options),
        "withdraw" => Describe(Publisher(options).Withdraw(options.Required("--version"))),
        "verify" => "PASS: " + Describe(Publisher(options).Verify()),
        "status" => Describe(Publisher(options).Status()),
        _ => throw new ReleaseRejected(Usage)
    };

    private static string Minimum(Options options)
    {
        var publisher = Publisher(options);
        if (options.Flag("--cancel-scheduled"))
            return Describe(publisher.CancelScheduledMinimum());
        return Describe(publisher.SetMinimum(options.Required("--version"), At(options)));
    }

    private static string VerifyUpload(Options options)
    {
        var version = options.Required("--version");
        var verified = ReleaseVerifier.VerifyUpload(Path.Combine(options.Required("--upload"), version), version, Trust(options));
        return $"PASS: release {verified.Version} protocol {verified.ProtocolVersion}, platforms " +
            string.Join(", ", verified.Platforms.Keys.Order(StringComparer.Ordinal));
    }

    // A scheduled minimum must say which time zone it means; a bare local time is refused.
    private static DateTimeOffset? At(Options options)
    {
        if (options.Optional("--at") is not { } text)
            return null;
        if (!ExplicitOffset().IsMatch(text) ||
            !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var at))
            throw new ReleaseRejected($"--at '{text}' is not an ISO 8601 time with Z or an offset, e.g. 2026-10-20T20:00+02:00.");
        return at;
    }

    [GeneratedRegex(@"\A\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(:\d{2})?(Z|[+-]\d{2}:\d{2})\z")]
    private static partial Regex ExplicitOffset();

    private static string KeyGen(Options options)
    {
        var key = ReleasePrivateKey.Generate();
        var publicPath = options.Required("--public-key");
        if (File.Exists(publicPath))
            throw new ReleaseRejected(publicPath + " already exists.");
        key.Save(options.Required("--private-key"));
        File.WriteAllText(publicPath, key.PublicKey.Encoded + "\n");
        return $"Ed25519 key {key.PublicKey.KeyId}; public key {key.PublicKey.Encoded}";
    }

    private static string Sign(Options options)
    {
        var build = ReleaseJson.Parse<ClientBuildInfo>(File.ReadAllBytes(options.Required("--build-info")), "The build info");
        var folder = ReleaseSigner.Sign(build, options.Required("--package"), options.Required("--installer"),
            File.ReadAllText(options.Required("--notes")), DownloadsLocation.Parse(options.Required("--downloads-url")),
            ReleasePrivateKey.Load(options.Required("--private-key")), options.Required("--output"), DateTimeOffset.UtcNow);
        return "Signed " + folder;
    }

    private static ReleasePublisher Publisher(Options options) =>
        new(new DistributionLayout(options.Required("--root")), Trust(options), TimeProvider.System);

    private static ReleaseTrust Trust(Options options)
    {
        var key = options.Required("--public-key");
        return new ReleaseTrust(ReleasePublicKey.Parse(key.StartsWith('@') ? File.ReadAllText(key[1..]) : key),
            DownloadsLocation.Parse(options.Required("--downloads-url")), ProtocolSupport.Current);
    }

    private static string Describe(ReleaseCatalog catalog)
    {
        var scheduled = catalog.ScheduledMinimum is { } next
            ? $"{next.Version} from {next.EffectiveAt.UtcDateTime:yyyy-MM-dd HH:mm:ss} UTC"
            : "none";
        return $"catalog revision {catalog.Revision}; minimum {(catalog.MinimumVersion.Length == 0 ? "none" : catalog.MinimumVersion)}; " +
            $"scheduled minimum {scheduled}; " +
            $"announced {string.Join(", ", catalog.Announced.Select(release => release.Version).DefaultIfEmpty("none"))}";
    }

    private sealed class Options
    {
        private readonly Dictionary<string, string> values = new(StringComparer.Ordinal);
        private readonly HashSet<string> flags = new(StringComparer.Ordinal);

        public static Options Parse(IEnumerable<string> args)
        {
            var options = new Options();
            using var items = args.GetEnumerator();
            while (items.MoveNext())
            {
                var name = items.Current;
                if (!name.StartsWith("--", StringComparison.Ordinal))
                    throw new ReleaseRejected("Unexpected argument " + name + "\n" + Usage);
                if (Flags.Contains(name))
                    options.flags.Add(name);
                else if (items.MoveNext())
                    options.values[name] = items.Current;
                else
                    throw new ReleaseRejected(name + " needs a value.");
            }
            return options;
        }

        public string Required(string name) => values.TryGetValue(name, out var value) && value.Length > 0
            ? value
            : throw new ReleaseRejected(name + " is required.\n" + Usage);

        public string? Optional(string name) => values.TryGetValue(name, out var value) && value.Length > 0 ? value : null;

        public bool Flag(string name) => flags.Contains(name);
    }
}
