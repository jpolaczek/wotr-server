using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

// UI-96: what a release offers for new tables. Production (the live MVP) offers only the base game;
// the expansion code stays and other environments or Wotr__Release__Expansions=true enable it.
public sealed class ReleaseScope(bool expansions)
{
    public const string ConfigurationKey = "Wotr:Release:Expansions";
    public static readonly ReleaseScope Full = new(true);
    public static readonly ReleaseScope BaseOnly = new(false);
    public bool Expansions { get; } = expansions;

    public static ReleaseScope Read(IConfiguration configuration, IHostEnvironment environment) =>
        new(configuration.GetValue<bool?>(ConfigurationKey) ?? !environment.IsProduction());

    public bool Offers(MatchConfiguration configuration) => Expansions || ConfigurationRules.IsBase(configuration);
}
