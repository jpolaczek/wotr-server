using Wotr.Contracts;

namespace Wotr.Server;

// EXP-02: the first client version that plays each expansion module. A client older than a module of a table's
// configuration is refused at that table with ClientUpdateRequired, the answer its updater already turns into a
// required update; the base game and the other tables stay open to it. Wotr:Release:ModuleClients
// (Wotr__Release__ModuleClients) pairs modules with versions, e.g. "fate=0.10.0, lords=0.11.0"; a module without a
// version is open to every client. Production refuses to start when a released module has no version.
public sealed class ModuleClientPolicy
{
    public const string Key = "Wotr:Release:ModuleClients";
    private static readonly char[] Separators = [',', ';', ' '];
    public static readonly ModuleClientPolicy Open = new(new Dictionary<string, ClientVersion>());
    private readonly IReadOnlyDictionary<string, ClientVersion> firstVersions;

    public ModuleClientPolicy(IReadOnlyDictionary<string, ClientVersion> firstVersions) =>
        this.firstVersions = firstVersions;

    // The modules only some client versions play; a client that plays all of them needs no table check.
    public bool Restricts => firstVersions.Count > 0;

    public static ModuleClientPolicy Read(IConfiguration configuration, IHostEnvironment environment, ReleaseScope release)
    {
        var policy = new ModuleClientPolicy(Parse(configuration[Key] ?? ""));
        if (environment.IsProduction())
            policy.RequireVersionsFor(release.Modules);
        return policy;
    }

    // "fate=0.10.0, lords=0.11.0": a module the server does not know, a malformed version or a repeated module is a
    // broken release, not a missing restriction.
    public static IReadOnlyDictionary<string, ClientVersion> Parse(string value)
    {
        var versions = new Dictionary<string, ClientVersion>(StringComparer.Ordinal);
        foreach (var pair in value.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var (module, version) = Pair(pair);
            if (!versions.TryAdd(module, version))
                throw new InvalidOperationException($"{Key} names the module '{module}' twice.");
        }
        return versions;
    }

    // The modules this client build does not play: those whose first version is newer than it. A client without a
    // version (accepted only outside Production) plays only the unrestricted modules.
    public IReadOnlySet<string> Unplayable(string? clientVersion)
    {
        var known = ClientVersion.TryParse(clientVersion, out var client);
        return firstVersions.Where(pair => !known || client!.CompareTo(pair.Value) < 0)
            .Select(pair => pair.Key)
            .ToHashSet(StringComparer.Ordinal);
    }

    public static bool Plays(IReadOnlySet<string> unplayable, MatchConfiguration configuration) =>
        !ExpansionModules.Of(configuration).Any(unplayable.Contains);

    private void RequireVersionsFor(IEnumerable<string> released)
    {
        var missing = released.FirstOrDefault(module => !firstVersions.ContainsKey(module));
        if (missing is not null)
            throw new InvalidOperationException(
                $"{Key} names no first client version for the released module '{missing}', e.g. {missing}=0.10.0.");
    }

    private static (string Module, ClientVersion Version) Pair(string pair)
    {
        var parts = pair.Split('=', 2, StringSplitOptions.TrimEntries);
        var module = parts[0].ToLowerInvariant();
        if (parts.Length != 2 || !ExpansionModules.IsKnown(module))
            throw new InvalidOperationException($"{Key} has '{pair}'; write module=version with fate, lords or kings.");
        if (!ClientVersion.TryParse(parts[1], out var version))
            throw new InvalidOperationException($"{Key} has '{pair}'; the version is MAJOR.MINOR.PATCH, e.g. 0.10.0.");
        return (module, version);
    }
}
