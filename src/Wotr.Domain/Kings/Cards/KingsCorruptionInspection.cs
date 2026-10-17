using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record SelectInspectedSovereignStep : EffectChoiceStep
{
    public override string Presentation => "Sovereign";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        state.Kings?.Sovereigns.Where(s => s.Status == SovereignStatus.Initial && s.Tiles.Count > 0).Select(s => s.Id).ToArray() ?? [];
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, TargetId = SovereignLifecycle.Region(state, SovereignLifecycle.Find(state, id)!),
        Title = SovereignCatalog.All[id].Title, Detail = "Look at this sovereign's Corruption tiles."
    };
    public override void Choose(EffectContext context, string id) => context.Execution.InspectedSovereign = id;
}

public sealed record InspectCorruptionStep(bool Optional) : EffectChoiceStep
{
    public override string Presentation => "InspectedCorruption";
    private static SovereignState? Sovereign(MatchState state, EffectExecution execution) =>
        execution.InspectedSovereign is { } id ? SovereignLifecycle.Find(state, id) : null;

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        if (Sovereign(state, execution) is not { Tiles.Count: > 0 } sovereign)
            return [];
        return KingsCardRestrictions.CanRemoveCorruption(state)
            ? sovereign.Tiles.Concat(Optional ? ["keep"] : Array.Empty<string>()).ToArray() : ["keep"];
    }

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, TargetId = execution.InspectedSovereign ?? "",
        Title = id == "keep" ? "Keep tiles" : KingsTiles.IsKings(id) ? "Kings tile" : $"{KingsTiles.CorruptionValue(id)} Corruption",
        Detail = id == "keep" ? "Finish looking without returning a tile." : "Return this tile to the Hunt Pool."
    };

    public override void Apply(EffectContext context)
    {
        var choices = Candidates(context.State, context.Execution);
        if (choices.Count == 0)
            context.Execution.InspectedSovereign = null;
        else
            OpenChoice(context, choices, 1); // Even a single tile must be visible before its return.
    }

    public override void Choose(EffectContext context, string id)
    {
        if (id != "keep")
            KingsCorruptionRemoval.Return(context, Sovereign(context.State, context.Execution)!, id);
        context.Execution.InspectedSovereign = null;
    }
}

public sealed record WisdomBranchStep : EffectChoiceStep
{
    public const string Politics = "kings.wisdom.politics";
    public const string Inspect = "kings.wisdom.inspect";
    public override string Presentation => "SovereignCardBranch";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) => [Politics, Inspect];
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, Title = id == Politics ? "Activate and advance" : "Inspect corruption",
        Detail = id == Politics ? "Activate one Free Peoples nation and advance it one step." : "Look at one sovereign's tiles and optionally return one."
    };
    public override void Choose(EffectContext context, string id) =>
        EffectRuntime.BeginProgram(context.State, context.Owner, context.Execution.SourceId, id);
}

public sealed record KingsNationStep(bool Activate) : EffectChoiceStep
{
    public override string Presentation => "Nation";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        state.Nations.Where(n => BoardRules.Nations[n.Id].Side == Side.FreePeoples).Select(n => n.Id).ToArray();
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, Title = id switch
        {
            "north" => "The North", "elves" => "Elves", "dwarves" => "Dwarves",
            "rohan" => "Rohan", "gondor" => "Gondor", _ => id
        },
        Detail = Activate ? "Activate and advance one step, if possible." : "Advance one step, if possible. This does not activate the nation."
    };
    public override void Choose(EffectContext context, string id)
    {
        context.Execution.NationId = id;
        new AdvanceNationStep(Activate).Apply(context);
    }
}
