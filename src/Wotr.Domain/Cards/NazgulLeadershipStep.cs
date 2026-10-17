using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record NazgulLeadershipCondition(int Minimum) : ICombatCondition
{
    public bool CanPlay(MatchState state, BattleState battle, Side actor) =>
        BattleLeadership.Nazgul(state, battle.Force(actor)).Values.Sum() >= Minimum;
}

public enum NazgulLeadershipBenefit { CombatBonus, RerollBonus, OpposingDicePenalty, OpposingLeadershipPenalty }

public sealed record NazgulLeadershipStep(NazgulLeadershipBenefit Benefit) : EffectChoiceStep
{
    public override string Presentation => "NazgulLeadership";
    public override int SelectionCount => Benefit switch
    {
        NazgulLeadershipBenefit.CombatBonus => 2,
        NazgulLeadershipBenefit.OpposingLeadershipPenalty => 0,
        _ => 1
    };
    public override int Maximum(MatchState state, EffectExecution execution) => Benefit switch
    {
        NazgulLeadershipBenefit.OpposingDicePenalty => Points(state, execution).Count,
        NazgulLeadershipBenefit.OpposingLeadershipPenalty => Math.Min(5, Points(state, execution).Count),
        _ => SelectionCount
    };

    private static Dictionary<string, string> Points(MatchState state, EffectExecution execution)
    {
        var force = BattleRuntime.Active(state)!.Force(execution.Owner);
        return BattleLeadership.Nazgul(state, force)
            .SelectMany(p => Enumerable.Range(1, p.Value).Select(index => (Id: $"{p.Key}:leadership:{index}", Figure: p.Key)))
            .ToDictionary(p => p.Id, p => p.Figure);
    }

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        var points = Points(state, execution);
        return points.Count >= SelectionCount ? points.Keys.ToArray() : [];
    }

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id)
    {
        var figure = Points(state, execution)[id];
        var witchKing = state.Characters.Any(c => c.Id == figure);
        return new()
        {
            Id = id,
            SourceId = figure,
            Title = witchKing ? "Witch-king ◆" : "Nazgûl ◆",
            Detail = "Forfeit one Leadership for this round.",
            SelectionGroup = "Leadership",
            GroupMinimum = SelectionCount,
            GroupMaximum = Maximum(state, execution)
        };
    }

    public override void Choose(EffectContext context, string id) => ChooseSelection(context, [id]);

    public override void ChooseSelection(EffectContext context, IReadOnlyList<string> ids)
    {
        var points = Points(context.State, context.Execution);
        var cost = ids.GroupBy(id => points[id]).ToDictionary(group => group.Key, group => group.Count());
        var battle = BattleRuntime.Active(context.State)!;
        var force = battle.Force(context.Owner);
        if (cost.Count > 0)
            BattleLeadership.Forfeit(context.State, force, cost);
        switch (Benefit)
        {
            case NazgulLeadershipBenefit.CombatBonus:
                force.Round.CombatModifier++;
                break;
            case NazgulLeadershipBenefit.RerollBonus:
                force.Round.RerollModifier++;
                break;
            case NazgulLeadershipBenefit.OpposingDicePenalty:
                battle.Opponent(context.Owner).Round.CombatDicePenalty += ids.Count;
                break;
            case NazgulLeadershipBenefit.OpposingLeadershipPenalty:
                battle.Opponent(context.Owner).Round.LeadershipPenalty += ids.Count;
                break;
        }
        context.Record("NazgulLeadershipForfeited", $"{context.Execution.SourceId}: forfeited {ids.Count} Nazgûl Leadership ({Benefit}).");
    }
}
