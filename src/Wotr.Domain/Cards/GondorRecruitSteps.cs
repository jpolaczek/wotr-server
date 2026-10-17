using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingArmyRules
{
    public static CharacterState? King(MatchState state) => state.Characters.FirstOrDefault(c =>
        CharacterRules.IsStriderOrAragorn(c) && CharacterRules.OnBoard(c));

    public static List<UnitState> Army(MatchState state) => King(state) is { } king ? state.Units.Where(u =>
        u.Location == king.Location && u.Garrison == king.Garrison && BoardRules.Owner(u) == Side.FreePeoples && BoardRules.IsArmyUnit(u)).ToList() : [];
}

public sealed record StewardsCondition : EventCondition
{
    public static CharacterState? Boromir(MatchState state) => state.Characters.FirstOrDefault(c =>
        CharacterRules.MatchesCardName(c, "base.character.boromir") && CharacterRules.OnBoard(c));
    public override bool CanPlay(MatchState state, Side actor) => Boromir(state) is { } boromir && BoardRules.Region(state, boromir.Location).Nation == "gondor";
}

public sealed record StewardsRegionStep : EffectStep
{
    public override void Apply(EffectContext context) => context.Execution.RegionId = StewardsCondition.Boromir(context.State)!.Location;
}

public sealed record GreyCompanyCondition : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => KingArmyRules.Army(state).Count > 0;
}

public sealed record GreyCompanyStep : EffectChoiceStep
{
    public override string Presentation => "ArmyFigures";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) => KingArmyRules.Army(state)
        .Where(u => u.Kind == UnitKind.Regular && Elite(state, u.Nation) is not null).Select(u => u.Id).ToList();

    private static UnitState? Elite(MatchState state, string nation) => state.Units.FirstOrDefault(u =>
        u.Location == "reserve" && u.Nation == nation && u.Kind == UnitKind.Elite);

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        TargetId = state.Units.Single(u => u.Id == id).Location,
        Title = "Regular → Elite",
        Detail = "Replace this regular with an elite of the same nation. The regular is a casualty."
    };

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        var regular = state.Units.Single(u => u.Id == ids.Single());
        preview.RemovedIds.Add(regular.Id);
        preview.Placements.Add(new() { UnitId = Elite(state, regular.Nation)!.Id, RegionId = regular.Location });
    }

    public override void Choose(EffectContext context, string id)
    {
        var regular = context.State.Units.Single(u => u.Id == id);
        var elite = Elite(context.State, regular.Nation)!;
        elite.Location = regular.Location;
        elite.Garrison = regular.Garrison;
        BoardRules.RemoveCasualty(context.State, regular);
        context.Record("EventUnitReplaced", $"The Grey Company: replaced a {regular.Nation} regular with an elite in {elite.Location}.");
    }
}
