using Wotr.Contracts;

namespace Wotr.Domain;

// There are at most three recruitment slots, not combinations of physical reserve figures.
internal sealed class SovereignRecruitPlan
{
    private sealed record Slot(string Region, UnitKind[] Kinds);
    private static readonly UnitKind[] RegularOrLeader = [UnitKind.Regular, UnitKind.Leader];
    private static readonly UnitKind[] RegularOrElite = [UnitKind.Regular, UnitKind.Elite];
    private readonly List<Slot[]> assignments;
    private readonly Dictionary<string, UnitState> reserve;
    public int Maximum { get; }

    public SovereignRecruitPlan(MatchState state, CharacterState character)
    {
        var nation = SovereignCatalog.All[character.Id].Nation;
        reserve = state.Units.Where(u => u.Nation == nation && u.Location == "reserve").ToDictionary(u => u.Id);
        var settlements = BoardRules.CurrentRegions(state).Where(r => r.Nation == nation && BoardRules.IsSettlement(r) &&
            KingsRulerAbilities.FreeRegion(state, r.Id)).Select(r => r.Id).ToArray();
        assignments = character.Id == SovereignCatalog.Dain
            ? IronGuard(state, character, settlements) : CallToArms(state, character, settlements);
        var counts = reserve.Values.GroupBy(u => u.Kind).ToDictionary(g => g.Key, g => g.Count());
        Maximum = assignments.Max(slots => Capacity(slots, counts));
    }

    private static Slot CreateSlot(MatchState state, CharacterState character, string region, UnitKind[] kinds) =>
        new(region, kinds.Where(kind => !KingsWeaknesses.BlocksRecruit(state,
            new() { Nation = SovereignCatalog.All[character.Id].Nation, Kind = kind }, region)).ToArray());

    private static List<Slot[]> IronGuard(MatchState state, CharacterState character, string[] settlements)
    {
        var local = KingsRulerAbilities.FreeRegion(state, character.Location) || BoardRules.Besieged(state, character.Location, Side.FreePeoples)
            ? new[] { CreateSlot(state, character, character.Location, RegularOrElite) } : [];
        var others = settlements.Where(r => r != character.Location).ToArray();
        return others.Length == 0 ? [local] : others.Select(r => local.Append(CreateSlot(state, character, r, RegularOrLeader)).ToArray()).ToList();
    }

    private static List<Slot[]> CallToArms(MatchState state, CharacterState character, string[] settlements)
    {
        var local = KingsRulerAbilities.FreeRegion(state, character.Location)
            ? new[] { CreateSlot(state, character, character.Location, RegularOrLeader) } : [];
        if (settlements.Length < 2)
            return [settlements.Select(r => CreateSlot(state, character, r, RegularOrLeader)).Concat(local).ToArray()];
        var result = new List<Slot[]>();
        for (var first = 0; first < settlements.Length; first++)
            for (var second = first + 1; second < settlements.Length; second++)
                result.Add([CreateSlot(state, character, settlements[first], RegularOrLeader),
                    CreateSlot(state, character, settlements[second], RegularOrLeader), .. local]);
        return result;
    }

    private static int Capacity(Slot[] slots, Dictionary<UnitKind, int> counts, int index = 0)
    {
        if (index == slots.Length)
            return 0;
        var best = Capacity(slots, counts, index + 1);
        foreach (var kind in slots[index].Kinds.Where(k => counts.GetValueOrDefault(k) > 0))
        {
            counts[kind]--;
            best = Math.Max(best, 1 + Capacity(slots, counts, index + 1));
            counts[kind]++;
        }
        return best;
    }

    public bool Valid(IReadOnlyList<UnitPlacementChoice> placements)
    {
        if (Maximum == 0 || placements.Count != Maximum || placements.Select(p => p.UnitId).Distinct().Count() != placements.Count ||
            placements.Any(p => string.IsNullOrEmpty(p.UnitId) || string.IsNullOrEmpty(p.RegionId) || !reserve.ContainsKey(p.UnitId)))
            return false;
        return assignments.Any(slots => Fits(slots, placements));
    }

    private bool Fits(Slot[] slots, IReadOnlyList<UnitPlacementChoice> placements, int index = 0, int used = 0)
    {
        if (index == placements.Count)
            return true;
        var placement = placements[index];
        for (var slot = 0; slot < slots.Length; slot++)
            if ((used & 1 << slot) == 0 && slots[slot].Region == placement.RegionId && slots[slot].Kinds.Contains(reserve[placement.UnitId].Kind) &&
                Fits(slots, placements, index + 1, used | 1 << slot))
                return true;
        return false;
    }

    public List<RecruitmentOption> Options() => reserve.Values.GroupBy(u => (u.Nation, u.Kind)).Select(group => new RecruitmentOption
    {
        Nation = group.Key.Nation, Kind = group.Key.Kind, ReserveIds = group.Select(u => u.Id).ToList(),
        Targets = assignments.SelectMany(a => a).Where(s => s.Kinds.Contains(group.Key.Kind)).Select(s => s.Region).Distinct().ToList()
    }).Where(o => o.Targets.Count > 0).ToList();
}
