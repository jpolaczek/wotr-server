using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record ShadowArmyRecruitStep(string RecruitingNation) : RecruitRegionStep(RecruitingNation)
{
    protected override bool Eligible(MatchState state, RegionDefinition region) => state.Units.Any(u =>
        u.Location == region.Id && BoardRules.Owner(u) == Side.Shadow && BoardRules.IsArmyUnit(u));
}

public sealed record AragornPresentCondition : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => CharacterRules.InPlay(state, CharacterRules.Aragorn) is not null;
}

public sealed record HillTrollsStep : EffectChoiceStep
{
    public override string Presentation => "ArmyFigures";
    private static IEnumerable<UnitState> Elites(MatchState state) => state.Units.Where(u =>
        u.Nation == "sauron" && u.Kind == UnitKind.Elite && u.Location == "reserve");

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) => Elites(state).Any()
        ? state.Units.Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && BoardRules.Regions.ContainsKey(u.Location))
            .Select(u => u.Id).ToArray()
        : [];

    public override int Minimum(MatchState state, EffectExecution execution) =>
        Math.Min(2, Math.Min(Candidates(state, execution).Count, Elites(state).Count()));
    public override int Maximum(MatchState state, EffectExecution execution) => Minimum(state, execution);

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        TargetId = state.Units.Single(u => u.Id == id).Location,
        Title = "Regular → Elite",
        SelectionGroup = "Replacements",
        GroupMinimum = Minimum(state, execution),
        GroupMaximum = Maximum(state, execution),
        Detail = "Replace this Sauron regular with an elite from the reserve."
    };

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        var elites = Elites(state).Take(ids.Count).ToArray();
        for (var i = 0; i < ids.Count; i++)
        {
            var regular = state.Units.Single(u => u.Id == ids[i]);
            preview.RemovedIds.Add(regular.Id);
            preview.Placements.Add(new() { UnitId = elites[i].Id, RegionId = regular.Location });
        }
    }

    public override void ChooseSelection(EffectContext context, IReadOnlyList<string> ids)
    {
        foreach (var id in ids)
            Choose(context, id);
    }

    public override void Choose(EffectContext context, string id)
    {
        var regular = context.State.Units.Single(u => u.Id == id);
        var elite = Elites(context.State).First();
        elite.Location = regular.Location;
        elite.Garrison = regular.Garrison;
        BoardRules.RemoveCasualty(context.State, regular);
        context.Record("EventUnitReplaced", $"Hill-Trolls: replaced a Sauron regular with an elite in {elite.Location}.");
    }
}
