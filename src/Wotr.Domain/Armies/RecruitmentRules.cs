using Wotr.Contracts;

namespace Wotr.Domain;

// Only trusted effect handlers choose exceptions. These values never come from a command payload.
public sealed record RecruitmentPolicy(bool RequireAtWar = true, bool RequireNationalSettlement = true,
    bool AllowBesiegedStronghold = false, bool AllowEnemyControl = false)
{
    public static RecruitmentPolicy Muster { get; } = new();
}

// UI-129: codes of RecruitmentRules.Blocker, also the condition codes of the recruit availability entries.
public static class RecruitmentBlock
{
    public const string Reserve = "reserve";
    public const string AtWar = "at-war";
    public const string Settlement = "settlement";
    public const string NazgulStronghold = "nazgul-stronghold";
    public const string Captured = "captured";
    public const string Besieged = "besieged";
    public const string EnemyArmy = "enemy-army";
    public const string KingsWeakness = "kings-weakness";
    public const string UsedThisAction = "used-this-action";
    public const string EliteBudget = "elite-budget";
}

public static class RecruitmentRules
{
    public static bool? EventGarrison(MatchState state, Side actor, string region)
    {
        if (!BoardRules.Regions.ContainsKey(region))
            return null;
        if (BoardRules.Besieged(state, region, actor))
            return true;
        if (BoardRules.HasEnemyArmy(state, region, actor, false))
            return null;
        var friendlyBesiegers = state.Units.Any(u => u.Location == region && !u.Garrison &&
            BoardRules.IsArmyUnit(u) && BoardRules.Owner(u) == actor);
        if (friendlyBesiegers && BoardRules.HasEnemyArmy(state, region, actor, true))
            return false;
        return !BoardRules.IsSettlement(BoardRules.Region(state, region)) || state.Control.GetValueOrDefault(region) == actor ? false : null;
    }

    public static bool CanPlace(MatchState state, Side actor, UnitState unit, string target, RecruitmentPolicy policy) =>
        Blocker(state, actor, unit, target, policy) is null;

    // UI-129: the first rule that keeps a reserve figure out of a target, or null when it may be placed. CanPlace
    // and the availability projection share it, so the rule and its explanation cannot drift apart.
    public static string? Blocker(MatchState state, Side actor, UnitState unit, string target, RecruitmentPolicy policy)
    {
        if (unit.Location != "reserve" || BoardRules.Owner(unit) != actor || string.IsNullOrEmpty(target) ||
            !BoardRules.Regions.ContainsKey(target))
            return RecruitmentBlock.Reserve;
        var region = BoardRules.Region(state, target);
        if (policy.RequireAtWar && state.Nations.Single(n => n.Id == unit.Nation).StepsToWar != 0)
            return RecruitmentBlock.AtWar;
        if (policy.RequireNationalSettlement && (!BoardRules.IsSettlement(region) || region.Nation != unit.Nation))
            return RecruitmentBlock.Settlement;
        if (policy.RequireNationalSettlement && unit.Kind == UnitKind.Nazgul && region.Settlement != "Stronghold")
            return RecruitmentBlock.NazgulStronghold;
        if (!policy.AllowEnemyControl && state.Control.GetValueOrDefault(target) == TurnFlow.Other(actor))
            return RecruitmentBlock.Captured;
        if (BoardRules.Besieged(state, target, actor))
            return policy.AllowBesiegedStronghold || LordsCharacters.CanMusterInSiege(state, actor, target)
                ? null : RecruitmentBlock.Besieged;
        return BoardRules.HasEnemyArmy(state, target, actor) ? RecruitmentBlock.EnemyArmy : null;
    }

    public static bool CanPlaceMuster(MatchState state, Side actor, UnitState unit, string target) =>
        MusterBlocker(state, actor, unit, target) is null;

    public static string? MusterBlocker(MatchState state, Side actor, UnitState unit, string target) =>
        Blocker(state, actor, unit, target, RecruitmentPolicy.Muster) ??
        (KingsWeaknesses.BlocksRecruit(state, unit, target) ? RecruitmentBlock.KingsWeakness : null);

    public static string? ValidateMuster(MatchState state, Side actor, IReadOnlyList<UnitPlacementChoice> placements)
    {
        if (placements.Count is < 1 or > 2 || placements.Select(p => p.UnitId).Distinct().Count() != placements.Count ||
            placements.Select(p => p.RegionId).Distinct().Count() != placements.Count)
            return "IllegalChoice";
        foreach (var choice in placements)
        {
            var unit = state.Units.Find(u => u.Id == choice.UnitId);
            if (unit == null || !CanPlaceMuster(state, actor, unit, choice.RegionId) ||
                placements.Count > 1 && unit.Kind == UnitKind.Elite)
                return "IllegalChoice";
        }
        return null;
    }

    public static void Place(MatchState state, Side actor, IReadOnlyList<UnitPlacementChoice> placements)
    {
        foreach (var choice in placements)
        {
            var unit = state.Units.Single(u => u.Id == choice.UnitId);
            unit.Location = choice.RegionId;
            unit.Garrison = BoardRules.Besieged(state, choice.RegionId, actor);
            state.Events.Add(new()
            {
                Revision = state.Revision,
                Kind = "UnitRecruited",
                Activity = HistoryFacts.Unit(state, unit),
                Text = $"{unit.Nation} {unit.Kind} recruited in {choice.RegionId}."
            });
        }
        foreach (var unit in state.Units.Where(u => u.Kind == UnitKind.Leader && placements.Any(p => p.UnitId == u.Id)))
            if (!state.Units.Any(u => u.Location == unit.Location && u.Garrison == unit.Garrison && BoardRules.Owner(u) == actor && BoardRules.IsArmyUnit(u)))
            {
                BoardRules.RemoveCasualty(state, unit);
                state.Events.Add(new() { Revision = state.Revision, Kind = "LeaderRemoved", Text = $"{unit.Id} was removed without an army." });
            }
    }
}
