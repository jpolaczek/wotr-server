using Wotr.Contracts;

namespace Wotr.Domain;

public abstract record EffectStep
{
    public abstract void Apply(EffectContext context);
}

public abstract record EffectChoiceStep : EffectStep
{
    public virtual Side Chooser(MatchState state, EffectExecution execution) => execution.Owner;
    public virtual string TargetId => "";
    public abstract IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution);
    public abstract string Presentation { get; }
    public abstract void Choose(EffectContext context, string id);
    public virtual int SelectionCount => 1;
    public virtual int Minimum(MatchState state, EffectExecution execution) => SelectionCount;
    public virtual int Maximum(MatchState state, EffectExecution execution) => SelectionCount;
    public virtual EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new() { Id = id, TargetId = TargetId };
    public virtual bool ValidSelection(MatchState state, EffectExecution execution, IReadOnlyList<string> ids) =>
        DraftSelection.IsSubset(ids, Candidates(state, execution), Minimum(state, execution), Maximum(state, execution));
    public virtual void ChooseSelection(EffectContext context, IReadOnlyList<string> ids) => Choose(context, ids.Single());
    public virtual void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) { }

    public virtual bool ValidPayload(MatchState state, EffectExecution execution, CommandPayload payload) =>
        payload.Placements.Count == 0 && ValidSelection(state, execution, payload.Ids);
    public virtual void ChoosePayload(EffectContext context, CommandPayload payload) => ChooseSelection(context, payload.Ids);
    public virtual void PreviewPayload(MatchState state, EffectExecution execution, CommandPayload payload, ActionPreview preview) =>
        DescribePreview(state, execution, payload.Ids, preview);
    public virtual IEnumerable<EffectSelection> Selections(MatchState state, EffectExecution execution, CommandPayload payload) =>
        payload.Ids.Select(id => new EffectSelection(Chooser(state, execution), execution.SourceId, id, Describe(state, execution, id).TargetId));

    public virtual ActionOption Option(MatchState state, EffectExecution execution)
    {
        var candidates = Candidates(state, execution);
        return new()
        {
            Kind = CommandKind.ChooseEffect,
            Minimum = Minimum(state, execution),
            Maximum = Maximum(state, execution),
            EligibleIds = [.. candidates],
            EffectChoices = candidates.Select(id => Describe(state, execution, id)).ToList()
        };
    }

    public override void Apply(EffectContext context)
    {
        var choices = Candidates(context.State, context.Execution);
        if (choices.Count == 0)
            return;
        var minimum = Minimum(context.State, context.Execution);
        if (minimum == Maximum(context.State, context.Execution) && choices.Count == minimum && ValidSelection(context.State, context.Execution, choices))
        {
            ChooseSelection(context, choices);
            return;
        }
        OpenChoice(context, choices, minimum);
    }

    protected void OpenChoice(EffectContext context, IReadOnlyList<string> choices, int minimum)
    {
        context.Execution.Wait = EffectWait.Choice;
        context.State.Open(DecisionKind.EffectChoice, Chooser(context.State, context.Execution), [CommandKind.ChooseEffect], minimum);
        context.State.Pending!.PrivateChoices = [.. choices];
        context.State.Pending.Presentation = Presentation;
        context.State.Pending.SourceId = context.Execution.SourceId;
    }
}
