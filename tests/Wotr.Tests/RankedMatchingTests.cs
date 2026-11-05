using System.Collections.Concurrent;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

// RANK-05 (R35, docs/RANKED_PLAY.md D1-D5): a search's validation and the pairing order of D5.
public class RankedMatchingTests
{
    internal static readonly string[] All = [ExpansionModules.Lords, ExpansionModules.Kings, ExpansionModules.Fate];

    [Fact]
    public void ASearchIsValidatedBeforeItQueues()
    {
        Assert.Null(RankedRules.Invalid(new RankedSearchRequest(), All));
        Assert.Equal(RankedRules.IllegalSearch, RankedRules.Invalid(new RankedSearchRequest { Standard = RankedChoice.Off }, All));
        Assert.Equal(RankedRules.IllegalSearch, RankedRules.Invalid(new RankedSearchRequest
        {
            Fast = RankedChoice.Enforce, Standard = RankedChoice.Enforce
        }, All));
        Assert.Equal(RankedRules.IllegalSearch, RankedRules.Invalid(new RankedSearchRequest
        {
            Faction = Side.Shadow, FactionChoice = RankedChoice.Off
        }, All));
        Assert.Equal(RankedRules.IllegalSearch, RankedRules.Invalid(new RankedSearchRequest { Lords = (RankedChoice)7 }, All));
        Assert.Equal(RankedRules.ExpansionsUnavailable, RankedRules.Invalid(new RankedSearchRequest(), [ExpansionModules.Fate]));
        Assert.Null(RankedRules.Invalid(new RankedSearchRequest { Lords = RankedChoice.Off, Kings = RankedChoice.Off }, [ExpansionModules.Fate]));
    }

    [Theory]
    [InlineData(0, 150)]
    [InlineData(9, 150)]
    [InlineData(10, 200)]
    [InlineData(119, 700)]
    public void TheRatingWindowWidensWithTheWait(int seconds, double window) =>
        Assert.Equal(window, RankedMatching.Window(seconds));

    [Fact]
    public void AfterTwoMinutesAnyRatingMeets()
    {
        Assert.Equal(double.PositiveInfinity, RankedMatching.Window(120));
        var far = Pair(Ticket("a", 1500, 0), Ticket("b", 2400, 0));
        Assert.Empty(far);
        Assert.Single(Pair(Ticket("a", 1500, 120), Ticket("b", 2400, 0)));
    }

    // D5: both faction preferences met comes first, then the closest rating.
    [Fact]
    public void PreferencesComeBeforeTheClosestRating()
    {
        var chooser = Ticket("a", 1500, 30, Side.FreePeoples);
        var close = Ticket("b", 1510, 0, Side.FreePeoples);
        var farther = Ticket("c", 1600, 0, Side.Shadow);
        var pairing = Assert.Single(Pair(chooser, close, farther));
        Assert.Equal(("a", "c"), (pairing.FreePeoples.Account, pairing.Shadow.Account));
        var nearest = Assert.Single(Pair(Ticket("a", 1500, 30), Ticket("b", 1600, 0), Ticket("c", 1520, 0)));
        Assert.Contains(nearest.FreePeoples.Account == "a" ? nearest.Shadow.Account : nearest.FreePeoples.Account, new[] { "c" });
    }

    [Fact]
    public void TenCompatibleSearchesMakeFivePairsAndNoAccountPlaysTwice()
    {
        var tickets = Enumerable.Range(0, 10).Select(i => Ticket("p" + i, 1500 + i * 5, i)).ToArray();
        var pairings = Pair(tickets);
        Assert.Equal(5, pairings.Count);
        var accounts = pairings.SelectMany(p => new[] { p.FreePeoples.Account, p.Shadow.Account }).ToList();
        Assert.Equal(10, accounts.Distinct().Count());
    }

    [Fact]
    public void IncompatibleSearchesNeverMeetHoweverLongTheyWait()
    {
        var lordsOnly = Ticket("a", 1500, 600);
        lordsOnly.Search.Lords = RankedChoice.Enforce;
        var noLords = Ticket("b", 1500, 600);
        noLords.Search.Lords = RankedChoice.Off;
        Assert.Empty(Pair(lordsOnly, noLords));
    }

