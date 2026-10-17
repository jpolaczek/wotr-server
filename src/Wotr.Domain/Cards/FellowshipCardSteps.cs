using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record AnyFellowshipCompanion : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => state.Fellowship.Companions.Count > 0;
}

public sealed record FellowshipDefinitionPresent(string DefinitionId) : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => CompanionRules.Inside(state).Any(c => CharacterRules.MatchesCardName(c, DefinitionId));
}

public sealed record ShadowHuntBeforeDraw : PublicEventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => HuntRuntime.Active(state) is { DrawnBy: Side.Shadow, Stage: HuntStage.Draw, TileId.Length: 0 };
}

public sealed record PreventHuntDrawStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        HuntRuntime.Active(context.State)!.Stage = HuntStage.Done;
        context.Record("HuntDrawPrevented", $"{context.Execution.SourceId}: prevented the Hunt tile draw.");
    }
}

public sealed record AthelasStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var threshold = CharacterRules.GuideDefinition(context.State) is CharacterRules.Strider or CouncilCompanions.Strider ? 3 : 5;
        var dice = Enumerable.Range(0, 3).Select(_ => context.Random.Next(6) + 1).ToArray();
        context.Record("HealingRoll", $"{context.Execution.SourceId}: [{string.Join(", ", dice)}], healing on {threshold}+.", rolls: dice, hitTarget: threshold, hits: dice.Count(d => d >= threshold));
        new HealCorruptionStep(dice.Count(d => d >= threshold)).Apply(context);
    }
}

public sealed record AnotherWayStep : EffectChoiceStep
{
    public const string Stay = "fellowship.stay";
    public const string Hide = "fellowship.hide";
    public const string Move = "fellowship.move";
    public override string Presentation => "FellowshipAction";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        CharacterRules.GuideDefinition(state) != CharacterRules.Gollum ? [] : [Stay, state.Fellowship.Hidden ? Move : Hide];

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        Title = id == Stay ? "Stay" : id == Move ? "Move" : "Hide",
        Detail = id == Stay ? "Finish after healing." : id == Move ? "Move and resolve the Hunt. The Event die stays spent." : "Hide the Fellowship."
    };

    public override void Choose(EffectContext context, string id)
    {
        if (id == Hide)
            FellowshipFlow.Hide(context.State);
        else if (id == Move)
            HuntRuntime.BeginMove(context.State, context.Random, HuntReturn.Parent);
    }

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        if (ids.Contains(Move))
            HuntPreviewRules.Describe(state, new() { Kind = CommandKind.MoveFellowship }, preview);
    }
}

public sealed record SeparateCompanionsStep(int Bonus = 0) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        if (context.State.Fellowship.Companions.Count == 0)
            return;
        FellowshipFlow.BeginSeparation(context.State, context.Execution.SourceId);
        FellowshipFlow.Current(context.State)!.Fellowship!.SeparationBonus = Bonus;
    }
}
