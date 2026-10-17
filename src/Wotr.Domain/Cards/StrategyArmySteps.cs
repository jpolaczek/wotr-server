using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record MixedShadowArmyCondition : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => ArmyEventDamageStep.Targets(state, true).Any();
}

public sealed record ArmyEventDamageStep(int Dice, bool MixedNations) : EffectChoiceStep
{
    public override string Presentation => "Region";
    public static IEnumerable<string> Targets(MatchState state, bool mixed) => state.Units
        .Where(u => BoardRules.IsArmyUnit(u) && BoardRules.Owner(u) == Side.Shadow && BoardRules.Regions.ContainsKey(u.Location))
        .GroupBy(u => u.Location).Where(g => mixed ? g.Select(u => u.Nation).Distinct().Count() >= 2
            : g.Key is "osgiliath" or "north-ithilien" or "south-ithilien").Select(g => g.Key);

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) => Targets(state, MixedNations).ToList();
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        TargetId = id,
        Title = "Roll " + Dice + " dice",
        Detail = "Each 5+ hits this army. Nazgûl and characters are unaffected."
    };
    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.HasUnknownResult = true;

    public override void Choose(EffectContext context, string id)
    {
        var rolls = Enumerable.Range(0, Dice).Select(_ => context.Random.Next(6) + 1).ToList();
        var hits = rolls.Count(r => r >= 5);
        context.Record("EventArmyRoll", $"{context.Execution.SourceId}: [{string.Join(", ", rolls)}], {hits} hit(s) in {id}.", rolls: rolls, hitTarget: 5, hits: hits, targets: [id]);
        EventLosses.Begin(context, ArmyTarget.At(context.State, Side.Shadow, id)!, hits, rolls, CasualtyCause.Event);
    }
}

public sealed record RangersRecruitStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        // An empty destination makes subsequent recruitment steps unavailable, without ending the event early.
        context.Execution.RegionId = ArmyTarget.At(context.State, Side.FreePeoples, "osgiliath") is null ? "" : "osgiliath";
    }
}