    private static RankedTicket Ticket(string account, double rating, int wait, Side? faction = null) =>
        new(account, new RankedSearchRequest { Faction = faction }, rating, wait);

    private static List<RankedPairing> Pair(params RankedTicket[] tickets) =>
        RankedMatching.Pair(tickets, new BaseCorpusFixture.ZeroRandom());
}

// A test in this collection runs with no other beside it. Taking every core for seconds starves the thread pool, and
// other tests' hosts then time out connecting to their database (RANK-06: 3 of 6 runs of the ranked group failed so).
[CollectionDefinition(nameof(HeavyCpuTests), DisableParallelization = true)]
public sealed class HeavyCpuTests;

// RANK-05 (R35, docs/RANKED_PLAY.md D2-D4): every pair of valid searches against an oracle written from the rules' text
// in another form.
[Collection(nameof(HeavyCpuTests))]
public class RankedMatchingExhaustiveTests
{
    [Fact]
    public void EveryPairOfSearchesAgreesWithTheRulesAndNeitherOrderMatters()
    {
        var searches = ValidSearches().ToList();
        Assert.Equal(27 * 19 * 5, searches.Count);
        var failures = new ConcurrentBag<string>();
        Parallel.ForEach(searches, a =>
        {
            foreach (var b in searches)
            {
                var problem = Disagreement(a, b);
                if (problem is not null && failures.Count < 10)
                    failures.Add(problem + " for " + Describe(a) + " / " + Describe(b));
            }
        });
        Assert.Empty(failures);
    }

    // Returns null when the domain agrees with the oracle and is symmetric for this pair.
    private static string? Disagreement(RankedSearchRequest a, RankedSearchRequest b)
    {
        var compatible = RankedRules.Compatible(a, b);
        if (compatible != RankedRules.Compatible(b, a))
            return "asymmetric compatibility";
        var expansions = OracleExpansions(a, b);
        var preset = OraclePreset(a, b);
        var sides = OracleSides(a, b);
        var expected = expansions is not null && preset is not null && sides.Count > 0;
        if (compatible != expected)
            return $"compatible {compatible}, oracle {expected}";
        if (!compatible)
            return null;
        var configuration = RankedRules.Configuration(a, b);
        if ((configuration.Lords, configuration.Kings, configuration.Fate) != expansions!.Value)
            return "configuration";
        if (RankedRules.Configuration(b, a) is var mirrored && (mirrored.Lords, mirrored.Kings, mirrored.Fate) != expansions.Value)
            return "asymmetric configuration";
        if (configuration.Council || configuration.VisibleCorruption || configuration.SequentialCorruption ||
            configuration.RemovingRulerTokens || configuration.NewCities)
            return "a variant";
        if (RankedRules.Preset(a, b) != preset || RankedRules.Preset(b, a) != preset)
            return "preset";
        var options = RankedRules.SideOptions(a, b).Order().ToList();
        if (!options.SequenceEqual(sides.Order()))
            return "sides";
        if (!RankedRules.SideOptions(b, a).Select(TurnFlow.Other).Order().SequenceEqual(options))
            return "asymmetric sides";
        return null;
    }

    // D2 as sets: each player's acceptable values of a module; a pair needs a shared value, and the module is in when
    // being in is shared.
    private static (bool Lords, bool Kings, bool Fate)? OracleExpansions(RankedSearchRequest a, RankedSearchRequest b)
    {
        bool? Module(RankedChoice x, RankedChoice y)
        {
            var shared = Values(x).Intersect(Values(y)).ToList();
            return shared.Count == 0 ? null : shared.Contains(true);
        }
        var (lords, kings, fate) = (Module(a.Lords, b.Lords), Module(a.Kings, b.Kings), Module(a.Fate, b.Fate));
        return lords is null || kings is null || fate is null ? null : (lords.Value, kings.Value, fate.Value);
    }

