using Wotr.Contracts;
using static Wotr.Domain.AvailabilityDescription;

namespace Wotr.Domain;

// UI-129: why each own nation can or cannot recruit with a Muster die, for the Reinforcements panel. One entry
// per nation ("recruit"); targets are the settlements a reserve figure can take now, and the conditions name the
// rule that closes the others. Only public facts: politics, control, armies, sieges and the viewer's own action.
internal static class RecruitmentAvailabilityProjection
{
    private static readonly Dictionary<string, string> Names = new()
    {
        ["dwarves"] = "Dwarves", ["elves"] = "Elves", ["gondor"] = "Gondor", ["north"] = "The North",
        ["rohan"] = "Rohan", ["isengard"] = "Isengard", ["sauron"] = "Sauron", ["southrons"] = "Southrons & Easterlings"
    };

    public static IEnumerable<ActionAvailabilityView> For(MatchState state, Side viewer)
    {
        var sequence = state.Resolution.ActionSequence is { Kind: CommandKind.Recruit } current && current.Owner == viewer
            ? current : null;
        foreach (var nation in state.Nations.Where(n => BoardRules.Nations[n.Id].Side == viewer))
            yield return Nation(state, viewer, nation, sequence);
    }

    private static ActionAvailabilityView Nation(MatchState state, Side viewer, NationState nation, ActionSequenceState? sequence)
    {
        var name = Names.GetValueOrDefault(nation.Id, nation.Id);
        var reserves = state.Units.Where(u => u.Location == "reserve" && u.Nation == nation.Id &&
            BoardRules.Owner(u) == viewer && sequence?.UsedIds.Contains(u.Id) != true).ToList();
        var kinds = reserves.GroupBy(u => u.Kind).Select(g => g.First())
            .Where(u => sequence is null || u.Kind != UnitKind.Elite || sequence.Remaining >= 2).ToList();
        var conditions = new List<AvailabilityConditionView>
        {
            Condition("Condition", RecruitmentBlock.AtWar, AtWar(name, nation), nation.StepsToWar == 0),
            Condition("Condition", "reserves", reserves.Count > 0 ? $"{reserves.Count} {name} figures in reserve."
                : $"No {name} figures left in reserve.", reserves.Count > 0)
        };
        var targets = new List<string>();
        foreach (var region in BoardRules.CurrentRegions(state).Where(r => r.Nation == nation.Id && BoardRules.IsSettlement(r)))
        {
            var reason = Reason(state, viewer, kinds, region.Id, sequence);
            if (reason.Length == 0)
                targets.Add(region.Id);
            conditions.Add(Condition("Target", "settlement:" + region.Id + (reason.Length > 0 ? ":" + reason : ""),
                Settlement(region.Name, reason), reason.Length == 0));
        }
        if (sequence is not null && sequence.Remaining < 2 && reserves.Any(u => u.Kind == UnitKind.Elite))
            conditions.Add(Condition("Cost", RecruitmentBlock.EliteBudget, "An Elite needs both recruitment points.", false));
        var window = ActionWindow(state, viewer) ||
            sequence is not null && state.Pending is { Kind: DecisionKind.ActionSequence } pending && pending.Owner == viewer;
        var result = Create(state, viewer, nation.Id, "recruit", "Recruit · " + name, "BASE pp.20, 26–27",
            "One Muster result (Army/Muster or Will of the West too); an Elite takes both points, other figures one each in different settlements.",
            "During your normal action.", window, conditions.ToArray());
        // Settlement conditions explain the closed ones; the nation can act when any settlement is open.
        result.ConditionsMet = targets.Count > 0;
        result.CanAct = result.ConditionsMet && result.WindowOpen;
        result.Targets = targets;
        result.EligibleDice = sequence is not null ? [sequence.DieId] : state.Dice.Where(d => d.Owner == viewer &&
            d.Location == DieLocation.Available && DiceRules.CanPay(d, DieFace.Muster)).Select(d => d.Id).ToList();
        return result;
    }

    // Empty when some reserve figure may take the settlement; otherwise the code of the first rule that closes it.
    private static string Reason(MatchState state, Side viewer, List<UnitState> kinds, string region, ActionSequenceState? sequence)
    {
        if (sequence?.UsedRegions.Contains(region) == true)
            return RecruitmentBlock.UsedThisAction;
        if (kinds.Count == 0)
            return "reserves";
        var blockers = kinds.Select(unit => RecruitmentRules.MusterBlocker(state, viewer, unit, region)).ToList();
        return blockers.Any(b => b is null) ? "" : blockers.First()!;
    }

    private static string AtWar(string name, NationState nation) => nation.StepsToWar == 0 ? $"{name} is at war."
        : $"{name} is {nation.StepsToWar} step{(nation.StepsToWar == 1 ? "" : "s")} from war" + (nation.Active ? "." : " and passive.");

    private static string Settlement(string name, string reason) => reason switch
    {
        "" => $"{name} can take a reinforcement.",
        RecruitmentBlock.AtWar => $"{name}: the nation is not at war.",
        RecruitmentBlock.Captured => $"{name} is held by the enemy.",
        RecruitmentBlock.Besieged => $"{name} is under siege.",
        RecruitmentBlock.EnemyArmy => $"{name} has an enemy army.",
        RecruitmentBlock.UsedThisAction => $"{name} already took a reinforcement in this action.",
        RecruitmentBlock.NazgulStronghold => $"{name}: Nazgûl enter only a Stronghold.",
        RecruitmentBlock.KingsWeakness => $"{name}: a weakness card forbids these figures here.",
        "reserves" => $"{name}: no figure left to recruit.",
        _ => $"{name} cannot take a reinforcement now."
    };
}
