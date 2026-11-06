using Wotr.Contracts;

namespace Wotr.Domain;

// RANK-05 (R35, docs/RANKED_PLAY.md D1-D4): whether two ranked searches can meet, and the game they meet in. Each
// dimension is decided apart: the expansions, the faction and the time preset.
public static class RankedRules
{
    public const string IllegalSearch = "IllegalChoice";
    public const string ExpansionsUnavailable = "ExpansionsUnavailable";

    // Longest first: when several presets are shared, the longest is used, since running out of time loses.
    private static readonly RankedPreset[] Longest = [RankedPreset.Long, RankedPreset.Standard, RankedPreset.Fast];

    // A search the server accepts: defined values; a faction choice that is a preference or a requirement; at least
    // one preset not off and at most one enforced; nothing but Off for a module the player cannot have.
    public static string? Invalid(RankedSearchRequest search, IReadOnlyCollection<string> playable)
    {
        RankedChoice[] values = [search.Lords, search.Kings, search.Fate, search.Fast, search.Standard, search.Long];
        if (values.Any(value => !Enum.IsDefined(value)) || search.Faction is { } side && !Enum.IsDefined(side))
            return IllegalSearch;
        if (search.Faction is not null && search.FactionChoice == RankedChoice.Off || !Enum.IsDefined(search.FactionChoice))
            return IllegalSearch;
        var presets = Presets(search).Select(pair => pair.Choice).ToList();
        if (presets.All(choice => choice == RankedChoice.Off) || presets.Count(choice => choice == RankedChoice.Enforce) > 1)
            return IllegalSearch;
        return Modules(search).Any(pair => pair.Choice != RankedChoice.Off && !playable.Contains(pair.Module))
            ? ExpansionsUnavailable : null;
    }

    public static bool Compatible(RankedSearchRequest a, RankedSearchRequest b) =>
        ExpansionsAgree(a, b) && Preset(a, b) is not null && SideOptions(a, b).Count > 0;

    // D2: in when either enforces it or both allow it; a module enforced by one and off for the other forbids the pair.
    public static MatchConfiguration Configuration(RankedSearchRequest a, RankedSearchRequest b) =>
        ConfigurationRules.Normalize(new MatchConfiguration
        {
            Lords = InGame(a.Lords, b.Lords),
            Kings = InGame(a.Kings, b.Kings),
            Fate = InGame(a.Fate, b.Fate)
        });

    // D4: an enforced preset the other accepts; otherwise the longest both accept; none when nothing is shared or both
    // enforce different presets.
    public static RankedPreset? Preset(RankedSearchRequest a, RankedSearchRequest b)
    {
        var shared = Acceptable(a).Intersect(Acceptable(b)).ToHashSet();
        return Longest.Where(shared.Contains).Select(preset => (RankedPreset?)preset).FirstOrDefault();
    }

    // D3: the sides A may take, best first by how many stated preferences they meet. A requirement narrows the sides;
    // two requirements of the same faction leave none.
    public static IReadOnlyList<Side> SideOptions(RankedSearchRequest a, RankedSearchRequest b)
    {
        var valid = new[] { Side.FreePeoples, Side.Shadow }
            .Where(side => Allowed(a).Contains(side) && Allowed(b).Contains(TurnFlow.Other(side))).ToList();
        if (valid.Count == 0)
            return valid;
        var best = valid.Max(side => Met(a, b, side));
        return valid.Where(side => Met(a, b, side) == best).ToList();
    }

    // Whether A taking this side meets every preference either player stated.
    public static bool PreferencesMet(RankedSearchRequest a, RankedSearchRequest b, Side side) =>
        (a.Faction is null || a.Faction == side) && (b.Faction is null || b.Faction == TurnFlow.Other(side));

    private static int Met(RankedSearchRequest a, RankedSearchRequest b, Side side) =>
        (a.Faction == side ? 1 : 0) + (b.Faction == TurnFlow.Other(side) ? 1 : 0);

    private static Side[] Allowed(RankedSearchRequest search) =>
        search.Faction is { } side && search.FactionChoice == RankedChoice.Enforce ? [side] : [Side.FreePeoples, Side.Shadow];

    private static bool ExpansionsAgree(RankedSearchRequest a, RankedSearchRequest b) =>
        Modules(a).Zip(Modules(b)).All(pair => !Forbidden(pair.First.Choice, pair.Second.Choice));

    private static bool Forbidden(RankedChoice one, RankedChoice other) =>
        one == RankedChoice.Enforce && other == RankedChoice.Off || one == RankedChoice.Off && other == RankedChoice.Enforce;

    private static bool InGame(RankedChoice one, RankedChoice other) =>
        one == RankedChoice.Enforce || other == RankedChoice.Enforce || one == RankedChoice.Allow && other == RankedChoice.Allow;

    private static IEnumerable<RankedPreset> Acceptable(RankedSearchRequest search)
    {
        var presets = Presets(search).ToList();
        var enforced = presets.Where(pair => pair.Choice == RankedChoice.Enforce).Select(pair => pair.Preset).ToList();
        return enforced.Count > 0 ? enforced : presets.Where(pair => pair.Choice != RankedChoice.Off).Select(pair => pair.Preset);
    }

    private static IEnumerable<(RankedPreset Preset, RankedChoice Choice)> Presets(RankedSearchRequest search) =>
    [
        (RankedPreset.Fast, search.Fast), (RankedPreset.Standard, search.Standard), (RankedPreset.Long, search.Long)
    ];

    private static IEnumerable<(string Module, RankedChoice Choice)> Modules(RankedSearchRequest search) =>
    [
        (ExpansionModules.Lords, search.Lords), (ExpansionModules.Kings, search.Kings), (ExpansionModules.Fate, search.Fate)
    ];
}
