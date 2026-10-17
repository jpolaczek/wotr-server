using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record ArmyTarget(Side Owner, string Region, bool Garrison, List<string> Participants)
{
    public IEnumerable<UnitState> Units(MatchState state) => state.Units.Where(u =>
        u.Location == Region && u.Garrison == Garrison && BoardRules.Owner(u) == Owner && Participants.Contains(u.Id));

    public static ArmyTarget? At(MatchState state, Side owner, string region)
    {
        var army = state.Units.FirstOrDefault(u => u.Location == region && BoardRules.Owner(u) == owner && BoardRules.IsArmyUnit(u));
        if (army is null)
            return null;
        var ids = state.Units.Where(u => u.Location == region && u.Garrison == army.Garrison && BoardRules.Owner(u) == owner).Select(u => u.Id)
            .Concat(state.Characters.Where(c => c.Location == region && c.Garrison == army.Garrison && c.Side == owner).Select(c => c.Id));
        return new(owner, region, army.Garrison, ids.ToList());
    }
}

public sealed record ArmyLossPlan(MatchState Board, List<string> Participants, int AppliedHits, IReadOnlyList<ActionLossFact>? LossFacts = null);

public static class ArmyLossRules
{
    public static ArmyLossPlan? Plan(MatchState state, ArmyTarget target, int hits, IReadOnlyList<BattleLossChoice> choices, CasualtyCause cause)
    {
        if (choices.Count == 0 || choices.Select(c => c.UnitId).Distinct().Count() != choices.Count)
            return null;
        var eligible = target.Units(state).Where(BoardRules.IsArmyUnit).Select(u => u.Id).ToHashSet();
        var board = MovementPlan.CopyBoard(state);
        var participants = target.Participants.ToHashSet();
        var paid = 0;
        var facts = new List<ActionLossFact>();
        foreach (var choice in choices)
        {
            if (!eligible.Contains(choice.UnitId) || !Enum.IsDefined(choice.Kind))
                return null;
            var unit = board.Units.Single(u => u.Id == choice.UnitId);
            if (choice.Kind == BattleLossKind.Downgrade && unit.Kind != UnitKind.Elite)
                return null;
            var fact = new ActionLossFact
            {
                Figure = MovementFacts.Unit(unit), Operation = choice.Kind, RegionId = unit.Location,
                Hits = choice.Kind == BattleLossKind.Remove && unit.Kind == UnitKind.Elite ? 2 : 1
            };
            paid += fact.Hits;
            if (paid > hits)
                return null;
            if (choice.Kind == BattleLossKind.Downgrade)
            {
                var reduction = UnitReduction.ReduceEliteWithOrigin(board, unit);
                if (reduction.Replacement is { } replacement)
                {
                    participants.Add(replacement.Id);
                    fact.ReplacementId = replacement.Id;
                    fact.ReplacementFrom = reduction.PreviousLocation;
                }
            }
            else
                BoardRules.RemoveCasualty(board, unit);
            facts.Add(fact);
        }
        var survivors = target with { Participants = [.. participants] };
        if (paid < hits && survivors.Units(board).Any(BoardRules.IsArmyUnit))
            return null;
        CasualtyRules.RemoveUnaccompanied(board, target.Region, target.Garrison, target.Owner, cause, participants);
        return new(board, [.. participants], paid, facts);
    }

    public static void ApplyFigures(MatchState state, ArmyLossPlan plan)
    {
        CouncilHobbitReturn.RecordLosses(state, plan.Board);
        state.Units = plan.Board.Units;
        state.Characters = plan.Board.Characters;
        SovereignLifecycle.Reconcile(state);
        CharacterLifecycle.ReconcileDice(state);
    }
}
