using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record FateThereAndBackStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var state = context.State;
        if (!state.Characters.Any(c => c.Location is "carrock" or "iron-hills" or "woodland-realm" &&
            FreePeoplesCharacterCards01.AxeCompanions.Any(id => CharacterRules.MatchesCardName(c, id))))
            return;
        PoliticalRules.Activate(state, "north");
        foreach (var nation in new[] { "dwarves", "elves", "north" })
            PoliticalRules.Advance(state, nation, PoliticalAdvanceCause.Event);
    }
}

public sealed record FateBookWarStep : EffectStep
{
    public static bool HasCompanion(MatchState state, string region) => state.Characters.Any(c =>
        c.Location == region && CharacterRules.IsCompanion(c));

    public override void Apply(EffectContext context)
    {
        var state = context.State;
        if (!HasCompanion(state, "iron-hills") && !HasCompanion(state, "ered-luin"))
            return;
        PoliticalRules.Activate(state, "dwarves");
        var nation = state.Nations.Single(n => n.Id == "dwarves");
        while (PoliticalRules.CanAdvance(nation))
            PoliticalRules.Advance(state, "dwarves", PoliticalAdvanceCause.Event);
    }
}

public sealed record FateBookRegionStep() : RecruitRegionStep("dwarves")
{
    protected override bool Eligible(MatchState state, RegionDefinition region) =>
        region.Id is "iron-hills" or "ered-luin" && FateBookWarStep.HasCompanion(state, region.Id);
}
