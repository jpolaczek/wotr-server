using Wotr.Contracts;

namespace Wotr.Domain;

// A live search: the account, its choices, its rating and how long it has waited.
public sealed record RankedTicket(string Account, RankedSearchRequest Search, double Rating, int WaitSeconds);

// A pair the server starts at once: who plays which side, in which game, on which clock.
public sealed record RankedPairing(RankedTicket FreePeoples, RankedTicket Shadow, MatchConfiguration Configuration,
    RankedPreset Preset);

// RANK-05 (R35, docs/RANKED_PLAY.md D5): pairs the live searches. The longest wait chooses first; it takes the
// compatible partner whose pairing meets both faction preferences, then the closest rating, within a window that
// widens with the wait (150 points, plus 50 per 10 s, unlimited after 2 minutes, as W3Champions loosens its range).
// Compatibility itself is never loosened.
public static class RankedMatching
{
    public const double BaseWindow = 150;
    public const double WindowStep = 50;
    public const int WindowStepSeconds = 10;
    public const int UnlimitedAfterSeconds = 120;

    public static double Window(int waitSeconds) => waitSeconds >= UnlimitedAfterSeconds
        ? double.PositiveInfinity
        : BaseWindow + WindowStep * (Math.Max(0, waitSeconds) / WindowStepSeconds);

    public static List<RankedPairing> Pair(IReadOnlyList<RankedTicket> tickets, IRandomSource random)
    {
        var waiting = tickets.OrderByDescending(t => t.WaitSeconds).ThenBy(t => t.Account, StringComparer.Ordinal).ToList();
        var taken = new HashSet<string>(StringComparer.Ordinal);
        var pairings = new List<RankedPairing>();
        foreach (var ticket in waiting)
        {
            if (taken.Contains(ticket.Account))
                continue;
            var partner = Partner(ticket, waiting.Where(other => !taken.Contains(other.Account)));
            if (partner is null)
                continue;
            taken.Add(ticket.Account);
            taken.Add(partner.Account);
            pairings.Add(Pairing(ticket, partner, random));
        }
        return pairings;
    }

    // The game two compatible searches meet in; a tie between sides is the server's draw.
    public static RankedPairing Pairing(RankedTicket a, RankedTicket b, IRandomSource random)
    {
        var options = RankedRules.SideOptions(a.Search, b.Search);
        var side = options.Count == 1 ? options[0] : options[random.Next(options.Count)];
        var (freePeoples, shadow) = side == Side.FreePeoples ? (a, b) : (b, a);
        return new(freePeoples, shadow, RankedRules.Configuration(a.Search, b.Search), RankedRules.Preset(a.Search, b.Search)!.Value);
    }

    private static RankedTicket? Partner(RankedTicket ticket, IEnumerable<RankedTicket> others) =>
        others.Where(other => other.Account != ticket.Account && Reaches(ticket, other) &&
                              RankedRules.Compatible(ticket.Search, other.Search))
            .OrderByDescending(other => PreferencesMet(ticket, other))
            .ThenBy(other => Math.Abs(other.Rating - ticket.Rating))
            .ThenByDescending(other => other.WaitSeconds)
            .ThenBy(other => other.Account, StringComparer.Ordinal)
            .FirstOrDefault();

    private static bool Reaches(RankedTicket a, RankedTicket b) =>
        Math.Abs(a.Rating - b.Rating) <= Window(Math.Max(a.WaitSeconds, b.WaitSeconds));

    private static bool PreferencesMet(RankedTicket a, RankedTicket b) =>
        RankedRules.SideOptions(a.Search, b.Search).Any(side => RankedRules.PreferencesMet(a.Search, b.Search, side));
}
