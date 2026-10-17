using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record RecruitmentQuota(string Nation, string Region, IReadOnlyList<UnitKind> Kinds, int Maximum);

internal static class RecruitmentCapacity
{
    public static void Validate(IReadOnlyList<RecruitmentQuota> quotas, int maximumRegions, int distinctNations = 0)
    {
        if (distinctNations < 0 || distinctNations > 0 && (maximumRegions != 0 ||
            quotas.Any(q => q.Maximum != 1 || q.Kinds.Any(k => k is not (UnitKind.Regular or UnitKind.Leader)))))
            throw new ArgumentException("Distinct nations require one Regular/Leader per nation and no region limit.");
        if (maximumRegions < 0 || quotas.Any(q => q.Maximum < 1 || q.Kinds.Count == 0 || q.Kinds.Distinct().Count() != q.Kinds.Count ||
            distinctNations == 0 && q.Kinds.Count > 1 && !q.Kinds.Order().SequenceEqual(new[] { UnitKind.Regular, UnitKind.Elite }.Order())))
            throw new ArgumentException("Recruitment quotas require positive limits and a fixed kind or Regular/Elite choice.");
        if (quotas.SelectMany(q => q.Kinds.Select(k => (q.Nation, q.Region, k))).GroupBy(k => k).Any(g => g.Count() > 1))
            throw new ArgumentException("Recruitment quotas must not overlap in the same region.");
        if (maximumRegions > 1 && quotas.Any(q => q.Kinds.Count != 1 || q.Nation != quotas[0].Nation ||
            q.Kinds[0] != quotas[0].Kinds[0] || q.Maximum != quotas[0].Maximum))
            throw new ArgumentException("A multiple-region limit requires equal quotas of one nation and kind.");
    }

    public static int Maximum(IReadOnlyList<RecruitmentQuota> quotas, IReadOnlyList<UnitState> reserve, int maximumRegions, int distinctNations = 0)
    {
        if (quotas.Count == 0)
            return 0;
        if (distinctNations > 0)
            return Math.Min(distinctNations, reserve.Select(u => u.Nation).Distinct().Count());
        if (maximumRegions == 1)
            return quotas.GroupBy(q => q.Region).Max(group => Unrestricted(group.ToArray(), reserve));
        if (maximumRegions > 1)
            return Math.Min(reserve.Count, Math.Min(maximumRegions, quotas.Count) * quotas[0].Maximum);
        return Unrestricted(quotas, reserve);
    }

    private static int Unrestricted(IReadOnlyList<RecruitmentQuota> quotas, IReadOnlyList<UnitState> reserve)
    {
        var available = reserve.GroupBy(u => (u.Nation, u.Kind)).ToDictionary(g => g.Key, g => g.Count());
        var count = 0;
        foreach (var group in quotas.Where(q => q.Kinds.Count == 1).GroupBy(q => (q.Nation, Kind: q.Kinds[0])))
        {
            var taken = Math.Min(group.Sum(q => q.Maximum), available.GetValueOrDefault(group.Key));
            available[group.Key] = available.GetValueOrDefault(group.Key) - taken;
            count += taken;
        }
        foreach (var group in quotas.Where(q => q.Kinds.Count > 1).GroupBy(q => q.Nation))
            count += Math.Min(group.Sum(q => q.Maximum), available.GetValueOrDefault((group.Key, UnitKind.Regular)) +
                available.GetValueOrDefault((group.Key, UnitKind.Elite)));
        return count;
    }
}
