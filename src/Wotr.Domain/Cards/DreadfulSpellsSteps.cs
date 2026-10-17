using Wotr.Contracts;

namespace Wotr.Domain;

public static class DreadfulSpellsRules
{
    public static string Key(ArmyTarget army) => army.Region + (army.Garrison ? ":garrison" : ":field");

    public static IEnumerable<ArmyTarget> Armies(MatchState state, Side side)
    {
        var characters = state.Characters.Where(c => c.Side == side).ToLookup(c => (c.Location, c.Garrison));
        var armies = state.Units.Where(u => BoardRules.Owner(u) == side && BoardRules.Regions.ContainsKey(u.Location))
            .GroupBy(u => (u.Location, u.Garrison)).Where(g => g.Any(BoardRules.IsArmyUnit));
        return armies.Select(g => new ArmyTarget(side, g.Key.Location, g.Key.Garrison,
            g.Select(u => u.Id).Concat(characters[g.Key].Select(c => c.Id)).ToList()));
    }

    public static int Dice(MatchState state, ArmyTarget army) => Math.Min(5,
        army.Units(state).Count(u => u.Kind == UnitKind.Nazgul) +
        state.Characters.Count(c => army.Participants.Contains(c.Id) && CharacterRules.IsWitchKing(c)));

    public static IReadOnlyList<ArmyTarget> Targets(MatchState state, string source) =>
        Armies(state, Side.Shadow).FirstOrDefault(a => Key(a) == source) is { } army
            ? Armies(state, Side.FreePeoples).Where(a => a.Region == army.Region || BoardRules.Regions[army.Region].Neighbors.Contains(a.Region)).ToList()
            : [];

    public static IReadOnlyList<ArmyTarget> Sources(MatchState state)
    {
        var targets = Armies(state, Side.FreePeoples).ToList();
        return Armies(state, Side.Shadow).Where(a => Dice(state, a) > 0 &&
            targets.Any(t => t.Region == a.Region || BoardRules.Regions[a.Region].Neighbors.Contains(t.Region))).ToList();
    }

    public static EffectChoiceView Describe(ArmyTarget army, string title, string detail) => new()
    {
        Id = Key(army),
        TargetId = army.Region,
        Title = title + (army.Garrison ? " · Garrison" : " · Field"),
        Detail = detail
    };
}

public sealed record DreadfulCondition : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => DreadfulSpellsRules.Sources(state).Count > 0;
}

public sealed record DreadfulSourceStep : EffectChoiceStep
{
    public override string Presentation => "Region";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        DreadfulSpellsRules.Sources(state).Select(DreadfulSpellsRules.Key).ToList();
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id)
    {
        var army = DreadfulSpellsRules.Sources(state).Single(a => DreadfulSpellsRules.Key(a) == id);
        return DreadfulSpellsRules.Describe(army, $"{DreadfulSpellsRules.Dice(state, army)} dice · 5+", "Choose the Shadow Army whose Nazgûl cast the spells.");
    }
    public override void Choose(EffectContext context, string id) => context.Execution.RegionId = id;
}

public sealed record DreadfulTargetStep : EffectChoiceStep
{
    public override string Presentation => "Region";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        DreadfulSpellsRules.Targets(state, execution.RegionId).Select(DreadfulSpellsRules.Key).ToList();
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) =>
        DreadfulSpellsRules.Describe(DreadfulSpellsRules.Targets(state, execution.RegionId).Single(a => DreadfulSpellsRules.Key(a) == id),
            "Target", "Each 5+ hits this army. Companions survive these event losses.");
    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) => preview.HasUnknownResult = true;
    public override void Choose(EffectContext context, string id)
    {
        var source = DreadfulSpellsRules.Sources(context.State).Single(a => DreadfulSpellsRules.Key(a) == context.Execution.RegionId);
        var target = DreadfulSpellsRules.Targets(context.State, context.Execution.RegionId).Single(a => DreadfulSpellsRules.Key(a) == id);
        var rolls = Enumerable.Range(0, DreadfulSpellsRules.Dice(context.State, source)).Select(_ => context.Random.Next(6) + 1).ToList();
        context.Record("DreadfulSpells", $"Dreadful Spells in {target.Region}: [{string.Join(", ", rolls)}].", rolls: rolls, hitTarget: 5, hits: rolls.Count(r => r >= 5), targets: [target.Region]);
        EventLosses.Begin(context, target, rolls.Count(r => r >= 5), rolls, CasualtyCause.Event);
    }
}
