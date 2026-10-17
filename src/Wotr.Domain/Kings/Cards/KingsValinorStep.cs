using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record KingsValinorCondition : EventCondition
{
    public static int Corrupted(MatchState state) => state.Kings?.Sovereigns.Count(s => s.Status == SovereignStatus.Corrupted) ?? 0;
    public override bool CanPlay(MatchState state, Side actor) => Corrupted(state) > 0 || new ValinorCondition().CanPlay(state, actor);
}

public sealed record KingsValinorStep : EffectChoiceStep
{
    public override string Presentation => "Region";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        state.Units.Where(u => u.Nation == "elves" && BoardRules.IsArmyUnit(u) && BoardRules.Regions.ContainsKey(u.Location))
            .Select(u => u.Location).Distinct().Where(id => !execution.TargetIds.Contains(id) &&
                BoardRules.Region(state, id) is { Nation: "elves", Settlement: "Stronghold" } &&
                state.Control.GetValueOrDefault(id) == Side.FreePeoples &&
                !state.Units.Any(u => u.Location == id && u.Garrison && BoardRules.IsArmyUnit(u))).ToArray();

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, TargetId = id, Title = "Resolve Return to Valinor",
        Detail = KingsValinorCondition.Corrupted(state) >= 2 ? "Each 5+ hits the Army here." : "Each 6 hits the Army here."
    };
    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.HasUnknownResult = true;

    public override void Choose(EffectContext context, string id)
    {
        context.Execution.TargetIds.Add(id);
        context.Frame.NextStep--;
        var army = context.State.Units.Where(u => u.Location == id && BoardRules.Owner(u) == Side.FreePeoples &&
            !u.Garrison && BoardRules.IsArmyUnit(u)).ToArray();
        var count = Math.Min(5, army.Count(u => u.Nation == "elves"));
        var target = KingsValinorCondition.Corrupted(context.State) >= 2 ? 5 : 6;
        var rolls = Enumerable.Range(0, count).Select(_ => context.Random.Next(6) + 1).ToList();
        context.Record("ReturnToValinor", $"Return to Valinor in {id}: [{string.Join(", ", rolls)}], hits on {target}+.", rolls: rolls, hitTarget: target, hits: rolls.Count(r => r >= target), targets: [id]);
        EventLosses.Begin(context, new(Side.FreePeoples, id, false, army.Select(u => u.Id).ToList()),
            rolls.Count(r => r >= target), rolls, CasualtyCause.Event);
    }
}
