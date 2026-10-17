using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class DraftSelection
{
    // Linear selection validation; never generate 2^N combinations for an army or hand.
    public static bool IsSubset(IReadOnlyCollection<string> selected, IReadOnlyCollection<string> candidates, int minimum, int maximum)
    {
        if (selected.Count < minimum || selected.Count > maximum)
            return false;
        var set = candidates.ToHashSet(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return selected.All(id => set.Contains(id) && seen.Add(id));
    }
    public static bool CostMatches(string dieId, IReadOnlyCollection<string> eligibleDice) => eligibleDice.Contains(dieId, StringComparer.Ordinal);
}