    private static bool[] Values(RankedChoice choice) => choice switch
    {
        RankedChoice.Off => [false],
        RankedChoice.Enforce => [true],
        _ => [false, true]
    };

    // D4 as written: two different enforced presets meet nowhere; an enforced one is used if the other accepts it;
    // otherwise the longest shared.
    private static RankedPreset? OraclePreset(RankedSearchRequest a, RankedSearchRequest b)
    {
        var (ea, eb) = (Enforced(a), Enforced(b));
        if (ea is not null && eb is not null && ea != eb)
            return null;
        if (ea is not null)
            return Accepts(b, ea.Value) ? ea : null;
        if (eb is not null)
            return Accepts(a, eb.Value) ? eb : null;
        var minutes = new Dictionary<RankedPreset, int> { [RankedPreset.Fast] = 90, [RankedPreset.Standard] = 180, [RankedPreset.Long] = 300 };
        var shared = minutes.Keys.Where(p => Accepts(a, p) && Accepts(b, p)).OrderByDescending(p => minutes[p]).ToList();
        return shared.Count == 0 ? null : shared[0];
    }

    private static RankedPreset? Enforced(RankedSearchRequest s) =>
        s.Fast == RankedChoice.Enforce ? RankedPreset.Fast : s.Standard == RankedChoice.Enforce ? RankedPreset.Standard
        : s.Long == RankedChoice.Enforce ? RankedPreset.Long : null;

    private static bool Accepts(RankedSearchRequest s, RankedPreset preset) => preset switch
    {
        RankedPreset.Fast => s.Fast != RankedChoice.Off,
        RankedPreset.Standard => s.Standard != RankedChoice.Off,
        _ => s.Long != RankedChoice.Off
    };

    // D3 as written: two requirements of one faction meet nowhere; a requirement decides; then preferences; a tie or no
    // preference at all is the server's draw (both sides offered).
    private static List<Side> OracleSides(RankedSearchRequest a, RankedSearchRequest b)
    {
        var ra = a.FactionChoice == RankedChoice.Enforce ? a.Faction : null;
        var rb = b.FactionChoice == RankedChoice.Enforce ? b.Faction : null;
        if (ra is not null && ra == rb)
            return [];
        if (ra is not null)
            return [ra.Value];
        if (rb is not null)
            return [TurnFlow.Other(rb.Value)];
        var (pa, pb) = (a.Faction, b.Faction);
        if (pa is not null && pb is not null)
            return pa == pb ? [Side.FreePeoples, Side.Shadow] : [pa.Value];
        if (pa is not null)
            return [pa.Value];
        if (pb is not null)
            return [TurnFlow.Other(pb.Value)];
        return [Side.FreePeoples, Side.Shadow];
    }

    private static IEnumerable<RankedSearchRequest> ValidSearches()
    {
        var choices = Enum.GetValues<RankedChoice>();
        var factions = new (Side?, RankedChoice)[]
        {
            (null, RankedChoice.Allow), (Side.FreePeoples, RankedChoice.Allow), (Side.FreePeoples, RankedChoice.Enforce),
            (Side.Shadow, RankedChoice.Allow), (Side.Shadow, RankedChoice.Enforce)
        };
        foreach (var lords in choices)
            foreach (var kings in choices)
                foreach (var fate in choices)
                    foreach (var fast in choices)
                        foreach (var standard in choices)
                            foreach (var slow in choices)
                                foreach (var (faction, choice) in factions)
                                {
                                    var search = new RankedSearchRequest
                                    {
                                        Lords = lords, Kings = kings, Fate = fate, Fast = fast, Standard = standard, Long = slow,
                                        Faction = faction, FactionChoice = choice
                                    };
                                    if (RankedRules.Invalid(search, RankedMatchingTests.All) is null)
                                        yield return search;
                                }
    }

    private static string Describe(RankedSearchRequest s) =>
        $"[{s.Lords} {s.Kings} {s.Fate} | {s.Faction} {s.FactionChoice} | {s.Fast} {s.Standard} {s.Long}]";
}
