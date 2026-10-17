using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record BlackCaptainStep : EffectChoiceStep
{
    public const string Recruit = "captain.recruit";
    public const string Fly = "captain.fly";
    public override string Presentation => "CaptainOrder";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) => [Recruit, Fly];
    private static List<UnitState> Recruits(MatchState state) => state.Units.Where(u => u.Kind == UnitKind.Nazgul && u.Location == "reserve").Take(2).ToList();
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        Title = id == Recruit ? $"Recruit · {Recruits(state).Count} Nazgûl" : "Move Nazgûl",
        TargetId = id == Recruit ? CharacterRules.WitchKingInPlay(state)!.Location : "",
        Detail = id == Recruit ? "Recruit available Nazgûl with the Witch-king." : "Move any Nazgûl, including the Witch-king."
    };
    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        if (ids.Single() == Recruit)
            preview.Placements.AddRange(Recruits(state).Select(u => new UnitPlacementChoice { UnitId = u.Id, RegionId = CharacterRules.WitchKingInPlay(state)!.Location }));
    }
    public override void Choose(EffectContext context, string id)
    {
        if (id == Fly)
        {
            new MoveNazgulStep().Apply(context);
            return;
        }
        var king = CharacterRules.WitchKingInPlay(context.State)!;
        foreach (var unit in Recruits(context.State))
        {
            unit.Location = king.Location;
            unit.Garrison = king.Garrison;
            context.Record("UnitRecruited", $"{unit.Id}: recruited in {king.Location}.");
        }
    }
}
