using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class RingRecoveryState
{
    public List<int> Rolls { get; set; } = [];
    public int Successes { get; set; }
}

public sealed record RollRingRecoveryStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var rolls = Enumerable.Range(0, 3).Select(_ => context.Random.Next(6) + 1).ToList();
        context.Execution.RingRecovery = new() { Rolls = rolls, Successes = rolls.Count(r => r >= 5) };
        context.Record("RingRecoveryRoll", $"Three Rings: [{string.Join(", ", rolls)}], {context.Execution.RingRecovery.Successes} success(es).", rolls: rolls, hitTarget: 5, hits: context.Execution.RingRecovery.Successes);
    }
}

public sealed record RecoverRingsStep : EffectChoiceStep
{
    public override string Presentation => "ElvenRings";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        execution.RingRecovery is { Successes: > 0 } ? state.Rings.Where(r => r.Owner != Side.FreePeoples).Select(r => r.Id).ToArray() : [];
    public override int Minimum(MatchState state, EffectExecution execution) =>
        Math.Min(execution.RingRecovery?.Successes ?? 0, Candidates(state, execution).Count);
    public override int Maximum(MatchState state, EffectExecution execution) => Minimum(state, execution);

    public override bool ValidSelection(MatchState state, EffectExecution execution, IReadOnlyList<string> ids) =>
        base.ValidSelection(state, execution, ids) && state.Rings.Count(r => r.Owner is null && ids.Contains(r.Id)) == RemovedCount(state, execution);

    private int RemovedCount(MatchState state, EffectExecution execution) =>
        Math.Min(Minimum(state, execution), state.Rings.Count(r => r.Owner is null));

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id)
    {
        var removed = state.Rings.Single(r => r.Id == id).Owner is null;
        var count = removed ? RemovedCount(state, execution) : Minimum(state, execution) - RemovedCount(state, execution);
        return new()
        {
            Id = id, Title = Name(id), Detail = "Return to Free Peoples.",
            SelectionGroup = removed ? "Out of play" : "Held by Shadow", GroupMinimum = count, GroupMaximum = count
        };
    }

    private static string Name(string id) => id switch
    {
        LordsDiceSetup.NaryaRing => "Narya",
        LordsDiceSetup.NenyaRing => "Nenya",
        LordsDiceSetup.VilyaRing => "Vilya",
        _ => id
    };

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.Summary = "Recover " + string.Join(" and ", ids.Select(Name)) + ".";

    public override void ChooseSelection(EffectContext context, IReadOnlyList<string> ids)
    {
        foreach (var id in ids)
            Choose(context, id);
    }

    public override void Choose(EffectContext context, string id)
    {
        context.State.Rings.Single(r => r.Id == id).Owner = Side.FreePeoples;
        context.Record("RingRecovered", $"Free Peoples recovered {id}.");
    }
}
