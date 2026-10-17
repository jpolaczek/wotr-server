using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record DolGuldurCondition : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => actor == Side.Shadow &&
        (state.Units.Any(u => u.Kind == UnitKind.Nazgul && u.Location == "dol-guldur") ||
         state.Characters.Any(c => CharacterRules.IsWitchKing(c) && c.Location == "dol-guldur"));
}

public sealed record DolGuldurRecruitStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        const string region = "dol-guldur";
        var state = context.State;
        if (RecruitmentRules.EventGarrison(state, context.Owner, region) is not { } garrison)
            return;
        var leadership = state.Units.Count(u => u.Kind == UnitKind.Nazgul && u.Location == region && u.Garrison == garrison) +
            state.Characters.Where(c => CharacterRules.IsWitchKing(c) && c.Location == region && c.Garrison == garrison).Sum(c => c.Leadership);
        var size = state.Units.Count(u => BoardRules.Owner(u) == context.Owner && BoardRules.IsArmyUnit(u) &&
            u.Location == region && u.Garrison == garrison);
        var count = Math.Min(5, Math.Min(leadership, Math.Max(0, (garrison ? 5 : 10) - size)));
        foreach (var unit in state.Units.Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == "reserve").Take(count))
        {
            unit.Location = region;
            unit.Garrison = garrison;
            context.Record("UnitRecruited", $"{unit.Id}: recruited in {region}.");
        }
    }
}
