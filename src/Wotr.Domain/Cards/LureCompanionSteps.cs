using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record LureCompanionStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        if (CharacterRules.GuideDefinition(context.State) == CharacterRules.Gollum)
        {
            DirectCorruption.Add(context, 1);
            return;
        }
        var companions = context.State.Fellowship.Companions;
        var id = companions[context.Random.Next(companions.Count)];
        context.Execution.TargetIds = [id];
        context.Record("CompanionSelected", $"Lure of the Ring: selected {id}.");
    }
}

public sealed record LureChoiceStep : EffectChoiceStep
{
    public const string Corruption = "lure.corruption";
    public const string Eliminate = "lure.eliminate";
    public override string Presentation => "LureCompanion";
    public override Side Chooser(MatchState state, EffectExecution execution) => Side.FreePeoples;
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        execution.TargetIds.Count == 0 ? [] : [Corruption, Eliminate];

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id)
    {
        var companion = CharacterCatalog.Companion(state, execution.TargetIds.Single());
        return new()
        {
            Id = id,
            SourceId = companion.Id,
            Title = id == Corruption ? $"Corruption +{CompanionRules.Level(state, companion)}" : "Eliminate",
            Detail = CharacterCatalog.All[companion.DefinitionId].Title + (id == Corruption
                ? ": keep this Companion in the Fellowship." : ": eliminate this Companion; Take Them Alive still applies.")
        };
    }

    public override void Choose(EffectContext context, string id)
    {
        var companion = context.Execution.TargetIds.Single();
        if (id == Corruption)
            DirectCorruption.Add(context, CompanionRules.Level(context.State, CharacterCatalog.Companion(context.State, companion)));
        else
            HobbitDeparture.EliminateFromFellowship(context.State, companion);
    }

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        if (ids.Contains(Corruption))
            preview.Summary = $"Take {CompanionRules.Level(state, CharacterCatalog.Companion(state, execution.TargetIds.Single()))} Corruption.";
        else
            preview.Summary = "Eliminate the selected Companion. Take Them Alive applies outside Mordor.";
    }
}
