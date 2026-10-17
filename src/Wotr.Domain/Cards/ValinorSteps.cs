using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record ValinorCondition : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => BoardRules.CurrentRegions(state).Any(r =>
        r.Nation == "elves" && r.Settlement == "Stronghold" && state.Control.GetValueOrDefault(r.Id) == Side.Shadow);
}

public sealed record ValinorRegionStep : EffectChoiceStep
{
    public override string Presentation => "Region";

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        state.Units.Where(u => u.Nation == "elves" && BoardRules.IsArmyUnit(u) && BoardRules.Regions.ContainsKey(u.Location))
            .Select(u => u.Location).Distinct().Where(id => !execution.TargetIds.Contains(id) &&
                BoardRules.Region(state, id) is { Nation: "elves", Settlement: "Stronghold" } &&
                !state.Units.Any(u => u.Location == id && u.Garrison && BoardRules.IsArmyUnit(u))).ToList();

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        TargetId = id,
        Title = "Resolve Elven losses",
        Detail = "Each 6 hits an Elven Army unit. Resolve every highlighted stronghold."
    };

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.HasUnknownResult = true;

    public override void Choose(EffectContext context, string id)
    {
        context.Execution.TargetIds.Add(id);
        context.Frame.NextStep--;
        var elves = context.State.Units.Where(u => u.Location == id && u.Nation == "elves" && BoardRules.IsArmyUnit(u)).ToList();
        var rolls = Enumerable.Range(0, Math.Min(5, elves.Count)).Select(_ => context.Random.Next(6) + 1).ToList();
        context.Record("ReturnToValinor", $"Return to Valinor in {id}: [{string.Join(", ", rolls)}].", rolls: rolls, hitTarget: 6, hits: rolls.Count(r => r == 6), targets: [id]);
        EventLosses.Begin(context, new(Side.FreePeoples, id, false, elves.Select(u => u.Id).ToList()),
            rolls.Count(r => r == 6), rolls, CasualtyCause.Event);
    }
}
