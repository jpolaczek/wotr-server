using Wotr.Contracts;

namespace Wotr.Domain;

// Independent recruits in one printed instruction are selected and committed together.
public sealed record RecruitTogetherStep : EffectChoiceStep
{
    public IReadOnlyList<RecruitEffectStep> Groups { get; }

    public RecruitTogetherStep(IReadOnlyList<RecruitEffectStep> groups)
    {
        if (groups.Select(g => g.Nation).Distinct().Count() != groups.Count)
            throw new ArgumentException("Grouped recruits must use independent national reserves.", nameof(groups));
        Groups = groups;
    }

    public override string Presentation => "ReserveFigures";
    public override int Minimum(MatchState state, EffectExecution execution) => Groups.Sum(g => g.Minimum(state, execution));
    public override int Maximum(MatchState state, EffectExecution execution) => Groups.Sum(g => g.Maximum(state, execution));
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        Groups.SelectMany(g => g.Candidates(state, execution)).ToArray();

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id)
    {
        var group = Groups.Single(g => g.Candidates(state, execution).Contains(id));
        var choice = group.Describe(state, execution, id);
        choice.SelectionGroup = BoardRules.Regions[group.Region].Name;
        return choice;
    }

    public override bool ValidSelection(MatchState state, EffectExecution execution, IReadOnlyList<string> ids) =>
        base.ValidSelection(state, execution, ids) && Groups.All(g => g.ValidSelection(state, execution, Selected(g, state, execution, ids)));

    private static string[] Selected(RecruitEffectStep group, MatchState state, EffectExecution execution, IReadOnlyList<string> ids) =>
        ids.Intersect(group.Candidates(state, execution)).ToArray();

    public override void Choose(EffectContext context, string id) => ChooseSelection(context, [id]);

    public override void ChooseSelection(EffectContext context, IReadOnlyList<string> ids)
    {
        foreach (var group in Groups)
            group.ChooseSelection(context, Selected(group, context.State, context.Execution, ids));
    }

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        foreach (var group in Groups)
            group.DescribePreview(state, execution, Selected(group, state, execution, ids), preview);
    }
}
