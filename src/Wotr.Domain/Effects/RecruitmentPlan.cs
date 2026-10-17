using Wotr.Contracts;

namespace Wotr.Domain;

internal sealed class RecruitmentPlan
{
    public IReadOnlyList<RecruitmentQuota> Quotas { get; }
    public IReadOnlyList<UnitState> Units { get; }
    public int Maximum { get; }
    private readonly int maximumRegions;
    private readonly int distinctNations;
    private readonly Dictionary<string, UnitState> unitsById;
    private readonly Dictionary<(string Nation, string Region, UnitKind Kind), RecruitmentQuota> quotasByTarget;

    public RecruitmentPlan(MatchState state, Side owner, IReadOnlyList<RecruitmentQuota> quotas, int maximumRegions, int distinctNations = 0)
    {
        RecruitmentCapacity.Validate(quotas, maximumRegions, distinctNations);
        this.maximumRegions = maximumRegions;
        this.distinctNations = distinctNations;
        Quotas = quotas.Where(q => RecruitmentRules.EventGarrison(state, owner, q.Region) is not null).ToArray();
        var kinds = Quotas.SelectMany(q => q.Kinds.Select(kind => (q.Nation, kind))).ToHashSet();
        var reserve = state.Units.Where(u => u.Location == "reserve" && BoardRules.Owner(u) == owner && kinds.Contains((u.Nation, u.Kind))).ToArray();
        Maximum = RecruitmentCapacity.Maximum(Quotas, reserve, maximumRegions, distinctNations);
        Units = reserve.GroupBy(u => (u.Nation, u.Kind)).SelectMany(g => g.Take(Math.Min(distinctNations > 0 ? 1 : Maximum,
            Quotas.Where(q => q.Nation == g.Key.Nation && q.Kinds.Contains(g.Key.Kind)).Sum(q => q.Maximum)))).ToArray();
        unitsById = Units.ToDictionary(u => u.Id);
        quotasByTarget = Quotas.SelectMany(q => q.Kinds.Select(kind => (Key: (q.Nation, q.Region, kind), Quota: q)))
            .ToDictionary(pair => pair.Key, pair => pair.Quota);
    }

    public bool Valid(IReadOnlyList<UnitPlacementChoice> placements)
    {
        if (placements.Count != Maximum || placements.Select(p => p.UnitId).Distinct().Count() != placements.Count ||
            maximumRegions > 0 && placements.Select(p => p.RegionId).Distinct().Count() > maximumRegions)
            return false;
        var used = new Dictionary<RecruitmentQuota, int>();
        var nations = new HashSet<string>();
        foreach (var placement in placements)
        {
            if (!unitsById.TryGetValue(placement.UnitId, out var unit) ||
                !quotasByTarget.TryGetValue((unit.Nation, placement.RegionId, unit.Kind), out var quota))
                return false;
            if (distinctNations > 0 && !nations.Add(unit.Nation))
                return false;
            var count = used.GetValueOrDefault(quota) + 1;
            if (count > quota.Maximum)
                return false;
            used[quota] = count;
        }
        return true;
    }

    public List<RecruitmentOption> Options() => Units.GroupBy(u => (u.Nation, u.Kind)).Select(g => new RecruitmentOption
    {
        Nation = g.Key.Nation,
        Kind = g.Key.Kind,
        ReserveIds = g.Select(u => u.Id).ToList(),
        Targets = Quotas.Where(q => q.Nation == g.Key.Nation && q.Kinds.Contains(g.Key.Kind)).Select(q => q.Region).Distinct().ToList()
    }).ToList();

    public List<UnitPlacementChoice>? Automatic()
    {
        var groups = Options();
        if (groups.All(g => g.Targets.Count == 1) && Units.Count == Maximum)
            return groups.SelectMany(g => g.ReserveIds.Select(id => new UnitPlacementChoice { UnitId = id, RegionId = g.Targets[0] })).ToList();
        if (Quotas.Any(q => q.Kinds.Count != 1) || Maximum != Quotas.Sum(q => q.Maximum) ||
            maximumRegions > 0 && Quotas.Select(q => q.Region).Distinct().Count() > maximumRegions)
            return null;
        var reserve = Units.GroupBy(u => (u.Nation, u.Kind)).ToDictionary(g => g.Key, g => new Queue<UnitState>(g));
        return Quotas.SelectMany(q => Enumerable.Range(0, q.Maximum).Select(_ => new UnitPlacementChoice
        {
            UnitId = reserve[(q.Nation, q.Kinds[0])].Dequeue().Id,
            RegionId = q.Region
        })).ToList();
    }
}
