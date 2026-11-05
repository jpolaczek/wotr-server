using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

// UI-96: what a release offers for new tables; the expansion code stays whatever the release offers.
// EXP-01 (owner decision 2026-10-19): the expansions are released one by one, Fate, then Lords, then Kings.
// Wotr:Release:Modules (Wotr__Release__Modules) names the released modules, separated by commas or spaces, in any
// case; an unknown name stops the server at start. The older Wotr:Release:Expansions stays as an alias: true releases
// every module, false none. Modules wins when both are set, and an empty Modules counts as unset (compose passes it
// empty by default). Without either, Production releases no module and other environments release all.
public sealed class ReleaseScope
{
    public const string ModulesKey = "Wotr:Release:Modules";
    public const string ExpansionsKey = "Wotr:Release:Expansions";
    private static readonly char[] Separators = [',', ';', ' '];
    public static readonly ReleaseScope Full = new(ExpansionModules.All);
    public static readonly ReleaseScope BaseOnly = new([]);
    private readonly HashSet<string> released;

    public ReleaseScope(IEnumerable<string> modules)
    {
        released = modules.ToHashSet(StringComparer.Ordinal);
        Modules = ExpansionModules.All.Where(released.Contains).ToList();
    }

    // The released modules (ExpansionModules ids) in release order.
    public IReadOnlyList<string> Modules { get; }

    // Some expansion is released: RoomView.ExpansionsAvailable for clients from before EXP-01.
    public bool OffersExpansions => Modules.Count > 0;

    public static ReleaseScope Read(IConfiguration configuration, IHostEnvironment environment)
    {
        var modules = configuration[ModulesKey];
        if (!string.IsNullOrWhiteSpace(modules))
            return new ReleaseScope(Parse(modules));
        var expansions = configuration.GetValue<bool?>(ExpansionsKey) ?? !environment.IsProduction();
        return expansions ? Full : BaseOnly;
    }

    // EXP-01: module names in any case; a name the server does not know is a broken release, not an empty one.
    public static IReadOnlyList<string> Parse(string value)
    {
        var names = value.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(name => name.ToLowerInvariant())
            .ToList();
        var unknown = names.Find(name => !ExpansionModules.IsKnown(name));
        if (unknown is not null)
            throw new InvalidOperationException(
                $"{ModulesKey} lists the unknown module '{unknown}'; name fate, lords or kings, separated by commas.");
        return names;
    }

    // EXP-01: the base game is always offered, and a module only once it is released. A variant needs its module,
    // which ConfigurationRules.Conflict enforces, so a configuration it refuses is never offered.
    public bool Offers(MatchConfiguration configuration) =>
        ConfigurationRules.IsValid(configuration) && Offers(ExpansionModules.Of(configuration));

    // TEST-10: a test scenario, by the modules it plays with.
    public bool Offers(IEnumerable<string> modules) => modules.All(released.Contains);
}
